using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ItemDataManager;
using Jewelcrafting.GemEffects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Jewelcrafting;

public static class Synergy
{
	private static GameObject synergyWindow = null!;
	public static DisplaySynergyView activeSynergyDisplay = null!;
	private static Sprite inventoryIcon = null!;

	public static void initializeSynergy(AssetBundle assets)
	{
		synergyWindow = assets.LoadAsset<GameObject>("JC_Synergies_Window");
		inventoryIcon = assets.LoadAsset<Sprite>("JC_Gem_Tab");
	}

	public static Dictionary<GemType, int> GetGemDistribution(Player player)
	{
		List<GemType> gemTypes = new();
		Utils.ApplyToAllPlayerItems(player, item =>
		{
			if (item.Data().Get<Sockets>() is { } itemSockets)
			{
				foreach (SocketItem socket in itemSockets.socketedGems)
				{
					if (ObjectDB.instance.GetItemPrefab(socket.Name) is { } gem && GemStoneSetup.GemInfos.TryGetValue(gem.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info) && GemStoneSetup.shardColors.ContainsKey(info.Type))
					{
						gemTypes.Add(info.Type);
					}
					else if (MergedGemStoneSetup.mergedGemContents.TryGetValue(socket.Name, out List<GemInfo> mergedInfos))
					{
						gemTypes.AddRange(mergedInfos.Select(g => g.Type));
					}
				}
			}
		});
		return gemTypes.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
	private static class AddSynergyIcon
	{
		[HarmonyPriority(Priority.VeryLow)]
		private static void Postfix(InventoryGui __instance)
		{
			Transform inv = __instance.m_player.transform;
			GameObject synergy = Object.Instantiate(inv.Find("Armor").gameObject, inv);
			synergy.name = "Jewelcrafting Synergy";
			synergy.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, -78);
			synergy.transform.Find("armor_icon").GetComponent<Image>().sprite = inventoryIcon;
			synergy.transform.SetSiblingIndex(inv.Find("Armor").GetSiblingIndex());
			activeSynergyDisplay = synergy.AddComponent<DisplaySynergyView>();
			activeSynergyDisplay.synergyView = Object.Instantiate(synergyWindow, __instance.transform);
			activeSynergyDisplay.synergyView.SetActive(false);
			activeSynergyDisplay.synergyView.transform.Find("Bkg/BoxRight/Box_Right_Text").GetComponent<Text>().text = Localization.instance.Localize("$jc_right_text_details");
			activeSynergyDisplay.synergyView.transform.Find("Bkg/Left_Text").GetComponent<Text>().text = Localization.instance.Localize("$jc_possible_synergies");
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
	private static class FindTrash
	{
		private static GameObject? cached;

		[HarmonyPriority(Priority.VeryLow)]
		private static void Postfix(InventoryGui __instance)
		{
			if (cached)
			{
				return;
			}
			cached = __instance.gameObject;

			IEnumerator WaitOneFrame()
			{
				yield return null;
				Transform inv = __instance.m_player.transform;
				for (int i = 0; i < inv.childCount; ++i)
				{
					Transform child = inv.GetChild(i);
					if (child != activeSynergyDisplay.transform && Vector2.Distance(activeSynergyDisplay.transform.localPosition, child.localPosition) < 36 && child.Find("armor_icon") is not null)
					{
						activeSynergyDisplay.probablyTrash = child.gameObject;
						activeSynergyDisplay.probablyTrash.SetActive(false);
					}
				}
			}
			__instance.StartCoroutine(WaitOneFrame());
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupDragItem))]
	private static class ToggleTrashDisplay
	{
		private static void Postfix(ItemDrop.ItemData? item)
		{
			if (activeSynergyDisplay.probablyTrash is not null)
			{
				activeSynergyDisplay.probablyTrash.SetActive(item is not null);
				activeSynergyDisplay.gameObject.SetActive(item is null);
			}
		}
	}

	public class DisplaySynergyView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public GameObject synergyView = null!;
		public Text text = null!;
		public GameObject? probablyTrash;

		public void Awake()
		{
			text = transform.Find("ac_text").GetComponent<Text>();
		}

		public void RedrawSynergyCircle()
		{
			Dictionary<GemType, int> distribution = GetGemDistribution(Player.m_localPlayer);

			int total = distribution.Values.Sum();
			Color[] colors = new Color[1000];
			if (total == 0)
			{
				for (int i = 0; i < colors.Length; ++i)
				{
					colors[i] = Color.gray;
				}
			}
			else
			{
				int idx = 0;
				foreach (KeyValuePair<GemType, int> kv in distribution)
				{
					int endIdx = idx + kv.Value * colors.Length / total;
					Color color = GemStoneSetup.Colors[kv.Key].Color;
					while (idx < endIdx)
					{
						colors[idx++] = color;
					}
				}
				while (idx < colors.Length)
				{
					colors[idx] = colors[idx - 1];
					++idx;
				}
			}

			const int texRadius = 100;
			const int texWidth = texRadius * 2 + 1;
			Color[] texColors = new Color[texWidth * texWidth];
			for (int x = -texRadius; x < 0; ++x)
			{
				for (int y = -texRadius; y <= texRadius; ++y)
				{
					if (x * x + y * y <= texRadius * texRadius)
					{
						texColors[x + texRadius + (y + texRadius) * texWidth] = colors[250 + (int)(Math.Atan((double)y / x) / (Math.PI / 2) * 250)];
					}
				}
			}

			for (int y = 0; y < texRadius; ++y)
			{
				texColors[texRadius + y * texWidth] = colors[500];
			}
			texColors[texRadius + texRadius * texWidth] = Color.black;
			for (int y = 0; y < texRadius; ++y)
			{
				texColors[texRadius + (texRadius + 1 + y) * texWidth] = colors[0];
			}

			for (int x = 1; x <= texRadius; ++x)
			{
				for (int y = -texRadius; y <= texRadius; ++y)
				{
					if (x * x + y * y <= texRadius * texRadius)
					{
						texColors[x + texRadius + (y + texRadius) * texWidth] = colors[750 + (int)(Math.Atan((double)y / x) / (Math.PI / 2) * 250)];
					}
				}
			}

			Texture2D texture = new(texWidth, texWidth);
			texture.SetPixels(texColors);
			texture.Apply();

			synergyView.transform.Find("Bkg/GaugeBorder/Replace_Gauge").GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, texWidth, texWidth), Vector2.zero);
			Text legend = synergyView.transform.Find("Bkg/Color_Legend/Text_Color").GetComponent<Text>();
			legend.text = "";
			Text legendOverflow = synergyView.transform.Find("Bkg/Color_Legend_2/Text_Color").GetComponent<Text>();
			legendOverflow.text = "";
			int counter = 0;
			foreach (KeyValuePair<GemType, int> entry in distribution)
			{
				Color color = GemStoneSetup.Colors[entry.Key].Color;
				if (++counter > 7)
				{
					legend = legendOverflow;
				}
				legend.text += $"- {entry.Value} <color=#{ColorUtility.ToHtmlStringRGB(color)}>{Localization.instance.Localize($"$jc_merged_gemstone_{EffectDef.GemTypeNames[entry.Key].ToLower()}")}</color>\n";
			}

