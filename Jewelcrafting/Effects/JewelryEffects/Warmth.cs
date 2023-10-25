using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Warmth
{
	[HarmonyPatch(typeof(Player), nameof(Player.UpdateEnvStatusEffects))]
	private static class PreventColdNights
	{
		private static bool RemoveColdInColdNights(bool cold, Player player)
		{
			if (cold && EnvMan.instance.GetCurrentEnvironment().m_isColdAtNight && API.IsJewelryEquipped(player, "JC_Ring_Red"))
			{
				cold = false;
			}
			return cold;
		} 
		
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo isCold = AccessTools.DeclaredMethod(typeof(EnvMan), nameof(EnvMan.IsCold));
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.Calls(isCold))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PreventColdNights), nameof(RemoveColdInColdNights)));
				}
			}
		}
	}
}
