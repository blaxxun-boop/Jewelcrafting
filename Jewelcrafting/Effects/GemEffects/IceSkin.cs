using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class IceSkin
{
	private static readonly string[] AppliedEffects = new[]
	{
		"$se_burning_name",
		"$se_poison_name",
		"$se_lightning_name",
		"$se_slimed_name",
		"$se_frost_name",
		"$se_wet_name",
		"$se_tared_name",
	};
	
	static IceSkin()
	{
		EffectDef.ConfigTypes.Add(Effect.Iceskin, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch]
	public static class ReduceDuration
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(StatusEffect), nameof(StatusEffect.ResetTime)),
			AccessTools.DeclaredMethod(typeof(StatusEffect), nameof(StatusEffect.Setup)),
		};

		[UsedImplicitly]
		private static void Postfix(StatusEffect __instance)
		{
			if (__instance.m_character is Player player && player.GetEffect<Config>(Effect.Iceskin).Power is { } iceskin && AppliedEffects.Contains(__instance.m_name))
			{
				__instance.m_time += __instance.m_ttl * iceskin / 100;
			}
		}
	}
}
