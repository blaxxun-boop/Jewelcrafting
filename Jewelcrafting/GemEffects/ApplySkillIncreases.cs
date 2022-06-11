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

	[HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
	public static class IncreaseSkillLevel
	{
        [UsedImplicitly]
        private static void Postfix(Skills __instance, Skills.SkillType skillType, ref float __result)
        {
            __result += SkillIncrease(__instance.m_player, skillType) / 100f;
        }

		public static int SkillIncrease(Player player, Skills.SkillType skillType)
		{
			if (Effects.TryGetValue(skillType, out Effect effect))
			{
				return (int)player.GetEffect(effect);
			}

			return 0;
		}
	}
	
	[HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillRange))]
    public static class AdjustRandomSkillRange
    {
	    [UsedImplicitly]
        public static bool Prefix(Skills __instance, out float min, out float max, Skills.SkillType skillType)
        {
            float skillValue = Mathf.Lerp(0.4f, 1.0f, __instance.GetSkillFactor(skillType));
            min = Mathf.Max(0, skillValue - 0.15f);
            max = skillValue + 0.15f;
            
            return false;
        }
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillFactor))]
    public static class AdjustRandomSkillFactor
    {
	    [UsedImplicitly]
        public static bool Prefix(Skills __instance, out float __result, Skills.SkillType skillType)
        {
            __instance.GetRandomSkillRange(out float low, out float high, skillType);
            __result = Mathf.Lerp(low, high, Random.value);
            
            return false;
        }
    }

	[HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
	public static class DisplayAdditionalSkillLevel
	{
		[UsedImplicitly]
		private static void Postfix(SkillsDialog __instance, Player player)
		{
			List<Skills.Skill>? allSkills = player.m_skills.GetSkillList();
			foreach (GameObject? element in __instance.m_elements)
			{
				Skills.Skill? skill = allSkills.Find(s => s.m_info.m_description == element.GetComponentInChildren<UITooltip>().m_text);
				int extraSkill = IncreaseSkillLevel.SkillIncrease(player, skill.m_info.m_skill);
				if (extraSkill > 0)
				{
					Transform levelbar = global::Utils.FindChild(element.transform, "bar");
					GameObject extraLevelbar = Object.Instantiate(levelbar.gameObject, levelbar.parent);
					RectTransform rect = extraLevelbar.GetComponent<RectTransform>();
					rect.sizeDelta = new Vector2((skill.m_level + extraSkill) * 1.6f, rect.sizeDelta.y);
                    extraLevelbar.GetComponent<Image>().color = Color.magenta;
					extraLevelbar.transform.SetSiblingIndex(levelbar.GetSiblingIndex());
					Transform levelText = global::Utils.FindChild(element.transform, "leveltext");
					levelText.GetComponent<Text>().text += $" <color={Color.magenta}>+{extraSkill}</color>";
				}
			}
		}
	}
}
