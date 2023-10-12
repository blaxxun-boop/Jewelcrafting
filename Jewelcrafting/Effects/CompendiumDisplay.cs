using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using ItemDataManager;
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
	private struct CompendiumGem
	{
		public float[] Powers;
		public GemLocation Location;
		public int Tier;
	}

	[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.AddActiveEffects))]
	private class AddToCompendium
	{
		private static void Postfix(TextsDialog __instance)
		{
			if (Player.m_localPlayer is not { } player)
			{
				return;
			}

			Dictionary<Effect, CompendiumGem> gems = new();

			Utils.ActiveSockets active = new(player);
			Utils.ApplyToAllPlayerItems(player, item =>
			{
				if (item.Data().Get<Sockets>() is { } itemSockets)
				{
					GemLocation location = Utils.GetGemLocation(item.m_shared, player);
					GemLocation itemLocation = Utils.GetItemGemLocation(item);
					foreach (string socket in itemSockets.socketedGems.Select(i => i.Name).Where(s => s != "").Take(active.Sockets(item)))
					{
						int tier = 1;
						if (ObjectDB.instance.GetItemPrefab(socket) is { } gameObject && GemStoneSetup.GemInfos.TryGetValue(gameObject.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info))
						{
							tier = info.Tier;
						}

						if (Jewelcrafting.EffectPowers.TryGetValue(socket.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers))
						{
							void handleEffectPowers(List<EffectPower> effectPowers)
							{
								foreach (EffectPower effectPower in effectPowers)
								{
									float[] powers;
									FieldInfo[] powerFields = effectPower.Config.GetType().GetFields();
									if (gems.TryGetValue(effectPower.Effect, out CompendiumGem power))
									{
										int i = 0;
										powers = power.Powers;
										foreach (FieldInfo powerField in powerFields)
										{
											powers[i] = powerField.GetCustomAttribute<PowerAttribute>().Add(powers[i], (float)powerField.GetValue(effectPower.Config));
											++i;
										}
									}
									else
									{
										powers = powerFields.Select(p => (float)p.GetValue(effectPower.Config)).ToArray();
									}
									gems[effectPower.Effect] = new CompendiumGem { Powers = powers, Location = power.Location | location, Tier = tier };
								}
							}
							if (locationPowers.TryGetValue(location, out List<EffectPower> effectPowers))
							{
								handleEffectPowers(effectPowers);
							}
							if (locationPowers.TryGetValue(itemLocation, out effectPowers))
							{
								handleEffectPowers(effectPowers);
							}
						}
					}
				}
			});

			if (gems.Count > 0)
			{
				StringBuilder sb = new(Localization.instance.Localize("\n\n<color=yellow>$jc_gem_effects_compendium</color>"));
				foreach (KeyValuePair<Effect, CompendiumGem> kv in gems)
				{
					sb.Append("\n");
					sb.Append(Utils.LocalizeDescDetail(player, kv.Value.Tier, kv.Key, kv.Value.Powers));
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

			if (!iconElement.GetComponent<UITooltip>())
			{
				void ApplyTooltip(GameObject go)
				{
					UITooltip tooltip = go.AddComponent<UITooltip>();
					tooltip.m_tooltipPrefab = InventoryGui.instance.m_craftButton.GetComponent<UITooltip>().m_tooltipPrefab;
				}
				ApplyTooltip(iconElement);
				ApplyTooltip(textWithIcon.transform.Find("Icon").gameObject);
			}

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
							Sprite spr;
							string name;
							if ((ulong)location >> 32 != 0)
							{
								if (Utils.GetGemLocationItem(location) is { } item)
								{
									spr = item.m_itemData.GetIcon();
									name = item.m_itemData.m_shared.m_name;
								}
								else
								{
									continue;
								}
							}
							else
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
									GemLocation.Crossbow => "CrossbowArbalest",
									GemLocation.Weapon => "SwordIron",
									GemLocation.Tool => "Hammer",
									GemLocation.Shield => "ShieldBlackmetal",
									GemLocation.Utility => "JC_Necklace_Red",
									GemLocation.ElementalMagic => "StaffFireball",
									GemLocation.BloodMagic => "StaffSkeleton",
									GemLocation.Magic => "YagluthDrop",
									GemLocation.All => "QueenDrop",
									_ => throw new ArgumentOutOfRangeException(),
								};

								spr = ZNetScene.instance.GetPrefab(prefab).GetComponent<ItemDrop>().m_itemData.GetIcon();
								name = $"$jc_socket_slot_{location.ToString().ToLower()}";
							}

							Transform icon;
							if (firstMatch)
							{
								icon = elementIcon.transform.Find("Icon");

								firstMatch = false;
							}
							else
							{
								icon = Object.Instantiate(iconElement, elementIcon.transform).transform;
								icon.SetAsFirstSibling();
							}
							
							icon.GetComponent<Image>().sprite = spr;
							icon.GetComponent<UITooltip>().m_text = Localization.instance.Localize(name);
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

	[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.OnClose))]
	private static class ClearCompendiumPage
	{
		private static void Postfix()
		{
			JC_UI_Elements.ForEach(Object.Destroy);
			JC_UI_Elements.Clear();
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
	private static class CloseTheCompendiumViaOnClose
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
		{
			FieldInfo compendiumField = AccessTools.DeclaredField(typeof(InventoryGui), nameof(InventoryGui.m_textsDialog));
			CodeInstruction[] instructions = instructionsEnumerable.ToArray();
			for (int i = 0; i < instructions.Length; ++i)
			{
				yield return instructions[i];
				if (instructions[i].LoadsField(compendiumField) && instructions[i + 3].Calls(AccessTools.DeclaredMethod(typeof(GameObject), nameof(GameObject.SetActive))))
				{
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.DeclaredMethod(typeof(TextsDialog), nameof(TextsDialog.OnClose)));
					i += 3;
				}
			}
		}
	}
}
