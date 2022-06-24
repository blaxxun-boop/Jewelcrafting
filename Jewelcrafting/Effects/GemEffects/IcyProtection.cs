using System.Collections;
using System.Runtime.InteropServices;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jewelcrafting.GemEffects;

public static class IcyProtection
{
	static IcyProtection()
	{
		EffectDef.ConfigTypes.Add(Effect.Icyprotection, typeof(Config));
		ForcePet.RegisterPet("Hatchling");
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public readonly float DamageReduction;
		[MinPower] public readonly float MinCooldown;
		[MinPower] public readonly float MaxCooldown;
		[AdditivePower] public readonly float Duration;
		[MaxPower] public readonly float Drakes;
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
			yield return player.WaitEffect<Config>(Effect.Icyprotection, c => c.MinCooldown, c => c.MaxCooldown);
			Config config = player.GetEffect<Config>(Effect.Icyprotection);
			if (config.Duration > 0)
			{
				player.m_seman.AddStatusEffect(Jewelcrafting.icyProtection);

				if (player.transform.position.y > 4500 || player.m_underRoof)
				{
					continue;
				}
				
				Transform transform = player.transform;
				for (int i = 0; i < config.Drakes; ++i)
				{
					GameObject pet = Object.Instantiate(ZNetScene.instance.GetPrefab("Hatchling"), transform.position + Vector3.up * ((i + 1) * 2.5f), transform.rotation);
					pet.GetComponent<ForcePet>().MakePet(config.Duration);
					pet.GetComponent<MonsterAI>().SetAlerted(true);
				}
			}
		}
		// ReSharper disable once IteratorNeverReturns
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class ReduceDamageTaken
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && hit.GetAttacker() is { } attacker && attacker != __instance && player.m_seman.HaveStatusEffect(Jewelcrafting.icyProtection.name))
			{
				hit.ApplyModifier(1 - player.GetEffect<Config>(Effect.Icyprotection).DamageReduction / 100f);
			}
		}
	}
}
