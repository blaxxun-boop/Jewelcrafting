using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Gourmet
{
	static Gourmet()
	{
		EffectDef.ConfigTypes.Add(Effect.Gourmet, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
	private static class ReduceFoodDrain
	{
		private static void Prefix(Player __instance, float dt)
		{
			__instance.m_foodUpdateTimer -= dt * __instance.GetEffect(Effect.Gourmet) / 100f;
		}
	}
}
