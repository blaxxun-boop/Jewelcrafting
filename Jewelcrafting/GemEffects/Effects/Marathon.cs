using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Marathon
{
	[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyRunStaminaDrain))]
	private static class ReduceStaminaUsage
	{
		private static void Prefix(SEMan __instance, ref float drain)
		{
			if (__instance.m_character is Player player)
			{
				drain *= 1 - player.GetEffect(Effect.Marathon) / 100f;
			}
		}
	}
}
