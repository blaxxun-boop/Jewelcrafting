using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class LuckyMiner
{
	static LuckyMiner()
	{
		EffectDef.ConfigTypes.Add(Effect.Luckyminer, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[AdditivePower] public float Power;
	}

	[HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList), typeof(int))]
	private class IncreaseItemYield
	{
		[UsedImplicitly]
		[HarmonyPriority(Priority.VeryLow)]
		private static void Postfix(List<GameObject> __result)
		{
			if (SetMiningFlag.IsMining is not { } player)
			{
				return;
			}

			Config config = player.GetEffect<Config>(Effect.Luckyminer);
			if (Random.value <= config.Power / 100f)
			{
				__result.AddRange(__result.ToArray());
			}
		}
	}

	[HarmonyPatch(typeof(MineRock5), nameof(MineRock5.RPC_Damage))]
	public static class SetMiningFlag
	{
		public static Player? IsMining;

		private static void Prefix(HitData hit)
		{
			IsMining = hit.GetAttacker() as Player;
		}

		private static void Finalizer() => IsMining = null;
	}

	[HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
	public static class SetMiningFlagDestructible
	{
		private static void Prefix(Destructible __instance, HitData hit)
		{
			if (!SetMiningFlag.IsMining && __instance.m_damages.m_pickaxe != HitData.DamageModifier.Immune && __instance.m_damages.m_chop == HitData.DamageModifier.Immune && __instance.m_destructibleType != DestructibleType.Tree && hit.GetAttacker() is Player player)
			{
				SetMiningFlag.IsMining = player;
			}
		}

		private static void Finalizer() => SetMiningFlag.IsMining = null;
	}
}
