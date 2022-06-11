using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Unfazed
{
	[HarmonyPatch(typeof(Character), nameof(Character.GetStaggerTreshold))]
	private static class IncreaseStaggerThreshold
	{
		private static void Postfix(Character __instance, ref float __result)
		{
			if (__instance == Player.m_localPlayer)
			{
				__result *= 1 + ((Player)__instance).GetEffect(Effect.Unfazed) / 100f;
			}
		}
	}
}
