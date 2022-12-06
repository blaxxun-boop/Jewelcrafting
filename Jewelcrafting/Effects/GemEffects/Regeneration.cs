using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Regeneration
{
	[HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
	private static class IncreaseHealthRegeneration
	{
		private static float GetRegIncrease() => Player.m_localPlayer.GetEffect(Effect.Regeneration);

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();
			yield return instructionList[0];
			for (int i = 1; i < instructionList.Count; ++i)
			{
				yield return instructionList[i];
				if (instructionList[i - 1].opcode == OpCodes.Ldc_R4 && instructionList[i - 1].OperandIs(0f) && instructionList[i].IsStloc())
				{
					yield return new CodeInstruction(OpCodes.Ldloc_S, instructionList[i].operand);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IncreaseHealthRegeneration), nameof(GetRegIncrease)));
					yield return new CodeInstruction(OpCodes.Add);
					yield return instructionList[i];
				}
			}
		}
	}
}
