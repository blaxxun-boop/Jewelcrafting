using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Marathon
{
	static Marathon()
	{
		EffectDef.ConfigTypes.Add(Effect.Marathon, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyRunStaminaDrain))]
	private static class ReduceStaminaUsage
	{
		private static void Prefix(SEMan __instance, ref float drain)
		{
			if (__instance.m_character is Player player)
			{
				drain *= 1 - player.GetEffect(Effect.Marathon) / 100f;
			}
		}
	}
}
