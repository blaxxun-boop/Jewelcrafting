using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class TurtleShell
{
	static TurtleShell()
	{
		EffectDef.ConfigTypes.Add(Effect.Turtleshell, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	private static class ReduceBackDamage
	{
		private static void Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && (Mathf.Abs(Vector3.SignedAngle(player.transform.forward, hit.m_point - player.transform.position, Vector3.up)) > 90 || player.GetEffect(Effect.Turtleembrace) / 100f > Random.value))
			{
				hit.ApplyModifier(1 - player.GetEffect(Effect.Turtleshell) / 100f);
			}
		}
	}
}
