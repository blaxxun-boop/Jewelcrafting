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
			for (int i = 0; i < instructionList.Count; ++i)
			{
				if (i + 1 < instructionList.Count && instructionList[i + 1].opcode == OpCodes.Call && instructionList[i + 1].OperandIs(AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Heal))))
				{					
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IncreaseHealthRegeneration), nameof(GetRegIncrease)));
					yield return new CodeInstruction(OpCodes.Add);
				}
				yield return instructionList[i];
			}
		}
	}
}
