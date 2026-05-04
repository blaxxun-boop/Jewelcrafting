using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class LuckyLumberjack
{
	static LuckyLumberjack()
	{
		EffectDef.ConfigTypes.Add(Effect.Luckylumberjack, typeof(Config));
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
		private static void Postfix(ref List<GameObject> __result)
		{
			if (SetLumberjackingFlagTreeLog.IsLumberjacking is not { } player)
			{
				return;
			}

			Config config = player.GetEffect<Config>(Effect.Luckylumberjack);
			if (Random.value <= config.Power / 100f)
			{
				__result.AddRange(__result.ToArray());
			}
		}
	}

	[HarmonyPatch(typeof(TreeLog), nameof(TreeLog.RPC_Damage))]
	public static class SetLumberjackingFlagTreeLog
	{
		public static Player? IsLumberjacking;

		private static void Prefix(HitData hit) => IsLumberjacking = hit.GetAttacker() as Player;
		private static void Finalizer() => IsLumberjacking = null;
	}

	[HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
	public static class SetLumberjackingFlagTreeBase
	{
		private static void Prefix(HitData hit) => SetLumberjackingFlagTreeLog.IsLumberjacking = hit.GetAttacker() as Player;
		private static void Finalizer() => SetLumberjackingFlagTreeLog.IsLumberjacking = null;
	}
}
