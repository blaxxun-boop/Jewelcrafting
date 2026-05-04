using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting.LootSystem;

public static class GemDrops
{
	static GemDrops()
	{
		IEnumerable<CharacterDrop.Drop> drop(Character character)
		{
			if ((Jewelcrafting.lootSystem.Value & Jewelcrafting.LootSystem.GemDrops) != 0)
			{
				if (Jewelcrafting.gemDropBiomeDistribution.Value == Jewelcrafting.Toggle.On && ChestDrops.config.biomeConfig.TryGetValue(Heightmap.FindBiome(character.m_baseAI.m_spawnPoint), out GemDropBiome biomeDrops) && biomeDrops.distribution is { Count: > 0 } distribution)
				{
					float count = Jewelcrafting.gemDropChances.Values.Sum(c => c.Value) / 100f;
					for (int i = 1; i <= count; ++i)
					{
						yield return LootAdder.Drop(GemStoneSetup.uncutGems[ChestDrops.SelectGem(distribution)]);
					}
					if (Random.value < count - Mathf.FloorToInt(count))
					{
						yield return LootAdder.Drop(GemStoneSetup.uncutGems[ChestDrops.SelectGem(distribution)]);
					}
				}
				else
				{
					foreach (GameObject gem in Jewelcrafting.gemDropChances.Keys)
					{
						yield return LootAdder.Drop(gem, Jewelcrafting.gemDropChances[gem].Value / 100f);
					}
				}
			}
		}
		LootAdder.Loot.Add(drop);
	}
	
	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
	private static class CountGems
	{
		private static void Postfix(List<KeyValuePair<GameObject, int>> __result)
		{
			Stats.orbsDroppedCreature.Increment(__result.Where(kv => GemStoneSetup.uncutGems.ContainsValue(kv.Key)).Select(kv => kv.Value).Sum());
		}
	}
}
