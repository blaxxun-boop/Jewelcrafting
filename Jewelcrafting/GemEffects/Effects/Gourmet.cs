using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Gourmet
{
	[HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
	private static class ReduceFoodDrain
	{
		private static void Prefix(Player __instance, float dt)
		{
			__instance.m_foodUpdateTimer -= dt * __instance.GetEffect(Effect.Gourmet) / 100f;
		}
	}
}
