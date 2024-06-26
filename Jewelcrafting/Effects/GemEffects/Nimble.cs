﻿using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Nimble
{
	static Nimble()
	{
		EffectDef.ConfigTypes.Add(Effect.Nimble, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}
	
	[HarmonyPatch(typeof(Player), nameof(Player.UpdateDodge))]
	private static class ReduceStaminaUsage
	{
		[UsedImplicitly]
		private static void Prefix(Player __instance)
		{
			__instance.m_dodgeStaminaUsage *= 1 - __instance.GetEffect(Effect.Nimble) / 100f;
		}

		[UsedImplicitly]
		private static void Postfix(Player __instance)
		{
			__instance.m_dodgeStaminaUsage /= 1 - __instance.GetEffect(Effect.Nimble) / 100f;
		}
	}
}
