using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Resilience
{
	static Resilience()
	{
		EffectDef.ConfigTypes.Add(Effect.Resilience, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch]
	private static class ReduceArmorDurabilityLossOnHit
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(Player), nameof(Player.DamageArmorDurability)),
			AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.DrainEquipedItemDurability)),
		};

		private static bool Prefix(Player __instance)
		{
			return Random.value > __instance.GetEffect(Effect.Resilience) / 100f;
		}
	}
}
