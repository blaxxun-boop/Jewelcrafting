using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Jewelcrafting.Effects;

public class AoeEffects
{
	public static readonly HashSet<string> ExemptEffects = new();

	private static bool AggravatablePassthrough(BaseAI ai, bool result, Aoe aoe)
	{
		if (!result || ai.IsAggravated())
		{
			return false;
		}
		
		return !ExemptEffects.Contains(global::Utils.GetPrefabName(aoe.gameObject));
	}
	
	[HarmonyPatch(typeof(Aoe), nameof(Aoe.OnHit))]
	private static class PreventAggravatingInEffectAoes
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo aggravatable = AccessTools.DeclaredMethod(typeof(BaseAI), nameof(BaseAI.IsAggravatable));
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.Calls(aggravatable))
				{
					yield return new CodeInstruction(OpCodes.Dup);
					yield return instruction;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AoeEffects), nameof(AggravatablePassthrough)));
				}
				else
				{
					yield return instruction;
				}
			}
		}
	}
}
