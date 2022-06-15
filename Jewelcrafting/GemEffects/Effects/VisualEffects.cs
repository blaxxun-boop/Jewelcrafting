using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class VisualEffects
{
	public static readonly Dictionary<string, Dictionary<Skills.SkillType, GameObject>> weaponEffectPrefabs = new()
	{
		{ "Perfect_Red_Socket", Jewelcrafting.fireStarter },
		{ "Perfect_Blue_Socket", Jewelcrafting.iceHeart },
		{ "Perfect_Green_Socket", Jewelcrafting.snakeBite },
		{ "Perfect_Black_Socket", Jewelcrafting.shadowHit }
	};

	[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
	private static class ApplyGemEffects
	{
		static ApplyGemEffects()
		{
			foreach (GameObject effect in weaponEffectPrefabs.Values.SelectMany(g => g.Values))
			{
				effectHashMap.Add(effect.name.GetStableHashCode(), effect);
			}
		}

		private static readonly Dictionary<int, GameObject> effectHashMap = new();

		private static readonly ConditionalWeakTable<VisEquipment, Dictionary<string, EffectCache>> activeEffects = new();

		private struct EffectCache
		{
			public int hash;
			public GameObject equipObject;
		}

		private static void Postfix(VisEquipment __instance)
		{
			if (__instance.m_nview.m_zdo is { } zdo && __instance.m_isPlayer)
			{
				Dictionary<string, EffectCache> effectsActive = activeEffects.GetOrCreateValue(__instance);	
				
				for (int i = 0; i < 5; ++i)
				{
					string name = $"JewelCrafting LeftHand Effect {i}";
					int effect = zdo.GetInt(name);
					if (effectsActive.TryGetValue(name, out EffectCache active))
					{
						if (active.hash == effect && active.equipObject)
						{
							continue;
						}

						if (__instance.m_leftItemInstance?.transform is { } item)
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
					if (effect != 0 && __instance.m_leftItemInstance is { } equipObject)
					{
						effectsActive[name] = new EffectCache { hash = effect, equipObject = equipObject };
						Object.Instantiate(effectHashMap[effect], equipObject.transform, false);
					}
				}

				for (int i = 0; i < 5; ++i)
				{
					string name = $"JewelCrafting RightHand Effect {i}";
					int effect = zdo.GetInt(name);
					if (effectsActive.TryGetValue(name, out EffectCache active))
					{
						if (active.hash == effect && active.equipObject)
						{
							continue;
						}

						if (__instance.m_rightItemInstance?.transform is { } item)
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
					if (effect != 0 && __instance.m_rightItemInstance is { } equipObject)
					{
						effectsActive[name] = new EffectCache { hash = effect, equipObject = equipObject };
						Object.Instantiate(effectHashMap[effect], equipObject.transform, false);
					}
				}
			}
		}
	}
}
