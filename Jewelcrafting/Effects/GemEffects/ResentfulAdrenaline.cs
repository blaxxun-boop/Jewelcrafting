using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class ResentfulAdrenaline
{
	static ResentfulAdrenaline()
	{
		EffectDef.ConfigTypes.Add(Effect.Resentfuladrenaline, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[AdditivePower] public float Power;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.AddAdrenaline))]
	private static class IncreaseAdrenalineGain
	{
		private static void Prefix(Player __instance, ref float v)
		{
			if (v < 0 && __instance.GetEffect(Effect.Resentfuladrenaline) > 0)
			{
				v *= 1 - __instance.GetEffect(Effect.Resentfuladrenaline) / 100f;
			}
		}
	}
}
