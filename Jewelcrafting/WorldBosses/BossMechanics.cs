using System;
using System.Collections;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.WorldBosses;

public static class BossMechanics
{
	[HarmonyPatch(typeof(Aoe), nameof(Aoe.OnHit))]
	private class ApplyElementalDebuffs
	{
		private static void Prefix(Collider collider, Aoe __instance)
		{
			void Check(string aoe, Func<Player, IEnumerator> setDebuff)
			{
				if (!__instance.name.StartsWith(aoe, StringComparison.Ordinal))
				{
					return;
				}

				GameObject hitObject = Projectile.FindHitObject(collider);
				if (!__instance.m_hitList.Contains(hitObject) && hitObject.GetComponent<Character>() is Player player)
				{
					player.StartCoroutine(setDebuff(player));
				}
			}

			IEnumerator delayedFrostEffect(Player player)
			{
				yield return new WaitForSeconds(5);
				player.m_seman.AddStatusEffect(GemEffectSetup.frostBossDebuff.name.GetStableHashCode(), true);
			}
			Check("JC_Boss_Explosion_Frost", delayedFrostEffect);
			IEnumerator delayedPoisonEffect(Player player)
			{
				yield return new WaitForSeconds(5);
				player.m_seman.AddStatusEffect(GemEffectSetup.poisonBossDebuff.name.GetStableHashCode(), true);
			}
			Check("JC_Boss_Explosion_Poison", delayedPoisonEffect);
			IEnumerator delayedFireEffect(Player player)
			{
				yield return new WaitForSeconds(5);
				player.m_seman.AddStatusEffect(GemEffectSetup.fireBossDebuff.name.GetStableHashCode(), true);
			}
			Check("JC_Boss_Explosion_Flame", delayedFireEffect);
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class IncreaseElementalDamageTaken
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player)
			{
				if (player.m_seman.HaveStatusEffect(GemEffectSetup.fireBossDebuff.name.GetStableHashCode()))
				{
					hit.m_damage.m_fire *= 2;
				}

				if (player.m_seman.HaveStatusEffect(GemEffectSetup.frostBossDebuff.name.GetStableHashCode()))
				{
					hit.m_damage.m_frost *= 2;
				}

				if (player.m_seman.HaveStatusEffect(GemEffectSetup.poisonBossDebuff.name.GetStableHashCode()))
				{
					hit.m_damage.m_poison *= 2;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
	private static class IncrementDeathByWorldBoss
	{
		private static void Prefix(Player __instance)
		{
			if (__instance.m_lastHit?.GetAttacker() is BossSetup.BossCharacter)
			{
				Stats.deathByWorldBoss.Increment();
			}
		}
	}
}
