using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ItemDataManager;
using UnityEngine;
using static Jewelcrafting.VisualEffectCondition;

namespace Jewelcrafting.GemEffects;

public static class VisualEffects
{
	public static readonly Dictionary<string, Dictionary<VisualEffectCondition, GameObject>> attachEffectPrefabs = new()
	{
		{ "Perfect_Red_Socket", VisualEffectSetup.redGemEffects },
		{ "Perfect_Blue_Socket", VisualEffectSetup.blueGemEffects },
		{ "Perfect_Green_Socket", VisualEffectSetup.greenGemEffects },
		{ "Perfect_Black_Socket", VisualEffectSetup.blackGemEffects },
		{ "Perfect_Yellow_Socket", VisualEffectSetup.yellowGemEffects },
		{ "Perfect_Purple_Socket", VisualEffectSetup.purpleGemEffects },
		{ "Perfect_Orange_Socket", VisualEffectSetup.orangeGemEffects },
		{ "Perfect_Cyan_Socket", VisualEffectSetup.cyanGemEffects }
	};

	private static readonly Dictionary<VisualEffectCondition, Dictionary<string, GameObject[]>> effectPrefabsByType = new();

	[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
	private static class FillEffectHashMapOnStart
	{
		private static bool initialized = false;

		[HarmonyPriority(Priority.First)]
		public static void Prefix()
		{
			if (initialized)
			{
				return;
			}

			FillEffectHashMap();
			initialized = true;
		}
	}

	private static void FillEffectHashMap()
	{
		foreach (KeyValuePair<string, Dictionary<VisualEffectCondition, GameObject>> kv in attachEffectPrefabs)
		{
			foreach (KeyValuePair<VisualEffectCondition, GameObject> effectKv in kv.Value)
			{
				if (!effectPrefabsByType.TryGetValue(effectKv.Key, out Dictionary<string, GameObject[]> inverseDict))
				{
					inverseDict = effectPrefabsByType[effectKv.Key] = new Dictionary<string, GameObject[]>();
				}
				inverseDict.Add(kv.Key, new[] { effectKv.Value });

				effectHashMap.Add(effectKv.Value.name.GetStableHashCode(), effectKv.Value);
			}
		}

		foreach (KeyValuePair<VisualEffectCondition, Dictionary<string, GameObject[]>> inverseKv in effectPrefabsByType)
		{
			foreach (KeyValuePair<GemType, Dictionary<GemType, GameObject[]>> mergedKv in MergedGemStoneSetup.mergedGems)
			{
				foreach (KeyValuePair<GemType, GameObject[]> gemKv in mergedKv.Value)
				{
					for (int i = 0; i < gemKv.Value.Length; ++i)
					{
						Dictionary<VisualEffectCondition, GameObject>?[] prefabDicts = new Dictionary<VisualEffectCondition, GameObject>[2];
						attachEffectPrefabs.TryGetValue(GemStoneSetup.Gems[mergedKv.Key][i].Prefab.name, out prefabDicts[0]);
						attachEffectPrefabs.TryGetValue(GemStoneSetup.Gems[gemKv.Key][i].Prefab.name, out prefabDicts[1]);

						List<GameObject> prefabs = new();
						foreach (Dictionary<VisualEffectCondition, GameObject>? prefabDict in prefabDicts)
						{
							if (prefabDict?.TryGetValue(inverseKv.Key, out GameObject prefab) == true)
							{
								prefabs.Add(prefab);
							}
						}

						if (prefabs.Count > 0)
						{
							inverseKv.Value.Add(gemKv.Value[i].name, prefabs.ToArray());
						}
					}
				}
			}
		}

		foreach (GameObject effect in VisualEffectSetup.spearProjectiles.Values)
		{
			effectHashMap.Add(effect.name.GetStableHashCode(), effect);
		}
	}

	private static readonly Dictionary<int, GameObject> effectHashMap = new();

	[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
	private static class ApplyGemEffects
	{
		private static readonly ConditionalWeakTable<VisEquipment, Dictionary<VisSlot, Dictionary<int, GameObject>>> activeEffects = new();

		private static void Postfix(VisEquipment __instance)
		{
			if (__instance.m_isPlayer)
			{
				Dictionary<VisSlot, Dictionary<int, GameObject>> effectsActive = activeEffects.GetOrCreateValue(__instance);

				ZDO? zdo = __instance.m_nview.m_zdo;
				if (zdo is not null)
				{
					ZDOExtraData.s_ints.Init(zdo.m_uid);
				}
				void Apply(VisSlot part, GameObject? equipRoot) => ApplyEffects(effectsActive, zdo is not null ? ZDOExtraData.s_ints[zdo.m_uid] : TrackEquipmentChanges.VisualEquipmentInts, part, equipRoot);

				Apply(VisSlot.HandLeft, __instance.m_leftItemInstance);
				Apply(VisSlot.BackLeft, __instance.m_leftBackItemInstance);
				Apply(VisSlot.HandRight, __instance.m_rightItemInstance);
				Apply(VisSlot.BackRight, __instance.m_rightBackItemInstance);
			}
		}
	}

	private static void ApplyEffects(Dictionary<VisSlot, Dictionary<int, GameObject>> effectsActive, IDictionary<int, int> zdoInts, VisSlot part, GameObject? equipRoot)
	{
		if (equipRoot is null)
		{
			return;
		}

		if (!effectsActive.TryGetValue(part, out Dictionary<int, GameObject> partEffects))
		{
			partEffects = effectsActive[part] = new Dictionary<int, GameObject>();
		}

		ApplySlotEffects(partEffects, zdoInts, $"JewelCrafting {part}", equipRoot);
	}

	private static void ApplySlotEffects(Dictionary<int, GameObject> slotEffects, IDictionary<int, int> zdoInts, string keyPrefix, GameObject equipRoot)
	{
		if (Jewelcrafting.visualEffects.Value == Jewelcrafting.Toggle.Off)
		{
			if (slotEffects.Count > 0)
			{
				foreach (GameObject activeEffect in slotEffects.Values)
				{
					if (activeEffect)
					{
						Object.Destroy(activeEffect);
					}
				}
				slotEffects.Clear();
			}

			return;
		}

		bool changed = false;
		for (int i = 0;; ++i)
		{
			if (!zdoInts.TryGetValue($"{keyPrefix} Effect {i}".GetStableHashCode(), out int effect))
			{
				effect = 0;
			}
			if (effect == 0)
			{
				if (i != slotEffects.Count)
				{
					changed = true;
				}
				break;
			}

			if (!slotEffects.TryGetValue(effect, out GameObject activeEffect) || !activeEffect)
			{
				if (activeEffect is null)
				{
					changed = true;
				}
				slotEffects[effect] = Object.Instantiate(effectHashMap[effect], equipRoot.transform, false);
			}
		}

		if (changed)
		{
			HashSet<int> removeEffects = new(slotEffects.Keys);
			for (int i = 0; zdoInts.TryGetValue($"{keyPrefix} Effect {i}".GetStableHashCode(), out int effect) && effect != 0; ++i)
			{
				removeEffects.Remove(effect);
			}

			foreach (int effect in removeEffects)
			{
				GameObject effectObject = slotEffects[effect];
				if (effectObject)
				{
					Object.Destroy(effectObject);
				}
				slotEffects.Remove(effect);
			}
		}
	}

	[HarmonyPatch(typeof(ItemStand), nameof(ItemStand.UpdateVisual))]
	private static class ApplyItemStandGemEffects
	{
		private static readonly ConditionalWeakTable<ItemStand, Dictionary<int, GameObject>> activeEffects = new();

		private static void Postfix(ItemStand __instance)
		{
			if (__instance.m_nview.m_zdo is { } zdo && __instance.m_visualItem)
			{
				ZDOExtraData.s_ints.Init(zdo.m_uid);
				ApplySlotEffects(activeEffects.GetOrCreateValue(__instance), ZDOExtraData.s_ints[zdo.m_uid], "JewelCrafting Beard", __instance.m_visualItem);
			}
		}
	}

	public static Dictionary<string, GameObject[]>? prefabDict(ItemDrop.ItemData.SharedData shared)
	{
		VisualEffectCondition effectCondition = shared.m_itemType is ItemDrop.ItemData.ItemType.Bow or ItemDrop.ItemData.ItemType.Shield or ItemDrop.ItemData.ItemType.OneHandedWeapon or ItemDrop.ItemData.ItemType.TwoHandedWeapon ? SkillKey(shared) : ItemKey(shared);
		effectPrefabsByType.TryGetValue(effectCondition, out Dictionary<string, GameObject[]>? prefabs);
		return prefabs;
	}

	[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
	public static class DisplayEffectOnItemDrop
	{
		public static void Postfix(ItemDrop __instance)
		{
			if (Jewelcrafting.visualEffects.Value == Jewelcrafting.Toggle.On && prefabDict(__instance.m_itemData.m_shared) is { } effectPrefabs && __instance.m_itemData.Data().Get<Sockets>() is { } itemSockets)
			{
				foreach (string socket in itemSockets.socketedGems.Select(i => i.Name))
				{
					if (effectPrefabs.TryGetValue(socket, out GameObject[] effects))
					{
						foreach (GameObject effect in effects)
						{
							Object.Instantiate(effect, __instance.transform.Find("attach"), false);
						}
					}
				}
			}
		}

		public static void RemoveEffects(ItemDrop item)
		{
			if (prefabDict(item.m_itemData.m_shared) is { } effectPrefabs && item.m_itemData.Data().Get<Sockets>() is { } itemSockets)
			{
				foreach (string socket in itemSockets.socketedGems.Select(i => i.Name))
				{
					if (effectPrefabs.TryGetValue(socket, out GameObject[] effects))
					{
						Transform attach = item.transform.Find("attach");
						for (int j = 0, children = attach.childCount; j < children; ++j)
						{
							foreach (GameObject effect in effects)
							{
								if (global::Utils.GetPrefabName(attach.GetChild(j).gameObject) == effect.name)
								{
									Object.Destroy(attach.GetChild(j).gameObject);
								}
							}
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
	private static class DetermineProjectileEffects
	{
		private static void Prefix(Projectile __instance, ItemDrop.ItemData? item)
		{
			if (item?.m_shared.m_skillType == Skills.SkillType.Spears && item.Data().Get<Sockets>()?.socketedGems is { } gems)
			{
				int i = 0;
				foreach (string socket in gems.Select(i => i.Name))
				{
					if (ObjectDB.instance.GetItemPrefab(socket) is { } gem && GemStoneSetup.GemInfos.TryGetValue(gem.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo gemInfo) && VisualEffectSetup.spearProjectiles.TryGetValue(gemInfo.Type, out GameObject effect))
					{
						__instance.m_nview.GetZDO().Set($"Jewelcrafting Effect {i++}", effect.name.GetStableHashCode());
					}
				}
				AttachProjectileEffects.Postfix(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), nameof(Projectile.Awake))]
	private static class AttachProjectileEffects
	{
		public static void Postfix(Projectile __instance)
		{
			ZDO zdo = __instance.m_nview.GetZDO();
			for (int i = 0; zdo.GetInt($"Jewelcrafting Effect {i}") is { } hash and not 0; ++i)
			{
				if (effectHashMap.TryGetValue(hash, out GameObject effect))
				{
					Object.Instantiate(effect, __instance.transform);
				}
			}
		}
	}

	private static VisualEffectCondition ItemKey(ItemDrop.ItemData.SharedData shared) => (VisualEffectCondition)((int)shared.m_itemType << 12)
	                                                                                     | (shared.m_itemType is ItemDrop.ItemData.ItemType.Tool && shared.m_name.Contains("$item_hammer") ? Hammer : 0)
	                                                                                     | (shared.m_itemType is ItemDrop.ItemData.ItemType.Tool && shared.m_name.Contains("$item_hoe") ? Hoe : 0);

	private static VisualEffectCondition SkillKey(ItemDrop.ItemData.SharedData shared) => (VisualEffectCondition)shared.m_skillType
	                                                                                      | (shared.m_skillType is Skills.SkillType.Axes or Skills.SkillType.Clubs && shared.m_itemType is ItemDrop.ItemData.ItemType.TwoHandedWeapon ? TwoHanded : 0)
	                                                                                      | (shared.m_skillType is Skills.SkillType.Blocking && shared.m_timedBlockBonus <= 1 ? Towershield : 0)
	                                                                                      | (shared.m_skillType is Skills.SkillType.Blocking && shared.m_name.Contains("blackmetal") ? Blackmetal : 0)
	                                                                                      | (shared.m_skillType is Skills.SkillType.Blocking && shared.m_name.Contains("buckler") ? Buckler : 0)
	                                                                                      | (shared.m_skillType is Skills.SkillType.Bows && shared.m_name.Contains("$item_bow_finewood") ? FineWoodBow : 0)
	                                                                                      | (shared.m_skillType is Skills.SkillType.Bows && shared.m_name.Contains("$item_bow_huntsman") ? BowHuntsman : 0)
	                                                                                      | (shared.m_skillType is Skills.SkillType.Bows && shared.m_name.Contains("$item_bow_draugrfang") ? BowDraugrFang : 0)
	                                                                                      | (shared.m_skillType is Skills.SkillType.Pickaxes && shared.m_name.Contains("$item_pickaxe_iron") ? PickaxeIron : 0)
	                                                                                      | (shared.m_skillType is Skills.SkillType.Clubs && shared.m_name.Contains("$item_club") ? Club : 0);
}
