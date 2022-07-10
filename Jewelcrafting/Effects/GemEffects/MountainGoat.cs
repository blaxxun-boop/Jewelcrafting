using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class MountainGoat
{
	static MountainGoat()
	{
		EffectDef.ConfigTypes.Add(Effect.Mountaingoat, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.GetSlideAngle))]
	private static class IncreaseSlideAngle
	{
		private static void Postfix(Character __instance, ref float __result)
		{
			if (__instance is Player player && player.GetEffect(Effect.Mountaingoat) > 0)
			{
				__result *= 1 + player.GetEffect(Effect.Mountaingoat) / 100f;
			}
		}
	}
}
