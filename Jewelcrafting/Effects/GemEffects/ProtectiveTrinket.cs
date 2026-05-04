using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class ProtectiveTrinket
{
	static ProtectiveTrinket()
	{
		EffectDef.ConfigTypes.Add(Effect.Protectivetrinket, typeof(Config));
	}

	[PublicAPI]
	public struct Config
	{
		[MultiplicativePercentagePower] public float Power;
		[MaxPower] [OptionalPower(3f)] public float Duration;
	}

	[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyAdrenaline))]
	public static class ApplyStatusEffect
	{
		[UsedImplicitly]
		private static void Finalizer(SEMan __instance, float use)
		{
			if (__instance.m_character is Player player && player.m_adrenaline + use >= player.GetMaxAdrenaline() && player.GetMaxAdrenaline() > 0)
			{
				player.m_seman.AddStatusEffect(GemEffectSetup.protectedStatus).m_ttl = player.GetEffect<Config>(Effect.Protectivetrinket).Duration;
			}
		}
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class ReduceDamageTaken
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && hit.GetAttacker() is { } attacker && attacker != __instance && player.m_seman.HaveStatusEffect(GemEffectSetup.protectedStatus.name.GetStableHashCode()))
			{
				hit.ApplyModifier(1 - player.GetEffect<Config>(Effect.Protectivetrinket).Power / 100f);
			}
		}
	}
}
