using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class LoyalDevotion
{
	private static readonly string[] Aspects = new[]
	{
		"Eikthyr",
		"Elder",
		"Bonemass",
		"Moder",
		"Yagluth",
		"SeekerQueen",
		"Fader",
	};
	
	static LoyalDevotion()
	{
		EffectDef.ConfigTypes.Add(Effect.Loyaldevotion, typeof(Config));
		foreach (string pet in Aspects)
		{
			ForcePet.RegisterPet("Aspect_" + pet);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Config
	{
		[MaxPower] public readonly float Damage;
		[AdditivePower] public readonly float Duration;
		[MinPower] public readonly float MinCooldown;
		[MinPower] public readonly float MaxCooldown;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
	private class StartCoroutineForEffect
	{
		private static void Postfix(Player __instance)
		{
			__instance.StartCoroutine(DamageProtection(__instance));
		}
	}

	private static IEnumerator DamageProtection(Player player)
	{
		while (true)
		{
			yield return player.WaitEffect<Config>(Effect.Loyaldevotion, c => c.MinCooldown, c => c.MaxCooldown);
			Config config = player.GetEffect<Config>(Effect.Loyaldevotion);
			if (config.Duration > 0 && !player.IsDead() && !Utils.SkipBossPower())
			{
				player.m_seman.AddStatusEffect(GemEffectSetup.loyaltyStart);

				yield return new WaitForSeconds(4);
				
				LoyalityEffect se = (LoyalityEffect)player.m_seman.AddStatusEffect(GemEffectSetup.loyalty);
				se.m_ttl = config.Duration;
				se.frostDamage = config.Damage;

				if (player.transform.position.y > 4500 || player.m_underRoof)
				{
					continue;
				}
				
				Object.Instantiate(GemEffectSetup.loyaltyEffect, player.transform);
				
				Transform transform = player.transform;
				GameObject pet = Object.Instantiate(ZNetScene.instance.GetPrefab("Aspect_" + Aspects[Random.Range(0, Aspects.Length)]), transform.position + Vector3.up, transform.rotation);
				pet.GetComponent<ForcePet>().MakePet(config.Duration);
				pet.GetComponent<MonsterAI>().SetAlerted(true);
			}
		}
		// ReSharper disable once IteratorNeverReturns
	}
	
	public class LoyalityEffect : SE_Stats
	{
		public float frostDamage = 10;
		
		public override void Setup(Character character)
		{
			base.Setup(character);
			m_tickInterval = 1;
			m_healthPerTickMinHealthPercentage = float.PositiveInfinity;
		}

		public override void UpdateStatusEffect(float dt)
		{
			base.UpdateStatusEffect(dt);
			if (m_tickTimer == 0)
			{
				foreach (Character character in Character.s_characters.Where(c => c != m_character && Vector3.Distance(c.transform.position, m_character.transform.position) <= 4 && (BaseAI.IsEnemy(c, m_character) || (c is Player enemy && enemy.IsPVPEnabled()))).ToList())
				{
					character.Damage(new HitData
					{
						m_damage =
						{
							m_frost = frostDamage,
						},
						m_attacker = m_character.GetZDOID(),
						m_point = character.GetTopPoint(),
						m_hitType = HitData.HitType.Freezing,
					});
				}
			}
		}
	}

}
