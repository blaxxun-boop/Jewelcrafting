using System.Collections.Generic;
using ExtendedItemDataFramework;
using HarmonyLib;
using ItemManager;
using Jewelcrafting.GemEffects;
using UnityEngine;

namespace Jewelcrafting;

public static class JewelrySetup
{
	private static readonly HashSet<string> upgradeableJewelry = new();

	public static int greenRingHash;
	public static string redRingName = null!;

	private static GameObject customNecklacePrefab = null!;
	private static GameObject customRingPrefab = null!;

	public static GameObject CreateRingFromTemplate(string colorName, Color color) => GemStoneSetup.CreateItemFromTemplate(customRingPrefab, colorName, $"jc_{colorName.Replace(" ", "_").ToLower()}_ring", color);
	public static GameObject CreateNecklaceFromTemplate(string colorName, Color color) => GemStoneSetup.CreateItemFromTemplate(customNecklacePrefab, colorName, $"jc_{colorName.Replace(" ", "_").ToLower()}_necklace", color);
	public static void MarkJewelry(GameObject jewelry) => upgradeableJewelry.Add(jewelry.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);

	public static void initializeJewelry(AssetBundle assets)
	{
		customNecklacePrefab = assets.LoadAsset<GameObject>("JC_Custom_Necklace");
		customRingPrefab = assets.LoadAsset<GameObject>("JC_Custom_Ring");

		Item item = new(assets, "JC_Necklace_Red");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("Perfect_Red_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		upgradeableJewelry.Add(item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);

		item = new Item(assets, "JC_Necklace_Green");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("Perfect_Green_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		ItemDrop.ItemData.SharedData greenNecklaceShared = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		upgradeableJewelry.Add(greenNecklaceShared.m_name);
		greenNecklaceShared.m_equipStatusEffect = Utils.ConvertStatusEffect<MagicRepair>(greenNecklaceShared.m_equipStatusEffect);

		item = new Item(assets, "JC_Necklace_Blue");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("Perfect_Blue_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		upgradeableJewelry.Add(item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
		greenNecklaceShared.m_equipStatusEffect = Utils.ConvertStatusEffect<MagicRepair>(greenNecklaceShared.m_equipStatusEffect);

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
		
		item = new Item(assets, "JC_Ring_Blue");
		item.Crafting.Add("op_transmution_table", 2);
		item.RequiredItems.Add("Perfect_Blue_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		ItemDrop.ItemData.SharedData blueRingShared = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		upgradeableJewelry.Add(blueRingShared.m_name);
		blueRingShared.m_equipStatusEffect = Utils.ConvertStatusEffect<ModersBlessing>(blueRingShared.m_equipStatusEffect);
		
		ExtendedItemData.NewExtendedItemData += item =>
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
}
