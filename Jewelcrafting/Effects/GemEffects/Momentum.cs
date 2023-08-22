using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Momentum
{
	[HarmonyPatch]
	private static class ReplenishStamina
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(Character), nameof(Character.OnDamaged)),
			AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.OnDamaged)),
		};
		
		private static void Postfix(Character __instance, HitData hit)
		{
			if (hit.GetAttacker() is Player player && player.GetEffect(Effect.Momentum) is { } momentum and > 0 && __instance.GetHealth() <= 0)
			{
				player.m_nview.InvokeRPC("Jewelcrafting Replenish Stamina", momentum);
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
	private static class AddRPCs
	{
		private static void Postfix(Player __instance)
		{
			__instance.m_nview.Register<float>("Jewelcrafting Replenish Stamina", (_, stamina) => __instance.AddStamina(stamina));
		}
	}
}
