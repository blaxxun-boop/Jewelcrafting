using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using ItemDataManager;
using Jewelcrafting.GemEffects;
using Jewelcrafting.Setup;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting.LootSystem;

public class GemDropBiome
{
	public float? lowHp;
	public float? highHp;
	public Dictionary<GemType, float>? distribution;

	public GemDropBiome Clone()
	{
		GemDropBiome cfg = (GemDropBiome)MemberwiseClone();
		return cfg;
	}
}

public class GemDropDef
{
	public Dictionary<Heightmap.Biome, GemDropBiome> biomeConfig = new();
	public List<string>? blacklist;
}

public static class ChestDrops
{
	private static GemDropDef config = null!;

	public static GemDropDef? Parse(object? drops, List<string> errors)
	{
		if (drops is Dictionary<object, object?> dropsDict)
		{
			GemDropDef dropDef = new();
			foreach (KeyValuePair<string, object?> biomeKv in EffectDef.castDictToStringDict(dropsDict))
			{
				if (biomeKv.Key == "blacklist")
				{
					if (biomeKv.Value is List<object?> blacklist)
					{
						dropDef.blacklist = new List<string>();
						foreach (object? itemObj in blacklist)
						{
							if (itemObj is string item)
							{
								dropDef.blacklist.Add(item.ToLower());
							}
							else
							{
								errors.Add($"Found invalid item in 'blacklist' of 'equipment' section. Got unexpected {itemObj?.GetType().ToString() ?? "empty string (null)"}.");
							}
						}
					}
					else
					{
						errors.Add($"The 'blacklist' must be a list of item names. Got unexpected {biomeKv.Value?.GetType().ToString() ?? "empty string (null)"}.");
					}
				}
				else if (EffectDef.ValidBiomes.TryGetValue(biomeKv.Key, out Heightmap.Biome biome))
				{
					string errorLocation = $"Found in definition for biome {biomeKv.Key} of 'gem drops' section.";

					if (biomeKv.Value is Dictionary<object, object?> dropDictObj)
					{
						Dictionary<string, object?> dropDict = EffectDef.castDictToStringDict(dropDictObj);
						GemDropBiome dropBiome = new();
						HashSet<string> knownKeys = new(StringComparer.InvariantCultureIgnoreCase);

						bool HasKey(string key)
						{
							knownKeys.Add(key);
							return dropDict.ContainsKey(key);
						}

						if (HasKey("low health"))
						{
							if (dropDict["low health"] is string stringHealth && float.TryParse(stringHealth, NumberStyles.Float, CultureInfo.InvariantCulture, out float health) && health >= 0)
							{
								dropBiome.lowHp = health;
							}
							else
							{
								errors.Add($"The low health threshold must be a positive number. Got unexpected {(dropDict["low health"] is string stringValue ? $"'{stringValue}'" : dropDict["low health"]?.GetType().ToString() ?? "empty string (null)")}. {errorLocation}");
								continue;
							}
						}

						if (HasKey("high health"))
						{
							if (dropDict["high health"] is string stringHealth && float.TryParse(stringHealth, NumberStyles.Float, CultureInfo.InvariantCulture, out float health) && health >= 0)
							{
								dropBiome.highHp = health;
							}
							else
							{
								errors.Add($"The high health threshold must be a positive number. Got unexpected {(dropDict["high health"] is string stringValue ? $"'{stringValue}'" : dropDict["high health"]?.GetType().ToString() ?? "empty string (null)")}. {errorLocation}");
								continue;
							}
						}

						if (HasKey("distribution"))
						{
							if (dropDict["distribution"] is Dictionary<object, object?> gemDict)
							{
								Dictionary<GemType, float> dropChance = new();
								foreach (KeyValuePair<string, object?> gemKv in EffectDef.castDictToStringDict(gemDict))
								{
									if (EffectDef.ValidGemTypes.TryGetValue(gemKv.Key, out GemType gemType))
									{
										if (gemKv.Value is string stringChance && float.TryParse(stringChance, NumberStyles.Float, CultureInfo.InvariantCulture, out float chance) && chance is >= 0 and <= 1)
										{
											dropChance[gemType] = chance;
										}
										else
										{
											errors.Add($"The gem spawn chance of {gemKv.Key} must be a number between 0 and 1. Got unexpected {(gemKv.Value is string stringValue ? $"'{stringValue}'" : gemKv.Value?.GetType().ToString() ?? "empty string (null)")} for 'distribution' section. {errorLocation}");
										}
									}
									else
									{
										errors.Add($"'{gemKv.Key}' is not a valid type for a gem. Valid gem types are: '{string.Join("', '", EffectDef.ValidGemTypes.Keys)}'. {errorLocation}");
									}
								}
								dropBiome.distribution = dropChance;
							}
							else
							{
								errors.Add($"Gem drop chances are defined individually as a mapping of gem type and chance expressed by a number between 0 and 1. Got unexpected {dropDict["distribution"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
							}
						}

						dropDef.biomeConfig[biome] = dropBiome;
						errors.AddRange(from key in dropDict.Keys where !knownKeys.Contains(key) select $"A drop definition may not contain a key '{key}'. {errorLocation}");
					}
					else
					{
						errors.Add($"Gem chances are defined individually as a mapping of gem type and chance, or a global chance for all gems expressed by a number between 0 and 1. Got unexpected {(biomeKv.Value is string stringValue ? $"'{stringValue}'" : biomeKv.Value?.GetType().ToString() ?? "empty string (null)")}. {errorLocation}");
					}

				}
				else
				{
					errors.Add($"Found invalid biome '{biomeKv.Key}' in 'gem drops' section. Valid keys are any of the biomes: '{string.Join("', '", EffectDef.ValidBiomes.Keys)}'.");
				}
			}
			return dropDef;
		}

		errors.Add($"The equipment section must be a mapping of biomes to biome specific configuration, got unexpected {drops?.GetType().ToString() ?? "empty string (null)"}.");
		return null;
	}

	public static void Apply(GemDropDef config)
	{
		ChestDrops.config = config;
	}

	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.OnDeath))]
	private static class AddGemChestDrop
	{
		[HarmonyPriority(Priority.VeryLow - 1)]
		private static void Postfix(CharacterDrop __instance)
		{
			Character character = __instance.m_character;

			if (character.m_baseAI is not MonsterAI ai || (Jewelcrafting.lootSystem.Value & Jewelcrafting.LootSystem.GemChests) == 0 || character.IsTamed())
			{
				return;
			}

			Heightmap.Biome biome = Heightmap.FindBiome(ai.m_spawnPoint);
			if (config.biomeConfig.TryGetValue(biome, out GemDropBiome drops) && drops.distribution is IDictionary { Count: >0 } && Random.value < (character.GetMaxHealth() < drops.lowHp!.Value ? Jewelcrafting.lootLowHpChance : Jewelcrafting.lootDefaultChance).Value / 100f)
			{
				// between 0 and 1
				float hpFactor = (Mathf.Pow(Mathf.Clamp(character.GetMaxHealth() / drops.highHp!.Value, 0.125f, 8), 1 / 3f) - 0.5f) / 1.5f;

				// now, skew the probability distribution towards the hpFactor
				float worthFactor = 0;
				const int repetitions = 4;
				for (int i = 0; i < repetitions; ++i)
				{
					worthFactor += (Random.value > Jewelcrafting.lootSkew.Value / 100f ? Random.Range(0, hpFactor) : Random.Range(hpFactor, 1)) / repetitions;
				}
				worthFactor = Mathf.Clamp01(worthFactor);
				int gemDelta = Jewelcrafting.gemChestMaxGems.Value - Jewelcrafting.gemChestMinGems.Value + 1;
				
				GameObject prefab;
				int[] numTiers = new int[3];
				if (worthFactor < 0.5)
				{
					prefab = LootSystemSetup.gemChests[0];
					numTiers[0] = Jewelcrafting.gemChestMinGems.Value + Math.Max(0, Mathf.CeilToInt(worthFactor * 2 * gemDelta) - 1);
				}
				else if (worthFactor < 0.9)
				{
					prefab = LootSystemSetup.gemChests[1];
					numTiers[1] = 1 + Math.Max(0, Mathf.CeilToInt((Jewelcrafting.gemChestMinGems.Value + gemDelta - 1) * (worthFactor - 0.75f) / 0.15f));
					numTiers[0] = Math.Max(0, Jewelcrafting.gemChestMinGems.Value - numTiers[1] + Math.Max(0, Mathf.CeilToInt(worthFactor % 0.25f / 0.25f * gemDelta) - 1));
				}
				else
				{
					prefab = LootSystemSetup.gemChests[2];
					numTiers[2] = 1 + Math.Max(0, Mathf.CeilToInt((Jewelcrafting.gemChestMinGems.Value + gemDelta - 1) * (worthFactor - 0.97f) / 0.03f));
					numTiers[1] = Math.Max(0, Jewelcrafting.gemChestMinGems.Value - numTiers[2] + Math.Max(0, Mathf.CeilToInt(worthFactor % 0.07f / 0.27f * gemDelta) - 1));
				}

				GemType Select(IEnumerable<GemType> skip)
				{
					Dictionary<GemType, float> distribution = new(drops.distribution);
					foreach (GemType type in skip)
					{
						distribution.Remove(type);
					}

					GemType selected = GemType.Green;
					float total = Random.Range(0, distribution.Values.Sum());
					foreach (KeyValuePair<GemType, float> kv in distribution)
					{
						total -= kv.Value;
						if (total <= 0.00001f)
						{
							selected = kv.Key;
							break;
						}
					}
					return selected;
				}

				int totalGems = numTiers.Sum();
				GemType[] selectedGems = new GemType[totalGems];
				List<int> preferForUpgrade = new(); 
				for (int i = 0; i < totalGems; ++i)
				{
					GemType selected = Select(Enumerable.Empty<GemType>());
					selectedGems[i] = selected;
					for (int j = 0; j < i; ++j)
					{
						if (selectedGems[i] == selected)
						{
							preferForUpgrade.Add(j);
							selectedGems[i] = Select(selectedGems.Take(i));
							break;
						}
					}
				}

				List<int> indexOrder = preferForUpgrade.GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
				int indexOrderIndex = 0;
				indexOrder.AddRange(Enumerable.Range(0, totalGems).Except(indexOrder));
				Dictionary<GemType, int> choices = new();
				for (int i = numTiers.Length - 1; i >= 0; --i)
				{
					for (int j = 0; j < numTiers[i]; ++j)
					{
						choices[selectedGems[indexOrder[indexOrderIndex++]]] = i;
					}
				}

				ItemInfo info = Utils.DropPrefabItem(prefab, character).GetComponent<ItemDrop>().m_itemData.Data();
				DropChest chest = info.GetOrCreate<DropChest>();
				chest.removableItemAmount = Jewelcrafting.gemChestAllowedAmount.Value;
				Inventory chestInventory = chest.ReadInventory();
				chestInventory.m_height = 1;
				chestInventory.m_width = choices.Count;

				foreach (KeyValuePair<GemType, int> gem in choices)
				{
					List<GemDefinition> gemstone = GemStoneSetup.Gems[gem.Key];
					chestInventory.AddItem(gemstone[Math.Min(gem.Value, gemstone.Count - 1)].Prefab, 1);
				}
				
				chest.Save();
			}
			
			EquipmentDrops.DoDrop(__instance, Jewelcrafting.LootSystem.EquipmentDrops, (biome, character, drops) =>
			{
				List<ItemDrop.ItemData> items = new();
				float worth = 0;
				for (int i = Random.Range(Jewelcrafting.equipmentChestMinItems.Value, Jewelcrafting.equipmentChestMaxItems.Value); i >= 0; --i)
				{
					worth = Mathf.Max(worth, EquipmentDrops.SpawnEquipment(biome, character, drops, prefab =>
					{
						ItemDrop.ItemData item = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
						items.Add(item);
						return item;
					}));
				}

				if (worth < 0)
				{
					return;
				}

				int prefabIndex = 2;
				if (worth < 0.5)
				{
					prefabIndex = 0;
				}
				else if (worth < 0.9)
				{
					prefabIndex = 1;
				}

				ItemInfo info = Utils.DropPrefabItem(LootSystemSetup.equipmentChests[prefabIndex], character).GetComponent<ItemDrop>().m_itemData.Data();
				DropChest chest = info.GetOrCreate<DropChest>();
				chest.removableItemAmount = Jewelcrafting.equipmentChestAllowedAmount.Value;
				Inventory chestInventory = chest.ReadInventory();
				chestInventory.m_height = 1;
				chestInventory.m_width = items.Count;
				foreach (ItemDrop.ItemData item in items)
				{
					chestInventory.AddItem(item);
				}
				chest.Save();
			});
		}
	}
}
