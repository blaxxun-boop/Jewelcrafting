using HarmonyLib;

namespace Jewelcrafting.GemEffects;

[HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
public class Student
{
	private static void Prefix(ref float factor)
	{
		factor *= 1 + Player.m_localPlayer.GetEffect(Effect.Student) / 100f;
	}
}