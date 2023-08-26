using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Glider
{
	static Glider()
	{
		EffectDef.ConfigTypes.Add(Effect.Glider, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[AdditivePower] public float Power;
		[MinPower] [OptionalPower(3.5f)] public float RequiredHeight;
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.UpdateGroundContact))]
	public static class DisableFallDamage
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, ref float ___m_maxAirAltitude)
		{
			if (__instance != Player.m_localPlayer || (!__instance.m_groundContact && !__instance.IsSwimming()))
			{
				return;
			}

			GlideInsteadOfFalling.glidingUsed = false;

			if (__instance.m_seman.HaveStatusEffect(GemEffectSetup.gliding.name) || __instance.m_seman.HaveStatusEffect(GemEffectSetup.glidingDark.name))
			{
				___m_maxAirAltitude = Mathf.Min(3.49f + __instance.transform.position.y, ___m_maxAirAltitude);
				__instance.m_seman.RemoveStatusEffect(GemEffectSetup.gliding.NameHash(), true);
				__instance.m_seman.RemoveStatusEffect(GemEffectSetup.glidingDark.NameHash(), true);
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
	public static class GlideInsteadOfFalling
	{
		public static bool glidingUsed = false;
		public static bool gliding = false;

		[UsedImplicitly]
		private static void Postfix(Player __instance)
		{
			if (!__instance.IsOnGround() && !__instance.IsSwimming() && !Physics.SphereCast(__instance.transform.position + Vector3.up, __instance.GetComponent<CapsuleCollider>().radius, Vector3.down, out RaycastHit _, __instance.GetEffect<Config>(Effect.Glider).RequiredHeight, Character.s_groundRayMask))
			{
				float effect = __instance.GetEffect(Effect.Glider) * (Jewelcrafting.featherGliding.Value == Jewelcrafting.Toggle.Off && __instance.m_shoulderItem?.m_shared.m_name == "$item_cape_feather" ? 1 + Jewelcrafting.featherGlidingBuff.Value / 100f : 1);
				if (!glidingUsed && effect > 0)
				{
					if (EnvMan.instance.IsNight())
					{
						__instance.m_seman.AddStatusEffect(GemEffectSetup.gliding).m_ttl = effect;
					}
					else
					{
						__instance.m_seman.AddStatusEffect(GemEffectSetup.glidingDark).m_ttl = effect;
					}
					glidingUsed = true;
					gliding = true;
				}
			}

			if ((__instance.m_seman.HaveStatusEffect(GemEffectSetup.gliding.name) || __instance.m_seman.HaveStatusEffect(GemEffectSetup.glidingDark.name)) && __instance.m_body && gliding)
			{
				Vector3 velocity = __instance.m_body.velocity;
				velocity.y = Math.Max(-2, velocity.y);
				__instance.m_body.velocity = velocity;
				__instance.m_maxAirAltitude = __instance.transform.position.y;
			}
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Jump))]
	private static class ToggleGliding
	{
		private static void Prefix()
		{
			GlideInsteadOfFalling.gliding = !GlideInsteadOfFalling.gliding;
		}
	}
}
