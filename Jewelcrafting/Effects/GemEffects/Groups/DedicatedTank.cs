using HarmonyLib;
using JetBrains.Annotations;
using Jewelcrafting.GemEffects;

namespace Jewelcrafting.Effects.GemEffects.Groups;

public static class DedicatedTank
{
	static DedicatedTank()
	{
		EffectDef.ConfigTypes.Add(Effect.Dedicatedtank, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private class ReduceDamageDealt
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player attacker && attacker.GetEffect(Effect.Dedicatedtank) > 0 && Utils.GetNearbyGroupMembers(attacker, 40f).Count > 0)
			{
				hit.ApplyModifier(attacker.GetEffect(Effect.Dedicatedtank) / 100f);
			}
		}

		[HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers))]
		private static class RemoveSpeedMalus
		{
			private static void Prefix(Player __instance, ref float __state)
			{
				if (__instance.m_leftItem?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield && __instance.GetEffect(Effect.Dedicatedtank) > 0 && Utils.GetNearbyGroupMembers(__instance, 40f).Count > 0)
				{
					__state = __instance.m_leftItem.m_shared.m_movementModifier;
					__instance.m_leftItem.m_shared.m_movementModifier = 0;
				}
			}

			private static void Finalizer(Player __instance, ref float __state)
			{
				if (__state != 0)
				{
					__instance.m_leftItem.m_shared.m_movementModifier = __state;
				}
			}
		}
		
		[HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillLevel))]
		private static class IncreaseSkillLevel
		{
			private static void Postfix(Skills __instance, Skills.SkillType skillType, ref float __result)
			{
				if (skillType == Skills.SkillType.Blocking && __instance.m_player.GetEffect(Effect.Dedicatedtank) > 0 && Utils.GetNearbyGroupMembers(__instance.m_player, 40f).Count > 0)
				{
					__result = 100;
				}
			}
		}
	}
}
