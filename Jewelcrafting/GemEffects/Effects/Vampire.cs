using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Vampire
{
	static Vampire()
	{
		EffectDef.ConfigTypes.Add(Effect.Vampire, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private static class AddLifeSteal
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player attacker && Random.value < attacker.GetEffect(Effect.Vampire) / 100f)
			{
				attacker.Heal(Random.value * 6);
			}
		}
	}
}
