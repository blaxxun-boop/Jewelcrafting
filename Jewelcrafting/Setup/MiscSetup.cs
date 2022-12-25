using System.Collections.Generic;
using HarmonyLib;
using ItemDataManager;
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
		SocketBag socketBag = item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<SocketBag>()!;
		for (int i = 0; i < Jewelcrafting.gemBagSlotsRows.Value * Jewelcrafting.gemBagSlotsColumns.Value - 1; ++i)
		{
			socketBag.socketedGems.Add(new SocketItem(""));
		}
		socketBag.Save();
		gemBagName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

		item = new Item(assets, "JC_Gem_Box");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("FineWood", 15);
		item.RequiredItems.Add("LeatherScraps", 10);
		item.RequiredItems.Add("Resin", 5);
		item.RequiredItems.Add("GreydwarfEye", 5);
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<InventoryBag>();
		gemBoxName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

		item = new Item(assets, "Blue_Crystal_Frame");
		framePrefabs.Add(item.Prefab);
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<Frame>();
		chaosFrameName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		item = new Item(assets, "Black_Crystal_Frame");
		framePrefabs.Add(item.Prefab);
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<Frame>();
		chanceFrameName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		item = new Item(assets, "JC_Blessed_Crystal_Mirror");
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<Frame>();
		blessedMirrorName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		item = new Item(assets, "JC_Celestial_Crystal_Mirror");
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<Frame>();
		celestialMirrorName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
	private static class AddItemToBag
	{
		private static bool Prefix(Humanoid __instance, GameObject go, bool autoPickupDelay)
		{
			if (__instance is not Player player || go.GetComponent<ItemDrop>() is not { } itemDrop || !itemDrop.CanPickup(autoPickupDelay) || itemDrop.m_nview.GetZDO() is null)
			{
				return true;
			}
			
			int originalAmount = itemDrop.m_itemData.m_stack;
			itemDrop.m_itemData.m_dropPrefab ??= ObjectDB.instance.GetItemPrefab(global::Utils.GetPrefabName(itemDrop.gameObject)); 
			if (Do(player.m_inventory, itemDrop.m_itemData))
			{
				ZNetScene.instance.Destroy(go);
				player.m_pickupEffects.Create(player.transform.position, Quaternion.identity);
				player.ShowPickupMessage(itemDrop.m_itemData, originalAmount);
				return false;
			}
			return true;
		}

		public static bool Do(Inventory inventory, ItemDrop.ItemData item, bool dryRun = false)
		{
			if (Jewelcrafting.gemBagAutofill.Value == Jewelcrafting.Toggle.Off || !Utils.ItemAllowedInGemBag(item))
			{
				return false;
			}

			int remainingStack = item.m_stack;
			foreach (ItemDrop.ItemData invItem in inventory.m_inventory)
			{
				if (invItem.m_shared.m_name == gemBagName && invItem.Data().Get<SocketBag>() is { } socketBag)
				{
					int startingAmount = remainingStack;
					void FinishPickup()
					{
						if (!dryRun)
						{
							socketBag.Save();
							if (GemStones.AddFakeSocketsContainer.openEquipment == socketBag.Info)
							{
								GemStones.AddFakeSocketsContainer.openInventory!.AddItem(item.m_dropPrefab, startingAmount);
							}
						}
					}

					for (int i = 0; i < socketBag.socketedGems.Count; ++i)
					{
						SocketItem slot = socketBag.socketedGems[i];
						if (slot.Name == item.m_dropPrefab.name)
						{
							bool canFillLastItem = item.m_shared.m_maxStackSize - slot.Count >= remainingStack;
							if (canFillLastItem)
							{
								slot.Count += remainingStack;
							}
							else
							{
								remainingStack -= item.m_shared.m_maxStackSize - slot.Count;
								slot.Count = item.m_shared.m_maxStackSize;
							}
							if (!dryRun)
							{
								socketBag.socketedGems[i] = slot;
							}
							if (canFillLastItem)
							{
								FinishPickup();
								return true;
							}
						}
					}

					for (int i = 0; i < socketBag.socketedGems.Count; ++i)
					{
						if (socketBag.socketedGems[i].Count == 0)
						{
							if (!dryRun)
							{
								socketBag.socketedGems[i] = new SocketItem(item.m_dropPrefab.name, remainingStack);
							}
							FinishPickup();
							return true;
						}
					}

					if (!dryRun)
					{
						socketBag.Save();
						if (GemStones.AddFakeSocketsContainer.openEquipment == socketBag.Info)
						{
							GemStones.AddFakeSocketsContainer.openInventory!.AddItem(item.m_dropPrefab, startingAmount - remainingStack);
						}
					}
				}
			}

			if (!dryRun)
			{
				item.m_stack = remainingStack;
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.AutoPickup))]
	private static class CheckAutoPickupActive
	{
		public static bool PickingUp = false;
		private static void Prefix() => PickingUp = true;
		private static void Finalizer() => PickingUp = false;
	}

	[HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
	private static class AutoPickupGemsWithFullInventory
	{
		private static void Postfix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
		{
			if (!__result && CheckAutoPickupActive.PickingUp)
			{
				__result = AddItemToBag.Do(__instance, item, true);
			}
		}
	}
}
