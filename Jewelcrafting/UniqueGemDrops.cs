using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting;

public static class UniqueGemDrops
{
	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
	private static class AddGemDrop
	{
		[HarmonyPriority(Priority.VeryLow - 1)]
		private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
		{
			if (__instance.m_character.IsBoss() && Jewelcrafting.uniqueGemDropSystem.Value != Jewelcrafting.UniqueDrop.Disabled && GemStones.bossToGem.TryGetValue(global::Utils.GetPrefabName(__instance.gameObject), out GameObject bossDrop))
			{
				if (Jewelcrafting.uniqueGemDropSystem.Value == Jewelcrafting.UniqueDrop.TrulyUnique)
				{
					if (BossKilled(__instance.m_character))
					{
						return;
					}

					__result.Add(new KeyValuePair<GameObject, int>(bossDrop, 1));
				}
				else if (Random.value < Jewelcrafting.uniqueGemDropChance.Value / 100f)
				{
					__result.Add(new KeyValuePair<GameObject, int>(bossDrop, Jewelcrafting.uniqueGemDropOnePerPlayer.Value == Jewelcrafting.Toggle.On ? ZNet.instance.GetNrOfPlayers() : 1));
				}
			}
		}

		private static bool BossKilled(Character boss) => ZoneSystem.instance.GetGlobalKey(boss.m_defeatSetGlobalKey);
	}
}
