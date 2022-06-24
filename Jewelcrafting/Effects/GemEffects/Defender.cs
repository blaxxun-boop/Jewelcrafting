using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Defender
{
	[HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
	private class IncreaseArmor
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += __instance.GetEffect(Effect.Defender);
		}
	}
}
