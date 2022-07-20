using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class ApplySkillIncreases
{
	public static readonly Dictionary<Skills.SkillType, Effect> Effects = new();

	[HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillLevel))]
	private static class IncreaseSkillLevel
	{
		private static void Postfix(Skills __instance, Skills.SkillType skillType, ref float __result)
		{
			if (Effects.TryGetValue(skillType, out Effect effect))
			{
				__result += __instance.m_player.GetEffect(effect);
			}
		}
	}
}
