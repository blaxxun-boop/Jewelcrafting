using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Energetic
{
	static Energetic()
	{
		EffectDef.ConfigTypes.Add(Effect.Energetic, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}
	
	[HarmonyPatch]
	public static class SetStaminaUsageMarker
	{
		public static bool reduceStamina = false;
		
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(Attack), nameof(Attack.Update)),
			AccessTools.DeclaredMethod(typeof(Attack), nameof(Attack.Start)),
			AccessTools.DeclaredMethod(typeof(Player), nameof(Player.UpdatePlacement)),
			AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Repair))
		};

		private static void Prefix()
		{
			reduceStamina = true;
		}

		private static void Finalizer() => reduceStamina = false;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
	private static class ReduceStaminaUsage
	{
		private static void Prefix(Player __instance, ref float v)
		{
			if (SetStaminaUsageMarker.reduceStamina && __instance is { } player && player.GetEffect(Effect.Energetic) > 0)
			{
				v *= 1 - player.GetEffect(Effect.Energetic) / 100f;
			}
		}
	}
	
	[HarmonyPatch(typeof(Player), nameof(Player.HaveStamina))]
	private static class ReduceStaminaUsageCheck
	{
		private static void Prefix(Player __instance, ref float amount)
		{
			if (SetStaminaUsageMarker.reduceStamina && __instance is { } player && player.GetEffect(Effect.Energetic) > 0)
			{
				amount *= 1 - player.GetEffect(Effect.Energetic) / 100f;
			}
		}
	}
}
