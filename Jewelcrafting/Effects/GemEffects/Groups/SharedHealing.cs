using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects.Groups;

public static class SharedHealing
{
	static SharedHealing()
	{
		EffectDef.ConfigTypes.Add(Effect.Sharedhealing, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(SE_Stats), nameof(SE_Stats.UpdateStatusEffect))]
	private static class InterceptStatusEffectHeal
	{
		private static float HealGroup(float heal, SE_Stats statusEffect)
		{
			if (statusEffect.m_character == Player.m_localPlayer && Player.m_localPlayer.GetEffect(Effect.Sharedhealing) is { } healFactor and > 0)
			{
				foreach (Player player in Utils.GetNearbyGroupMembers(Player.m_localPlayer, 40))
				{
					player.Heal(heal * healFactor / 100f);
				}
			}
			return heal;
		}
		
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
		{
			MethodInfo heal = AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Heal));
			List<CodeInstruction> instructions = instructionsEnumerable.ToList();
			for (int i = 0; i < instructions.Count; ++i)
			{
				if (i < instructions.Count - 1 && instructions[i + 1].Calls(heal))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(InterceptStatusEffectHeal), nameof(HealGroup)));
				}
				yield return instructions[i];
			}
		}
	}
}
