using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
public class Student
{
	static Student()
	{
		EffectDef.ConfigTypes.Add(Effect.Student, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	private static void Prefix(ref float factor)
	{
		factor *= 1 + Player.m_localPlayer.GetEffect(Effect.Student) / 100f;
	}
}