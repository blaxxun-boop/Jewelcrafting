using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.LootSystem;

public static class LootAdder
{
	public static readonly List<Func<Character, IEnumerable<CharacterDrop.Drop>>> Loot = new();
	
	[HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
	private class AddGemStonesToDrops
	{
		[HarmonyPriority(Priority.High)]
		private static void Prefix(Character __instance)
		{
			if (__instance.GetComponent<CharacterDrop>() is { } drops)
			{
				foreach (Func<Character, IEnumerable<CharacterDrop.Drop>> loot in Loot)
				{
					drops.m_drops.AddRange(loot(__instance));
				}
			}
		}
	}

	public static CharacterDrop.Drop Drop(GameObject prefab, float chance = 1, int num = 1) => new() { m_prefab = prefab, m_dontScale = true, m_levelMultiplier = false, m_amountMin = num, m_amountMax = num, m_chance = chance };
	public static CharacterDrop.Drop DropAmount(GameObject prefab, int num) => new() { m_prefab = prefab, m_dontScale = true, m_levelMultiplier = false, m_amountMin = num, m_amountMax = num, m_chance = 1 };
}
