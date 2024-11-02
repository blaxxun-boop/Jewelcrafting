using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Jewelcrafting.GemEffects;

namespace Jewelcrafting.Effects.GemEffects.Groups;

public static class Cowardice
{
	static Cowardice()
	{
		EffectDef.ConfigTypes.Add(Effect.Cowardice, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
		[MaxPower] [OptionalPower(20f)] public float Duration;
		[MaxPower] [OptionalPower(5f)] public float MaxStacks;
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class ReduceDamageTaken
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && hit.GetAttacker() is { } attacker && attacker != __instance && player.GetEffect(Effect.Cowardice) > 0)
			{
				List<Player> nearbyPlayers = Utils.GetNearbyGroupMembers(player, 40);
				if (nearbyPlayers.Count > 0)
				{
					player.m_seman.AddStatusEffect(GemEffectSetup.cowardice, true);
					SE_Stats coward = (SE_Stats)player.m_seman.GetStatusEffect(GemEffectSetup.cowardice.name.GetStableHashCode());
					coward.m_speedModifier += player.GetEffect(Effect.Cowardice) / 100f;
					coward.m_speedModifier = Math.Min(player.GetEffect(Effect.Cowardice) / 100f * (int)player.GetEffect<Config>(Effect.Cowardice).MaxStacks, coward.m_speedModifier);
					coward.m_time = player.GetEffect<Config>(Effect.Cowardice).Duration;
				}
			}
		}
	}
}
