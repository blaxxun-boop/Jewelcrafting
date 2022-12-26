using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class SafeHaven
{
	static SafeHaven()
	{
		EffectDef.ConfigTypes.Add(Effect.Safehaven, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class ReduceDamageTaken
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && hit.GetAttacker() is { } attacker && attacker != __instance)
			{
				foreach (Player groupMember in Utils.GetNearbyGroupMembers(player, 40))
				{
					hit.ApplyModifier(1 - groupMember.GetEffect(Effect.Safehaven) / 100f);
				}
			}
		}
	}
}
