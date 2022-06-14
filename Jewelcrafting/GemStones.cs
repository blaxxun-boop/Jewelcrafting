using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using ExtendedItemDataFramework;
using HarmonyLib;
using Jewelcrafting.GemEffects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using SkillManager;

namespace Jewelcrafting;

public static class GemStones
{
	public static readonly List<GameObject> socketableGemStones = new();
	public static readonly Dictionary<string, GameObject> gemToShard = new();
	public static Sprite emptySocketSprite = null!;
	public static readonly Dictionary<string, GameObject> bossToGem = new();

	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
	private class AddGemStonesToDrops
	{
		private static void Postfix(List<KeyValuePair<GameObject, int>> __result)
		{
			if (Jewelcrafting.socketSystem.Value == Jewelcrafting.Toggle.On)
			{
				__result.AddRange(from gem in Jewelcrafting.gemDropChances.Keys where Random.value < Jewelcrafting.gemDropChances[gem].Value / 100f select new KeyValuePair<GameObject, int>(gem, 1));
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
	private class TryUpgradingGems
	{
		private static bool Prefix(InventoryGui __instance, Player player)
		{
			if (Jewelcrafting.gemUpgradeChances.TryGetValue(__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_name, out ConfigEntry<float> upgradeChance) && __instance.m_craftRecipe.m_resources[0].m_amount == 1)
			{
				player.RaiseSkill("Jewelcrafting");

				if (!Player.m_localPlayer.m_noPlacementCost && Random.value > upgradeChance.Value / 100f * (1 + player.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f))
				{
					if (player.HaveRequirements(__instance.m_craftRecipe, false, 1))
					{
						player.ConsumeResources(__instance.m_craftRecipe.m_resources, 1);
						player.m_inventory.AddItem(gemToShard[__instance.m_craftRecipe.m_item.name], 1);
						player.Message(MessageHud.MessageType.Center, "$jc_gemstone_cut_fail");

						__instance.UpdateCraftingPanel();

						return false;
					}
				}
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
	public class AddSocketAddingTab
	{
		public static Transform tab = null!;
		private static readonly UnityAction clearInteractable = () =>
		{
			tab.GetComponent<Button>().interactable = true;
			InventoryGui.instance.UpdateCraftingPanel();
		};

		[HarmonyPriority(Priority.High)]
		private static void Postfix(InventoryGui __instance)
		{
			tab = Object.Instantiate(__instance.m_tabUpgrade.gameObject, __instance.m_tabUpgrade.transform.parent).transform;
			tab.Find("Text").GetComponent<Text>().text = "SOCKET";
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
			});
			tab.GetComponent<Button>().onClick = buttonClick;
		}

		public static bool TabOpen() => !tab.GetComponent<Button>().interactable;
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
	private class LimitTabToGemCutterTable
	{
		private static void Postfix()
		{
			AddSocketAddingTab.tab.gameObject.SetActive(Player.m_localPlayer.m_currentStation?.name.StartsWith("op_transmution_table", StringComparison.Ordinal) ?? false);
			if (!AddSocketAddingTab.tab.gameObject.activeSelf)
			{
				AddSocketAddingTab.tab.GetComponent<Button>().interactable = true;
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

		private static void Postfix(InventoryGui __instance)
		{
			RectTransform craftTypeRect = __instance.m_itemCraftType.GetComponent<RectTransform>();
			Vector2 anchoredPosition = craftTypeRect.anchoredPosition;

			foreach (GameObject socketIcon in AddSocketIcons.socketIcons)
			{
				socketIcon.SetActive(false);
			}
			if (__instance.m_selectedRecipe.Value?.Extended()?.GetComponent<Sockets>() is { } sockets)
			{
				for (int i = 0; i < sockets.socketedGems.Count; ++i)
				{
					AddSocketIcons.socketIcons[i].SetActive(true);
					AddSocketIcons.socketIcons[i].GetComponent<Image>().sprite = ObjectDB.instance.GetItemPrefab(sockets.socketedGems[i])?.GetComponent<ItemDrop>().m_itemData.GetIcon() ?? emptySocketSprite;
				}
				AddSocketIcons.socketingButton.gameObject.SetActive(true);
			}
			else
			{
				AddSocketIcons.socketingButton.gameObject.SetActive(false);
			}

			if (AddSocketAddingTab.TabOpen() && __instance.m_selectedRecipe.Value is { } activeRecipe)
			{
				__instance.m_recipeRequirementList[0].transform.parent.gameObject.SetActive(false);
				__instance.m_craftButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$jc_add_socket_button");
				__instance.m_craftButton.interactable = activeRecipe is not null && CanAddMoreSockets(activeRecipe);
				__instance.m_itemCraftType.text = Localization.instance.Localize("$jc_socket_adding_warning", Math.Min(Mathf.RoundToInt(Jewelcrafting.chanceToAddSocket.Value * (1 + Player.m_localPlayer.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f)), 100).ToString());
				if (craftTypeRect.pivot.y != 1)
				{
					Vector2 sizeDelta = craftTypeRect.sizeDelta;
					originalCraftSize = sizeDelta.y;
					sizeDelta = new Vector2(sizeDelta.x, 67);
					craftTypeRect.sizeDelta = sizeDelta;
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

				if (__instance.m_selectedRecipe.Key is { } recipe && Jewelcrafting.gemUpgradeChances.TryGetValue(recipe.m_item.m_itemData.m_shared.m_name, out ConfigEntry<float> chance) && recipe.m_resources[0].m_amount == 1)
				{
					__instance.m_itemCraftType.horizontalOverflow = HorizontalWrapMode.Wrap;
					__instance.m_itemCraftType.text = Localization.instance.Localize("$jc_gem_cutting_warning", Math.Min(Mathf.RoundToInt(chance.Value * (1 + Player.m_localPlayer.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f)), 100).ToString());

					__instance.m_itemCraftType.gameObject.SetActive(true);
					displayGemChance = true;
				}
				else if (displayGemChance)
				{
					__instance.m_itemCraftType.horizontalOverflow = HorizontalWrapMode.Overflow;
					displayGemChance = false;
				}
			}
		}
	}

	private static bool CanAddMoreSockets(ItemDrop.ItemData item)
	{
		return (item.Extended()?.GetComponent<Sockets>()?.socketedGems.Count ?? 0) < Jewelcrafting.maximumNumberSockets.Value;
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
	private class AddItemsToRecipeList
	{
		private static bool Prefix(InventoryGui __instance)
		{
			if (AddSocketAddingTab.TabOpen())
			{
				__instance.m_availableRecipes.Clear();
				foreach (GameObject recipe in __instance.m_recipeList)
				{
					Object.Destroy(recipe);
				}

				__instance.m_recipeList.Clear();
				foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.GetInventory().m_inventory.Where(i => Utils.IsSocketableItem(i.m_shared)))
				{
					ItemDrop component = Utils.Clone(itemData.m_dropPrefab.GetComponent<ItemDrop>());
					component.m_itemData = itemData;
					Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
					recipe.m_item = component;
					__instance.AddRecipeToList(Player.m_localPlayer, recipe, itemData, CanAddMoreSockets(itemData));
				}

				__instance.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(__instance.m_recipeListBaseSize, __instance.m_recipeList.Count * __instance.m_recipeListSpace));

				return false;
			}

			return true;
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

			Player.m_localPlayer.RaiseSkill("Jewelcrafting");

			if (!Player.m_localPlayer.m_noPlacementCost && Random.value > Jewelcrafting.chanceToAddSocket.Value / 100f * (1 + Player.m_localPlayer.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f))
			{
				if (__instance.m_craftUpgradeItem.Extended()?.GetComponent<Sockets>() is { } sockets)
				{
					Inventory itemInventory = ReadSocketsInventory(sockets);

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

				if (Jewelcrafting.resourceReturnRate.Value > 0 && ObjectDB.instance.GetRecipe(__instance.m_craftUpgradeItem) is { } recipe)
				{
					foreach (Piece.Requirement requirement in recipe.m_resources)
					{
						int amount = Mathf.FloorToInt(Random.value + requirement.m_amount * (Jewelcrafting.resourceReturnRate.Value / 100f));
						if (amount > 0 && !Player.m_localPlayer.m_inventory.AddItem(requirement.m_resItem.m_itemData.m_dropPrefab, amount))
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
				ExtendedItemData extended = __instance.m_craftUpgradeItem.Extended();
				if (extended.GetComponent<Sockets>() is not { } sockets)
				{
					extended.AddComponent<Sockets>();
				}
				else
				{
					sockets.socketedGems.Add("");
				}
				extended.Save();

				Player.m_localPlayer.GetCurrentCraftingStation().m_craftItemDoneEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);

				__instance.UpdateCraftingPanel();

				__instance.SetRecipe(recipeIndex, false);
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
	private class DisplaySocketTooltip
	{
		private static GameObject originalTooltip = null!;
		public static readonly ConditionalWeakTable<UITooltip, ExtendedItemData> tooltipItem = new();

		private static void Postfix(ItemDrop.ItemData item, UITooltip tooltip)
		{
			if (item.Extended()?.GetComponent<Sockets>() is not null && Jewelcrafting.socketSystem.Value == Jewelcrafting.Toggle.On)
			{
				if (tooltip.m_tooltipPrefab != GemStoneSetup.SocketTooltip)
				{
					originalTooltip = tooltip.m_tooltipPrefab;
					tooltip.m_tooltipPrefab = GemStoneSetup.SocketTooltip;
				}
				tooltipItem.Remove(tooltip);
				tooltipItem.Add(tooltip, item.Extended());
			}
			else if (tooltip.m_tooltipPrefab == GemStoneSetup.SocketTooltip)
			{
				tooltip.m_tooltipPrefab = originalTooltip;
				tooltipItem.Remove(tooltip);
			}
		}
	}

	[HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
	private class UpdateSocketTooltip
	{
		private static void Postfix(UITooltip __instance)
		{
			if (UITooltip.m_tooltip is not null && __instance.m_tooltipPrefab == GemStoneSetup.SocketTooltip && DisplaySocketTooltip.tooltipItem.TryGetValue(__instance, out ExtendedItemData item) && item.GetComponent<Sockets>() is { } sockets)
			{
				if (UITooltip.m_tooltip.transform.Find("Bkg (1)/Transmute_Press_Interact") is { } interact)
				{
					interact.GetComponent<Text>().text = Localization.instance.Localize("$jc_press_interact", Localization.instance.Localize("<color=yellow><b>$KEY_Use</b></color>"));
				}

				int numSockets = sockets.socketedGems.Count;
				for (int i = 1; i <= 5; ++i)
				{
					if (UITooltip.m_tooltip.transform.Find($"Bkg (1)/TrannyHoles/Transmute_{i}") is { } transmute)
					{
						transmute.gameObject.SetActive(i <= numSockets);
						if (i <= numSockets)
						{
							string socket = sockets.socketedGems[i - 1];
							string text = "$jc_empty_socket_text";
							Sprite sprite = emptySocketSprite;
							if (ObjectDB.instance.GetItemPrefab(socket) is { } gameObject)
							{
								if (Jewelcrafting.EffectPowers.TryGetValue(socket.GetStableHashCode(), out Dictionary<GemLocation, EffectPower> locationPowers) && locationPowers.TryGetValue(Utils.GetGemLocation(item.m_shared), out EffectPower gem))
								{
									text = $"$jc_effect_{gem.Effect.ToString().ToLower()} {gem.Power}";
								}
								else
								{
									text = "$jc_effect_no_effect";
								}
								sprite = gameObject.GetComponent<ItemDrop>().m_itemData.GetIcon();
							}
							transmute.Find($"Transmute_Text_{i}").GetComponent<Text>().text = Localization.instance.Localize(text);
							transmute.GetComponent<Image>().sprite = sprite;
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool))]
	private class DisplayEffectsOnGems
	{
		private static void Postfix(ItemDrop.ItemData item, ref string __result)
		{
			if (item.m_dropPrefab is { } prefab && Jewelcrafting.EffectPowers.TryGetValue(prefab.name.GetStableHashCode(), out Dictionary<GemLocation, EffectPower> gem))
			{
				__result += "\n";

				GemLocation seenGems = 0;
				foreach (GemLocation gemLocation in gem.Keys.OrderByDescending(g => (int)g))
				{
					if ((seenGems & gemLocation) == 0)
					{
						__result += $"\n<color=orange>{gemLocation}:</color> {Localization.instance.Localize($"$jc_effect_{gem[gemLocation].Effect.ToString().ToLower()}")} {gem[gemLocation].Power}";
						seenGems |= gemLocation;
					}
				}
			}
		}
	}

	[HarmonyPatch]
	public class AddFakeSocketsContainer
	{
		public static ExtendedItemData? openEquipment;
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
			List<string> gems = openEquipment!.GetComponent<Sockets>().socketedGems;
			for (int i = 0; i < gems.Count; ++i)
			{
				gems[i] = "";
			}
			foreach (ItemDrop.ItemData item in openInventory!.m_inventory)
			{
				gems[item.m_gridPos.x] = item.m_dropPrefab.name;
			}
			openEquipment.Save();

			TrackEquipmentChanges.CalculateEffects();
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
				AddFakeSocketsContainer.SaveGems();

				RectTransform takeAllButton = (RectTransform)__instance.m_takeAllButton.transform;
				Vector2 anchoredPosition = takeAllButton.anchoredPosition;
				anchoredPosition = new Vector2(anchoredPosition.x, -anchoredPosition.y);
				takeAllButton.anchoredPosition = anchoredPosition;

				AddFakeSocketsContainer.openEquipment = null;
				AddFakeSocketsContainer.openInventory = null;
			}
		}
	}

	private static Inventory ReadSocketsInventory(Sockets sockets)
	{
		Inventory inv = new("Sockets", Player.m_localPlayer.GetInventory().m_bkg, sockets.socketedGems.Count, 1);
		int slot = 0;
		foreach (string gem in sockets.socketedGems)
		{
			if (gem != "")
			{
				GameObject prefab = ObjectDB.instance.GetItemPrefab(gem);
				ItemDrop.ItemData itemData = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
				itemData.m_dropPrefab = prefab;
				itemData.m_stack = 1;
				itemData.m_gridPos = new Vector2i(slot, 0);
				inv.m_inventory.Add(itemData);
			}
			++slot;
		}
		return inv;
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
	public class OpenFakeSocketsContainer
	{
		public static bool Open(InventoryGui invGui, ItemDrop.ItemData? item)
		{
			ExtendedItemData? extended = item?.Extended();

			if (extended?.GetComponent<Sockets>() is { } sockets)
			{
				if (invGui.IsContainerOpen())
				{
					invGui.CloseContainer();
				}

				RectTransform takeAllButton = (RectTransform)invGui.m_takeAllButton.transform;
				Vector2 anchoredPosition = takeAllButton.anchoredPosition;
				anchoredPosition = new Vector2(anchoredPosition.x, -anchoredPosition.y);
				takeAllButton.anchoredPosition = anchoredPosition;

				Inventory inv = ReadSocketsInventory(sockets);
				inv.m_onChanged += AddFakeSocketsContainer.SaveGems;

				AddFakeSocketsContainer.openEquipment = extended;
				AddFakeSocketsContainer.openInventory = inv;
			}

			if (AddFakeSocketsContainer.openInventory is { } inventory)
			{
				ExtendedItemData weapon = AddFakeSocketsContainer.openEquipment!;
				if (invGui.m_playerGrid.GetInventory().GetItemAt(weapon.m_gridPos.x, weapon.m_gridPos.y)?.Extended() != weapon)
				{
					invGui.CloseContainer();
					return true;
				}

				invGui.m_container.gameObject.SetActive(true);
				invGui.m_containerGrid.UpdateInventory(inventory, null, invGui.m_dragItem);
				invGui.m_containerName.text = Localization.instance.Localize("$jc_socket_container_title", Localization.instance.Localize(weapon.m_shared.m_name));
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
			if (ZInput.GetButton("Use") || ZInput.GetButton("JoyUse"))
			{
				Vector2 pos = Input.mousePosition;
				item = __instance.m_playerGrid.GetItem(new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)))?.Extended();
			}
			return Open(__instance, item);
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
	private class CatchInventoryUseButton
	{
		private static bool ShallPreventInventoryClose()
		{
			return Jewelcrafting.socketSystem.Value == Jewelcrafting.Toggle.On && (ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")) && RectTransformUtility.RectangleContainsScreenPoint(InventoryGui.instance.m_playerGrid.m_gridRoot, Input.mousePosition);
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
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CatchInventoryUseButton), nameof(ShallPreventInventoryClose)));
					yield return new CodeInstruction(OpCodes.Brtrue, target!.Value);
				}
				yield return instructions[i];
			}
		}
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
					if (existingItem is not null && existingItem.m_dropPrefab != item.m_dropPrefab)
					{
						__result = false;
						return false;
					}
				}

				return true;
			}

			if (socketableGemStones.Contains(item.m_dropPrefab))
			{
				if (__instance.m_inventory.GetItemAt(pos.x, pos.y) == item)
				{
					return true;
				}

				if (__instance.m_inventory.HaveItem(item.m_shared.m_name))
				{
					__result = false;
					return false;
				}

				if (CanAddUniqueSocket(item.m_dropPrefab, pos.x) is { } equippedUnique)
				{
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_equipped_unique_gem", equippedUnique));
					__result = false;
					return false;
				}

				amount = 1;
				return true;
			}

			__result = false;
			return false;
		}
	}

	private static string? CanAddUniqueSocket(GameObject socket, int replacedOffset)
	{
		if (AddFakeSocketsContainer.openEquipment is ItemDrop.ItemData targetItem && Jewelcrafting.EffectPowers.TryGetValue(socket.name.GetStableHashCode(), out Dictionary<GemLocation, EffectPower> effectPowers) && effectPowers.TryGetValue(Utils.GetGemLocation(targetItem.m_shared), out EffectPower effectPower) && effectPower.Unique)
		{
			if (targetItem.m_equiped && HasEquippedAnyUniqueGem() is { } otherUnique)
			{
				return otherUnique;
			}

			foreach (EffectDef def in Jewelcrafting.SocketEffects.Values.SelectMany(e => e).Where(d => d.Unique))
			{
				foreach (GemDefinition gem in GemStoneSetup.Gems[def.Type])
				{
					if (targetItem.Extended()?.GetComponent<Sockets>() is { } itemSockets)
					{
						int gemIndex = itemSockets.socketedGems.IndexOf(gem.Prefab.name);
						if (replacedOffset != gemIndex && gemIndex != -1)
						{
							return targetItem.m_shared.m_name;
						}
					}
				}
			}
		}

		return null;
	}

	private static string? HasEquippedAnyUniqueGem()
	{
		foreach (EffectDef def in Jewelcrafting.SocketEffects.Values.SelectMany(e => e).Where(d => d.Unique))
		{
			foreach (GemDefinition gem in GemStoneSetup.Gems[def.Type])
			{
				if (HasEquippedUniqueGem(gem.Prefab.name) is { } otherUnique)
				{
					return otherUnique;
				}
			}
		}

		return null;
	}

	private static string? HasEquippedUniqueGem(string gem)
	{
		string?[] alreadyEquipped = new string[1];
		Utils.ApplyToAllPlayerItems(Player.m_localPlayer, slot =>
		{
			if (slot?.Extended() != AddFakeSocketsContainer.openEquipment && slot?.Extended()?.GetComponent<Sockets>() is { } itemSockets)
			{
				if (itemSockets.socketedGems.Contains(gem))
				{
					alreadyEquipped[0] = slot.m_shared.m_name;
				}
			}
		});
		return alreadyEquipped[0];
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
	private class PreventMovingInvalidItems
	{
		private static bool Prefix(InventoryGrid grid, ItemDrop.ItemData? item, InventoryGrid.Modifier mod)
		{
			if (item is not null && mod == InventoryGrid.Modifier.Move && AddFakeSocketsContainer.openInventory is not null && AddFakeSocketsContainer.openInventory != grid.m_inventory)
			{
				if (!socketableGemStones.Contains(item.m_dropPrefab) || AddFakeSocketsContainer.openInventory.HaveItem(item.m_shared.m_name) || item.m_stack > 1)
				{
					return false;
				}
				if (CanAddUniqueSocket(item.m_dropPrefab, -2) is { } equippedUnique)
				{
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_equipped_unique_gem", equippedUnique));
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
	private class PreventEquippingMultipleUniqueGems
	{
		private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
		{
			if (__instance.IsItemEquiped(item) || item.Extended()?.GetComponent<Sockets>() is not { } itemSockets)
			{
				return true;
			}

			GemLocation location = Utils.GetGemLocation(item.m_shared);
			foreach (string gem in itemSockets.socketedGems)
			{
				if (Jewelcrafting.EffectPowers.TryGetValue(gem.GetStableHashCode(), out Dictionary<GemLocation, EffectPower> effectPowers) && effectPowers.TryGetValue(location, out EffectPower effectPower) && effectPower.Unique)
				{
					if (HasEquippedAnyUniqueGem() is { } equippedUnique)
					{
						Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_equipped_unique_gem", equippedUnique));
						__result = false;
						return false;
					}
				}
			}

			return true;
		}
	}
}
