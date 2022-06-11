using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class ParryMaster
{
	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
	public static class IncreaseParryFrame
	{
		[UsedImplicitly]
		private static void Prefix(Humanoid __instance, ref float ___m_blockTimer, ref float __state)
		{
			if (__instance is Player player && ___m_blockTimer > -1)
			{
				float parryFrameIncrease = player.GetEffect(Effect.Parrymaster);
				___m_blockTimer -= parryFrameIncrease / 1000;
				__state = parryFrameIncrease;
			}
		}

		[UsedImplicitly]
		private static void Postfix(Humanoid __instance, ref float ___m_blockTimer, float __state)
		{
			if (__instance is Player player && player.GetEffect(Effect.Parrymaster) > 0 && ___m_blockTimer > -1)
			{
				___m_blockTimer += __state / 1000;
			}
		}
	}
}
