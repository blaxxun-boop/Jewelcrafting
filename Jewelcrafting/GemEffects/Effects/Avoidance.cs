using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Avoidance
{
	static Avoidance()
	{
		EffectDef.ConfigTypes.Add(Effect.Avoidance, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class AddChanceToAvoidDamage
	{
		[UsedImplicitly]
		private static bool Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && hit.GetAttacker() is { } attacker && attacker != __instance)
			{
				return !(Random.Range(0f, 1f) < player.GetEffect(Effect.Avoidance) / 100f);
			}

			return true;
		}
	}
}
