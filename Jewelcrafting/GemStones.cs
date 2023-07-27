using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using ItemDataManager;
using Jewelcrafting.GemEffects;
using SkillManager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting;

public static class GemStones
{
	public static readonly HashSet<string> socketableGemStones = new();
	public static readonly Dictionary<string, GameObject> gemToShard = new();
	public static Sprite emptySocketSprite = null!;
	public static readonly Dictionary<string, GameObject> bossToGem = new();

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
	private class TryUpgradingGems
	{
		private static bool Prefix(InventoryGui __instance, Player player, ref Tuple<int, Recipe>? __state)
		{
			if (Jewelcrafting.gemUpgradeChances.TryGetValue(__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_name, out ConfigEntry<float> upgradeChance) && __instance.m_craftRecipe.m_resources.Length > 0 && __instance.m_craftRecipe.m_resources[0].m_amount == __instance.m_craftRecipe.m_amount)
			{
				player.RaiseSkill("Jewelcrafting", __instance.m_craftRecipe.m_amount);

				int successCount = __instance.m_craftRecipe.m_amount;

				if (!Player.m_localPlayer.m_noPlacementCost && player.HaveRequirements(__instance.m_craftRecipe, false, 1))
				{
					for (int i = 0; i < __instance.m_craftRecipe.m_amount; ++i)
					{
						if (Random.value > upgradeChance.Value / 100f * (1 + player.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f) + player.GetEffect(Effect.Carefulcutting) / 100f)
						{
							player.ConsumeResources(new Piece.Requirement[] { new() { m_resItem = __instance.m_craftRecipe.m_resources[0].m_resItem } }, 1);
							--successCount;
						}
					}

					if (successCount == 0)
					{
						player.m_inventory.AddItem(gemToShard[__instance.m_craftRecipe.m_item.name], __instance.m_craftRecipe.m_amount);
						__instance.UpdateCraftingPanel();
						player.Message(MessageHud.MessageType.Center, "$jc_gemstone_cut_fail");
						return false;
					}
				}

				if (successCount < __instance.m_craftRecipe.m_amount)
				{
					__state = new Tuple<int, Recipe>(__instance.m_craftRecipe.m_amount - successCount, __instance.m_craftRecipe);
					__instance.m_craftRecipe = ScriptableObject.CreateInstance<Recipe>();
					__instance.m_craftRecipe.m_item = __state.Item2.m_item;
					__instance.m_craftRecipe.m_craftingStation = __state.Item2.m_craftingStation;
					__instance.m_craftRecipe.m_amount = successCount;
					__instance.m_craftRecipe.m_resources = new Piece.Requirement[] { new() { m_resItem = __state.Item2.m_resources[0].m_resItem, m_amount = successCount } };
				}
			}

			return true;
		}

		private static void Postfix(InventoryGui __instance, Player player, Tuple<int, Recipe>? __state)
		{
			if (__state is not null)
			{
				Object.Destroy(__instance.m_craftRecipe);
				__instance.m_craftRecipe = __state.Item2;
				GameObject shard = gemToShard[__instance.m_craftRecipe.m_item.name];
				int shards = __state.Item1;
				if (player.m_inventory.CanAddItem(shard, shards))
				{
					player.m_inventory.AddItem(shard, shards);
				}
				else
				{
					Utils.DropPlayerItems(shard.GetComponent<ItemDrop>().m_itemData, shards);
				}
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
	public class AddSocketAddingTab
	{
		public static Transform tab = null!;
		private static readonly UnityAction clearInteractable = () =>
		{
			if (!tab.GetComponent<Button>().interactable)
			{
				InventoryGui.instance.m_craftTimer = -1;
				tab.GetComponent<Button>().interactable = true;
				InventoryGui.instance.UpdateCraftingPanel();
			}
		};

		[HarmonyPriority(Priority.High)]
		private static void Postfix(InventoryGui __instance)
		{
			tab = Object.Instantiate(__instance.m_tabUpgrade.gameObject, __instance.m_tabUpgrade.transform.parent).transform;
			tab.Find("Text").GetComponent<Text>().text = Localization.instance.Localize("$jc_socket_button_text");
			Transform parent = tab.parent;
			int childCount = parent.childCount;
			tab.SetSiblingIndex(childCount - 2);
			tab.transform.localPosition = tab.transform.localPosition with { x = parent.GetChild(childCount - 3).localPosition.x + (__instance.m_tabUpgrade.transform.localPosition.x - __instance.m_tabCraft.transform.localPosition.x) };
			Button.ButtonClickedEvent buttonClick = new();
			buttonClick.AddListener(tab.GetComponent<ButtonSfx>().OnClick);
			buttonClick.AddListener(() =>
			{
				for (int i = 0; i < tab.parent.childCount; ++i)
				{
					if (tab.parent.GetChild(i).GetComponent<Button>() is { } button && button.transform != tab)
					{
						button.interactable = true;
						button.onClick.AddListener(clearInteractable);
					}
				}
				tab.GetComponent<Button>().interactable = false;
				InventoryGui.instance.UpdateCraftingPanel();
				__instance.m_craftTimer = -1;
			});
			tab.GetComponent<Button>().onClick = buttonClick;
		}

		public static bool TabOpen() => tab.gameObject.activeSelf && !tab.GetComponent<Button>().interactable;
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
	private class LimitTabToGemCutterTable
	{
		private static void Prefix(InventoryGui __instance)
		{
			AddSocketAddingTab.tab.gameObject.SetActive(Player.m_localPlayer.m_currentStation && Player.m_localPlayer.m_currentStation.name.StartsWith("op_transmution_table", StringComparison.Ordinal));
			if (!AddSocketAddingTab.tab.gameObject.activeSelf && !AddSocketAddingTab.tab.GetComponent<Button>().interactable)
			{
				AddSocketAddingTab.tab.GetComponent<Button>().interactable = true;
				__instance.m_tabCraft.interactable = false;
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
	public class AddSocketIcons
	{
		public static readonly GameObject[] socketIcons = new GameObject[5];
		public static Button socketingButton = null!;

		private static void Postfix(InventoryGui __instance)
		{
			Transform recipeName = __instance.m_recipeName.transform;
			Transform parent = recipeName.parent;

			for (int i = 0; i < 5; ++i)
			{
				GameObject socket = new($"Jewelcrafting Socket {i}");
				socket.transform.SetParent(parent, false);
				socket.AddComponent<Image>();
				RectTransform rect = socket.GetComponent<RectTransform>();
				rect.sizeDelta = new Vector2(32, 32);
				rect.localPosition = new Vector3(-(5 - i) * 36 - 77, -50);
				socket.SetActive(false);

				socketIcons[i] = socket;
			}

			socketingButton = Object.Instantiate(AddSocketAddingTab.tab.gameObject, parent, false).GetComponent<Button>();
			RectTransform buttonRect = socketingButton.GetComponent<RectTransform>();
			buttonRect.localPosition = new Vector3(-49, -50);
			buttonRect.sizeDelta = new Vector2(88, 32);
			Button.ButtonClickedEvent buttonClick = new();
			buttonClick.AddListener(() =>
			{
				OpenFakeSocketsContainer.Open(__instance, __instance.m_selectedRecipe.Value);
			});
			socketingButton.onClick = buttonClick;
			socketingButton.interactable = true;
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
	private class ChangeDisplay
	{
		private static float originalCraftSize;
		private static bool displayGemChance;

		private static int RevertUpgradingQuality() => AddSocketAddingTab.TabOpen() ? 1 : 0;

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo qualityField = AccessTools.DeclaredField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_quality));
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.opcode == OpCodes.Ldfld && instruction.OperandIs(qualityField))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ChangeDisplay), nameof(RevertUpgradingQuality)));
					yield return new CodeInstruction(OpCodes.Sub);
				}
			}
		}

