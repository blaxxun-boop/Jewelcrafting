using System.Collections.Generic;
using ExtendedItemDataFramework;
using HarmonyLib;
using ItemManager;
using UnityEngine;

namespace Jewelcrafting;

public static class MiscSetup
{
	public static string gemBagName = null!;
	public static string gemBoxName = null!;
	public static string chaosFrameName = null!;
	public static string chanceFrameName = null!;
	public static string blessedMirrorName = null!;
	public static string celestialMirrorName = null!;
	public static readonly List<GameObject> framePrefabs = new();

	public static void initializeMisc(AssetBundle assets)
	{
		Item item = new(assets, "JC_Gem_Bag");
		item.Crafting.Add("op_transmution_table", 1);
		item.RequiredItems.Add("DeerHide", 8);
		item.RequiredItems.Add("LeatherScraps", 10);
		item.RequiredItems.Add("Resin", 5);
		item.RequiredItems.Add("GreydwarfEye", 1);
		gemBagName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

		item = new Item(assets, "JC_Gem_Box");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("FineWood", 15);
		item.RequiredItems.Add("LeatherScraps", 10);
		item.RequiredItems.Add("Resin", 5);
		item.RequiredItems.Add("GreydwarfEye", 5);
		gemBoxName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		
		item = new Item(assets, "Blue_Crystal_Frame");
		framePrefabs.Add(item.Prefab);
		chaosFrameName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		item = new Item(assets, "Black_Crystal_Frame");
		framePrefabs.Add(item.Prefab);
		chanceFrameName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		item = new Item(assets, "JC_Blessed_Crystal_Mirror");
		blessedMirrorName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		item = new Item(assets, "JC_Celestial_Crystal_Mirror");
		celestialMirrorName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

		ExtendedItemData.NewExtendedItemData += item =>
		{
			if (item.m_shared.m_name == gemBagName && item.m_quality == 1 && item.GetComponent<SocketBag>() is null)
			{
				SocketBag sockets = item.AddComponent<SocketBag>();
				for (int i = 0; i < Jewelcrafting.gemBagSlotsRows.Value * Jewelcrafting.gemBagSlotsColumns.Value - 1; ++i)
				{
					sockets.socketedGems.Add(new SocketItem(""));
				}
				sockets.Save();
			}
			
			if (item.m_shared.m_name == gemBoxName && item.m_quality == 1 && item.GetComponent<InventoryBag>() is null)
			{
				item.AddComponent<InventoryBag>().Save();
			}

			if ((item.m_shared.m_name == chaosFrameName || item.m_shared.m_name == chanceFrameName || item.m_shared.m_name == blessedMirrorName || item.m_shared.m_name == celestialMirrorName) && item.GetComponent<Frame>() is null)
			{
				item.AddComponent<Frame>().Save();
			}
		};
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
	private static class AddItemToBag
	{
		private static bool Prefix(Humanoid __instance, GameObject go, bool autoPickupDelay)
		{
			if (__instance is not Player player || Jewelcrafting.gemBagAutofill.Value == Jewelcrafting.Toggle.Off || go.GetComponent<ItemDrop>() is not { } itemDrop || !itemDrop.CanPickup(autoPickupDelay) || !Utils.ItemAllowedInGemBag(itemDrop.m_itemData) || itemDrop.m_nview.GetZDO() is null)
			{
				return true;
			}

			string prefabName = global::Utils.GetPrefabName(go);
			int originalAmount = itemDrop.m_itemData.m_stack;
			foreach (ItemDrop.ItemData item in player.m_inventory.m_inventory)
			{
				if (item.m_shared.m_name == gemBagName && item.Extended().GetComponent<SocketBag>() is { } socketBag)
				{
					void FinishPickup()
					{
						ZNetScene.instance.Destroy(go);
						socketBag.Save();
						player.m_pickupEffects.Create(player.transform.position, Quaternion.identity);
						player.ShowPickupMessage(itemDrop.m_itemData, originalAmount);
					}

					for (int i = 0; i < socketBag.socketedGems.Count; ++i)
					{
						SocketItem slot = socketBag.socketedGems[i];
						if (slot.Name == prefabName)
						{
							bool canFillLastItem = itemDrop.m_itemData.m_shared.m_maxStackSize - slot.Count >= itemDrop.m_itemData.m_stack;
							if (canFillLastItem)
							{
								slot.Count += itemDrop.m_itemData.m_stack;
							}
							else
							{
								itemDrop.m_itemData.m_stack -= itemDrop.m_itemData.m_shared.m_maxStackSize - slot.Count;
								slot.Count = itemDrop.m_itemData.m_shared.m_maxStackSize;
							}
							socketBag.socketedGems[i] = slot;
							if (canFillLastItem)
							{
								FinishPickup();
								return false;
							}
						}
					}

					for (int i = 0; i < socketBag.socketedGems.Count; ++i)
					{
						if (socketBag.socketedGems[i].Count == 0)
						{
							socketBag.socketedGems[i] = new SocketItem(prefabName, itemDrop.m_itemData.m_stack);
							FinishPickup();
							return false;
						}
					}

					socketBag.Save();
				}
			}

			return true;
		}
	}
}
