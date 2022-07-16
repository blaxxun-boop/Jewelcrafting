using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Echo
{
	[HarmonyPatch(typeof(Attack), nameof(Attack.ProjectileAttackTriggered))]
	public class SendSecondProjectile
	{
		private static void Postfix(Attack __instance)
		{
			if (__instance.m_character is Player player && Random.value < player.GetEffect(Effect.Echo) / 100f)
			{
				__instance.m_projectileAttackStarted = true;
				__instance.m_projectileBursts *= 2;
				if (__instance.m_projectileBursts == 2)
				{
					__instance.m_projectileBurstsFired = 1;
					__instance.m_projectileFireTimer = 0.1f;
				}
			}
		}
	}
}
