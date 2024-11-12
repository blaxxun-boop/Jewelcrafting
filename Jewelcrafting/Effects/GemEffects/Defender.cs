using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Defender
{
	[HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
	private class IncreaseArmor
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += __instance.GetEffect(Effect.Defender);
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCharacterStats))]
	private static class RoundArmorDisplay
	{
		private static readonly MethodInfo fetchArmor = AccessTools.DeclaredMethod(typeof(Character), nameof(Character.GetBodyArmor));
        
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.Calls(fetchArmor))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Mathf), nameof(Mathf.Round)));
				}
			}
		}
	}
}
