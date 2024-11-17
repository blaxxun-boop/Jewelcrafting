using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class AirDried
{
	static AirDried()
	{
		EffectDef.ConfigTypes.Add(Effect.Airdried, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[AdditivePower] public float Power;
	}

	[HarmonyPatch(typeof(SE_Wet), nameof(SE_Wet.Setup))]
	private static class ReduceWetDuration
	{
		private static void Postfix(SE_Wet __instance, Character character)
		{
			if (character is Player player)
			{
				__instance.m_ttl -= player.GetEffect(Effect.Airdried);
			}
		}
	}
}
