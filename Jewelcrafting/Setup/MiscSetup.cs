using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ItemDataManager;
using ItemManager;
using UnityEngine;
using Random = UnityEngine.Random;

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
	private static SocketBag socketBag = null!;
	public static InventoryBag jewelryBag = null!;
	private static GameObject divinityOrbPrefab = null!;
	public static string divinityOrbName = null!;
	public static List<Recipe> vanillaGemCraftingRecipes = new();

	public static void initializeMisc(AssetBundle assets)
	{
		Item item = new(assets, "JC_Gem_Bag");
		item.Crafting.Add("op_transmution_table", 1);
		item.RequiredItems.Add("DeerHide", 8);
		item.RequiredItems.Add("LeatherScraps", 10);
		item.RequiredItems.Add("Resin", 5);
		item.RequiredItems.Add("GreydwarfEye", 1);
		socketBag = item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<SocketBag>()!;
		UpdateGemBagSize();
		gemBagName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

		item = new Item(assets, "JC_Gem_Box");
		item.Crafting.Add("op_transmution_table", 3);
		item.RequiredItems.Add("FineWood", 15);
		item.RequiredItems.Add("LeatherScraps", 10);
		item.RequiredItems.Add("Resin", 5);
		item.RequiredItems.Add("GreydwarfEye", 5);
		jewelryBag = item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<InventoryBag>()!;
		gemBoxName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

		item = new Item(assets, "Blue_Crystal_Frame")
		{
			Configurable = Configurability.Recipe,
		};
		framePrefabs.Add(item.Prefab);
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<Frame>();
		chaosFrameName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		item = new Item(assets, "Black_Crystal_Frame")
		{
			Configurable = Configurability.Recipe,
		};
		framePrefabs.Add(item.Prefab);
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<Frame>();
		chanceFrameName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		item = new Item(assets, "JC_Blessed_Crystal_Mirror")
		{
			Configurable = Configurability.Recipe,
		};
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<Frame>();
		blessedMirrorName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		item = new Item(assets, "JC_Celestial_Crystal_Mirror")
		{
			Configurable = Configurability.Recipe,
		};
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<Frame>();
		celestialMirrorName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

		item = new Item(assets, "JC_Orb_of_Divinity")
		{
			Configurable = Configurability.Recipe,
		};
		item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().Add<Frame>();
		divinityOrbPrefab = item.Prefab;
		divinityOrbName = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
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

					Dictionary<string, uint> seed = item.Data().GetAll<SocketSeed>().ToDictionary(kv => kv.Key, kv => kv.Value.Seed);

					if (seed.Count == 0)
					{
						for (int i = 0; i < socketBag.socketedGems.Count; ++i)
						{
							SocketItem slot = socketBag.socketedGems[i];
							if (slot.Name == item.m_dropPrefab.name && slot.Seed is null)
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
					}

					for (int i = 0; i < socketBag.socketedGems.Count; ++i)
					{
						if (socketBag.socketedGems[i].Count == 0)
						{
							if (!dryRun)
							{
								socketBag.socketedGems[i] = new SocketItem(item.m_dropPrefab.name, count: remainingStack, seed: seed);
							}
							FinishPickup();
							return true;
						}
					}

					if (!dryRun)
					{
						socketBag.Save();
						if (GemStones.AddFakeSocketsContainer.openEquipment == socketBag.Info && seed.Count == 0)
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

	public static void UpdateGemBagSize()
	{
		socketBag.socketedGems.Clear();

		for (int i = 0; i < Jewelcrafting.gemBagSlotsRows.Value * Jewelcrafting.gemBagSlotsColumns.Value; ++i)
		{
			socketBag.socketedGems.Add(new SocketItem(""));
		}
		socketBag.Save();
	}

	[HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenRespons))]
	private static class DropDivinityOrb
	{
		private static void Prefix(Container __instance, bool granted)
		{
			if (!Player.m_localPlayer || !granted || Jewelcrafting.GemsUsingPowerRanges.Count == 0 || !__instance.name.StartsWith("TreasureChest_", StringComparison.Ordinal))
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
				__instance.m_inventory.AddItem(divinityOrbPrefab, 1);
			}
		}
	}

	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class AddRecipes
	{
		private static void Postfix(ObjectDB __instance)
		{
			if (__instance.GetItemPrefab("Wood") == null)
			{
				return;
			}
			
			vanillaGemCraftingRecipes.Clear();
			
			Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
			recipe.name = "Emerald_To_Jade";
			recipe.m_amount = 1;
			recipe.m_resources = new[]
			{
				new Piece.Requirement { m_amount = 3, m_resItem = __instance.GetItemPrefab("Perfect_Green_Socket").GetComponent<ItemDrop>() },
				new Piece.Requirement { m_amount = 10, m_resItem = __instance.GetItemPrefab("Eitr").GetComponent<ItemDrop>() },
			};
			recipe.m_item = __instance.GetItemPrefab("GemstoneGreen").GetComponent<ItemDrop>();
			recipe.m_craftingStation = BuildingPiecesSetup.gemcuttersTable.GetComponent<CraftingStation>();
			recipe.m_minStationLevel = 4;
			recipe.m_enabled = Jewelcrafting.vanillaGemCrafting.Value == Jewelcrafting.Toggle.On;
			
			__instance.m_recipes.Add(recipe);
			vanillaGemCraftingRecipes.Add(recipe);
			
			recipe = ScriptableObject.CreateInstance<Recipe>();
			recipe.name = "Ruby_To_Bloodstone";
			recipe.m_amount = 1;
			recipe.m_resources = new[]
			{
				new Piece.Requirement { m_amount = 3, m_resItem = __instance.GetItemPrefab("Perfect_Red_Socket").GetComponent<ItemDrop>() },
				new Piece.Requirement { m_amount = 10, m_resItem = __instance.GetItemPrefab("Eitr").GetComponent<ItemDrop>() },
			};
			recipe.m_item = __instance.GetItemPrefab("GemstoneRed").GetComponent<ItemDrop>();
			recipe.m_craftingStation = BuildingPiecesSetup.gemcuttersTable.GetComponent<CraftingStation>();
			recipe.m_minStationLevel = 4;
			recipe.m_enabled = Jewelcrafting.vanillaGemCrafting.Value == Jewelcrafting.Toggle.On;
			
			__instance.m_recipes.Add(recipe);
			vanillaGemCraftingRecipes.Add(recipe);
			
			recipe = ScriptableObject.CreateInstance<Recipe>();
			recipe.name = "Sapphire_To_Iolite";
			recipe.m_amount = 1;
			recipe.m_resources = new[]
			{
				new Piece.Requirement { m_amount = 3, m_resItem = __instance.GetItemPrefab("Perfect_Blue_Socket").GetComponent<ItemDrop>() },
				new Piece.Requirement { m_amount = 10, m_resItem = __instance.GetItemPrefab("Eitr").GetComponent<ItemDrop>() },
			};
			recipe.m_item = __instance.GetItemPrefab("GemstoneBlue").GetComponent<ItemDrop>();
			recipe.m_craftingStation = BuildingPiecesSetup.gemcuttersTable.GetComponent<CraftingStation>();
			recipe.m_minStationLevel = 4;
			recipe.m_enabled = Jewelcrafting.vanillaGemCrafting.Value == Jewelcrafting.Toggle.On;
			
			__instance.m_recipes.Add(recipe);
			vanillaGemCraftingRecipes.Add(recipe);
		}
	}
}
