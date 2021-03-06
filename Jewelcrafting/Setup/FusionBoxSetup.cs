using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ExtendedItemDataFramework;
using HarmonyLib;
using ItemManager;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting;

public static class FusionBoxSetup
{
	public static readonly Dictionary<GameObject, int> boxTier = new();
	public static readonly GameObject[] Boxes = new GameObject[3];

	public static void initializeFusionBoxes(AssetBundle assets)
	{
		Boxes[0] = new Item(assets, "JC_Common_Gembox").Prefab;
		Boxes[1] = new Item(assets, "JC_Epic_Gembox").Prefab;
		Boxes[2] = new Item(assets, "JC_Legendary_Gembox").Prefab;

		for (int tier = 0; tier < Boxes.Length; ++tier)
		{
			boxTier[Boxes[tier]] = tier;
		}

		ExtendedItemData.NewExtendedItemData += item =>
		{
			if (Jewelcrafting.boxMergeChances.ContainsKey(item.m_shared.m_name))
			{
				if (item.GetComponent<Box>() is null)
				{
					item.AddComponent<Box>();
				}
			}
		};
	}

	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
	private class AddFusionBoxToDrops
	{
		private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
		{
			if (Jewelcrafting.socketSystem.Value == Jewelcrafting.Toggle.On)
			{
				for (int tier = 0; tier < Boxes.Length; ++tier)
				{
					if (Jewelcrafting.crystalFusionBoxDropRate[tier].Value == 0)
					{
						continue;
					}
					if (Random.value < 1f / Jewelcrafting.crystalFusionBoxDropRate[tier].Value * Mathf.Pow(__instance.GetComponent<Character>().GetMaxHealth() / Jewelcrafting.healthBaseBoxDrop.Value, 1 / 3f))
					{
						__result.Add(new KeyValuePair<GameObject, int>(Boxes[tier], 1));
					}
				}
			}
		}
	}

	public static void IncreaseBoxProgress(IEnumerable<float> progress)
	{
		if (Player.m_localPlayer)
		{
			ItemDrop.ItemData[] boxes = Player.m_localPlayer.GetInventory().GetAllItems().Where(i => i.Extended()?.GetComponent<Box>() is { boxSealed: true }).ToArray();
			if (boxes.Length > 0)
			{
				Box box = boxes[Random.Range(0, boxes.Length)].Extended().GetComponent<Box>();
				box.AddProgress(progress.ToArray()[boxTier[box.ItemData.m_dropPrefab]]);
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
	private static class AdvanceBoxProgressByActivity
	{
		private static Vector3 lastPos = Vector3.zero;

		private static IEnumerator MeasureActivity()
		{
			for (;;)
			{
				if (Player.m_localPlayer && lastPos != Player.m_localPlayer.transform.position)
				{
					lastPos = Player.m_localPlayer.transform.position;
					IncreaseBoxProgress(Jewelcrafting.crystalFusionBoxMergeActivityProgress.Select(c => c.Value));
				}
				yield return new WaitForSeconds(60);
			}
			// ReSharper disable once IteratorNeverReturns
		}

		private static void Postfix(Player __instance)
		{
			__instance.StartCoroutine(MeasureActivity());
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
	public static class AddSealButton
	{
		public static GameObject SealButton = null!;

		private static void Seal()
		{
			if (GemStones.AddFakeSocketsContainer.openEquipment?.GetComponent<Box>() is not { boxSealed: false } box || box.socketedGems.Count < 2)
			{
				return;
			}

			GameObject? gem1 = ObjectDB.instance.GetItemPrefab(box.socketedGems[0]);
			GameObject? gem2 = ObjectDB.instance.GetItemPrefab(box.socketedGems[1]);

			if (gem1 is null || gem2 is null)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_gembox_seal_missing_gem"));
				return;
			}

			if (gem1 == gem2)
			{
				return;
			}

			if (GemStoneSetup.GemInfos.TryGetValue(gem1.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info1) && GemStoneSetup.GemInfos.TryGetValue(gem2.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info2))
			{
				if (info1.Tier != info2.Tier)
				{
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_gembox_seal_tier_mismatch"));
					return;
				}

				if (!GemStoneSetup.shardColors.ContainsKey(info1.Type) || !GemStoneSetup.shardColors.ContainsKey(info2.Type))
				{
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_gembox_seal_unique_gem"));
					return;
				}

				box.boxSealed = true;
				box.Save();
				InventoryGui.instance.CloseContainer();
				InventoryGui.instance.UpdateCraftingPanel();
			}
			else
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_gembox_seal_merged_gem"));
			}
		}

		private static void Postfix(InventoryGui __instance)
		{
			SealButton = Object.Instantiate(__instance.m_takeAllButton.gameObject, __instance.m_takeAllButton.transform.parent);
			Button.ButtonClickedEvent onClick = new();
			onClick.AddListener(Seal);
			SealButton.GetComponent<Button>().onClick = onClick;
			SealButton.transform.Find("Text").GetComponent<Text>().text = Localization.instance.Localize("$jc_gembox_merge");
			RectTransform rect = SealButton.GetComponent<RectTransform>();
			Vector2 anchoredPosition = rect.anchoredPosition;
			anchoredPosition = new Vector2(-anchoredPosition.x, -anchoredPosition.y);
			rect.anchoredPosition = anchoredPosition;
			SealButton.SetActive(false);
		}
	}

	[HarmonyPatch(typeof(ItemStand), nameof(ItemStand.SetVisualItem))]
	private static class RotateBox
	{
		private class Q
		{
			public Quaternion q;
		}

		private static readonly ConditionalWeakTable<ItemStand, Q> rotation = new();

		private static void Prefix(ItemStand __instance, out GameObject __state)
		{
			__state = __instance.m_visualItem;
		}
		
		private static void Postfix(ItemStand __instance, string itemName, GameObject __state)
		{
			if (__instance.m_visualItem != __state && !__instance.name.StartsWith("itemstandh", StringComparison.Ordinal))
			{
				Transform rotate = __instance.m_visualItem.transform.parent;
				if (Boxes.Any(b => b.name == itemName))
				{
					rotation.Remove(__instance);
					rotation.Add(__instance, new Q { q = rotate.rotation });
					rotate.rotation = Quaternion.Euler(0, 45, 0);
					__instance.m_visualItem.transform.localPosition = new Vector3(0.25f, -0.25f, 0);
				}
				else if (rotation.TryGetValue(__instance, out Q original))
				{
					rotate.rotation = original.q;
					rotation.Remove(__instance);
				}
			}
		}
	}
}
