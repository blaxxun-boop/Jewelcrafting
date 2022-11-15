using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class FireStarter
{
	static FireStarter()
	{
		EffectDef.ConfigTypes.Add(Effect.Firestarter, typeof(Config));
	}
	
	[PublicAPI]
	public struct Config
	{
		[AdditivePower] public float Power;
		[MaxPower] [OptionalPower(20f)] public float Chance;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private class AddBonusFireDamage
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player attacker)
			{
				Config config = attacker.GetEffect<Config>(Effect.Firestarter);
				if (Random.value <= config.Chance / 100f * (1 + attacker.GetEffect(Effect.Pyromaniac) / 100))
				{
					hit.m_damage.m_fire += hit.GetTotalDamage() * config.Power / 100f;
				}
			}
		}
	}
}
