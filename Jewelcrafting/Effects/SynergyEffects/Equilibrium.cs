using HarmonyLib;
using JetBrains.Annotations;
using Jewelcrafting.GemEffects;

namespace Jewelcrafting.SynergyEffects;

public static class Equilibrium
{
	static Equilibrium()
	{
		EffectDef.ConfigTypes.Add(Effect.Equilibrium, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class ModifyDamage
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && hit.GetAttacker() is { } attacker && attacker != __instance)
			{
				hit.ApplyModifier(1 - player.GetEffect(Effect.Equilibrium) / 100f);
			}
			
			if (hit.GetAttacker() is Player attackingPlayer && attackingPlayer.GetEffect(Effect.Equilibrium) is { } power and > 0)
			{
				hit.ApplyModifier(1 + power / 100f);
			}
		}
	}
}
