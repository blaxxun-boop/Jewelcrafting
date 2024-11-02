using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting.LootSystem;

public static class GemDrops
{
	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
	private class AddGemStonesToDrops
	{
		private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
		{
			if ((Jewelcrafting.lootSystem.Value & Jewelcrafting.LootSystem.GemDrops) != 0)
			{
				List<KeyValuePair<GameObject, int>> drops;
				if (Jewelcrafting.gemDropBiomeDistribution.Value == Jewelcrafting.Toggle.On && ChestDrops.config.biomeConfig.TryGetValue(Heightmap.FindBiome(__instance.m_character.m_baseAI.m_spawnPoint), out GemDropBiome biomeDrops) && biomeDrops.distribution is { Count: > 0 } distribution)
				{
					drops = new List<KeyValuePair<GameObject, int>>();
					float count = Jewelcrafting.gemDropChances.Values.Sum(c => c.Value) / 100f;
					for (int i = 1; i <= count; ++i)
					{
						drops.Add(new KeyValuePair<GameObject, int>(GemStoneSetup.uncutGems[ChestDrops.SelectGem(distribution)], 1));
					}
					if (Random.value < count - Mathf.FloorToInt(count))
					{
						drops.Add(new KeyValuePair<GameObject, int>(GemStoneSetup.uncutGems[ChestDrops.SelectGem(distribution)], 1));
					}
				}
				else
				{
					drops = (from gem in Jewelcrafting.gemDropChances.Keys where Random.value < Jewelcrafting.gemDropChances[gem].Value / 100f select new KeyValuePair<GameObject, int>(gem, 1)).ToList();
				}
				Stats.gemsDroppedCreature.Increment(drops.Count);
				__result.AddRange(drops);
			}
		}
	}
}
