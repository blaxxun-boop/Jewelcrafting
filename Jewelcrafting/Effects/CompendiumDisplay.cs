using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ExtendedItemDataFramework;
using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class CompendiumDisplay
{
	public static TextsDialog.TextInfo compendiumPage = null!;

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
								gems[effectPower.Effect] = new KeyValuePair<float, GemLocation>(power.Key + effectPower.Power, power.Value | location);
							}
						}
					}
				}
			});

			StringBuilder sb = new(Localization.instance.Localize("\n\n<color=yellow>$jc_gem_effects_compendium</color>"));
			if (gems.Count > 0)
			{
				foreach (KeyValuePair<Effect, KeyValuePair<float, GemLocation>> kv in gems)
				{
					sb.Append(Localization.instance.Localize($"\n$jc_effect_{kv.Key.ToString().ToLower()}_desc_detail", kv.Value.Key.ToString(CultureInfo.InvariantCulture)));
				}
				__instance.m_texts[0].m_text += sb.ToString();
			}

			sb.Clear();
			foreach (KeyValuePair<GemType, List<GemDefinition>> kv in GemStoneSetup.Gems)
			{
				sb.Append(kv.Value.Count > 1 ? $"$jc_uncut_{kv.Key.ToString().ToLower()}_stone:\n" : $"{kv.Value[0].Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name}:\n");
				int gemHash = kv.Value[0].Prefab.name.GetStableHashCode();
				if (Jewelcrafting.EffectPowers.ContainsKey(gemHash))
				{
					Dictionary<Effect, IEnumerable<GemLocation>> effects = Jewelcrafting.EffectPowers[gemHash].SelectMany(kv => kv.Value.Select(p => new KeyValuePair<GemLocation, Effect>(kv.Key, p.Effect))).GroupBy(g => g.Value).ToDictionary(g => g.Key, g => g.Select(kv => kv.Key));
					foreach (KeyValuePair<Effect, IEnumerable<GemLocation>> effect in effects)
					{
						/*GemLocation seenGems = 0;
						foreach (GemLocation gemLocation in effect.Value.OrderByDescending(g => (int)g))
						{
							if ((seenGems & gemLocation) == 0)
							{
								if (MaterialPos.TryGetValue(gemLocation, out string spritePos))
								{
									sb.Append($"<quad material={MaterialIndex} size=20 {spritePos} /> ");
								}
								else
								{
									sb.Append($"[{gemLocation.ToString()}] ");
								}
								seenGems |= gemLocation;
							}
						}*/
						sb.Append($"<color=orange>$jc_effect_{effect.Key.ToString().ToLower()}</color> - $jc_effect_{effect.Key.ToString().ToLower()}_desc\n");
					}
				}
				sb.Append("\n");
			}

			if (Jewelcrafting.EffectPowers.Count > 0)
			{
				compendiumPage = new TextsDialog.TextInfo(Localization.instance.Localize("$jc_socket_compendium"), Localization.instance.Localize(sb.ToString()));
				__instance.m_texts.Add(compendiumPage);
			}
		}
	}

	/*private static int MaterialIndex;
	private static readonly Dictionary<GemLocation, string> MaterialPos = new();

	private static Material tmpmat = null!;

	[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.Awake))]
	private class AddIconsForSlots
	{
		private static void Postfix(TextsDialog __instance)
		{
			Texture2D texture = Jewelcrafting.slotIcons.First().Value.texture;
			MaterialIndex = __instance.m_textArea.canvasRenderer.materialCount++;
			tmpmat = new Material(Shader.Find("UI/Default")) { mainTexture = texture };
			__instance.m_textArea.canvasRenderer.SetMaterial(tmpmat, MaterialIndex);
			__instance.m_textArea.material = tmpmat;
			foreach (KeyValuePair<GemLocation, Sprite> kv in Jewelcrafting.slotIcons)
			{
				Rect rect = kv.Value.textureRect;
				MaterialPos[kv.Key] = $"x={rect.x / texture.width} y={rect.y / texture.height} width={rect.width / texture.width} height={rect.height / texture.height}";
			}
		}
	}*/
}
