using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Vitality
{
	[HarmonyPatch(typeof(Player), nameof(Player.GetBaseFoodHP))]
	private class IncreaseBaseHealth
	{
		[UsedImplicitly]
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += __instance.GetEffect(Effect.Vitality);
		}
	}
	
	[HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
	private class IncreaseFoodHealth
	{
		[UsedImplicitly]
		private static void Postfix(Player __instance, ref float hp)
		{
			hp += __instance.GetEffect(Effect.Vitality);
		}
	}
}
