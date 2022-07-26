using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class FieryDoom
{
	static FieryDoom()
	{
		EffectDef.ConfigTypes.Add(Effect.Fierydoom, typeof(Config));
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public readonly float StaggerChance;
		[MinPower] public readonly float MinCooldown;
		[MinPower] public readonly float MaxCooldown;
		[AdditivePower] public readonly float Duration;
		[AdditivePower] public readonly float FireDamage;
	}
	
	[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
	private class StartCoroutineForEffect
	{
		private static void Postfix(Player __instance)
		{
			__instance.StartCoroutine(IncreaseStaggerChance(__instance));
		}
	}
	
	private static IEnumerator IncreaseStaggerChance(Player player)
	{
		while (true)
		{
			yield return player.WaitEffect<Config>(Effect.Fierydoom, c => c.MinCooldown, c => c.MaxCooldown);
			Config config = player.GetEffect<Config>(Effect.Fierydoom);
			if (config.Duration > 0)
			{
				player.m_seman.AddStatusEffect(Jewelcrafting.fireStart);

				yield return new WaitForSeconds(4);
				
				StatusEffect se = player.m_seman.AddStatusEffect(Jewelcrafting.fieryDoom);
				se.m_ttl = config.Duration;
				GameObject aoe = Object.Instantiate(Jewelcrafting.fieryDoomExplosion, player.transform);
				aoe.GetComponent<Aoe>().Setup(player, Vector3.zero, 50, new HitData
				{
					m_damage = new HitData.DamageTypes { m_fire = config.FireDamage }
				}, null);
				se.m_startEffectInstances = se.m_startEffectInstances.Concat(new []{ aoe }).ToArray();
			}
		}
		// ReSharper disable once IteratorNeverReturns
	}

	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	private static class AddStaggerOnAttack
	{
		private static void Postfix(Character __instance, HitData hit)
		{
			if (!__instance.IsStaggering() && hit.GetAttacker() is Player attacker && attacker.GetSEMan().HaveStatusEffect(Jewelcrafting.fieryDoom.name) && Random.value < attacker.GetEffect<Config>(Effect.Fierydoom).StaggerChance / 100f )
			{
				__instance.Stagger(hit.m_dir);
			}
		}
	}
}
