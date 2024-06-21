using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Apotheosis
{
	static Apotheosis()
	{
		EffectDef.ConfigTypes.Add(Effect.Apotheosis, typeof(Config));
		ApplyAttackSpeed.Modifiers.Add(player => player.m_seman.HaveStatusEffect(GemEffectSetup.apotheosis.name.GetStableHashCode()) ? player.GetEffect<Config>(Effect.Apotheosis).AttackSpeed / 100f : 0);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Config
	{
		[AdditivePower] public readonly float EitrReduction;
		[MinPower] public readonly float MinCooldown;
		[MinPower] public readonly float MaxCooldown;
		[AdditivePower] public readonly float Duration;
		[MultiplicativePercentagePower] public readonly float MagicDamageIncrease;
		[MultiplicativePercentagePower] public readonly float AttackSpeed;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
	private class StartCoroutineForEffect
	{
		private static void Postfix(Player __instance)
		{
			__instance.StartCoroutine(BurstOfPower(__instance));
		}
	}

	private static IEnumerator BurstOfPower(Player player)
	{
		while (true)
		{
			yield return player.WaitEffect<Config>(Effect.Apotheosis, c => c.MinCooldown, c => c.MaxCooldown);
			Config config = player.GetEffect<Config>(Effect.Apotheosis);
			if (config.Duration > 0 && !player.IsDead() && !Utils.SkipBossPower())
			{
				player.m_seman.AddStatusEffect(GemEffectSetup.apotheosisStart);

				yield return new WaitForSeconds(4);

				GemEffectSetup.apotheosis.m_ttl = config.Duration;
				if (player.m_seman.AddStatusEffect(GemEffectSetup.apotheosis) is SE_Stats statusEffect)
				{
					statusEffect.m_damageModifier = 1 + config.MagicDamageIncrease / 100f;
					statusEffect.m_modifyAttackSkill = Skills.SkillType.ElementalMagic;

					player.m_eitr = player.m_maxEitr;
				}
			}
		}
		// ReSharper disable once IteratorNeverReturns
	}

	[HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr))]
	private class ReduceEitrUsage
	{
		private static void Postfix(Attack __instance, ref float __result)
		{
			if (__instance.m_character is Player player && player.m_seman.HaveStatusEffect(GemEffectSetup.apotheosis.name.GetStableHashCode()))
			{
				__result *= 1 - player.GetEffect<Config>(Effect.Apotheosis).EitrReduction / 100f;
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.QueueReloadAction))]
	private class ReduceEitrUsageDundr
	{
		private static void Prefix(Player __instance, out int __state) => __state = __instance.m_actionQueue.Count;

		private static void Postfix(Player __instance, int __state)
		{
			if (__state < __instance.m_actionQueue.Count && __instance.m_seman.HaveStatusEffect(GemEffectSetup.apotheosis.name.GetStableHashCode()))
			{
				__instance.m_actionQueue.Last().m_eitrDrain *= 1 - __instance.GetEffect<Config>(Effect.Apotheosis).EitrReduction / 100f;
			}
		}
	}

	public class ApotheosisEffect : SE_Stats
	{
		public override string GetTooltipString()
		{
			Config config = Player.m_localPlayer.GetEffect<Config>(Effect.Apotheosis);
			return Localization.instance.Localize(m_tooltip, config.EitrReduction.ToString("0.#"), config.MinCooldown.ToString("0.#"), config.MaxCooldown.ToString("0.#"), config.Duration.ToString("0.#"), config.MagicDamageIncrease.ToString("0.#"), config.AttackSpeed.ToString("0.#"));
		}
	}
}
