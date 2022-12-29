using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Nimble
{
	[HarmonyPatch(typeof(Player), nameof(Player.UpdateDodge))]
	private static class ReduceStaminaUsage
	{
		[UsedImplicitly]
		private static void Prefix(Player __instance)
		{
			__instance.m_dodgeStaminaUsage -= Mathf.Max(__instance.GetEffect(Effect.Nimble), 1);
		}

		[UsedImplicitly]
		private static void Postfix(Player __instance)
		{
			__instance.m_dodgeStaminaUsage += Mathf.Max(__instance.GetEffect(Effect.Nimble), 1);
		}
	}
}
