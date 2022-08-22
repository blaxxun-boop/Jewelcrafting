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
	public struct Config
	{
		[AdditivePower] public float Power;
		[MaxPower] [OptionalPower(20f)] public float Chance;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private class AddBonusFrostDamage
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player attacker)
			{
				Config config = attacker.GetEffect<Config>(Effect.Snakebite);
				if (Random.value <= config.Chance)
				{
					hit.m_damage.m_poison += hit.GetTotalDamage() * config.Power / 100f;
				}
			}
		}
	}
}
