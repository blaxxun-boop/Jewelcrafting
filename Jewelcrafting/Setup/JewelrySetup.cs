using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ItemDataManager;
using ItemManager;
using Jewelcrafting.GemEffects;
using SkillManager;
using UnityEngine;

namespace Jewelcrafting;

public static class JewelrySetup
{
	public static readonly HashSet<string> upgradeableJewelry = new();
	
	private static GameObject customNecklacePrefab = null!;
	private static GameObject customRingPrefab = null!;
	private static ItemDrop orangeNecklace = null!;
	private static Item purpleRing = null!;
	public static Sprite gemstoneFormationIcon = null!;

	public static GameObject CreateRingFromTemplate(string colorName, MaterialColor color) => GemStoneSetup.CreateItemFromTemplate(customRingPrefab, colorName, $"jc_ring_{colorName.Replace(" ", "_").ToLower()}", color);
	public static GameObject CreateNecklaceFromTemplate(string colorName, MaterialColor color) => GemStoneSetup.CreateItemFromTemplate(customNecklacePrefab, colorName, $"jc_necklace_{colorName.Replace(" ", "_").ToLower()}", color);
	public static void MarkJewelry(GameObject jewelry) => upgradeableJewelry.Add(jewelry.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);

	public static void initializeJewelry(AssetBundle assets)
	{
		customNecklacePrefab = assets.LoadAsset<GameObject>("JC_Custom_Necklace");
		customRingPrefab = assets.LoadAsset<GameObject>("JC_Custom_Ring");
		gemstoneFormationIcon = assets.LoadAsset<Sprite>("gemformationicon");
		
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

		item = new Item(assets, "JC_Necklace_Yellow");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("Perfect_Yellow_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		upgradeableJewelry.Add(item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);

		item = new Item(assets, "JC_Necklace_Purple");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("Perfect_Purple_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		ItemDrop.ItemData.SharedData purpleNecklaceShared = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		upgradeableJewelry.Add(purpleNecklaceShared.m_name);
		purpleNecklaceShared.m_equipStatusEffect = Utils.ConvertStatusEffect<Guidance>(purpleNecklaceShared.m_equipStatusEffect);

		item = new Item(assets, "JC_Necklace_Orange");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("Perfect_Orange_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.RequiredItems.Add("Coins", 3000);
		item.MaximumRequiredStationLevel = 3;
		orangeNecklace = item.Prefab.GetComponent<ItemDrop>();
		ItemDrop.ItemData.SharedData orangeNecklaceShared = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		upgradeableJewelry.Add(orangeNecklaceShared.m_name);
		orangeNecklaceShared.m_armor = 4;
		orangeNecklaceShared.m_armorPerLevel = 0;
		orangeNecklaceShared.m_equipStatusEffect = Utils.ConvertStatusEffect<Attunement>(orangeNecklaceShared.m_equipStatusEffect);
		
		item = new Item(assets, "JC_Ring_Purple");
		item.Crafting.Add("op_transmution_table", 2);
		item.RequiredItems.Add("Perfect_Purple_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		purpleRing = item;
		SetPurpleRingSockets();
		string purpleRingName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		upgradeableJewelry.Add(purpleRingName);

		item = new Item(assets, "JC_Ring_Green");
		item.Crafting.Add("op_transmution_table", 2);
		item.RequiredItems.Add("Perfect_Green_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		upgradeableJewelry.Add(item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);

		item = new Item(assets, "JC_Ring_Red");
		item.Crafting.Add("op_transmution_table", 2);
		item.RequiredItems.Add("Perfect_Red_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		upgradeableJewelry.Add(item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
		ItemDrop.ItemData.SharedData redRingShared = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		redRingShared.m_equipStatusEffect = Utils.ConvertStatusEffect<NightWarmth>(redRingShared.m_equipStatusEffect);
		GemEffectSetup.warmth = (SE_Stats)redRingShared.m_equipStatusEffect;

		item = new Item(assets, "JC_Ring_Blue");
		item.Crafting.Add("op_transmution_table", 2);
		item.RequiredItems.Add("Perfect_Blue_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		ItemDrop.ItemData.SharedData blueRingShared = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		upgradeableJewelry.Add(blueRingShared.m_name);
		blueRingShared.m_equipStatusEffect = Utils.ConvertStatusEffect<ModersBlessing>(blueRingShared.m_equipStatusEffect);
		
		item = new Item(assets, "JC_Ring_Black");
		item.Crafting.Add("op_transmution_table", 2);
		item.RequiredItems.Add("Perfect_Black_Socket", 1);
		item.RequiredItems.Add("Chain", 1);
		item.MaximumRequiredStationLevel = 3;
		item.RequiredUpgradeItems.Add("Coins", 500);
		ItemDrop.ItemData.SharedData blackRingShared = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		upgradeableJewelry.Add(blackRingShared.m_name);
		blackRingShared.m_equipStatusEffect = Utils.ConvertStatusEffect<Legacy>(blackRingShared.m_equipStatusEffect);
	}

	public static void SetPurpleRingSockets()
	{
		Sockets purpleSockets = purpleRing.Prefab.GetComponent<ItemDrop>().m_itemData.Data().GetOrCreate<Sockets>();
		purpleSockets.socketedGems.Clear();
		for (int i = 0; i < Jewelcrafting.maximumNumberSockets.Value; ++i)
		{
			purpleSockets.socketedGems.Add(new SocketItem(""));
		}
		purpleSockets.Save();
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
			Visual visual = Visual.visuals[__instance.m_visEquipment];
			if (visual.equippedFingerItem is { } finger && upgradeableJewelry.Contains(finger.m_shared.m_name))
			{
				__result += finger.GetArmor();
			}
			if (visual.equippedNeckItem is { } neck && upgradeableJewelry.Contains(neck.m_shared.m_name))
			{
				__result += neck.GetArmor();
			}
		}
	}

	[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
	private static class DisplayUtilityArmor
	{
		private static void Postfix(ItemDrop.ItemData item, int qualityLevel, float worldLevel, ref string __result)
		{
			if (upgradeableJewelry.Contains(item.m_shared.m_name))
			{
				__result += $"\n$item_armor: <color=orange>{item.GetArmor(qualityLevel, worldLevel)}</color>";
			}
		}
	}

	private static int OrangeNecklaceQuality() => Mathf.RoundToInt(Player.m_localPlayer.GetSkillFactor("Jewelcrafting") * 100); 
	
	[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
	private static class SetOrangeNecklaceQualityInCraftMenu
	{
		private static void Prefix(ItemDrop.ItemData item, ref int qualityLevel)
		{
			if (item == orangeNecklace.m_itemData)
			{
				qualityLevel = OrangeNecklaceQuality();
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
	private static class UpdateQualityAfterOrangeNecklaceCraft
	{
		private static readonly MethodInfo AddItem = AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool) });

		private static int SetQuality(int quality) => InventoryGui.instance.m_craftRecipe.m_item.gameObject == orangeNecklace.gameObject ? OrangeNecklaceQuality() : quality;
		
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
		{
			List<CodeInstruction> instructions = instructionsEnumerable.ToList();
			for (int i = 0; i < instructions.Count; ++i)
			{
				if (instructions[i].Calls(AddItem))
				{
					while (instructions[--i].opcode != OpCodes.Ldloc_0) ;
					instructions.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UpdateQualityAfterOrangeNecklaceCraft), nameof(SetQuality))));
					break;
				}
			}
			return instructions;
		}
	}
}
