using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class VampiricParry
{
	static VampiricParry()
	{
		EffectDef.ConfigTypes.Add(Effect.Vampiricparry, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[AdditivePower] public float Power;
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
	private static class HealOnParry
	{
		[HarmonyPriority(Priority.VeryHigh)]
		private static void Postfix(Humanoid __instance, ref bool __result)
		{
			if (__instance is Player player && player.GetEffect(Effect.Vampiricparry) > 0 && __result && player.GetCurrentBlocker() is { } blocker && blocker.m_shared.m_timedBlockBonus > 1.0 && player.m_blockTimer != -1.0 && player.m_blockTimer < Humanoid.m_perfectBlockInterval)
			{
				player.Heal(player.GetMaxHealth() * (player.GetEffect(Effect.Vampiricparry) / 100f));
			}
		}
	}
}
