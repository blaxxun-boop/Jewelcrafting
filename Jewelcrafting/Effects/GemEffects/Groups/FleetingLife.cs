using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Jewelcrafting.GemEffects;
using UnityEngine;

namespace Jewelcrafting.Effects.GemEffects.Groups;

public static class FleetingLife
{
	static FleetingLife()
	{
		EffectDef.ConfigTypes.Add(Effect.Fleetinglife, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private static class AddLifeSteal
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player attacker && Random.value < attacker.GetEffect(Effect.Fleetinglife) / 100f)
			{
				if (Utils.GetNearbyGroupMembers(attacker, 40).OrderBy(p => p.GetHealth() / p.GetMaxHealth()).FirstOrDefault() is { } target)
				{
					target.Heal(Random.value * 3 * (3 - Vector3.Distance(attacker.transform.position, target.transform.position) / 15));
				}
			}
		}
	}
}