			Text possibleSynergy = synergyView.transform.Find("Bkg/Left_Text/Left_Text_1").GetComponent<Text>();
			possibleSynergy.text = "";
			string formatNumber(float num) => num.ToString(num < 100 ? "G2" : "0");
			foreach (SynergyDef synergyDef in Jewelcrafting.Synergies)
			{
				bool active = synergyDef.IsActive(distribution);
				foreach (EffectPower effectPower in synergyDef.EffectPowers)
				{
					string conditionLocalization = $"jc_synergy_condition_{synergyDef.Name.Replace(" ", "_")}";
					string desc = Localization.instance.Localize($"$jc_effect_{EffectDef.EffectNames[effectPower.Effect].ToLower()} - {(Localization.instance.m_translations.ContainsKey(conditionLocalization) ? $"${conditionLocalization}" : synergyDef.Name)} - $jc_effect_{EffectDef.EffectNames[effectPower.Effect].ToLower()}_desc_detail\n", effectPower.Config.GetType().GetFields().Select(p => formatNumber((float)p.GetValue(effectPower.Config))).ToArray());
					possibleSynergy.text += $"<color={(active ? "yellow" : "#222222")}>{desc}</color><size=5>\n</size>";
				}
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (!synergyView.activeSelf)
			{
				RedrawSynergyCircle();
				StartCoroutine(nameof(StartDisplaying));
			}
		}

		private IEnumerator StartDisplaying()
		{
			yield return new WaitForSeconds(0.2f);
			synergyView.SetActive(true);
		}
		
		public void OnPointerExit(PointerEventData eventData)
		{
			StopCoroutine(nameof(StartDisplaying));
			synergyView.SetActive(false);
		}
	}
}
