using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Lumberjacking
{
	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private static class ReduceDamgeDone
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player player && player.m_visEquipment.m_currentUtilityItemHash == JewelrySetup.yellowNecklaceHash && hit.m_skill == Skills.SkillType.Axes)
			{
				hit.ApplyModifier(0.5f);
			}
		}
	}
}
