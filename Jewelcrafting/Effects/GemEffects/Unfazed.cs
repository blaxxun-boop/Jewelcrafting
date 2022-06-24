using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Unfazed
{
	static Unfazed()
	{
		EffectDef.ConfigTypes.Add(Effect.Unfazed, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.GetStaggerTreshold))]
	private static class IncreaseStaggerThreshold
	{
		private static void Postfix(Character __instance, ref float __result)
		{
			if (__instance == Player.m_localPlayer)
			{
				__result *= 1 + ((Player)__instance).GetEffect(Effect.Unfazed) / 100f;
			}
		}
	}
}
