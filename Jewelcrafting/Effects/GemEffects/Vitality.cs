using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

public static class Vitality
{
	[HarmonyPatch(typeof(Player), nameof(Player.GetBaseFoodHP))]
	private class IncreaseBaseHealth
	{
		[UsedImplicitly]
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += __instance.GetEffect(Effect.Vitality);
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
	private class PlayerUseMethodForBaseHP
	{
		[UsedImplicitly]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo baseHpField = AccessTools.DeclaredField(typeof(Player), nameof(Player.m_baseHP));
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldfld && instruction.OperandIs(baseHpField))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Player), nameof(Player.GetBaseFoodHP)));
				}
				else
				{
					yield return instruction;
				}
			}
		}
	}
}
