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
		private static void Postfix(List<KeyValuePair<GameObject, int>> __result)
		{
			if ((Jewelcrafting.lootSystem.Value & Jewelcrafting.LootSystem.GemDrops) != 0)
			{
				List<KeyValuePair<GameObject, int>> drops = (from gem in Jewelcrafting.gemDropChances.Keys where Random.value < Jewelcrafting.gemDropChances[gem].Value / 100f select new KeyValuePair<GameObject, int>(gem, 1)).ToList();
				Stats.gemsDroppedCreature.Increment(drops.Count);
				__result.AddRange(drops);
			}
		}
	}
}
