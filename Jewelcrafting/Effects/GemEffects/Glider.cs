using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Glider
{
	[HarmonyPatch(typeof(Character), nameof(Character.UpdateGroundContact))]
	public static class DisableFallDamage
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, ref float ___m_maxAirAltitude)
		{
			if (__instance != Player.m_localPlayer || (!__instance.m_groundContact && !__instance.IsSwiming()))
			{
				return;
			}

			GlideInsteadOfFalling.glidingUsed = false;

			if (__instance.m_seman.HaveStatusEffect(Jewelcrafting.gliding.name) || __instance.m_seman.HaveStatusEffect(Jewelcrafting.glidingDark.name))
			{
				___m_maxAirAltitude = Mathf.Min(3.99f + __instance.transform.position.y, ___m_maxAirAltitude);
				__instance.m_seman.RemoveStatusEffect(Jewelcrafting.gliding.name, true);
				__instance.m_seman.RemoveStatusEffect(Jewelcrafting.glidingDark.name, true);
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
	public static class GlideInsteadOfFalling
	{
		public static bool glidingUsed = false;

		[UsedImplicitly]
		private static void Postfix(Player __instance)
		{
			if (!__instance.IsOnGround() && !__instance.IsSwiming() && !Physics.SphereCast(__instance.transform.position + Vector3.up, __instance.GetComponent<CapsuleCollider>().radius, Vector3.down, out RaycastHit _, 4f, Character.m_groundRayMask))
			{
				float effect = __instance.GetEffect(Effect.Glider);
				if (!glidingUsed && effect > 0)
				{
					if (EnvMan.instance.IsNight())
					{
						__instance.m_seman.AddStatusEffect(Jewelcrafting.gliding).m_ttl = effect;
					}
					else
					{
						__instance.m_seman.AddStatusEffect(Jewelcrafting.glidingDark).m_ttl = effect;
					}
					glidingUsed = true;
				}
			}

			if ((__instance.m_seman.HaveStatusEffect(Jewelcrafting.gliding.name) || __instance.m_seman.HaveStatusEffect(Jewelcrafting.glidingDark.name)) && __instance.m_body)
			{
				Vector3 velocity = __instance.m_body.velocity;
				velocity.y = Math.Max(-2, velocity.y);
				__instance.m_body.velocity = velocity;
				__instance.m_maxAirAltitude = __instance.transform.position.y;
			}
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Jump))]
	private static class CancelGliding
	{
		private static void Prefix(Character __instance)
		{
			__instance.m_seman.RemoveStatusEffect(Jewelcrafting.gliding.name, true);
			__instance.m_seman.RemoveStatusEffect(Jewelcrafting.glidingDark.name, true);
		}
	}
}
