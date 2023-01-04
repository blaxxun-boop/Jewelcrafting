using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class PoisonousDrain
{
	static PoisonousDrain()
	{
		EffectDef.ConfigTypes.Add(Effect.Poisonousdrain, typeof(Config));
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Config
	{
		[MultiplicativePercentagePower] public readonly float HealingIncrease;
		[MinPower] public readonly float MinCooldown;
		[MinPower] public readonly float MaxCooldown;
		[AdditivePower] public readonly float Duration;
		[AdditivePower] public readonly float PoisonDamage;
		[MultiplicativePercentagePower] public readonly float LifeSteal;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
	private class StartCoroutineForEffect
	{
		private static void Postfix(Player __instance)
		{
			__instance.StartCoroutine(HealingReceived(__instance));
		}
	}

	private class PoisonCloudEffect : StatusEffect
	{
		public bool newCloud;
		public float damage;
		public Character attacker = null!;

		public override void SetAttacker(Character attacker)
		{
			base.SetAttacker(attacker);
			this.attacker = attacker;
		}

		public override bool IsDone() => damage <= 0;
	}

	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class AddPoisonCloudStatusEffect
	{
		public static StatusEffect StatusEffect = null!;

		private static void Postfix(ObjectDB __instance)
		{
			StatusEffect = ScriptableObject.CreateInstance<PoisonCloudEffect>();
			StatusEffect.name = "JewelCrafting_PoisonCloudStatusEffect";
			__instance.m_StatusEffects.Add(StatusEffect);
		}
	}

	private static IEnumerator HealingReceived(Player player)
	{
		while (true)
		{
			yield return player.WaitEffect<Config>(Effect.Poisonousdrain, c => c.MinCooldown, c => c.MaxCooldown);
			Config config = player.GetEffect<Config>(Effect.Poisonousdrain);
			if (config.Duration > 0 && !player.IsDead() && !Utils.SkipBossPower())
			{
				player.m_seman.AddStatusEffect(Jewelcrafting.poisonStart);

				yield return new WaitForSeconds(4);

				StatusEffect se = player.m_seman.AddStatusEffect(Jewelcrafting.poisonousDrain);
				se.m_ttl = config.Duration;
				GameObject aoe = Object.Instantiate(Jewelcrafting.poisonousDrainCloud, player.transform);
				aoe.GetComponent<Aoe>().Setup(player, Vector3.zero, 30, new HitData
				{
					m_damage = new HitData.DamageTypes { m_poison = config.PoisonDamage },
					m_statusEffect = AddPoisonCloudStatusEffect.StatusEffect.name
				}, null, null);
				se.m_startEffectInstances = se.m_startEffectInstances.Concat(new[] { aoe }).ToArray();
			}
		}
		// ReSharper disable once IteratorNeverReturns
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Heal))]
	public static class IncreaseHealingReceived
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, ref float hp)
		{
			if (__instance is Player player && player.m_seman.HaveStatusEffect(Jewelcrafting.poisonousDrain.name))
			{
				hp *= 1 + player.GetEffect<Config>(Effect.Poisonousdrain).HealingIncrease / 100f;
			}
		}
	}

	[HarmonyPatch(typeof(SE_Poison), nameof(SE_Poison.AddDamage))]
	private static class PoisonLifeStealTrackDamage
	{
		private static void Prefix(SE_Poison __instance, float damage)
		{
			if (__instance.m_character.m_seman.GetStatusEffect(AddPoisonCloudStatusEffect.StatusEffect.name) is PoisonCloudEffect { newCloud: true } activeCloud)
			{
				activeCloud.damage = damage;
				activeCloud.newCloud = false;
			}
		}
	}

	[HarmonyPatch(typeof(SE_Poison), nameof(SE_Poison.UpdateStatusEffect))]
	private static class PoisonLifeSteal
	{
		private static void Postfix(SE_Poison __instance)
		{
			if (__instance.m_timer == __instance.m_damageInterval && __instance.m_character?.m_seman.GetStatusEffect(AddPoisonCloudStatusEffect.StatusEffect.name) is PoisonCloudEffect activeCloud)
			{
				activeCloud.damage -= __instance.m_damagePerHit;
				if (activeCloud.attacker is Player player)
				{
					player.Heal(__instance.m_damagePerHit * player.GetEffect<Config>(Effect.Poisonousdrain).LifeSteal / 100f);
				}
			}
		}
	}
}
