using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class PreciousBlood
{
	static PreciousBlood()
	{
		EffectDef.ConfigTypes.Add(Effect.Preciousblood, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackHealth))]
	private static class ReduceHealthUsage
	{
		private static void Postfix(Attack __instance, ref float __result)
		{
			if (__instance.m_character is Player player)
			{
				__result *= 1 - player.GetEffect(Effect.Preciousblood) / 100f;
			}
		}
	}
}
