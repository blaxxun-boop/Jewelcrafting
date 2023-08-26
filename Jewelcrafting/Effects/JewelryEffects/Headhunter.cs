using System.Collections.Generic;
using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Headhunter
{
	static Headhunter()
	{
		API.OnEffectRecalc += () =>
		{
			if (!Utils.IsJewelryEquipped(Player.m_localPlayer, "JC_Ring_Green"))
			{
				Player.m_localPlayer.m_seman.RemoveStatusEffect(GemEffectSetup.headhunter);
			}
		};
	}
	
	[HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.DelayedSpawnBoss))]
	private static class ApplyBossBuffs
	{
		private static void Postfix(OfferingBowl __instance)
		{
			List<Player> nearbyPlayers = new();
			Player.GetPlayersInRange(__instance.m_bossSpawnPoint, 50f, nearbyPlayers);

			foreach (Player p in nearbyPlayers)
			{
				if (Utils.IsJewelryEquipped(p, "JC_Ring_Green"))
				{
					p.m_seman.AddStatusEffect(GemEffectSetup.headhunter.NameHash(), true);
				}
			}
		}
	}
}
