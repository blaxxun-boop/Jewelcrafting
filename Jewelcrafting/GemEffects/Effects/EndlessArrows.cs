using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class EndlessArrows
{
	static EndlessArrows()
	{
		EffectDef.ConfigTypes.Add(Effect.Endlessarrows, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Attack), nameof(Attack.UseAmmo))]
	private static class ReduceAmmoUsage
	{
		private static bool Prefix(Attack __instance, ref bool __result)
		{
			if (__instance.m_character is Player player && Random.value < player.GetEffect(Effect.Endlessarrows) / 100f)
			{
				if (__instance.m_character.GetInventory().GetAmmoItem(__instance.m_weapon.m_shared.m_ammoType) is not { } ammoItem)
				{
					return true;
				}
				
				__instance.m_ammoItem = ammoItem;
				__result = true;
				return false;
			}

			return true;
		}
	}
}
