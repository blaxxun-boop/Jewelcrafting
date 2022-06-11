using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class MercifulDeath
{
	[HarmonyPatch(typeof(Player), nameof(Player.HardDeath))]
	private static class PreventXPLoss
	{
		private static void Prefix(Player __instance)
		{
			if (Random.value < __instance.GetEffect(Effect.Mercifuldeath))
			{
				__instance.m_timeSinceDeath = 0;
			}
		}
	}
}
