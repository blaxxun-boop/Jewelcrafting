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
		[MaxPower] [OptionalPower(100f)] public float DamageReduction;
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class AddChanceToAvoidDamage
	{
		[UsedImplicitly]
		private static bool Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && hit.GetAttacker() is { } attacker && attacker != __instance && Random.Range(0f, 1f) < player.GetEffect(Effect.Avoidance) / 100f)
			{
				if (player.GetEffect<Config>(Effect.Avoidance).DamageReduction >= 100f)
				{
					return false;
				}
				
				hit.ApplyModifier(1 - player.GetEffect<Config>(Effect.Avoidance).DamageReduction / 100f);
			}

			return true;
		}
	}
}
