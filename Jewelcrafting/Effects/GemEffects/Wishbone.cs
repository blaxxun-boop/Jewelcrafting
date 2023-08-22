using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class Wishbone
{
	private static float beaconRangeMultiplier = 1;
	
	static Wishbone()
	{
		API.OnEffectRecalc += () =>
		{
			beaconRangeMultiplier = 1;
			if (Jewelcrafting.wishboneGem.Value == Jewelcrafting.Toggle.On)
			{
				if (Player.m_localPlayer.GetEffect(Effect.Wishbone) == 0)
				{
					Player.m_localPlayer.m_seman.RemoveStatusEffect("Wishbone".GetStableHashCode());
				}
				else
				{
					Player.m_localPlayer.m_seman.AddStatusEffect("Wishbone".GetStableHashCode());
					beaconRangeMultiplier = 1 + Player.m_localPlayer.GetEffect(Effect.Wishbone) / 100f;
				}
			}
		};
		
		EffectDef.ConfigTypes.Add(Effect.Wishbone, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}
	
	[HarmonyPatch]
	private static class AlterBeaconRange
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(SE_Finder), nameof(SE_Finder.UpdateStatusEffect)),
			AccessTools.DeclaredMethod(typeof(Beacon), nameof(Beacon.FindBeaconsInRange)),
			AccessTools.DeclaredMethod(typeof(Beacon), nameof(Beacon.FindClosestBeaconInRange)),
		};

		private static readonly FieldInfo range = AccessTools.DeclaredField(typeof(Beacon), nameof(Beacon.m_range));

		private static float ModifyBeaconRange(float range) => range * beaconRangeMultiplier;
		
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.LoadsField(range))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AlterBeaconRange), nameof(ModifyBeaconRange)));
				}
			}
		}
	}
}
