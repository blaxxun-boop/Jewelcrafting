using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Sprinter
{
	static Sprinter()
	{
		EffectDef.ConfigTypes.Add(Effect.Sprinter, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.GetJogSpeedFactor))]
	private class IncreaseJogSpeed
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += __instance.GetEffect(Effect.Sprinter) / 100f;
		}
	}
		
	[HarmonyPatch(typeof(Player), nameof(Player.GetRunSpeedFactor))]
	private class IncreaseRunSpeed
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += __instance.GetEffect(Effect.Sprinter) / 100f;
		}
	}
}
