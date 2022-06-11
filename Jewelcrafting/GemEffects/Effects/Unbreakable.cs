using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Unbreakable
{
	[HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
	private static class ReduceToolDurabilityLoss
	{
		private static void Prefix(Attack __instance)
		{
			if (__instance.m_character is Player player)
			{
				__instance.m_weapon.m_shared.m_durabilityDrain *= 1 - player.GetEffect(Effect.Unbreakable) / 100f;
			}
		}

		private static void Finalizer(Attack __instance)
		{
			if (__instance.m_character is Player player)
			{
				__instance.m_weapon.m_shared.m_durabilityDrain /= 1 - player.GetEffect(Effect.Unbreakable) / 100f;
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
	private static class ReduceHammerDurabilityLoss
	{
		private static void Prefix(Player __instance)
		{
			if (__instance.GetRightItem() is { } hammer)
			{
				hammer.m_shared.m_durabilityDrain *= 1 - __instance.GetEffect(Effect.Unbreakable) / 100f;
			}
		}

		private static void Finalizer(Player __instance)
		{
			if (__instance.GetRightItem() is { } hammer)
			{
				hammer.m_shared.m_durabilityDrain /= 1 - __instance.GetEffect(Effect.Unbreakable) / 100f;
			}
		}
	}
}
