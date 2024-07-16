using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Windwalk
{
	static Windwalk()
	{
		EffectDef.ConfigTypes.Add(Effect.Windwalk, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch]
	private class IncreaseSpeed
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(Player), nameof(Player.GetJogSpeedFactor)),
			AccessTools.DeclaredMethod(typeof(Player), nameof(Player.GetRunSpeedFactor)),
		};
		
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += __instance.GetEffect(Effect.Windwalk) * (Jewelcrafting.asksvinRunning.Value == Jewelcrafting.Toggle.Off && __instance.m_shoulderItem?.m_shared.m_name == "$item_cape_asksvin" ? 1 + Jewelcrafting.asksvinRunningBuff.Value / 100f : 1) / 100f * Mathf.Max(0, Vector3.Dot(Vector3.Normalize(__instance.m_moveDir), EnvMan.instance.GetWindDir()));
		}
	}
}
