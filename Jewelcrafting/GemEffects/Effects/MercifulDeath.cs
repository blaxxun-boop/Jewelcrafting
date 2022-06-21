using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class MercifulDeath
{
	static MercifulDeath()
	{
		EffectDef.ConfigTypes.Add(Effect.Mercifuldeath, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.HardDeath))]
	private static class PreventXPLoss
	{
		private static void Prefix(Player __instance)
		{
			if (Random.value < __instance.GetEffect(Effect.Mercifuldeath) / 100)
			{
				__instance.m_timeSinceDeath = 0;
			}
		}
	}
}
