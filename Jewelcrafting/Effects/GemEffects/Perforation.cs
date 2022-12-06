using System.Collections;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Perforation
{
	static Perforation()
	{
		EffectDef.ConfigTypes.Add(Effect.Perforation, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private class ApplyBleeding
	{
		private static void Postfix(Character __instance, HitData hit)
		{
			if (hit.GetAttacker() is Player { m_currentAttackIsSecondary: true } player && player.GetEffect(Effect.Perforation) > 0)
			{
				__instance.StartCoroutine(DamageEnemy(__instance, hit, player)); 
			}
		}

		private static IEnumerator DamageEnemy(Character character, HitData hit, Player attacker)
		{
			HitData bleedHit = new()
			{
				m_damage = new HitData.DamageTypes { m_damage = hit.GetTotalDamage() * attacker.GetEffect(Effect.Perforation) / 100f / 4f },
				m_point = character.transform.position
			};
			for (int i = 0; i < 4; ++i)
			{
				yield return new WaitForSeconds(0.5f);
				character.Damage(bleedHit);
			}
		}
	}
}
