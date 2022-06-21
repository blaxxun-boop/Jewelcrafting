using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class SnakeBite
{
	static SnakeBite()
	{
		EffectDef.ConfigTypes.Add(Effect.Snakebite, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private class AddBonusPoisonDamage
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player attacker && Random.value <= 0.2)
			{
				hit.m_damage.m_poison += hit.GetTotalDamage() * attacker.GetEffect(Effect.Snakebite) / 100f;
			}
		}
	}
}
