using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class VisualEffects
{
	private static readonly Dictionary<string, Dictionary<Skills.SkillType, GameObject>> handAttachEffectPrefabs = new()
	{
		{ "Perfect_Red_Socket", VisualEffectSetup.redGemEffects },
		{ "Perfect_Blue_Socket", VisualEffectSetup.blueGemEffects },
		{ "Perfect_Green_Socket", VisualEffectSetup.greenGemEffects },
		{ "Perfect_Black_Socket", VisualEffectSetup.blackGemEffects },
		{ "Perfect_Yellow_Socket", VisualEffectSetup.yellowGemEffects },
		{ "Perfect_Purple_Socket", VisualEffectSetup.purpleGemEffects }
	};

	private static readonly Dictionary<string, Dictionary<ItemDrop.ItemData.ItemType, GameObject>> armorEffectPrefabs = new()
	{
		{ "Perfect_Red_Socket", VisualEffectSetup.redArmorEffects },
		{ "Perfect_Blue_Socket", VisualEffectSetup.blueArmorEffects },
		{ "Perfect_Green_Socket", VisualEffectSetup.greenArmorEffects },
		{ "Perfect_Black_Socket", VisualEffectSetup.blackArmorEffects },
		{ "Perfect_Yellow_Socket", VisualEffectSetup.yellowArmorEffects },
		{ "Perfect_Purple_Socket", VisualEffectSetup.purpleArmorEffects }
	};

	private static readonly Dictionary<Skills.SkillType, Dictionary<string, GameObject[]>> handAttachPrefabsBySkill = new();
	private static readonly Dictionary<ItemDrop.ItemData.ItemType, Dictionary<string, GameObject[]>> armorPrefabsByType = new();

	private const int TwoHandedVal = 0x80000;
	private const int TowershieldVal = 0x100000;
	private const int BlackmetalVal = 0x120000;
	private const int BucklerVal = 0x140000;
	private const int FineWoodBowVal = 0x160000;
	private const int HuntsmanBowVal = 0x180000;
	private const int DraugrFangVal = 0x200000;
	private const int HammerVal = 0x220000;
	private const int HoeVal = 0x240000;
	private const int PickaxeIronVal = 0x260000;

	private static void FillEffectHashMap()
	{
		void AddToEffectMap<T>(Dictionary<string, Dictionary<T, GameObject>> effectPrefabs, Dictionary<T, Dictionary<string, GameObject[]>> inverse)
		{
			foreach (KeyValuePair<string, Dictionary<T, GameObject>> kv in effectPrefabs)
			{
				foreach (KeyValuePair<T, GameObject> effectKv in kv.Value)
				{
					if (!inverse.TryGetValue(effectKv.Key, out Dictionary<string, GameObject[]> inverseDict))
					{
						inverseDict = inverse[effectKv.Key] = new Dictionary<string, GameObject[]>();
					}
					inverseDict.Add(kv.Key, new []{ effectKv.Value });

					effectHashMap.Add(effectKv.Value.name.GetStableHashCode(), effectKv.Value);
				}
			}
			
			foreach (KeyValuePair<T, Dictionary<string, GameObject[]>> inverseKv in inverse)
			{
				foreach (KeyValuePair<GemType, Dictionary<GemType, GameObject[]>> mergedKv in MergedGemStoneSetup.mergedGems)
				{
					foreach (KeyValuePair<GemType, GameObject[]> gemKv in mergedKv.Value)
					{
						for (int i = 0; i < gemKv.Value.Length; ++i)
						{
							Dictionary<T, GameObject>?[] prefabDicts = new Dictionary<T, GameObject>[2];
							effectPrefabs.TryGetValue(GemStoneSetup.Gems[mergedKv.Key][i].Prefab.name, out prefabDicts[0]);
							effectPrefabs.TryGetValue(GemStoneSetup.Gems[gemKv.Key][i].Prefab.name, out prefabDicts[1]);

							List<GameObject> prefabs = new();
							foreach (Dictionary<T, GameObject>? prefabDict in prefabDicts)
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
		}

		AddToEffectMap(handAttachEffectPrefabs, handAttachPrefabsBySkill);
		AddToEffectMap(armorEffectPrefabs, armorPrefabsByType);
	}

	private static readonly Dictionary<int, GameObject> effectHashMap = new();

	private struct EffectCache
	{
		public int hash;
		public GameObject equipObject;
	}

	[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
	private static class ApplyGemEffects
	{
		static ApplyGemEffects()
		{
			FillEffectHashMap();
		}

		private static readonly ConditionalWeakTable<VisEquipment, Dictionary<string, EffectCache>> activeEffects = new();

		private static void Postfix(VisEquipment __instance)
		{
			if (__instance.m_nview.m_zdo is { } zdo && __instance.m_isPlayer)
			{
				Dictionary<string, EffectCache> effectsActive = activeEffects.GetOrCreateValue(__instance);

				void Apply(VisSlot part, GameObject? equipRoot) => ApplyEffects(effectsActive, zdo, part, equipRoot);

				Apply(VisSlot.HandLeft, __instance.m_leftItemInstance);
				Apply(VisSlot.BackLeft, __instance.m_leftBackItemInstance);
				Apply(VisSlot.HandRight, __instance.m_rightItemInstance);
				Apply(VisSlot.BackRight, __instance.m_rightBackItemInstance);
			}
		}
	}

	private static void ApplyEffects(Dictionary<string, EffectCache> effectsActive, ZDO zdo, VisSlot part, GameObject? equipRoot)
	{
		for (int i = 0; i < 10; ++i)
		{
			string name = $"JewelCrafting {part} Effect {i}";
			int effect = zdo.GetInt(name);
			if (effectsActive.TryGetValue(name, out EffectCache active))
			{
				if (active.hash == effect && active.equipObject && Jewelcrafting.visualEffects.Value == Jewelcrafting.Toggle.On)
				{
					continue;
				}

				if (equipRoot?.transform is { } item)
				{
					string effectName = effectHashMap[active.hash].name;
					for (int j = 0, children = item.childCount; j < children; ++j)
					{
						if (global::Utils.GetPrefabName(item.GetChild(j).gameObject) == effectName)
						{
							Object.Destroy(item.GetChild(j).gameObject);
						}
					}
				}
				effectsActive.Remove(name);
			}
			if (effect != 0 && equipRoot is not null && Jewelcrafting.visualEffects.Value == Jewelcrafting.Toggle.On)
			{
				effectsActive[name] = new EffectCache { hash = effect, equipObject = equipRoot };
				Object.Instantiate(effectHashMap[effect], equipRoot.transform, false);
			}
		}
	}

	[HarmonyPatch(typeof(ItemStand), nameof(ItemStand.UpdateVisual))]
	private static class ApplyItemStandGemEffects
	{
		private static readonly ConditionalWeakTable<ItemStand, Dictionary<string, EffectCache>> activeEffects = new();

		private static void Postfix(ItemStand __instance)
		{
			if (__instance.m_nview.m_zdo is { } zdo && __instance.m_visualItem)
			{
				Dictionary<string, EffectCache> effectsActive = activeEffects.GetOrCreateValue(__instance);
				ApplyEffects(effectsActive, zdo, VisSlot.Beard, __instance.m_visualItem);
			}
		}
	}

	public static Dictionary<string, GameObject[]>? prefabDict(ItemDrop.ItemData.SharedData shared)
	{
		Dictionary<string, GameObject[]>? prefabs;
		if (shared.m_itemType is ItemDrop.ItemData.ItemType.Bow or ItemDrop.ItemData.ItemType.Shield or ItemDrop.ItemData.ItemType.OneHandedWeapon or ItemDrop.ItemData.ItemType.TwoHandedWeapon)
		{
			handAttachPrefabsBySkill.TryGetValue(SkillKey(shared), out prefabs);
		}
		else
		{
			armorPrefabsByType.TryGetValue(ItemKey(shared), out prefabs);
		}

		return prefabs;
	}

	[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
	public static class DisplayEffectOnItemDrop
	{
		public static void Postfix(ItemDrop __instance)
		{
			if (Jewelcrafting.visualEffects.Value == Jewelcrafting.Toggle.On && prefabDict(__instance.m_itemData.m_shared) is { } effectPrefabs && __instance.m_itemData.Extended()?.GetComponent<Sockets>() is { } itemSockets)
			{
				foreach (string socket in itemSockets.socketedGems)
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
			if (prefabDict(item.m_itemData.m_shared) is { } effectPrefabs && item.m_itemData.Extended()?.GetComponent<Sockets>() is { } itemSockets)
			{
				foreach (string socket in itemSockets.socketedGems)
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

	private static ItemDrop.ItemData.ItemType ItemKey(ItemDrop.ItemData.SharedData shared) => (ItemDrop.ItemData.ItemType)((int)shared.m_itemType
		                                      | (shared.m_itemType is ItemDrop.ItemData.ItemType.Tool && shared.m_name.Contains("$item_hammer") ? HammerVal : 0)
		                                      | (shared.m_itemType is ItemDrop.ItemData.ItemType.Tool && shared.m_name.Contains("$item_hoe") ? HoeVal : 0)
			);

	public static ItemDrop.ItemData.ItemType Hammer() => (ItemDrop.ItemData.ItemType)(HammerVal | (int)ItemDrop.ItemData.ItemType.Tool);
	public static ItemDrop.ItemData.ItemType Hoe() => (ItemDrop.ItemData.ItemType)(HoeVal | (int)ItemDrop.ItemData.ItemType.Tool);

	private static Skills.SkillType SkillKey(ItemDrop.ItemData.SharedData shared) => (Skills.SkillType)((int)shared.m_skillType
		                          | (shared.m_skillType is Skills.SkillType.Axes or Skills.SkillType.Clubs && shared.m_itemType is ItemDrop.ItemData.ItemType.TwoHandedWeapon ? TwoHandedVal : 0)
		                          | (shared.m_skillType is Skills.SkillType.Blocking && shared.m_timedBlockBonus <= 1 ? TowershieldVal : 0)
		                          | (shared.m_skillType is Skills.SkillType.Blocking && shared.m_name.Contains("blackmetal") ? BlackmetalVal : 0)
		                          | (shared.m_skillType is Skills.SkillType.Blocking && shared.m_name.Contains("buckler") ? BucklerVal : 0)
		                          | (shared.m_skillType is Skills.SkillType.Bows && shared.m_name.Contains("$item_bow_finewood") ? FineWoodBowVal : 0)
		                          | (shared.m_skillType is Skills.SkillType.Bows && shared.m_name.Contains("$item_bow_huntsman") ? HuntsmanBowVal : 0)
		                          | (shared.m_skillType is Skills.SkillType.Bows && shared.m_name.Contains("$item_bow_draugrfang") ? DraugrFangVal : 0)
		                          | (shared.m_skillType is Skills.SkillType.Pickaxes && shared.m_name.Contains("$item_pickaxe_iron") ? PickaxeIronVal : 0)
			);

	public static Skills.SkillType TwoHanded(Skills.SkillType type) => (Skills.SkillType)(TwoHandedVal | (int)type);
	public static Skills.SkillType Blackmetal(Skills.SkillType type) => (Skills.SkillType)(BlackmetalVal | (int)type);
	public static Skills.SkillType Buckler() => (Skills.SkillType)(BucklerVal | (int)Skills.SkillType.Blocking);
	public static Skills.SkillType Towershield() => (Skills.SkillType)(TowershieldVal | (int)Skills.SkillType.Blocking);
	public static Skills.SkillType FineWoodBow() => (Skills.SkillType)(FineWoodBowVal | (int)Skills.SkillType.Bows);
	public static Skills.SkillType BowHuntsman() => (Skills.SkillType)(HuntsmanBowVal | (int)Skills.SkillType.Bows);
	public static Skills.SkillType BowDraugrFang() => (Skills.SkillType)(DraugrFangVal | (int)Skills.SkillType.Bows);
	public static Skills.SkillType PickaxeIron() => (Skills.SkillType)(PickaxeIronVal | (int)Skills.SkillType.Pickaxes);
}
