using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class PowerRecovery
{
	[HarmonyPatch(typeof(Player), nameof(Player.ActivateGuardianPower))]
	private class ReduceGuardianPowerCooldown
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionList)
		{
			List<CodeInstruction> instructions = instructionList.ToList();
			CodeInstruction returnLoad = instructions[instructions.Count - 2];
			if (returnLoad.opcode == OpCodes.Ldc_I4_0)
			{
				returnLoad.opcode = OpCodes.Ldc_I4_1;
			}
			return instructions;
		}

		private static void Postfix(Player __instance, ref bool __result)
		{
			if (__result)
			{
				__instance.m_guardianPowerCooldown *= 1 - __instance.GetEffect(Effect.Powerrecovery) / 100f;
			}
		}
	}
}
