using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class IceHeart
{
	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private class AddBonusFrostDamage
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player attacker)
			{
				hit.m_damage.m_frost += hit.GetTotalDamage() * attacker.GetEffect(Effect.Iceheart) / 100f;
			}
		}
	}
}
