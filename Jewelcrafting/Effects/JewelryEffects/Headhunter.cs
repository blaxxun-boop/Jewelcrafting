using System.Collections.Generic;
using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Headhunter
{
	static Headhunter()
	{
		API.OnEffectRecalc += () =>
		{
			if (Player.m_localPlayer.m_utilityItem?.m_shared.m_name != "$jc_ring_green")
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
				if (p.m_visEquipment.m_currentUtilityItemHash == JewelrySetup.greenRingHash)
				{
					p.m_seman.AddStatusEffect(GemEffectSetup.headhunter.name, true);
				}
			}
		}
	}
}
