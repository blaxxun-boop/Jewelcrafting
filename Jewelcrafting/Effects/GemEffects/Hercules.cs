using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Hercules
{
	static Hercules()
	{
		EffectDef.ConfigTypes.Add(Effect.Hercules, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
	private static class IncreaseCarryWeight
	{
		[UsedImplicitly]
		private static void Postfix(Player __instance, ref float __result)
		{
			__result *= 1 + __instance.GetEffect(Effect.Hercules) / 100f;
		}
	}
}
