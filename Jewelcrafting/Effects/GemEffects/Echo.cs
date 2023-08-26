using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Echo
{
	static Echo()
	{
		EffectDef.ConfigTypes.Add(Effect.Echo, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[AdditivePower] public float Power;
		[MaxPower] [OptionalPower(1f)] public float BonusProjectiles;
	}
	
	[HarmonyPatch(typeof(Attack), nameof(Attack.ProjectileAttackTriggered))]
	public class SendSecondProjectile
	{
		private static void Postfix(Attack __instance)
		{
			if (__instance.m_character is Player player && player.GetEffect(Effect.Echo) / 100f is { } echoChance && Random.value < echoChance * (1 + player.GetEffect(Effect.Resonatingechoes) / 100f))
			{
				__instance.m_projectileAttackStarted = true;
				__instance.m_projectileBursts *= 1 + (int)player.GetEffect<Config>(Effect.Echo).BonusProjectiles;
				if (__instance.m_projectileBursts >= 2)
				{
					__instance.m_projectileBurstsFired = 1;
					__instance.m_projectileFireTimer = 0.1f;
				}
			}
		}
	}
}
