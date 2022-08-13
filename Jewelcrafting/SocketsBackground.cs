using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Jewelcrafting;

public static class SocketsBackground
{
	public static GameObject background = null!;
	private static GameObject? gridElementPrefab = null!;
	private static GameObject? hotkeyElementPrefab = null!;
	private static readonly Color[] colorSteps =
	{
		Color.green,
		Color.cyan,
		Color.magenta,
		new(1, 0.6f, 0)
	};
	private static Color[] colors = null!;
	private static int maxWorth;

	public static void CalculateColors()
	{
		maxWorth = Jewelcrafting.maximumNumberSockets.Value * 6 + Jewelcrafting.maximumNumberSockets.Value * (Jewelcrafting.maximumNumberSockets.Value + 1) / 2;
		colors = new Color[maxWorth + 1];
		for (int i = 0; i < maxWorth; ++i)
		{
			float currentStep = (float)i / maxWorth * (colorSteps.Length - 1);
			int prevStep = Mathf.FloorToInt(currentStep);
			colors[i] = Color.Lerp(colorSteps[prevStep], colorSteps[prevStep + 1], currentStep - prevStep);
		}
		colors[maxWorth] = colorSteps[colorSteps.Length - 1];
	}

	public static void UpdateSocketBackground()
	{
		foreach (HotkeyBar hotkeyBar in Resources.FindObjectsOfTypeAll<HotkeyBar>().Where(b => b.m_elementPrefab.scene.name is not null))
		{
			DoEquipedSwap(hotkeyBar.m_elementPrefab, true);
			foreach (HotkeyBar.ElementData elementData in hotkeyBar.m_elements)
			{
				Object.DestroyImmediate(elementData.m_go);
			}
			hotkeyBar.m_elements.Clear();
		}
		foreach (InventoryGrid grid in Resources.FindObjectsOfTypeAll<InventoryGrid>().Where(b => b.m_elementPrefab.scene.name is not null))
		{
			DoEquipedSwap(grid.m_elementPrefab, false);
			grid.m_width = 0;
		}
	}

	private static void DoEquipedSwap(GameObject root, bool disableImage)
	{
		if (Jewelcrafting.displaySocketBackground.Value == Jewelcrafting.Toggle.On)
		{
			if (root.transform.Find("equiped_jc_disabled") is null)
			{
				GameObject originalEquiped = root.transform.Find("equiped").gameObject;
				originalEquiped.name = "equiped_jc_disabled";
				if (disableImage)
				{
					originalEquiped.GetComponent<Image>().enabled = false;
				}
				else
				{
					originalEquiped.SetActive(false);
				}
				GameObject equiped = Object.Instantiate(background.transform.Find("JC_SelectedItem").gameObject, root.transform);
				equiped.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
				equiped.name = "equiped";
				equiped.transform.SetAsFirstSibling();
			}
		}
		else if (root.transform.Find("equiped_jc_disabled")?.gameObject is { } originalEquiped)
		{
			Object.DestroyImmediate(root.transform.Find("equiped").gameObject);
			originalEquiped.name = "equiped";
			if (disableImage)
			{
				originalEquiped.GetComponent<Image>().enabled = true;
			}
			else
			{
				originalEquiped.SetActive(true);
			}
		}
	}

	private static void ApplyToElementPrefab(ref GameObject activePrefab, ref GameObject? elementPrefab, bool disableImage)
	{
		if (elementPrefab is null)
		{
			elementPrefab = Object.Instantiate(activePrefab, MergedGemStoneSetup.gemList.transform);

			DoEquipedSwap(elementPrefab, disableImage);

			GameObject backgroundElement = Object.Instantiate(background, elementPrefab.transform);
			backgroundElement.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
			backgroundElement.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
			backgroundElement.name = background.name;
			backgroundElement.transform.SetAsFirstSibling();
		}

		activePrefab = elementPrefab;
	}

	[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.Awake))]
	private static class ReplaceBackgroundInventory
	{
		private static void Prefix(InventoryGrid __instance) => ApplyToElementPrefab(ref __instance.m_elementPrefab, ref gridElementPrefab, false);
	}

	[HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
	private static class ReplaceBackgroundHotkeyBar
	{
		private static void Prefix(Hud __instance) => ApplyToElementPrefab(ref __instance.m_rootObject.transform.Find("HotKeyBar").GetComponent<HotkeyBar>().m_elementPrefab, ref hotkeyElementPrefab, true);
	}

	private static void UpdateElement(ItemDrop.ItemData item, GameObject root)
	{
		GameObject bg = root.transform.Find(background.name).gameObject;
		if (item.Extended()?.GetComponent<Sockets>() is { } sockets && Jewelcrafting.displaySocketBackground.Value == Jewelcrafting.Toggle.On)
		{
			root.transform.Find("equiped").GetComponent<Image>().enabled = false;
			bg.SetActive(true);
			bg.GetComponent<Image>().color = ItemColor(sockets);
			GameObject selected = bg.transform.Find("JC_SelectedItem").gameObject;
			selected.SetActive(item.m_equiped);
			//selected.GetComponent<Image>().color = color;
		}
		else
		{
			bg.SetActive(false);
		}
	}

	[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
	private static class ColorBackgroundInventory
	{
		private static void Postfix(InventoryGrid __instance)
		{
			foreach (ItemDrop.ItemData item in __instance.m_inventory.m_inventory)
			{
				UpdateElement(item, __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, __instance.m_inventory.GetWidth()).m_go);
			}

			foreach (InventoryGrid.Element element in __instance.m_elements.Where(element => !element.m_used))
			{
				element.m_go.transform.Find(background.name).gameObject.SetActive(false);
			}
		}
	}

	[HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
	private static class ColorBackgroundHotkeyBar
	{
		private static void Postfix(HotkeyBar __instance)
		{
			if (__instance.m_items.Count > 0 && __instance.m_elements.Count > 0)
			{
				int firstItemX = __instance.m_items[0].m_gridPos.x;
				int firstItemElement = 0;
				while (!__instance.m_elements[firstItemElement].m_icon.gameObject.activeSelf)
				{
					++firstItemElement;
				}
				int gridPosOffset = firstItemX - firstItemElement;

				foreach (ItemDrop.ItemData item in __instance.m_items)
				{
					UpdateElement(item, __instance.m_elements[item.m_gridPos.x - gridPosOffset].m_go);
				}
			}

			foreach (HotkeyBar.ElementData element in __instance.m_elements.Where(element => !element.m_used))
			{
				element.m_go.transform.Find(background.name).gameObject.SetActive(false);
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
	private static class ColorInventoryTooltip
	{
		private static string ColorItemName(string name, ItemDrop.ItemData item)
		{
			if (item.Extended()?.GetComponent<Sockets>() is not { } sockets || Jewelcrafting.colorItemName.Value == Jewelcrafting.Toggle.Off)
			{
				return name;
			}
			return $"<color=#{ColorUtility.ToHtmlStringRGB(ItemColor(sockets))}>{name}</color>";
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo name = AccessTools.DeclaredField(typeof(ItemDrop.ItemData.SharedData), nameof(ItemDrop.ItemData.SharedData.m_name));
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.LoadsField(name))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ColorInventoryTooltip), nameof(ColorItemName)));
				}
			}
		}
	}

	private static Color ItemColor(Sockets sockets) => sockets.Worth <= maxWorth ? colors[sockets.Worth] : Color.red;
}
