using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class SnowPiercer
{
	static SnowPiercer()
	{
		EffectDef.ConfigTypes.Add(Effect.Snowpiercer, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.UpdateWalking))]
	public static class ReducePenalty
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, ref float __state)
		{
			__state = __instance.m_deepSnowSlowMax;
			if (__instance is Player player)
			{
				__state *= 1 - player.GetEffect<Config>(Effect.Snowpiercer).Power / 100;
			}
		}

		[UsedImplicitly]
		private static void Postfix(Character __instance, ref float __state)
		{
			__instance.m_deepSnowSlowMax = __state;
		}
	}
}
