using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Comfortable
{
	[HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.CalculateComfortLevel))]
	public static class IncreaseComfortLevel
	{
		[UsedImplicitly]
		private static void Postfix(Player player, ref int __result)
		{
			__result += (int)player.GetEffect(Effect.Comfortable);
		}
	}
}
