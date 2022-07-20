using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Opportunity
{
	static Opportunity()
	{
		EffectDef.ConfigTypes.Add(Effect.Opportunity, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	private static class IncreaseDamageDealt
	{
		private static void Prefix(Character __instance, HitData hit)
		{
			if (hit.GetAttacker() is Player player && player.GetEffect(Effect.Opportunity) is { } power and > 0)
			{
				hit.ApplyModifier(1 + power / 100f * (1 - __instance.GetHealth() / __instance.GetMaxHealth()));
			}
		}
	}
}
