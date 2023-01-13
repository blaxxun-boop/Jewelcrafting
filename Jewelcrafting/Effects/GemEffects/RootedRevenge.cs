using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class RootedRevenge
{
	static RootedRevenge()
	{
		EffectDef.ConfigTypes.Add(Effect.Rootedrevenge, typeof(Config));
		ForcePet.RegisterPet("TentaRoot");
	}
	
	[StructLayout(LayoutKind.Sequential)]
	private struct Config
	{
		[MultiplicativePercentagePower] public readonly float BonusDamage;
		[MinPower] public readonly float MinCooldown;
		[MinPower] public readonly float MaxCooldown;
		[AdditivePower] public readonly float Duration;
		[MaxPower] public readonly float Range;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
	private class StartCoroutineForEffect
	{
		private static void Postfix(Player __instance)
		{
			__instance.StartCoroutine(IncreaseDamage(__instance));
		}
	}

	private class RootedEffect : StatusEffect
	{
		// ReSharper disable once RedundantAssignment
		public override void ModifySpeed(float baseSpeed, ref float speed)
		{
			speed = 0;
		}

		public override void OnDamaged(HitData hit, Character attacker)
		{
			base.OnDamaged(hit, attacker);
			if (attacker is Player player && player.m_seman.HaveStatusEffect(GemEffectSetup.rootedRevenge.name))
			{
				hit.m_damage.Modify(1 + player.GetEffect<Config>(Effect.Rootedrevenge).BonusDamage / 100f);
			}
		}
	}
	
	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class AddRootedStatusEffect
	{
		public static StatusEffect StatusEffect = null!;
		
		private static void Postfix(ObjectDB __instance)
		{
			StatusEffect = ScriptableObject.CreateInstance<RootedEffect>();
			StatusEffect.name = "JewelCrafting_RootedStatusEffect";
			StatusEffect.m_ttl = 8f; // overwritten below
			__instance.m_StatusEffects.Add(StatusEffect);
		}
	}
	
	private static IEnumerator IncreaseDamage(Player player)
	{
		while (true)
		{
			yield return player.WaitEffect<Config>(Effect.Rootedrevenge, c => c.MinCooldown, c => c.MaxCooldown);
			Config config = player.GetEffect<Config>(Effect.Rootedrevenge);
			if (config.Duration > 0 && !player.IsDead() && !Utils.SkipBossPower())
			{
				player.m_seman.AddStatusEffect(GemEffectSetup.rootStart);

				yield return new WaitForSeconds(4);
				
				player.m_seman.AddStatusEffect(GemEffectSetup.rootedRevenge).m_ttl = config.Duration;

				GameObject root = ZNetScene.instance.GetPrefab("TentaRoot");

				foreach (Character character in Character.m_characters.Where(c => Vector3.Distance(c.transform.position, player.transform.position) <= config.Range && (BaseAI.IsEnemy(c, player) || (c is Player enemy && enemy.IsPVPEnabled()))).ToList())
				{
					AddRootedStatusEffect.StatusEffect.m_ttl = config.Duration;
					character.m_seman.AddStatusEffect(AddRootedStatusEffect.StatusEffect, true);
					Character pet = Object.Instantiate(root, character.transform.position, Quaternion.identity).GetComponent<Character>();
					pet.GetComponent<ForcePet>().MakePet(config.Duration);
					pet.GetComponent<MonsterAI>().SetAlerted(true);
				}
			}
		}
		// ReSharper disable once IteratorNeverReturns
	}
}
