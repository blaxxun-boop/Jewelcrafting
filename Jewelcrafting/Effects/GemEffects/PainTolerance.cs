using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class PainTolerance
{
	static PainTolerance()
	{
		EffectDef.ConfigTypes.Add(Effect.Paintolerance, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class ReduceDamageTaken
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && hit.GetAttacker() is { } attacker && attacker != __instance)
			{
				hit.ApplyModifier(1 - player.GetEffect(Effect.Paintolerance) / 100f);
			}
		}
	}
}
