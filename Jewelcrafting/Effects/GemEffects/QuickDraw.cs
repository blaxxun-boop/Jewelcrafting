using System;
using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class QuickDraw
{
	static QuickDraw()
	{
		EffectDef.ConfigTypes.Add(Effect.Quickdraw, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage))]
	private static class IncreaseDrawSpeed
	{
		private static void Postfix(Humanoid __instance, ref float __result)
		{
			if (__instance is Player player && player.GetEffect(Effect.Quickdraw) > 0)
			{
				__result *= 1 + player.GetEffect(Effect.Quickdraw) / 100f;
				__result = Math.Min(__result, 1f);
			}
		}
	}
}
