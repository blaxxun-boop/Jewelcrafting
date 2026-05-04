using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting.LootSystem;

public static class OrbDrops
{
	private static GameObject[] orbs =
	[
		MiscSetup.divinityOrbPrefab,
		MiscSetup.finalityOrbPrefab,
		MiscSetup.corruptionOrbPrefab,
		MiscSetup.whimsicalityOrbPrefab,
		MiscSetup.prophecyOrbPrefab,
		MiscSetup.misfortuneOrbPrefab,
	];
	
	static OrbDrops()
	{
		IEnumerable<CharacterDrop.Drop> drop(Character character)
		{
			if (Utils.UsesPowerRanges())
			{
				yield return LootAdder.Drop(MiscSetup.divinityOrbPrefab, Jewelcrafting.divinityOrbGlobalDropChance.Value / 100f);
				yield return LootAdder.Drop(MiscSetup.finalityOrbPrefab, Jewelcrafting.finalityOrbGlobalDropChance.Value / 100f * (EnvMan.IsNight() ? Jewelcrafting.finalityOrbDropMultiplier.Value : 1));
				yield return LootAdder.Drop(MiscSetup.corruptionOrbPrefab, (character.m_name == "$enemy_surtling" ? Jewelcrafting.corruptionOrbDropChance.Value : Jewelcrafting.corruptionOrbGlobalDropChance.Value) / 100f);
				yield return LootAdder.Drop(MiscSetup.whimsicalityOrbPrefab, Jewelcrafting.whimsicalOrbGlobalDropChance.Value / 100f);
				yield return LootAdder.Drop(MiscSetup.prophecyOrbPrefab, Jewelcrafting.prophecyOrbGlobalDropChance.Value / 100f);
			}
			yield return LootAdder.Drop(MiscSetup.misfortuneOrbPrefab, Jewelcrafting.misfortuneOrbDropChance.Value / 100f);
			yield return LootAdder.Drop(MiscSetup.chanceFramePrefab, Jewelcrafting.chanceFrameOrbGlobalDropChance.Value / 100f);
			yield return LootAdder.Drop(MiscSetup.chaosFramePrefab, Jewelcrafting.chaosFrameOrbGlobalDropChance.Value / 100f);
		}
		LootAdder.Loot.Add(drop);
	}

	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
	private static class CountOrbs
	{
		private static void Postfix(List<KeyValuePair<GameObject, int>> __result)
		{
			Stats.orbsDroppedCreature.Increment(__result.Where(kv => orbs.Contains(kv.Key)).Select(kv => kv.Value).Sum());
		}
	}

	[HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenRespons))]
	private static class DropDivinityOrb
	{
		private static void Prefix(Container __instance, bool granted)
		{
			if (!Player.m_localPlayer || !granted || !Utils.UsesPowerRanges() || !__instance.name.StartsWith("TreasureChest_", StringComparison.Ordinal))
			{
				return;
			}

			if (__instance.m_nview.GetZDO().GetBool("Jewelcrafting Treasure Looted"))
			{
				return;
			}

			__instance.m_nview.GetZDO().Set("Jewelcrafting Treasure Looted", true);

			if (Random.value < Jewelcrafting.divinityOrbDropChance.Value / 100f)
			{
				__instance.m_inventory.AddItem(MiscSetup.divinityOrbPrefab, 1);
			}
		}
	}

	[HarmonyPatch(typeof(Leviathan), nameof(Leviathan.FixedUpdate))]
	private static class DropWhimsicalityOrb
	{
		private static ZNetView DropOrb(ZNetView netView)
		{
			if (Utils.UsesPowerRanges())
			{
				CharacterDrop.DropItems([new KeyValuePair<GameObject, int>(MiscSetup.whimsicalityOrbPrefab, Jewelcrafting.whimsicalOrbDroprate.Value)], netView.GetZDO().GetPosition(), 10);
			}
			return netView;
		}

		private static readonly MethodInfo destroy = AccessTools.DeclaredMethod(typeof(ZNetView), nameof(ZNetView.Destroy));

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.Calls(destroy))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(DropWhimsicalityOrb), nameof(DropOrb)));
				}
				yield return instruction;
			}
		}
	}
}
