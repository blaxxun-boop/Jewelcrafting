using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class Raging
{
	static Raging()
	{
		EffectDef.ConfigTypes.Add(Effect.Raging, typeof(Config));
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
			if (Random.value < __instance.GetEffect(Effect.Raging) / 100)
			{
				v *= 2;
			}
		}
	}
}
