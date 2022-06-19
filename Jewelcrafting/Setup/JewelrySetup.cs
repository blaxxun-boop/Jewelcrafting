using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ItemManager;
using UnityEngine;

namespace Jewelcrafting;

public static class JewelrySetup
{
	private static readonly HashSet<string> upgradeableJewelry = new();

	private static int greenRingHash;
	private static string redRingName = null!;

	public static void initializeJewelry(AssetBundle assets)
	{
		Item item = new(assets, "JC_Necklace_Red");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("Perfect_Red_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		upgradeableJewelry.Add(item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
		
		item = new Item(assets, "JC_Ring_Purple");
		item.Crafting.Add("op_transmution_table", 2);
		item.RequiredItems.Add("Perfect_Purple_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		string purpleRingName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		upgradeableJewelry.Add(purpleRingName);

		item = new Item(assets, "JC_Ring_Green");
		item.Crafting.Add("op_transmution_table", 2);
		item.RequiredItems.Add("Perfect_Green_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		upgradeableJewelry.Add(item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
		greenRingHash = item.Prefab.name.GetStableHashCode();
		
		item = new Item(assets, "JC_Ring_Red");
		item.Crafting.Add("op_transmution_table", 2);
		item.RequiredItems.Add("Perfect_Red_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		redRingName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		upgradeableJewelry.Add(redRingName);
		
		ExtendedItemDataFramework.ExtendedItemData.NewExtendedItemData += item =>
		{
			if (item.m_shared.m_name == purpleRingName && item.m_quality == 1 && item.GetComponent<Sockets>() is null)
			{
				Sockets sockets = item.AddComponent<Sockets>();
				for (int i = 0; i < Jewelcrafting.maximumNumberSockets.Value - 1; ++i)
				{
					sockets.socketedGems.Add("");
				}
				sockets.Save();
			}
		};
	}

	[HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
	private static class ApplyUtilityArmor
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			if (__instance.m_utilityItem is { } jewelry && upgradeableJewelry.Contains(jewelry.m_shared.m_name))
			{
				__result += jewelry.GetArmor();
			}
		}
	}

	[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool))]
	private static class DisplayUtilityArmor
	{
		private static void Postfix(ItemDrop.ItemData item, int qualityLevel, ref string __result)
		{
			if (upgradeableJewelry.Contains(item.m_shared.m_name))
			{
				__result += $"\n$item_armor: <color=orange>{item.GetArmor(qualityLevel)}</color>";
			}
		}
	}

	[HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.DelayedSpawnBoss))]
	private static class ApplyBossBuffs
	{
		private static void Postfix(OfferingBowl __instance)
		{
			List<Player> nearbyPlayers = new();
			Player.GetPlayersInRange(__instance.m_bossSpawnPoint, 50f, nearbyPlayers);

			foreach (Player p in nearbyPlayers)
			{
				if (p.m_visEquipment.m_currentUtilityItemHash == greenRingHash)
				{
					p.m_seman.AddStatusEffect(Jewelcrafting.headhunter.name, true);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.UpdateEnvStatusEffects))]
	private static class PreventColdNights
	{
		private static bool RemoveColdInColdNights(bool cold, Player player)
		{
			if (cold && EnvMan.instance.GetCurrentEnvironment().m_isColdAtNight && player.m_utilityItem?.m_shared.m_name == redRingName)
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
