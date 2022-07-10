using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
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
	public static readonly HashSet<string> socketableGemStones = new();
	public static readonly Dictionary<string, GameObject> gemToShard = new();
	public static Sprite emptySocketSprite = null!;
	public static readonly Dictionary<string, GameObject> bossToGem = new();

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

		public static bool TabOpen() => !tab.GetComponent<Button>().interactable;
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
			if (__instance.m_selectedRecipe.Value?.Extended()?.GetComponent<Socketable>() is { } sockets)
			{
				for (int i = 0; i < sockets.socketedGems.Count; ++i)
				{
					AddSocketIcons.socketIcons[i].SetActive(true);
					AddSocketIcons.socketIcons[i].GetComponent<Image>().sprite = ObjectDB.instance.GetItemPrefab(sockets.socketedGems[i])?.GetComponent<ItemDrop>().m_itemData.GetIcon() ?? emptySocketSprite;
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
				__instance.m_craftButton.interactable = activeRecipe is not null && CanAddMoreSockets(activeRecipe);
				int socketNumber = Math.Min(activeRecipe.Extended()?.GetComponent<Sockets>()?.socketedGems.Count ?? 0, 4);
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

				if (__instance.m_selectedRecipe.Key is { } recipe && Jewelcrafting.gemUpgradeChances.TryGetValue(recipe.m_item.m_itemData.m_shared.m_name, out ConfigEntry<float> chance) && recipe.m_resources[0].m_amount == 1)
				{
					__instance.m_itemCraftType.horizontalOverflow = HorizontalWrapMode.Wrap;
					__instance.m_itemCraftType.text = Localization.instance.Localize("$jc_gem_cutting_warning", Math.Min(Mathf.RoundToInt(chance.Value * (1 + Player.m_localPlayer.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f)), 100).ToString());

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
		return item.Extended()?.GetComponent<Box>() is null && (item.Extended()?.GetComponent<Sockets>()?.socketedGems.Count ?? 0) < Jewelcrafting.maximumNumberSockets.Value;
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
				foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.GetInventory().m_inventory.Where(i => Utils.IsSocketableItem(i.m_shared) || i.Extended()?.GetComponent<Socketable>() is { boxSealed: false }))
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

	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class ItemSharedMap
	{
		public static readonly Dictionary<string, GameObject> items = new();

		private static void Postfix(ObjectDB __instance)
		{
			items.Clear();
			foreach (GameObject item in __instance.m_items)
			{
				items[item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name] = item;
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

			Player.m_localPlayer.RaiseSkill("Jewelcrafting");

			int socketNumber = Math.Min(__instance.m_craftUpgradeItem.Extended()?.GetComponent<Sockets>()?.socketedGems.Count ?? 0, 4);

			if (!Player.m_localPlayer.m_noPlacementCost && Random.value > Jewelcrafting.socketAddingChances[socketNumber].Value / 100f * (1 + Player.m_localPlayer.GetSkillFactor("Jewelcrafting") * Jewelcrafting.upgradeChanceIncrease.Value / 100f))
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
			if (item.Extended()?.GetComponent<Socketable>() is not null && Jewelcrafting.socketSystem.Value == Jewelcrafting.Toggle.On)
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
			if (UITooltip.m_tooltip is not null && __instance.m_tooltipPrefab == GemStoneSetup.SocketTooltip && DisplaySocketTooltip.tooltipItem.TryGetValue(__instance, out ExtendedItemData item) && item.GetComponent<Socketable>() is { } sockets)
			{
				if (UITooltip.m_tooltip.transform.Find("Bkg (1)/Transmute_Press_Interact") is { } interact)
				{
					string text;
					if (Jewelcrafting.inventoryInteractBehaviour.Value != Jewelcrafting.InteractBehaviour.Enabled && (Jewelcrafting.inventorySocketing.Value == Jewelcrafting.Toggle.On || (Player.m_localPlayer?.GetCurrentCraftingStation() is { } craftingStation && global::Utils.GetPrefabName(craftingStation.gameObject) == "op_transmution_table")))
					{
						text = Localization.instance.Localize(sockets is Box { progress: >= 100 } ? "$jc_gembox_interact_finished" : "$jc_press_interact", Localization.instance.Localize("<color=yellow><b>$KEY_Use</b></color>"));
					}
					else
					{
						text = Localization.instance.Localize("$jc_table_required");
					}
					interact.GetComponent<Text>().text = text;
					interact.gameObject.SetActive(!sockets.boxSealed);
				}

				int numSockets = 0;
				if (sockets is not Box { progress: >= 100 })
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
							string socket = sockets.socketedGems[i - 1];
							string text = "$jc_empty_socket_text";
							Sprite? sprite = null;
							if (ObjectDB.instance.GetItemPrefab(socket) is { } gameObject)
							{
								if (sockets is not Box)
								{
									if (Jewelcrafting.EffectPowers.TryGetValue(socket.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(Utils.GetGemLocation(item.m_shared), out List<EffectPower> effectPowers))
									{
										text = string.Join("\n", effectPowers.Select(gem => $"$jc_effect_{EffectDef.EffectNames[gem.Effect].ToLower()} {gem.Power}"));
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
					HashSet<Effect> allEffects = collectEffects(GemLocation.All);
					
					foreach (GemLocation gemLocation in gem.Keys.OrderByDescending(g => (int)g))
					{
						List<EffectPower> specificEffects = gem[gemLocation].Where(p => ((gemLocation & EffectDef.WeaponGemlocations) == 0 || !weaponEffects.Contains(p.Effect)) && (gemLocation == GemLocation.All || !allEffects.Contains(p.Effect))).ToList();
						if (specificEffects.Count > 0)
						{
							sb.Append($"\n<color=orange>$jc_socket_slot_{gemLocation.ToString().ToLower()}:</color> {string.Join(", ", specificEffects.Select(effectPower => $"$jc_effect_{EffectDef.EffectNames[effectPower.Effect].ToLower()} {effectPower.Power}"))}");
						}
					}

					string flavorText = $"{item.m_shared.m_description.Substring(1)}_flavor";
					if (Localization.instance.m_translations.ContainsKey(flavorText))
					{
						sb.Append($"\n\n<color=purple>${flavorText}</color>");
					}
					__result += sb.ToString();
				}
				if (FusionBoxSetup.boxTier.TryGetValue(prefab, out int tier) && item.Extended()?.GetComponent<Box>() is { } box)
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
							sb.Append($"\n\n$jc_merge_success_chance: <color=orange>{mergeChances[box.Tier].Value}%</color>");
						}
						else
						{
							sb.Append("\n\n$jc_merge_success_chances:");
							sb.Append($"\n$jc_gem_tier_1: <color=orange>{mergeChances[0].Value}%</color>");
							sb.Append($"\n$jc_gem_tier_2: <color=orange>{mergeChances[1].Value}%</color>");
							sb.Append($"\n$jc_gem_tier_3: <color=orange>{mergeChances[2].Value}%</color>");
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
			if (openEquipment?.GetComponent<Socketable>() is not { } sockets)
			{
				// Socket component may have been removed upon item upgrade
				return;
			}

			List<string> gems = sockets.socketedGems;

			for (int i = 0; i < gems.Count; ++i)
			{
				gems[i] = "";
			}
			foreach (ItemDrop.ItemData item in openInventory!.m_inventory)
			{
				gems[item.m_gridPos.x] = item.m_dropPrefab.name;
			}
			openEquipment.Save();

			if (sockets is Box { progress: >= 100 } && gems.All(g => g == ""))
			{
				openEquipment.RemoveComponent<Box>();
				ItemDrop.ItemData deleteBox = openEquipment;
				IEnumerator DelayedBoxDelete()
				{
					yield return null;
					InventoryGui.instance.CloseContainer();
					yield return null;
					Player.m_localPlayer.GetInventory().RemoveItem(deleteBox);
				}

				InventoryGui.instance.StartCoroutine(DelayedBoxDelete());
			}

			TrackEquipmentChanges.CalculateEffects();
		}
	}

	[HarmonyPatch]
	private class CloseFakeSocketsContainer
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.Hide)),
			AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.CloseContainer))
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

				FusionBoxSetup.AddSealButton.SealButton.SetActive(false);

				AddFakeSocketsContainer.openEquipment = null;
				AddFakeSocketsContainer.openInventory = null;
			}
		}
	}

	private static Inventory ReadSocketsInventory(Socketable sockets)
	{
		Inventory inv = new("Sockets", Player.m_localPlayer.GetInventory().m_bkg, sockets.socketedGems.Count, 1);
		int slot = 0;
		foreach (string gem in sockets.socketedGems)
		{
			if (gem != "" && ObjectDB.instance.GetItemPrefab(gem) is { } prefab)
			{
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

			if (extended?.GetComponent<Socketable>() is { boxSealed: false } sockets)
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
			if (Jewelcrafting.inventorySocketing.Value == Jewelcrafting.Toggle.On || (Player.m_localPlayer?.GetCurrentCraftingStation() is { } craftingStation && global::Utils.GetPrefabName(craftingStation.gameObject) == "op_transmution_table"))
			{
				ItemDrop.ItemData? item = null;
				if (Jewelcrafting.inventoryInteractBehaviour.Value != Jewelcrafting.InteractBehaviour.Enabled && (ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")))
				{
					Vector2 pos = Input.mousePosition;
					item = __instance.m_playerGrid.GetItem(new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)))?.Extended();
				}
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
			if (Jewelcrafting.socketSystem.Value == Jewelcrafting.Toggle.On && (ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")) && RectTransformUtility.RectangleContainsScreenPoint(invGui.m_playerGrid.m_gridRoot, Input.mousePosition))
			{
				if (Jewelcrafting.inventoryInteractBehaviour.Value == Jewelcrafting.InteractBehaviour.Disabled)
				{
					return true;
				}

				Vector2 pos = Input.mousePosition;
				return invGui.m_playerGrid.GetItem(new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)))?.Extended()?.GetComponent<Socketable>() is not null;
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
						if (!socketableGemStones.Contains(existingItem.m_shared.m_name) || AddFakeSocketsContainer.openEquipment?.GetComponent<Box>() is { progress: >= 100 })
						{
							__result = false;
							return false;
						}

						if (CanAddUniqueSocket(existingItem.m_dropPrefab, item.m_gridPos.x) is { } equippedUnique)
						{
							Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_equipped_unique_gem", equippedUnique));
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

			if (socketableGemStones.Contains(item.m_shared.m_name) && AddFakeSocketsContainer.openEquipment?.GetComponent<Box>() is not { progress: >= 100 })
			{
				ItemDrop.ItemData? oldItem = __instance.m_inventory.GetItemAt(pos.x, pos.y);
				if (oldItem == item)
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

	private static string? CanAddUniqueSocket(GameObject socket, int replacedOffset)
	{
		if (AddFakeSocketsContainer.openEquipment is ItemDrop.ItemData targetItem && Jewelcrafting.EffectPowers.TryGetValue(socket.name.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(Utils.GetGemLocation(targetItem.m_shared), out List<EffectPower> effectPowers) && effectPowers.Any(e => e.Unique))
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
				if (!socketableGemStones.Contains(item.m_shared.m_name) || AddFakeSocketsContainer.openInventory.HaveItem(item.m_shared.m_name) || item.m_stack > 1 || AddFakeSocketsContainer.openEquipment?.GetComponent<Box>() is { progress: >= 100 })
				{
					return false;
				}
				if (CanAddUniqueSocket(item.m_dropPrefab, -2) is { } equippedUnique)
				{
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_equipped_unique_gem", equippedUnique));
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
				if (Jewelcrafting.EffectPowers.TryGetValue(gem.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(location, out List<EffectPower> effectPowers) && effectPowers.Any(e => e.Unique))
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

	private static bool ShallDestroyGem(ItemDrop.ItemData item, Inventory inventory)
	{
		if (AddFakeSocketsContainer.openEquipment?.GetComponent<Box>() is not null)
		{
			return false;
		}

		if (socketableGemStones.Contains(item.m_shared.m_name))
		{
			float chance;
			if (GemStoneSetup.GemInfos.TryGetValue(item.m_shared.m_name, out GemInfo info))
			{
				if (!GemStoneSetup.shardColors.ContainsKey(info.Type))
				{
					// unique gem
					return false;
				}

				chance = (info.Tier switch
				{
					1 => Jewelcrafting.breakChanceUnsocketSimple,
					2 => Jewelcrafting.breakChanceUnsocketAdvanced,
					3 => Jewelcrafting.breakChanceUnsocketPerfect,
					_ => throw new IndexOutOfRangeException($"Found unexpected tier {info.Tier}")
				}).Value / 100f;
			}
			else
			{
				chance = Jewelcrafting.breakChanceUnsocketMerged.Value / 100f;
			}
			if (chance > Random.value)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$jc_gemstone_remove_fail");
				inventory.RemoveItem(item);
				return true;
			}
		}

		return false;
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
	private class DestroyGemItemOnMove
	{
		private static void Prefix(InventoryGrid grid, ref ItemDrop.ItemData? item, InventoryGrid.Modifier mod)
		{
			if (item is not null && grid.m_inventory == AddFakeSocketsContainer.openInventory && mod == InventoryGrid.Modifier.Move && ShallDestroyGem(item, grid.m_inventory))
			{
				item = null;
			}
		}
	}
}
