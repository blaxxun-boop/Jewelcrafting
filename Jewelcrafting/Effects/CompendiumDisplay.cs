using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Jewelcrafting.GemEffects;

public static class CompendiumDisplay
{
	public static TextsDialog.TextInfo compendiumPage = null!;

	private static GameObject textWithIcon = null!;
	private static GameObject emptyElement = null!;
	private static GameObject iconElement = null!;

	public static void initializeCompendiumDisplay(AssetBundle assets)
	{
		textWithIcon = assets.LoadAsset<GameObject>("JC_ElementIcon");
		emptyElement = assets.LoadAsset<GameObject>("JC_EmptyElement");
		emptyElement.GetComponent<Text>().fontSize = 9;
		iconElement = assets.LoadAsset<GameObject>("JC_IconElement");
	}

	private static readonly List<GameObject> JC_UI_Elements = new();

	[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.AddActiveEffects))]
	private class AddToCompendium
	{
		private static void Postfix(TextsDialog __instance)
		{
			if (Player.m_localPlayer is not { } player)
			{
				return;
			}

			Dictionary<Effect, KeyValuePair<float, GemLocation>> gems = new();

			Utils.ApplyToAllPlayerItems(player, item =>
			{
				if (item?.Extended()?.GetComponent<Sockets>() is { } itemSockets)
				{
					GemLocation location = Utils.GetGemLocation(item.m_shared);
					foreach (string socket in itemSockets.socketedGems.Where(s => s != ""))
					{
						if (Jewelcrafting.EffectPowers.TryGetValue(socket.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(location, out List<EffectPower> effectPowers))
						{
							foreach (EffectPower effectPower in effectPowers)
							{
								gems.TryGetValue(effectPower.Effect, out KeyValuePair<float, GemLocation> power);
								FieldInfo primaryPower = effectPower.Config.GetType().GetFields().First();
								gems[effectPower.Effect] = new KeyValuePair<float, GemLocation>(primaryPower.GetCustomAttribute<PowerAttribute>().Add(power.Key, effectPower.Power), power.Value | location);
							}
						}
					}
				}
			});

			if (gems.Count > 0)
			{
				StringBuilder sb = new(Localization.instance.Localize("\n\n<color=yellow>$jc_gem_effects_compendium</color>"));
				foreach (KeyValuePair<Effect, KeyValuePair<float, GemLocation>> kv in gems)
				{
					sb.Append(Localization.instance.Localize($"\n$jc_effect_{EffectDef.EffectNames[kv.Key].ToLower()}_desc_detail", kv.Value.Key.ToString(CultureInfo.InvariantCulture)));
				}
				__instance.m_texts[0].m_text += sb.ToString();
			}

			if (Jewelcrafting.EffectPowers.Count > 0)
			{
				compendiumPage = new TextsDialog.TextInfo(Localization.instance.Localize("$jc_socket_compendium"), Localization.instance.Localize("$jc_socket_compendium_description"));
				__instance.m_texts.Add(compendiumPage);
			}
		}
	}

	[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.OnSelectText))]
	public static class DisplayGemEffectOverview
	{
		private static void Postfix(TextsDialog __instance, TextsDialog.TextInfo text)
		{
			Transform content = __instance.m_textArea.transform.parent;

			JC_UI_Elements.ForEach(Object.Destroy);
			JC_UI_Elements.Clear();
			content.GetComponent<VerticalLayoutGroup>().spacing = 10;
			if (text == compendiumPage)
			{
				Render(__instance);
			}
		}

		public static void Render(TextsDialog compendium)
		{
			Transform content = compendium.m_textArea.transform.parent;
			content.GetComponent<VerticalLayoutGroup>().spacing = 3;

			foreach (KeyValuePair<GemType, List<GemDefinition>> kv in GemStoneSetup.Gems)
			{
				JC_UI_Elements.Add(Object.Instantiate(emptyElement, content));
				GameObject elementIcon = Object.Instantiate(textWithIcon, content);
				JC_UI_Elements.Add(elementIcon);
				string mainText = $"{(GemStoneSetup.uncutGems.TryGetValue(kv.Key, out GameObject uncutGem) ? uncutGem : kv.Value[0].Prefab).GetComponent<ItemDrop>().m_itemData.m_shared.m_name}:";
				elementIcon.transform.Find("Text").GetComponent<Text>().text = Localization.instance.Localize(mainText);
				elementIcon.transform.Find("Icon").GetComponent<Image>().sprite = kv.Value[0].Prefab.GetComponent<ItemDrop>().m_itemData.GetIcon();
				int gemHash = kv.Value[0].Prefab.name.GetStableHashCode();

				if (Jewelcrafting.EffectPowers.TryGetValue(gemHash, out Dictionary<GemLocation, List<EffectPower>> gem))
				{
					Dictionary<Effect, IEnumerable<GemLocation>> effects = GemStones.GroupEffectsByGemLocation(gem).SelectMany(kv => kv.Value.Select(e => new KeyValuePair<GemLocation, Effect>(kv.Key, e.Effect))).GroupBy(g => g.Value).ToDictionary(g => g.Key, g => g.Select(kv => kv.Key));
					foreach (KeyValuePair<Effect, IEnumerable<GemLocation>> effect in effects)
					{
						elementIcon = Object.Instantiate(CompendiumDisplay.textWithIcon, content);
						JC_UI_Elements.Add(elementIcon);
						string textWithIcon = $"<color=orange>$jc_effect_{EffectDef.EffectNames[effect.Key].ToLower()}</color> - $jc_effect_{EffectDef.EffectNames[effect.Key].ToLower()}_desc";
						elementIcon.transform.Find("Text").GetComponent<Text>().text = Localization.instance.Localize(textWithIcon);
						bool firstMatch = true;
						foreach (GemLocation location in effect.Value)
						{
							string prefab = location switch
							{
								GemLocation.Head => "HelmetBronze",
								GemLocation.Cloak => "CapeWolf",
								GemLocation.Legs => "ArmorWolfLegs",
								GemLocation.Chest => "ArmorWolfChest",
								GemLocation.Sword => "SwordIron",
								GemLocation.Knife => "KnifeBlackMetal",
								GemLocation.Club => "MaceSilver",
								GemLocation.Polearm => "AtgeirIron",
								GemLocation.Spear => "SpearBronze",
								GemLocation.Axe => "AxeBlackMetal",
								GemLocation.Bow => "BowFineWood",
								GemLocation.Weapon => "SwordIron",
								GemLocation.Tool => "Hammer",
								GemLocation.Shield => "ShieldBlackmetal",
								GemLocation.Utility => "JC_Necklace_Red",
								GemLocation.All => "YagluthDrop",
								_ => throw new ArgumentOutOfRangeException()
							};

							Sprite spr = ZNetScene.instance.GetPrefab(prefab).GetComponent<ItemDrop>().m_itemData.GetIcon();
							if (firstMatch)
							{
								elementIcon.transform.Find("Icon").GetComponent<Image>().sprite = spr;

								firstMatch = false;
							}
							else
							{
								GameObject newIcon = Object.Instantiate(iconElement, elementIcon.transform);
								newIcon.transform.SetAsFirstSibling();
								newIcon.GetComponent<Image>().sprite = spr;
							}
						}
					}
				}
			}

			JC_UI_Elements.Add(Object.Instantiate(emptyElement, content));

			List<ContentSizeFitter> AllFilters = new();
			JC_UI_Elements.ForEach(element => AllFilters.Add(element.GetComponent<ContentSizeFitter>()));
			Canvas.ForceUpdateCanvases();
			AllFilters.ForEach(filter => filter.enabled = false);
			AllFilters.ForEach(filter => filter.enabled = true);
		}
	}
	
	[HarmonyPatch(typeof(TextsDialog),nameof(TextsDialog.OnClose))]
	private static class ClearCompendiumPage
	{

		private static void Postfix()
		{
			JC_UI_Elements.ForEach(Object.Destroy);
			JC_UI_Elements.Clear(); 
		}
	}
}