		private static void Postfix(InventoryGui __instance)
		{
			RectTransform craftTypeRect = __instance.m_itemCraftType.GetComponent<RectTransform>();
			Vector2 anchoredPosition = craftTypeRect.anchoredPosition;

			foreach (GameObject socketIcon in AddSocketIcons.socketIcons)
			{
				socketIcon.SetActive(false);
			}
			if (__instance.m_selectedRecipe.Value?.Data().Get<ItemContainer>() is { } container)
			{
				if (container is Socketable sockets and not ItemBag)
				{
					for (int i = 0; i < sockets.socketedGems.Count; ++i)
					{
						AddSocketIcons.socketIcons[i].SetActive(true);
						AddSocketIcons.socketIcons[i].GetComponent<Image>().sprite = ObjectDB.instance.GetItemPrefab(sockets.socketedGems[i].Name)?.GetComponent<ItemDrop>().m_itemData.GetIcon() ?? emptySocketSprite;
					}
				}
				AddSocketIcons.socketingButton.gameObject.SetActive(AddSocketAddingTab.TabOpen());
			}
			else
			{
				AddSocketIcons.socketingButton.gameObject.SetActive(false);
			}

			if (AddSocketAddingTab.TabOpen() && __instance.m_selectedRecipe.Value is { } activeRecipe)
			{
				__instance.m_recipeRequirementList[0].transform.parent.gameObject.SetActive(false);
				__instance.m_craftButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$jc_add_socket_button");
				__instance.m_craftButton.interactable = CanAddMoreSockets(activeRecipe);
				int socketNumber = Math.Min(activeRecipe.Data().Get<Sockets>()?.socketedGems.Count ?? 0, 4);
				__instance.m_itemCraftType.text = Localization.instance.Localize("$jc_socket_adding_warning", Math.Min(Mathf.RoundToInt(Jewelcrafting.socketAddingChances[socketNumber].Value * (1 + Player.m_localPlayer.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f)), 100).ToString());
				if (craftTypeRect.pivot.y != 1)
				{
					Vector2 sizeDelta = craftTypeRect.sizeDelta;
					originalCraftSize = sizeDelta.y;
					craftTypeRect.sizeDelta = new Vector2(sizeDelta.x, 67);
					__instance.m_itemCraftType.horizontalOverflow = HorizontalWrapMode.Wrap;
					craftTypeRect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y - originalCraftSize / 2);
					craftTypeRect.pivot = new Vector2(0.5f, 1);
				}
			}
			else
			{
				if (craftTypeRect.pivot.y == 1)
				{
					__instance.m_itemCraftType.horizontalOverflow = HorizontalWrapMode.Overflow;
					craftTypeRect.sizeDelta = new Vector2(craftTypeRect.sizeDelta.x, originalCraftSize);
					craftTypeRect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + originalCraftSize / 2);
					craftTypeRect.pivot = new Vector2(0.5f, 0.5f);
				}

				__instance.m_recipeRequirementList[0].transform.parent.gameObject.SetActive(true);

				if (__instance.m_selectedRecipe.Key is { } recipe && Jewelcrafting.gemUpgradeChances.TryGetValue(recipe.m_item.m_itemData.m_shared.m_name, out ConfigEntry<float> chance) && recipe.m_resources.Length > 0 && recipe.m_resources[0].m_amount == recipe.m_amount)
				{
					__instance.m_itemCraftType.horizontalOverflow = HorizontalWrapMode.Wrap;
					__instance.m_itemCraftType.text = Localization.instance.Localize("$jc_gem_cutting_warning", Math.Min(Mathf.RoundToInt(chance.Value * (1 + Player.m_localPlayer.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f) + Player.m_localPlayer.GetEffect(Effect.Carefulcutting)), 100).ToString());

					if (!displayGemChance)
					{
						RectTransform descriptionRect = __instance.m_recipeDecription.GetComponent<RectTransform>();
						Vector2 sizeDelta = descriptionRect.sizeDelta;
						descriptionRect.sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y - 30);
						anchoredPosition = descriptionRect.anchoredPosition;
						descriptionRect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + 15);
					}

