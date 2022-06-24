using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class FireStarter
{
	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private class AddBonusFireDamage
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player attacker && Random.value <= 0.2)
			{
				hit.m_damage.m_fire += hit.GetTotalDamage() * attacker.GetEffect(Effect.Firestarter) / 100f;
			}
		}
	}
}
