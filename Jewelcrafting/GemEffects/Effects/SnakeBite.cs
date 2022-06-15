using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class SnakeBite
{
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
