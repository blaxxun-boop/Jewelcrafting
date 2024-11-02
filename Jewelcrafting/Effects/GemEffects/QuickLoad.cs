using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class QuickLoad
{
	static QuickLoad()
	{
		EffectDef.ConfigTypes.Add(Effect.Quickload, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
	private static class IncreaseLoadSpeed
	{
		private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
		{
			if (Player.m_localPlayer.GetEffect(Effect.Quickload) > 0 && __instance.m_shared.m_attack.m_requiresReload)
			{
				__result *= 1 - Player.m_localPlayer.GetEffect(Effect.Quickload) / 100f;
			}
		}
	}
}
