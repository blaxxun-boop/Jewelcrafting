using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class MagicalBargain
{
	static MagicalBargain()
	{
		EffectDef.ConfigTypes.Add(Effect.Magicalbargain, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr))]
	private static class ReduceEitrUsage
	{
		private static void Postfix(Attack __instance, ref float __result)
		{
			if (__instance.m_character is Player player)
			{
				__result *= 1 - player.GetEffect(Effect.Magicalbargain) / 100f;
			}
		}
	}
}
