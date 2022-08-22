using System.Collections.Generic;
using HarmonyLib;

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
				__result += __instance.m_player.GetEffect(effect) * (1 + __instance.m_player.GetEffect(Effect.Eternalstudent) / 100f);
			}
		}
	}
}
