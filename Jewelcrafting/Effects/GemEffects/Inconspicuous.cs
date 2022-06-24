using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Inconspicuous
{
	static Inconspicuous()
	{
		EffectDef.ConfigTypes.Add(Effect.Inconspicuous, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyNoise))]
	private class ReduceNoise
	{
		private static void Prefix(SEMan __instance, ref float noise)
		{
			if (__instance.m_character is Player player)
			{
				noise *= 1 - player.GetEffect(Effect.Inconspicuous) / 100f;
			}
		}
	}
	
	[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyStealth))]
	private class IncreaseStealth
	{
		private static void Prefix(SEMan __instance, ref float stealth)
		{
			if (__instance.m_character is Player player)
			{
				stealth *= 1 - player.GetEffect(Effect.Inconspicuous) / 100f;
			}
		}
	}
}
