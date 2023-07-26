using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Jewelcrafting.GemEffects;
using UnityEngine;

namespace Jewelcrafting.Effects.GemEffects.Groups;

public static class ArbalistMentor
{
	static ArbalistMentor()
	{
		EffectDef.ConfigTypes.Add(Effect.Arbalistmentor, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillLevel))]
	private static class IncreaseSkillLevel
	{
		private static void Postfix(Skills __instance, Skills.SkillType skillType, ref float __result)
		{
			if (skillType == Skills.SkillType.Crossbows)
			{
				float level = __result;
				__result = Mathf.Max(level, Utils.GetNearbyGroupMembers(__instance.m_player, 40).Select(p => Mathf.Max(level + (p.m_nview.GetZDO().GetInt("Jewelcrafting Crossbow Skill") - level) * p.GetEffect(Effect.Arbalistmentor) / 100f)).DefaultIfEmpty(0).Max());
			}
		}
	}

	[HarmonyPatch]
	private static class StoreArchery
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(Player), nameof(Player.OnSkillLevelup)),
			AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.ResetSkill)),
			AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.CheatRaiseSkill)),
			AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Load)),
		};

		private static void Postfix()
		{
			Player.m_localPlayer?.m_nview?.GetZDO()?.Set("Jewelcrafting Crossbow Skill", (int)Player.m_localPlayer.GetSkills().GetSkill(Skills.SkillType.Crossbows).m_level);
		}
	}
}