					__instance.m_itemCraftType.gameObject.SetActive(true);
					displayGemChance = true;
				}
				else if (displayGemChance)
				{
					__instance.m_itemCraftType.horizontalOverflow = HorizontalWrapMode.Overflow;
					displayGemChance = false;

					RectTransform descriptionRect = __instance.m_recipeDecription.GetComponent<RectTransform>();
					Vector2 sizeDelta = descriptionRect.sizeDelta;
					descriptionRect.sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + 30);
					anchoredPosition = descriptionRect.anchoredPosition;
					descriptionRect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y - 15);
				}
			}
		}
	}

	private static bool CanAddMoreSockets(ItemDrop.ItemData item)
	{
		return item.Data().Get<ItemContainer>() is not { } container || ((container as Sockets)?.socketedGems.Count ?? int.MaxValue) < Jewelcrafting.maximumNumberSockets.Value;
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
	private class AddItemsToRecipeList
	{
		private static bool Prefix(InventoryGui __instance)
		{
			if (AddSocketAddingTab.TabOpen())
			{
				HashSet<ItemDrop.ItemData> socketItems = new(Player.m_localPlayer.GetInventory().m_inventory.Where(i => Utils.IsSocketableItem(i) || i.Data().Get<ItemContainer>() is { boxSealed: false }));

				List<KeyValuePair<Recipe, ItemDrop.ItemData>> recipes = new();
				foreach (KeyValuePair<Recipe, ItemDrop.ItemData> recipe in __instance.m_availableRecipes)
				{
					if (recipe.Key.m_item && socketItems.Remove(recipe.Value))
					{
						recipes.Add(recipe);
					}
				}

				__instance.m_availableRecipes.Clear();
				foreach (GameObject recipe in __instance.m_recipeList)
				{
					Object.Destroy(recipe);
				}

				__instance.m_recipeList.Clear();
				foreach (ItemDrop.ItemData itemData in socketItems)
				{
					ItemDrop component = Utils.Clone(itemData.m_dropPrefab.GetComponent<ItemDrop>());
					component.m_itemData = itemData;
					Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
					recipe.m_item = component;
					recipes.Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(recipe, itemData));
				}

				foreach (KeyValuePair<Recipe, ItemDrop.ItemData> recipe in recipes)
				{
					__instance.AddRecipeToList(Player.m_localPlayer, recipe.Key, recipe.Value, CanAddMoreSockets(recipe.Value));
				}

				__instance.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(__instance.m_recipeListBaseSize, __instance.m_recipeList.Count * __instance.m_recipeListSpace));

				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class ItemSharedMap
	{
		public static readonly Dictionary<string, GameObject> items = new();

		private static void Postfix(ObjectDB __instance)
		{
			items.Clear();
			foreach (GameObject item in __instance.m_items)
			{
				if (item.GetComponent<ItemDrop>() is { } itemDrop)
				{
					items[itemDrop.m_itemData.m_shared.m_name] = item;
				}
			}
			ApplyWisplightGem();
		}
	}

	public static void ApplyWisplightGem()
	{
		GemStoneSetup.DisableGemColor(GemType.Wisplight);
		if (ObjectDB.instance?.GetItemPrefab("Demister") is { } wisplight)
		{
			GemStoneSetup.RegisterGem(wisplight, GemType.Wisplight);
			ItemDrop.ItemData.SharedData sharedData = wisplight.GetComponent<ItemDrop>().m_itemData.m_shared;
			sharedData.m_itemType = Jewelcrafting.wisplightGem.Value == Jewelcrafting.Toggle.On ? ItemDrop.ItemData.ItemType.Material : ItemDrop.ItemData.ItemType.Utility;
			if (Jewelcrafting.wisplightGem.Value == Jewelcrafting.Toggle.Off)
			{
				socketableGemStones.Remove(sharedData.m_name);
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
	private class AddSocketToItem
	{
		private static bool Prefix(InventoryGui __instance)
		{
			if (!AddSocketAddingTab.TabOpen())
			{
				return true;
			}

			int recipeIndex = __instance.GetSelectedRecipeIndex(true);

			if (Jewelcrafting.socketingItemsExperience.Value == Jewelcrafting.Toggle.On)
			{
				Player.m_localPlayer.RaiseSkill("Jewelcrafting");
			}

			int socketNumber = Math.Min(__instance.m_craftUpgradeItem.Data().Get<Sockets>()?.socketedGems.Count ?? 0, 4);

			if (!Player.m_localPlayer.m_noPlacementCost && Random.value > Jewelcrafting.socketAddingChances[socketNumber].Value / 100f * (1 + Player.m_localPlayer.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f))
			{
				if (__instance.m_craftUpgradeItem.Data().Get<Sockets>() is { } sockets)
				{
					Inventory itemInventory = sockets.ReadInventory();

					Player.m_localPlayer.m_inventory.MoveAll(itemInventory);

					Transform playerPosition = Player.m_localPlayer.transform;
					foreach (ItemDrop itemDrop in itemInventory.GetAllItems().Select(gem => ItemDrop.DropItem(gem, 1, playerPosition.position + playerPosition.forward + playerPosition.up, playerPosition.rotation)))
					{
						itemDrop.OnPlayerDrop();
						itemDrop.GetComponent<Rigidbody>().velocity = (playerPosition.forward + Vector3.up) * 5f;
						Player.m_localPlayer.m_dropEffects.Create(playerPosition.position, Quaternion.identity);
					}
				}
				Player.m_localPlayer.UnequipItem(__instance.m_craftUpgradeItem);
				Player.m_localPlayer.GetInventory().RemoveItem(__instance.m_craftUpgradeItem);

				if ((Jewelcrafting.resourceReturnRate.Value > 0 || Jewelcrafting.resourceReturnRateUpgrade.Value > 0) && ObjectDB.instance.GetRecipe(__instance.m_craftUpgradeItem) is { } recipe)
				{
					bool returnNonTeleportable = !(Jewelcrafting.resourceReturnRateDistance.Value > 0 && __instance.m_craftUpgradeItem.Data().Get<PositionStorage>() is { } positionStorage && global::Utils.DistanceXZ(positionStorage.Position, Player.m_localPlayer.transform.position) > Jewelcrafting.resourceReturnRateDistance.Value);

					foreach (Piece.Requirement requirement in recipe.m_resources)
					{
						if (!returnNonTeleportable && !requirement.m_resItem.m_itemData.m_shared.m_teleportable)
						{
							continue;
						}
						int amount = Mathf.FloorToInt(Random.value + requirement.m_amount * Jewelcrafting.resourceReturnRate.Value / 100f + Enumerable.Range(2, Math.Max(0, __instance.m_craftUpgradeItem.m_quality - 1)).Sum(requirement.GetAmount) * (Jewelcrafting.resourceReturnRateUpgrade.Value / 100f));
						if (amount > 0 && !Player.m_localPlayer.m_inventory.AddItem(ItemSharedMap.items[requirement.m_resItem.m_itemData.m_shared.m_name], amount))
						{
							Transform transform = Player.m_localPlayer.transform;
							Vector3 position = transform.position;
							ItemDrop itemDrop = ItemDrop.DropItem(requirement.m_resItem.m_itemData, amount, position + transform.forward + transform.up, transform.rotation);
							itemDrop.OnPlayerDrop();
							itemDrop.GetComponent<Rigidbody>().velocity = (transform.forward + Vector3.up) * 5f;
							Player.m_localPlayer.m_dropEffects.Create(position, Quaternion.identity);
						}
					}
				}

				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$jc_socket_adding_fail");

				__instance.UpdateCraftingPanel();
			}
			else
			{
				ItemInfo itemInfo = __instance.m_craftUpgradeItem.Data();
				if (itemInfo.Get<Sockets>() is not { } sockets)
				{
					itemInfo.Add<Sockets>();
				}
				else
				{
					sockets.socketedGems.Add(new SocketItem(""));
				}
				itemInfo.Save();

				Player.m_localPlayer.GetCurrentCraftingStation().m_craftItemDoneEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);

				__instance.UpdateCraftingPanel();

				__instance.SetRecipe(recipeIndex, false);
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
	public class DisplaySocketTooltip
	{
		private static GameObject originalTooltip = null!;
		public static readonly ConditionalWeakTable<UITooltip, Tuple<InventoryGrid?, ItemInfo>> tooltipItem = new();

		private static void Postfix(InventoryGrid __instance, ItemDrop.ItemData item, UITooltip tooltip)
		{
			bool recreateTooltip = false;
			if (item.Data().Get<ItemContainer>() is not null)
			{
				if (tooltipItem.TryGetValue(tooltip, out Tuple<InventoryGrid?, ItemInfo>? itemInfo))
				{
					recreateTooltip = itemInfo.Item2 != item.Data();
					tooltipItem.Remove(tooltip);
				}
				tooltipItem.Add(tooltip, new Tuple<InventoryGrid?, ItemInfo>(__instance, item.Data()));
				if (tooltip.m_tooltipPrefab != GemStoneSetup.SocketTooltip)
				{
					originalTooltip = tooltip.m_tooltipPrefab;
					tooltip.m_tooltipPrefab = GemStoneSetup.SocketTooltip;
					recreateTooltip = true;
				}
			}
			else if (tooltip.m_tooltipPrefab == GemStoneSetup.SocketTooltip)
			{
				tooltip.m_tooltipPrefab = originalTooltip;
				tooltipItem.Remove(tooltip);
				recreateTooltip = true;
			}

			if (recreateTooltip && tooltip == UITooltip.m_current)
			{
				GameObject hovered = UITooltip.m_hovered;
				UITooltip.HideTooltip();
				tooltip.OnHoverStart(hovered);
			}
		}
	}

	[HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
	private class UpdateSocketTooltip
	{
		public static void Postfix(UITooltip __instance)
		{
			if (UITooltip.m_tooltip is not null && global::Utils.GetPrefabName(UITooltip.m_tooltip) == GemStoneSetup.SocketTooltip.name && DisplaySocketTooltip.tooltipItem.TryGetValue(__instance, out Tuple<InventoryGrid?, ItemInfo> itemInfo) && itemInfo.Item2.Get<ItemContainer>() is { } container)
			{
				if (UITooltip.m_tooltip.transform.Find("Bkg (1)/Transmute_Press_Interact") is { } interact)
				{
					string text = "";
					if (itemInfo.Item1 == InventoryGui.instance?.m_playerGrid)
					{
						if (Jewelcrafting.inventoryInteractBehaviour.Value != Jewelcrafting.InteractBehaviour.Enabled && container is Frame)
						{
							text = Localization.instance.Localize("$jc_press_frame_interact", Localization.instance.Localize("<color=yellow><b>$KEY_Use</b></color>"));
						}
						else if (Jewelcrafting.inventoryInteractBehaviour.Value != Jewelcrafting.InteractBehaviour.Enabled && container is ItemBag)
						{
							text = Localization.instance.Localize("$jc_press_gem_bag_interact", Localization.instance.Localize("<color=yellow><b>$KEY_Use</b></color>"));
						}
						else if (Jewelcrafting.inventoryInteractBehaviour.Value != Jewelcrafting.InteractBehaviour.Enabled && (Jewelcrafting.inventorySocketing.Value == Jewelcrafting.Toggle.On || (Player.m_localPlayer?.GetCurrentCraftingStation() is { } craftingStation && craftingStation && global::Utils.GetPrefabName(craftingStation.gameObject) == "op_transmution_table")))
						{
							text = Localization.instance.Localize(container is Box { progress: >= 100 } ? "$jc_gembox_interact_finished" : "$jc_press_interact", Localization.instance.Localize("<color=yellow><b>$KEY_Use</b></color>"));
						}
						else
						{
							text = Localization.instance.Localize("$jc_table_required");
						}
						text += "\n";
					}
					if (Jewelcrafting.advancedTooltipAlwaysOn.Value == Jewelcrafting.Toggle.Off && container is Sockets)
					{
						text += Localization.instance.Localize("$jc_hold_advanced", $"<color=yellow><b>{Jewelcrafting.advancedTooltipKey.Value.MainKey}</b></color>");
					}
					interact.GetComponent<Text>().text = text;
					interact.gameObject.SetActive(!container.boxSealed);
				}

				int numSockets = 0;
				if (container is Socketable sockets and not Box { progress: >= 100 } and not SocketBag and not Frame)
				{
					numSockets = sockets.socketedGems.Count;
				}
				for (int i = 1; i <= 5; ++i)
				{
					if (UITooltip.m_tooltip.transform.Find($"Bkg (1)/TrannyHoles/Transmute_Text_{i}") is { } transmute)
					{
						transmute.gameObject.SetActive(i <= numSockets);
						if (i <= numSockets)
						{
							string socket = ((Socketable)container).socketedGems[i - 1].Name;
							string text = "$jc_empty_socket_text";
							Sprite? sprite = null;
							if (ObjectDB.instance.GetItemPrefab(socket) is { } gameObject)
							{
								if (container is not Box)
								{
									if (Jewelcrafting.EffectPowers.TryGetValue(socket.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(Utils.GetGemLocation(itemInfo.Item2.ItemData.m_shared, Player.m_localPlayer), out List<EffectPower> effectPowers))
									{
										ReplaceTooltipText.keyDown = Jewelcrafting.advancedTooltipKey.Value.IsPressed();
										bool displayAdvanced = ReplaceTooltipText.keyDown || Jewelcrafting.advancedTooltipAlwaysOn.Value == Jewelcrafting.Toggle.On;
										// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
										if (Jewelcrafting.advancedTooltipMode.Value == Jewelcrafting.AdvancedTooltipMode.General)
										{
											text = string.Join("\n", effectPowers.Select(gem => $"$jc_effect_{EffectDef.EffectNames[gem.Effect].ToLower()}" + (displayAdvanced ? "_desc" : $" {gem.Power}")));
										}
										else
										{
											text = string.Join("\n", effectPowers.Select(gem => $"$jc_effect_{EffectDef.EffectNames[gem.Effect].ToLower()}" + (displayAdvanced ? " - " + Utils.LocalizeDescDetail(Player.m_localPlayer!, gem.Effect, gem.Config.GetType().GetFields().Select(p => (float)p.GetValue(gem.Config)).ToArray()) : $" {gem.Power}")));
										}
									}
									else
									{
										text = "$jc_effect_no_effect";
									}
								}
								else
								{
									text = gameObject.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
								}

								sprite = gameObject.GetComponent<ItemDrop>().m_itemData.GetIcon();
							}
							transmute.GetComponent<Text>().text = Localization.instance.Localize(text);
							transmute.Find("Border/Transmute_1").gameObject.SetActive(sprite is not null);
							transmute.Find("Border/Transmute_1").GetComponent<Image>().sprite = sprite;
						}
					}
				}

				foreach (LayoutGroup rect in UITooltip.m_tooltip.GetComponentsInChildren<LayoutGroup>())
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(rect.GetComponent<RectTransform>());
				}
			}
		}
	}

	[HarmonyPatch(typeof(UITooltip), nameof(UITooltip.LateUpdate))]
	private static class ReplaceTooltipText
	{
		public static bool keyDown = false;

		private static void Postfix(UITooltip __instance)
		{
			if (__instance == UITooltip.m_current && keyDown != Jewelcrafting.advancedTooltipKey.Value.IsPressed())
			{
				keyDown = !keyDown;
				UpdateSocketTooltip.Postfix(__instance);
			}
		}
	}

	public static Dictionary<GemLocation, List<EffectPower>> GroupEffectsByGemLocation(Dictionary<GemLocation, List<EffectPower>> gem)
	{
		HashSet<Effect> collectEffects(GemLocation location)
		{
			HashSet<Effect> effects = new();
			if (gem.TryGetValue(location, out List<EffectPower> weaponPowers))
			{
				foreach (EffectPower effectPower in weaponPowers)
				{
					effects.Add(effectPower.Effect);
				}
			}
			return effects;
		}
		HashSet<Effect> weaponEffects = collectEffects(GemLocation.Weapon);
		HashSet<Effect> magicEffects = collectEffects(GemLocation.Magic);
		HashSet<Effect> allEffects = collectEffects(GemLocation.All);

		Dictionary<GemLocation, List<EffectPower>> locations = new();

		foreach (GemLocation gemLocation in gem.Keys.OrderByDescending(g => (int)g))
		{
			List<EffectPower> specificEffects = gem[gemLocation].Where(p => ((gemLocation & EffectDef.WeaponGemlocations) == 0 || !weaponEffects.Contains(p.Effect)) && ((gemLocation & EffectDef.MagicGemlocations) == 0 || !magicEffects.Contains(p.Effect)) && (gemLocation == GemLocation.All || !allEffects.Contains(p.Effect))).ToList();
			if (specificEffects.Count > 0)
			{
				locations.Add(gemLocation, specificEffects);
			}
		}

		return locations;
	}

	[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool))]
	private class DisplayEffectsOnGems
	{
		private static void Postfix(ItemDrop.ItemData item, ref string __result)
		{
			if (item.m_dropPrefab is { } prefab)
			{
				if (Jewelcrafting.EffectPowers.TryGetValue(prefab.name.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> gem))
				{
					StringBuilder sb = new("\n");

					foreach (KeyValuePair<GemLocation, List<EffectPower>> kv in GroupEffectsByGemLocation(gem))
					{
						sb.Append($"\n<color=orange>$jc_socket_slot_{kv.Key.ToString().ToLower()}:</color> {string.Join(", ", kv.Value.Select(effectPower => $"$jc_effect_{EffectDef.EffectNames[effectPower.Effect].ToLower()} {effectPower.Power}"))}");
					}

					string flavorText = $"{item.m_shared.m_description.Substring(1)}_flavor";
					if (Localization.instance.m_translations.ContainsKey(flavorText))
					{
						sb.Append($"\n\n<color=purple>${flavorText}</color>");
					}
					__result += sb.ToString();
				}
				if (FusionBoxSetup.boxTier.TryGetValue(prefab, out int tier) && item.Data().Get<Box>() is { } box)
				{
					if (box.progress >= 100)
					{
						__result += "\n\n$jc_merge_completed";
					}
					else
					{
						StringBuilder sb = new("\n");
						if (box.boxSealed)
						{
							sb.Append($"\n$jc_merge_progress: <color=orange>{box.progress:0.#}%</color>");
						}
						sb.Append($"\n$jc_merge_activity_reward: <color=orange>{Jewelcrafting.crystalFusionBoxMergeActivityProgress[tier].Value}% / $jc_minute</color>");
						ConfigEntry<int>[] mergeChances = Jewelcrafting.boxMergeChances[item.m_shared.m_name];
						if (box.boxSealed)
						{
							sb.Append($"\n\n$jc_merge_success_chance: <color=orange>{(box.socketedGems[0].Name is "JC_Common_Gembox" or "JC_Epic_Gembox" ? Jewelcrafting.boxSelfMergeChances[item.m_shared.m_name] : mergeChances[box.Tier]).Value}%</color>");
						}
						else
						{
							sb.Append("\n\n$jc_merge_success_chances:");
							sb.Append($"\n$jc_gem_tier_1: <color=orange>{mergeChances[0].Value}%</color>");
							sb.Append($"\n$jc_gem_tier_2: <color=orange>{mergeChances[1].Value}%</color>");
							sb.Append($"\n$jc_gem_tier_3: <color=orange>{mergeChances[2].Value}%</color>");
							if (box.Item.m_shared.m_name != "$jc_legendary_gembox" && Jewelcrafting.boxSelfMergeChances[item.m_shared.m_name].Value > 0)
							{
								sb.Append($"\n{box.Item.m_shared.m_name}: <color=orange>{Jewelcrafting.boxSelfMergeChances[item.m_shared.m_name].Value}%</color>");
							}
							if (Groups.API.IsLoaded() && box.Item.m_shared.m_name == "$jc_legendary_gembox" && Jewelcrafting.boxBossGemMergeChance.Value > 0)
							{
								sb.Append($"\n$jc_gem_tier_boss: <color=orange>{Jewelcrafting.boxBossGemMergeChance.Value}%</color>");
							}
						}
						sb.Append("\n\n$jc_merge_boss_reward:");
						foreach (KeyValuePair<string, ConfigEntry<float>[]> progress in Jewelcrafting.boxBossProgress)
						{
							if (progress.Value[tier].Value > 0)
							{
								sb.Append($"\n{progress.Key}: <color=orange>{progress.Value[tier].Value}%</color>");
							}
						}
						__result += sb.ToString();
					}
				}
				if (prefab.name == "CapeFeather" && Jewelcrafting.featherGliding.Value == Jewelcrafting.Toggle.Off)
				{
					__result += Localization.instance.Localize("\n\n$jc_feather_cape_gliding_buff_description", Jewelcrafting.featherGlidingBuff.Value.ToString());
				}
			}
		}
	}

	[HarmonyPatch]
	public class AddFakeSocketsContainer
	{
		public static ItemInfo? openEquipment;
		public static Inventory? openInventory;

		private static Inventory? GetWeaponInventory()
		{
			if (openEquipment is null || openInventory is null)
			{
				return null;
			}

			return openInventory;
		}

		private static bool HasWeaponInventory() => GetWeaponInventory() is not null;

		// ReSharper disable once UnusedParameter.Local
		private static Inventory PopSecondValue(object _, Inventory inventory) => inventory;

		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.OnTakeAll)),
			AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem)),
			AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.IsContainerOpen)),
		};

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsList, ILGenerator ilg)
		{
			MethodInfo containerInventory = AccessTools.DeclaredMethod(typeof(Container), nameof(Container.GetInventory));
			FieldInfo containerField = AccessTools.DeclaredField(typeof(InventoryGui), nameof(InventoryGui.m_currentContainer));
			MethodInfo objectInequality = AccessTools.DeclaredMethod(typeof(Object), "op_Inequality");
			MethodInfo objectImplicit = AccessTools.DeclaredMethod(typeof(Object), "op_Implicit");
			List<CodeInstruction> instructions = instructionsList.ToList();
			for (int i = 0; i < instructions.Count; ++i)
			{
				CodeInstruction instruction = instructions[i];
				if (instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(containerInventory))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AddFakeSocketsContainer), nameof(GetWeaponInventory)));
					yield return new CodeInstruction(OpCodes.Dup);
					Label onWeaponInv = ilg.DefineLabel();
					yield return new CodeInstruction(OpCodes.Brtrue, onWeaponInv);
					yield return new CodeInstruction(OpCodes.Pop);
					yield return instruction;
					Label onInventoryFetch = ilg.DefineLabel();
					yield return new CodeInstruction(OpCodes.Br, onInventoryFetch);
					CodeInstruction pop = new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AddFakeSocketsContainer), nameof(PopSecondValue)));
					pop.labels.Add(onWeaponInv);
					yield return pop;
					instructions[i + 1].labels.Add(onInventoryFetch);
				}
				else if (instruction.opcode == OpCodes.Call && ((instruction.OperandIs(objectInequality) && instructions[i - 2].opcode == OpCodes.Ldfld && instructions[i - 2].OperandIs(containerField)) || (instruction.OperandIs(objectImplicit) && instructions[i - 1].opcode == OpCodes.Ldfld && instructions[i - 1].OperandIs(containerField))))
				{
					yield return instruction;
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AddFakeSocketsContainer), nameof(HasWeaponInventory)));
					yield return new CodeInstruction(OpCodes.Or);
				}
				else
				{
					yield return instruction;
				}
			}
		}

		public static void SaveGems()
		{
			if (openEquipment?.Get<ItemContainer>() is not { } sockets)
			{
				// Socket component may have been removed upon item upgrade
				return;
			}

			sockets.SaveSocketsInventory(openInventory!);
			openEquipment.Save();

			if (sockets is Box { progress: >= 100 } box && box.socketedGems.All(g => g.Name == ""))
			{
				openEquipment.Remove<Box>();
				ItemDrop.ItemData deleteBox = openEquipment.ItemData;
				IEnumerator DelayedBoxDelete()
				{
					yield return null;
					InventoryGui.instance.CloseContainer();
					yield return null;
					Player.m_localPlayer.GetInventory().RemoveItem(deleteBox);
				}

				InventoryGui.instance.StartCoroutine(DelayedBoxDelete());
			}

			TrackEquipmentChanges.CalculateEffects(Player.m_localPlayer);
		}
	}

	[HarmonyPatch]
	private class CloseFakeSocketsContainer
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.Hide)),
			AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.CloseContainer)),
		};

		private static void Prefix(InventoryGui __instance)
		{
			if (AddFakeSocketsContainer.openEquipment is not null && AddFakeSocketsContainer.openInventory is not null)
			{
				if (Player.m_localPlayer)
				{
					AddFakeSocketsContainer.SaveGems();
				}

				RectTransform takeAllButton = (RectTransform)__instance.m_takeAllButton.transform;
				Vector2 anchoredPosition = takeAllButton.anchoredPosition;
				anchoredPosition = new Vector2(anchoredPosition.x, -anchoredPosition.y);
				takeAllButton.anchoredPosition = anchoredPosition;
				takeAllButton.gameObject.SetActive(true);

				FusionBoxSetup.AddSealButton.SealButton.SetActive(false);

				GemCursor.ResetCursor(GemCursor.CursorState.Socketing);

				AddFakeSocketsContainer.openEquipment = null;
				AddFakeSocketsContainer.openInventory = null;
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
	public class OpenFakeSocketsContainer
	{
		public static bool Open(InventoryGui invGui, ItemDrop.ItemData? item)
		{
			ItemInfo? itemInfo = item?.Data();

			if (itemInfo?.Get<ItemContainer>() is { boxSealed: false } sockets)
			{
				if (invGui.IsContainerOpen())
				{
					invGui.CloseContainer();
				}

				RectTransform takeAllButton = (RectTransform)invGui.m_takeAllButton.transform;
				Vector2 anchoredPosition = takeAllButton.anchoredPosition;
				anchoredPosition = new Vector2(anchoredPosition.x, -anchoredPosition.y);
				takeAllButton.anchoredPosition = anchoredPosition;

				if (sockets is Box)
				{
					FusionBoxSetup.AddSealButton.SealButton.SetActive(true);
				}
				else if ((Jewelcrafting.allowUnsocketing.Value != Jewelcrafting.Unsocketing.All || Jewelcrafting.breakChanceUnsocketSimple.Value > 0 || Jewelcrafting.breakChanceUnsocketAdvanced.Value > 0 || Jewelcrafting.breakChanceUnsocketPerfect.Value > 0 || Jewelcrafting.breakChanceUnsocketMerged.Value > 0) && sockets is not ItemBag)
				{
					takeAllButton.gameObject.SetActive(false);
				}

				Inventory inv = sockets.ReadInventory();
				inv.m_onChanged += AddFakeSocketsContainer.SaveGems;

				GemCursor.SetCursor(GemCursor.CursorState.Socketing);

				AddFakeSocketsContainer.openEquipment = itemInfo;
				AddFakeSocketsContainer.openInventory = inv;
			}

			if (AddFakeSocketsContainer.openInventory is { } inventory)
			{
				ItemInfo weapon = AddFakeSocketsContainer.openEquipment!;
				if (invGui.m_playerGrid.GetInventory().GetItemAt(weapon.ItemData.m_gridPos.x, weapon.ItemData.m_gridPos.y)?.Data() != weapon)
				{
					invGui.CloseContainer();
					return true;
				}

				invGui.m_container.gameObject.SetActive(true);
				invGui.m_containerGrid.UpdateInventory(inventory, null, invGui.m_dragItem);
				invGui.m_containerName.text = weapon.Get<SocketBag>() is null && weapon.Get<InventoryBag>() is null ? Localization.instance.Localize(weapon.Get<Frame>() is not null ? "$jc_frame_container_title" : "$jc_socket_container_title", Localization.instance.Localize(weapon.ItemData.m_shared.m_name)) : Localization.instance.Localize(weapon.ItemData.m_shared.m_name);
				if (invGui.m_firstContainerUpdate)
				{
					invGui.m_containerGrid.ResetView();
					invGui.m_firstContainerUpdate = false;
				}
				return false;
			}

			return true;
		}

		private static bool Prefix(InventoryGui __instance)
		{
			ItemDrop.ItemData? item = null;
			if (Jewelcrafting.inventoryInteractBehaviour.Value != Jewelcrafting.InteractBehaviour.Enabled && (ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")))
			{
				Vector2 pos = Input.mousePosition;
				item = __instance.m_playerGrid.GetItem(new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)));
			}

			if (Jewelcrafting.inventorySocketing.Value == Jewelcrafting.Toggle.On || (item?.Data() ?? AddFakeSocketsContainer.openEquipment)?.Get<ItemContainer>() is ItemBag || (Player.m_localPlayer?.GetCurrentCraftingStation() is { } craftingStation && craftingStation && global::Utils.GetPrefabName(craftingStation.gameObject) == "op_transmution_table"))
			{
				return Open(__instance, item);
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
	private class CatchInventoryUseButton
	{
		private static bool ShallPreventInventoryClose(InventoryGui invGui)
		{
			if (Jewelcrafting.inventoryInteractBehaviour.Value == Jewelcrafting.InteractBehaviour.Enabled)
			{
				return false;
			}
			if (ZInput.GetButton("Use") || ZInput.GetButton("JoyUse"))
			{
				if (Jewelcrafting.inventoryInteractBehaviour.Value == Jewelcrafting.InteractBehaviour.Disabled)
				{
					return RectTransformUtility.RectangleContainsScreenPoint(invGui.m_playerGrid.m_gridRoot, Input.mousePosition);
				}

				Vector2 pos = Input.mousePosition;
				return invGui.m_playerGrid.GetItem(new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)))?.Data().Get<ItemContainer>() is not null;
			}
			return false;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsList)
		{
			List<CodeInstruction> instructions = instructionsList.ToList();
			MethodInfo buttonReset = AccessTools.DeclaredMethod(typeof(ZInput), nameof(ZInput.ResetButtonStatus));
			bool first = true;
			for (int i = 0; i < instructions.Count; ++i)
			{
				if (first && i + 1 < instructions.Count && instructions[i + 1].opcode == OpCodes.Call && instructions[i + 1].OperandIs(buttonReset))
				{
					first = false;
					int j = i;
					Label? target;
					while (!instructions[j].Branches(out target))
					{
						--j;
					}
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CatchInventoryUseButton), nameof(ShallPreventInventoryClose)));
					yield return new CodeInstruction(OpCodes.Brtrue, target!.Value);
				}
				yield return instructions[i];
			}
		}
	}

	private static bool AllowsUnsocketing(ItemDrop.ItemData item)
	{
		if (AddFakeSocketsContainer.openEquipment?["SocketsLock"] is not null)
		{
			return false;
		}

		if (Jewelcrafting.allowUnsocketing.Value == Jewelcrafting.Unsocketing.All)
		{
			return true;
		}

		if (!GemHasEffectInOpenEquipment(item))
		{
			return true;
		}

		return Jewelcrafting.allowUnsocketing.Value == Jewelcrafting.Unsocketing.UniquesOnly && bossToGem.Values.Any(g => g.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == item.m_shared.m_name);
	}

	private static void HandleSocketingFrameAndMirrors(ItemDrop.ItemData container, ItemDrop.ItemData item)
	{
		bool socketingFrame = container.m_shared.m_name == MiscSetup.chanceFrameName || container.m_shared.m_name == MiscSetup.chaosFrameName;

		Sockets? existingSockets = item.Data().Get<Sockets>();
		if (socketingFrame && !Utils.IsSocketableItem(item))
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$jc_frame_not_socketable");
		}
		else if (socketingFrame && existingSockets?.socketedGems.Any(s => s.Count > 0) == true)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$jc_frame_requires_empty_sockets");
		}
		else if (container.m_shared.m_name == MiscSetup.blessedMirrorName && !socketableGemStones.Contains(item.m_shared.m_name))
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$jc_blessed_mirror_no_gem");
		}
		else if (container.m_shared.m_name == MiscSetup.celestialMirrorName && (!Utils.IsSocketableItem(item) || item.Data().Get<ItemBag>() is not null))
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$jc_celestial_mirror_not_socketable");
		}
		else if (container.m_shared.m_name == MiscSetup.celestialMirrorName && Jewelcrafting.mirrorBlacklist.Value.Replace(" ", "").Split(',').Contains(item.m_dropPrefab.name))
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$jc_celestial_mirror_blacklisted");
		}
		else if (!socketingFrame || existingSockets?.socketedGems.Count != Jewelcrafting.maximumNumberSockets.Value)
		{
			IEnumerator DelayedFrameDelete()
			{
				yield return null;
				InventoryGui.instance.CloseContainer();
				InventoryGui.instance.SetupDragItem(null, null, 1);
				yield return null;
				Player.m_localPlayer.GetInventory().RemoveItem(container, 1);

				if (socketingFrame)
				{
					int newSockets;
					// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
					if (container.m_shared.m_name == MiscSetup.chaosFrameName)
					{
						newSockets = Random.Range(0, Jewelcrafting.maximumNumberSockets.Value + 1);
					}
					else
					{
						newSockets = Math.Max((existingSockets?.socketedGems.Count ?? 0) + (Random.Range(0, 100) < Jewelcrafting.frameOfChanceChance.Value ? -1 : 1), 0);
					}

					if (newSockets == 0)
					{
						if (existingSockets is not null)
						{
							item.Data().Remove<Sockets>();
						}
					}
					else
					{
						existingSockets ??= item.Data().Add<Sockets>()!;
						existingSockets.socketedGems = Enumerable.Repeat(new SocketItem(""), newSockets).ToList();
					}
					item.Data().Save();

					// refresh tooltip
					yield return null;
					if (UITooltip.m_current)
					{
						UITooltip.m_current.OnHoverStart(UITooltip.m_hovered);
						UITooltip.m_tooltip.SetActive(true);
					}
				}
				else if (container.m_shared.m_name == MiscSetup.blessedMirrorName || container.m_shared.m_name == MiscSetup.celestialMirrorName)
				{
					ItemDrop.ItemData itemClone = item.Clone();
					itemClone.m_stack = 1;
					if (!Player.m_localPlayer.m_inventory.AddItem(itemClone))
					{
						Transform transform = Player.m_localPlayer.transform;
						Vector3 position = transform.position;
						ItemDrop itemDrop = ItemDrop.DropItem(itemClone, 1, position + transform.forward + transform.up, transform.rotation);
						itemDrop.OnPlayerDrop();
						itemDrop.GetComponent<Rigidbody>().velocity = (transform.forward + Vector3.up) * 5f;
						Player.m_localPlayer.m_dropEffects.Create(position, Quaternion.identity);
					}
				}
			}

			InventoryGui.instance.StartCoroutine(DelayedFrameDelete());
		}
	}

	private static bool ItemEligibleForSocketing(ItemDrop.ItemData item)
	{
		Box? box = AddFakeSocketsContainer.openEquipment?.Get<Box>();
		return (socketableGemStones.Contains(item.m_shared.m_name) && box is not { progress: >= 100 } && AddFakeSocketsContainer.openInventory?.HaveItem(item.m_shared.m_name) == false) || (box?.Item.m_shared.m_name == item.m_shared.m_name && Jewelcrafting.boxSelfMergeChances.TryGetValue(item.m_shared.m_name, out ConfigEntry<int> selfMergeChance) && selfMergeChance.Value > 0 && item != box.Item && item.Data().Get<Box>() is { progress: 0, boxSealed: false });
	}

	[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
	private class AllowGemStonesOnly
	{
		private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, ref int amount, Vector2i pos, ref bool __result)
		{
			if (__instance.m_inventory != AddFakeSocketsContainer.openInventory)
			{
				if (fromInventory == AddFakeSocketsContainer.openInventory)
				{
					ItemDrop.ItemData existingItem = __instance.m_inventory.GetItemAt(pos.x, pos.y);

					if (AddFakeSocketsContainer.openEquipment?.Get<ItemContainer>() is ItemBag)
					{
						if (existingItem is null || Utils.ItemAllowedInGemBag(existingItem) || Utils.ItemAllowedInGemBox(existingItem))
						{
							return true;
						}
						__result = false;
						return false;
					}

					if (!AllowsUnsocketing(item))
					{
						__result = false;
						return false;
					}

					if (existingItem is not null && existingItem.m_dropPrefab != item.m_dropPrefab)
					{
						if (!socketableGemStones.Contains(existingItem.m_shared.m_name))
						{
							__result = false;
							return false;
						}

						if (AddFakeSocketsContainer.openEquipment?.Get<Box>() is { progress: >= 100 })
						{
							__result = false;
							return false;
						}

						if (CanAddUniqueSocket(existingItem.m_dropPrefab, item.m_gridPos.x) is { } error)
						{
							Player.m_localPlayer.Message(MessageHud.MessageType.Center, error);
							__result = false;
							return false;
						}

						if (existingItem.m_stack > 1)
						{
							Vector2i emptySlot = __instance.m_inventory.FindEmptySlot(false);
							if (!__instance.m_inventory.AddItem(existingItem, existingItem.m_stack - 1, emptySlot.x, emptySlot.y))
							{
								__result = false;
								return false;
							}
						}
					}

					if (ShallDestroyGem(item, fromInventory))
					{
						fromInventory.RemoveItem(item);
						IEnumerator ResetDragItem()
						{
							yield return null;
							InventoryGui.instance.SetupDragItem(null, __instance.GetInventory(), 0);
						}
						InventoryGui.instance.StartCoroutine(ResetDragItem());
						__result = false;
						return false;
					}
				}

				return true;
			}

			if (AddFakeSocketsContainer.openEquipment?.Get<Frame>() is not null)
			{
				HandleSocketingFrameAndMirrors(AddFakeSocketsContainer.openEquipment.ItemData, item);
				__result = false;
				return false;
			}

			if (AddFakeSocketsContainer.openEquipment?.Get<ItemContainer>() is ItemBag)
			{
				if (AddFakeSocketsContainer.openEquipment.ItemData.m_shared.m_name == MiscSetup.gemBagName && Utils.ItemAllowedInGemBag(item))
				{
					return true;
				}

				if (AddFakeSocketsContainer.openEquipment.ItemData.m_shared.m_name == MiscSetup.gemBoxName && Utils.ItemAllowedInGemBox(item))
				{
					return true;
				}
				__result = false;
				return false;
			}

			ItemDrop.ItemData? oldItem = __instance.m_inventory.GetItemAt(pos.x, pos.y);
			if (oldItem == item)
			{
				return true;
			}

			if (ItemEligibleForSocketing(item))
			{
				if (oldItem is not null && !AllowsUnsocketing(oldItem))
				{
					__result = false;
					return false;
				}

				if (CanAddUniqueSocket(item.m_dropPrefab, pos.x) is { } error)
				{
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, error);
					__result = false;
					return false;
				}

				if (fromInventory != __instance.GetInventory() && oldItem is not null)
				{
					ShallDestroyGem(oldItem, __instance.GetInventory());
				}

				if (amount > 1)
				{
					Vector2i emptySlot = fromInventory.FindEmptySlot(false);
					fromInventory.AddItem(item, item.m_stack - 1, emptySlot.x, emptySlot.y);
					amount = 1;
				}

				return true;
			}

			__result = false;
			return false;
		}
	}

	private static List<GemDefinition> EnumerateUniqueGemsToCheckAgainst(string socketName, HashSet<Uniqueness> uniquePowers, out List<string> errorType)
	{
		List<GemDefinition> checkAgainst;
		errorType = new List<string>();
		if (uniquePowers.Contains(Uniqueness.All))
		{
			checkAgainst = Jewelcrafting.SocketEffects.Values.SelectMany(e => e).Where(d => d.Unique == Uniqueness.All).SelectMany(def => GemStoneSetup.Gems[def.Type]).ToList();
			errorType.Add("$jc_equipped_unique_error_all");
		}
		else if (uniquePowers.Contains(Uniqueness.Gem))
		{
			GemInfo info = GemStoneSetup.GemInfos[socketName];
			checkAgainst = GemStoneSetup.Gems[info.Type];
			errorType.Add("$jc_equipped_unique_error_gem");
			errorType.Add(Localization.instance.Localize((GemStoneSetup.uncutGems.TryGetValue(info.Type, out GameObject uncutGem) ? uncutGem : GemStoneSetup.Gems[info.Type][info.Tier - 1].Prefab).GetComponent<ItemDrop>().m_itemData.m_shared.m_name));
		}
		else if (uniquePowers.Contains(Uniqueness.Tier))
		{
			GemInfo info = GemStoneSetup.GemInfos[socketName];
			checkAgainst = new List<GemDefinition> { GemStoneSetup.Gems[info.Type][info.Tier - 1] };
			errorType.Add("$jc_equipped_unique_error_tier");
			errorType.Add(Localization.instance.Localize(GemStoneSetup.Gems[info.Type][info.Tier - 1].Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name));
		}
		else
		{
			checkAgainst = new List<GemDefinition>();
		}

		return checkAgainst;
	}

	private static string FormatUniqueSocketError(List<string> errorType, string conflictItem)
	{
		string text = errorType[0];
		errorType[0] = conflictItem;
		return Localization.instance.Localize(text, errorType.ToArray());
	}

	private static string? CanAddUniqueSocket(GameObject socket, int replacedOffset)
	{
		if (AddFakeSocketsContainer.openEquipment is not { } targetItem)
		{
			return null;
		}

		foreach (GameObject individualSocket in MergedGemStoneSetup.mergedGemContents.TryGetValue(socket.name, out List<GemInfo> gemInfos) ? gemInfos.Select(g => GemStoneSetup.Gems[g.Type][g.Tier - 1].Prefab) : new[] { socket })
		{
			if (Jewelcrafting.EffectPowers.TryGetValue(individualSocket.name.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(Utils.GetGemLocation(targetItem.ItemData.m_shared), out List<EffectPower> effectPowers))
			{
				string socketName = individualSocket.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
				HashSet<Uniqueness> uniquePowers = new(effectPowers.Select(e => e.Unique));
				List<GemDefinition> checkAgainst = EnumerateUniqueGemsToCheckAgainst(socketName, uniquePowers, out List<string> errorType);
				if (IsEquippedItem(Player.m_localPlayer, targetItem.ItemData) && HasEquippedAnyUniqueGem(checkAgainst, AddFakeSocketsContainer.openEquipment) is { } otherUnique)
				{
					return FormatUniqueSocketError(errorType, otherUnique);
				}

				IEnumerable<GemDefinition> checkLocalInventory = checkAgainst;
				if (uniquePowers.Contains(Uniqueness.Item) && !uniquePowers.Contains(Uniqueness.All))
				{
					GemInfo info = GemStoneSetup.GemInfos[socketName];
					checkLocalInventory = GemStoneSetup.Gems[info.Type];
					errorType = new List<string> { "$jc_equipped_unique_error_item", Localization.instance.Localize((GemStoneSetup.uncutGems.TryGetValue(info.Type, out GameObject uncutGem) ? uncutGem : individualSocket).GetComponent<ItemDrop>().m_itemData.m_shared.m_name) };
				}
				else if (checkAgainst.Count == 0)
				{
					GemInfo info = GemStoneSetup.GemInfos[socketName];
					checkLocalInventory = new List<GemDefinition> { GemStoneSetup.Gems[info.Type][info.Tier - 1] };
					errorType = new List<string> { "$jc_equipped_unique_error_none", Localization.instance.Localize(individualSocket.GetComponent<ItemDrop>().m_itemData.m_shared.m_name) };
				}

				foreach (GameObject gem in checkLocalInventory.SelectMany(EnumerateWithMergedGems))
				{
					if (targetItem.Get<Sockets>() is { } itemSockets)
					{
						int gemIndex = itemSockets.socketedGems.IndexOf(new SocketItem(gem.name));
						if (replacedOffset != gemIndex && gemIndex != -1)
						{
							return FormatUniqueSocketError(errorType, targetItem.ItemData.m_shared.m_name);
						}
					}
				}
			}
		}

		return null;
	}

	private static IEnumerable<GameObject> EnumerateWithMergedGems(GemDefinition def)
	{
		yield return def.Prefab;

		GemInfo info = GemStoneSetup.GemInfos[def.Name];
		if (MergedGemStoneSetup.mergedGems.TryGetValue(info.Type, out Dictionary<GemType, GameObject[]> merged))
		{
			foreach (GameObject[] mergedGems in merged.Values)
			{
				yield return mergedGems[info.Tier - 1];
			}

			foreach (Dictionary<GemType, GameObject[]> firstMerged in MergedGemStoneSetup.mergedGems.Values)
			{
				if (firstMerged.TryGetValue(info.Type, out GameObject[] mergedGems))
				{
					yield return mergedGems[info.Tier - 1];
				}
			}
		}
	}

	private static string? HasEquippedAnyUniqueGem(IEnumerable<GemDefinition> checkAgainst, ItemInfo? ignoreContainer = null)
	{
		foreach (GameObject gem in checkAgainst.SelectMany(EnumerateWithMergedGems))
		{
			if (HasEquippedUniqueGem(gem.name, ignoreContainer) is { } otherUnique)
			{
				return otherUnique;
			}
		}

		return null;
	}

	private static string? HasEquippedUniqueGem(string gem, ItemInfo? ignoreContainer)
	{
		string?[] alreadyEquipped = new string[1];
		Utils.ApplyToAllPlayerItems(Player.m_localPlayer, slot =>
		{
			if (slot?.Data() != ignoreContainer && slot?.Data().Get<Sockets>() is { } itemSockets)
			{
				if (itemSockets.socketedGems.Contains(new SocketItem(gem)))
				{
					alreadyEquipped[0] = slot.m_shared.m_name;
				}
			}
		});
		return alreadyEquipped[0];
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
	private class PreventMovingInvalidItemsOrDestroyOnMove
	{
		private static bool Prefix(InventoryGrid grid, ref ItemDrop.ItemData? item, InventoryGrid.Modifier mod)
		{
			if (item is not null && mod == InventoryGrid.Modifier.Move && AddFakeSocketsContainer.openInventory == grid.m_inventory)
			{
				// Moving outside of container, into inventory
				if (AddFakeSocketsContainer.openEquipment?.Get<ItemContainer>() is ItemBag)
				{
					return true;
				}

				if (!AllowsUnsocketing(item))
				{
					return false;
				}
				if (ShallDestroyGem(item, grid.m_inventory))
				{
					item = null;
				}
			}
			else if (item is not null && mod == InventoryGrid.Modifier.Move && AddFakeSocketsContainer.openInventory is not null && AddFakeSocketsContainer.openInventory != grid.m_inventory)
			{
				// Moving into container
				if (AddFakeSocketsContainer.openEquipment?.Get<Frame>() is not null)
				{
					HandleSocketingFrameAndMirrors(AddFakeSocketsContainer.openEquipment.ItemData, item);
					return false;
				}
				if (AddFakeSocketsContainer.openEquipment?.Get<ItemContainer>() is ItemBag bag)
				{
					return bag is SocketBag ? Utils.ItemAllowedInGemBag(item) : Utils.ItemAllowedInGemBox(item);
				}
				if (!ItemEligibleForSocketing(item))
				{
					return false;
				}
				if (CanAddUniqueSocket(item.m_dropPrefab, -2) is { } error)
				{
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, error);
					return false;
				}
				if (item.m_stack > 1)
				{
					Vector2i emptySlot = grid.m_inventory.FindEmptySlot(false);
					return grid.m_inventory.AddItem(item, item.m_stack - 1, emptySlot.x, emptySlot.y);
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropItem))]
	private static class PreventThrowingGemsOnGround
	{
		private static bool Prefix(ref bool __result, Inventory inventory)
		{
			if (inventory == AddFakeSocketsContainer.openInventory && AddFakeSocketsContainer.openEquipment?.Get<ItemContainer>() is not ItemBag)
			{
				__result = false;
				return false;
			}

			return true;
		}
	}

	private static bool IsEquippedItem(Humanoid human, ItemDrop.ItemData item) => human.IsItemEquiped(item) || human.m_hiddenLeftItem == item || human.m_hiddenRightItem == item;

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
	private class PreventEquippingMultipleUniqueGems
	{
		private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
		{
			if (IsEquippedItem(__instance, item) || item.Data().Get<Sockets>() is not { } itemSockets)
			{
				return true;
			}

			GemLocation location = Utils.GetGemLocation(item.m_shared);
			foreach (string gem in itemSockets.socketedGems.Select(s => s.Name))
			{
				if (Jewelcrafting.EffectPowers.TryGetValue(gem.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(location, out List<EffectPower> effectPowers))
				{
					HashSet<Uniqueness> uniquePowers = new(effectPowers.Select(e => e.Unique));
					List<GemDefinition> checkAgainst = EnumerateUniqueGemsToCheckAgainst(ObjectDB.instance.GetItemPrefab(gem).GetComponent<ItemDrop>().m_itemData.m_shared.m_name, uniquePowers, out List<string> errorType);
					if (HasEquippedAnyUniqueGem(checkAgainst) is { } equippedUnique)
					{
						Player.m_localPlayer.Message(MessageHud.MessageType.Center, FormatUniqueSocketError(errorType, equippedUnique));
						__result = false;
						return false;
					}
				}
			}

			return true;
		}
	}

	private static bool GemHasEffectInOpenEquipment(ItemDrop.ItemData item) => AddFakeSocketsContainer.openEquipment is null || (Jewelcrafting.EffectPowers.TryGetValue(item.m_dropPrefab.name.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(Utils.GetGemLocation(AddFakeSocketsContainer.openEquipment.ItemData.m_shared), out List<EffectPower> powers) && powers.Count != 0);

	private static bool ShallDestroyGem(ItemDrop.ItemData item, Inventory inventory)
	{
		if (AddFakeSocketsContainer.openEquipment?.Get<Box>() is not null)
		{
			return false;
		}

		if (socketableGemStones.Contains(item.m_shared.m_name))
		{
			if (!GemHasEffectInOpenEquipment(item))
			{
				return false;
			}

			float chance;
			List<GemInfo> gemInfos;
			if (GemStoneSetup.GemInfos.TryGetValue(item.m_shared.m_name, out GemInfo info))
			{
				if (!GemStoneSetup.shardColors.ContainsKey(info.Type))
				{
					// unique gem
					return false;
				}

				gemInfos = new List<GemInfo> { info };

				chance = (info.Tier switch
				{
					1 => Jewelcrafting.breakChanceUnsocketSimple,
					2 => Jewelcrafting.breakChanceUnsocketAdvanced,
					3 => Jewelcrafting.breakChanceUnsocketPerfect,
					_ => throw new IndexOutOfRangeException($"Found unexpected tier {info.Tier}"),
				}).Value / 100f;
			}
			else
			{
				if (!MergedGemStoneSetup.mergedGemContents.TryGetValue(item.m_dropPrefab.name, out gemInfos))
				{
					gemInfos = new List<GemInfo>();
				}
				chance = Jewelcrafting.breakChanceUnsocketMerged.Value / 100f;
			}
			if (chance > Random.value)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$jc_gemstone_remove_fail");
				inventory.RemoveItem(item);

				foreach (GemInfo gemInfo in gemInfos)
				{
					if (GemStoneSetup.shardColors.TryGetValue(gemInfo.Type, out GameObject shard))
					{
						if (!Player.m_localPlayer.m_inventory.AddItem(shard, 1))
						{
							Transform transform = Player.m_localPlayer.transform;
							Vector3 position = transform.position;
							ItemDrop itemDrop = ItemDrop.DropItem(shard.GetComponent<ItemDrop>().m_itemData, 1, position + transform.forward + transform.up, transform.rotation);
							itemDrop.OnPlayerDrop();
							itemDrop.GetComponent<Rigidbody>().velocity = (transform.forward + Vector3.up) * 5f;
							Player.m_localPlayer.m_dropEffects.Create(position, Quaternion.identity);
						}
					}
				}

				return true;
			}
		}

		return false;
	}
}
