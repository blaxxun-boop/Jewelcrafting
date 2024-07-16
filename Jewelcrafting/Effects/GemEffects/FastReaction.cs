using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class FastReaction
{
	static FastReaction()
	{
		EffectDef.ConfigTypes.Add(Effect.Fastreaction, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MaxPower] public float ParryFrameDecrease;
		[MultiplicativePercentagePower] public float ParryPowerIncrease;
	}
	
	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
	public static class DecreaseParryFrame
	{
		[UsedImplicitly]
		private static void Prefix(Humanoid __instance, ref float ___m_blockTimer, ref KeyValuePair<Config, ItemDrop.ItemData.SharedData?> __state)
		{
			if (__instance is Player player && ___m_blockTimer > -1)
			{
				Config config = player.GetEffect<Config>(Effect.Fastreaction);
				___m_blockTimer += config.ParryFrameDecrease / 1000;

				ItemDrop.ItemData.SharedData? blocker = player.GetCurrentBlocker()?.m_shared;
				if (blocker is not null)
				{
					blocker.m_timedBlockBonus += config.ParryPowerIncrease / 100;
				}

				__state = new KeyValuePair<Config, ItemDrop.ItemData.SharedData?>(config, blocker);
			}
		}

		[UsedImplicitly]
		private static void Postfix(Humanoid __instance, ref float ___m_blockTimer, KeyValuePair<Config, ItemDrop.ItemData.SharedData?> __state)
		{
			if (__instance is Player && ___m_blockTimer > -1)
			{
				___m_blockTimer -= __state.Key.ParryFrameDecrease / 1000;
				if (__state.Value is { } blocker)
				{
					blocker.m_timedBlockBonus -= __state.Key.ParryPowerIncrease / 100;
				}
			}
		}
	}
}
