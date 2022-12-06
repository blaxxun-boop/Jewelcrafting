using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class EitrSurge
{
	static EitrSurge()
	{
		EffectDef.ConfigTypes.Add(Effect.Eitrsurge, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyEitrRegen))]
	private static class IncreaseEitrRegen
	{
		private static void Prefix(SEMan __instance, ref float eitrMultiplier)
		{
			if (__instance.m_character is Player player)
			{
				eitrMultiplier *= 1 + player.GetEffect(Effect.Eitrsurge) / 100f;
			}
		}
	}
}
