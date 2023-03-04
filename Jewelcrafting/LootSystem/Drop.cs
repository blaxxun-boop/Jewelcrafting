using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using ItemDataManager;
using Jewelcrafting.GemEffects;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting.LootSystem;

public class DropBiome
{
	public float? lowHp;
	public float? highHp;
	public List<string>? resourceMap;
}

public class DropDef
{
	public Dictionary<Heightmap.Biome, DropBiome> biomeConfig = new();
	public List<Heightmap.Biome> biomeOrder = new();
	public List<string>? blacklist;
}

public static class Drop
{
	public static DropDef? Parse(object? drops, List<string> errors)
	{
		if (drops is Dictionary<object, object?> dropsDict)
		{
			DropDef dropDef = new();
			foreach (KeyValuePair<string, object?> biomeKv in EffectDef.castDictToStringDict(dropsDict))
			{
				if (biomeKv.Key == "biome order")
				{
					if (biomeKv.Value is List<object?> biomesList)
					{
						foreach (object? biomeObj in biomesList)
						{
							if (biomeObj is string biomeString && EffectDef.ValidBiomes.TryGetValue(biomeString, out Heightmap.Biome biome))
							{
								dropDef.biomeOrder.Add(biome);
							}
							else
							{
								errors.Add($"Found invalid biome in 'biome order' of 'equipment' section. Valid biomes are: '{string.Join("', '", EffectDef.ValidBiomes.Keys)}'. Got unexpected {(biomeObj is string stringValue ? $"'{stringValue}'" : biomeObj?.GetType().ToString() ?? "empty string (null)")}.");
							}
						}
					}
					else
					{
						errors.Add($"The 'biome order' must be a list of names of biomes. Got unexpected {(biomeKv.Value?.GetType().ToString() ?? "empty string (null)")}.");
					}
				}
				else if (biomeKv.Key == "blacklist")
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
					string errorLocation = $"Found in definition for biome {biomeKv.Key} of 'equipment' section.";

					if (biomeKv.Value is Dictionary<object, object?> dropDictObj)
					{
						Dictionary<string, object?> dropDict = EffectDef.castDictToStringDict(dropDictObj);
						DropBiome dropBiome = new();
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

						if (HasKey("resource map")) 
						{
							if (dropDict["resource map"] is List<object?> resourceList)
							{
								dropBiome.resourceMap = new List<string>();
								foreach (object? resourceObj in resourceList)
								{
									if (resourceObj is string resource)
									{
										dropBiome.resourceMap.Add(resource.ToLower());
									}
									else
									{
										errors.Add($"The resource map list must only contain names of resources. Got unexpected {resourceObj?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
									}
								}
							}
							else
							{
								errors.Add($"The resource map must be a list of names of resources. Got unexpected {dropDict["resource map"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
							}
						}
						
						errors.AddRange(from key in dropDict.Keys where !knownKeys.Contains(key) select $"A drop definition may not contain a key '{key}'. {errorLocation}");
						dropDef.biomeConfig[biome] = dropBiome;
					}
					else
					{
						errors.Add($"Gem chances are defined individually as a mapping of gem type and chance, or a global chance for all gems expressed by a number between 0 and 1. Got unexpected {(biomeKv.Value is string stringValue ? $"'{stringValue}'" : biomeKv.Value?.GetType().ToString() ?? "empty string (null)")}. {errorLocation}");
					}

				}
				else
				{
					errors.Add($"Found invalid biome '{biomeKv.Key}' in 'equipment' section. Valid keys are 'biome order' or any of the biomes: '{string.Join("', '", EffectDef.ValidBiomes.Keys)}'.");
				}
			}
			return dropDef;
		}

		errors.Add($"The equipment section must be a mapping of biomes to biome specific configuration, got unexpected {drops?.GetType().ToString() ?? "empty string (null)"}.");
		return null;
	}

	private static readonly Dictionary<Heightmap.Biome, float> lowHp = new();
	private static readonly Dictionary<Heightmap.Biome, float> highHp = new();
	private static readonly Dictionary<Heightmap.Biome, string[]> biomeResourceMap = new();
	private static string[] dropBlacklist = Array.Empty<string>();
	private static Dictionary<Heightmap.Biome, int> biomeOrder = new();

	public static void Apply(DropDef dropDefs)
	{
		foreach (KeyValuePair<Heightmap.Biome, DropBiome> dropKv in dropDefs.biomeConfig)
		{
			lowHp[dropKv.Key] = dropKv.Value.lowHp!.Value;
			highHp[dropKv.Key] = Mathf.Max(dropKv.Value.highHp!.Value, dropKv.Value.lowHp!.Value);
			biomeResourceMap[dropKv.Key] = dropKv.Value.resourceMap!.ToArray();
		}
		biomeOrder = dropDefs.biomeOrder.Select((s, i) => new KeyValuePair<Heightmap.Biome, int>(s, i)).ToDictionary(kv => kv.Key, kv => kv.Value);
		dropBlacklist = dropDefs.blacklist!.ToArray();

		dropCache.Clear();
	}
	
	private static readonly Dictionary<Heightmap.Biome, List<Recipe>> dropCache = new();

	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class ClearDropCache
	{
		private static void Prefix() => dropCache.Clear();
	}

	private static void EnsureDropCache()
	{
		if (dropCache.Count > 0)
		{
			return;
		}
		
		HashSet<Recipe> processedRecipes = new();
		foreach (KeyValuePair<Heightmap.Biome, string[]> kv in biomeResourceMap.OrderByDescending(kv => biomeOrder.TryGetValue(kv.Key, out int order) ? order : 0))
		{
			List<Recipe> drops = new();
			foreach (Recipe recipe in ObjectDB.instance.m_recipes)
			{
				bool matchLocalized(ICollection<string> list, ItemDrop item) => list.Contains(item.name.ToLower()) || list.Contains(Localization.instance.Localize(item.m_itemData.m_shared.m_name).ToLower()) || list.Contains(Jewelcrafting.english.Localize(item.m_itemData.m_shared.m_name).ToLower()); 
				if (!processedRecipes.Contains(recipe) && recipe.m_resources.Any(r => matchLocalized(kv.Value, r.m_resItem)) && recipe.m_enabled && !matchLocalized(dropBlacklist, recipe.m_item))
				{
					processedRecipes.Add(recipe);
					if (Utils.IsSocketableItem(recipe.m_item.GetComponent<ItemDrop>()))
					{
						drops.Add(recipe);
					}
				}
			}
			dropCache.Add(kv.Key, drops);
		}
	}

	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.OnDeath))]
	private static class AddEquipmentDrop
	{
		[HarmonyPriority(Priority.VeryLow - 1)]
		private static void Postfix(CharacterDrop __instance)
		{
			Character character = __instance.m_character;

			if (character.m_baseAI is not MonsterAI ai || (Jewelcrafting.lootSystem.Value & Jewelcrafting.LootSystem.EquipmentDrops) == 0 || character.IsTamed())
			{
				return;
			}

			EnsureDropCache();

			Heightmap.Biome biome = Heightmap.FindBiome(ai.m_spawnPoint);
			if (dropCache.TryGetValue(biome, out List<Recipe> drops) && Random.value < (character.GetMaxHealth() < lowHp[biome] ? Jewelcrafting.lootLowHpChance : Jewelcrafting.lootDefaultChance).Value / 100f)
			{
				List<GameObject> filteredDrops = drops.Where(recipe => Jewelcrafting.lootRestriction.Value switch
				{
					Jewelcrafting.LootRestriction.KnownStation => recipe.m_craftingStation is null || (Player.m_localPlayer.m_knownStations.TryGetValue(recipe.m_craftingStation.m_name, out int level) && recipe.m_minStationLevel <= level),
					Jewelcrafting.LootRestriction.KnownRecipe => Player.m_localPlayer.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name),
					_ => true
				}).Select(r => r.m_item.gameObject).ToList();

				if (filteredDrops.Count == 0)
				{
					return;
				}
				
				GameObject prefab = filteredDrops[Random.Range(0, filteredDrops.Count)];
				GameObject item = Object.Instantiate(prefab, character.GetCenterPoint() + Random.insideUnitSphere * 0.5f, Quaternion.Euler(0, Random.Range(0, 360), 0));
				Vector3 insideUnitSphere = Random.insideUnitSphere;
				if (insideUnitSphere.y < 0)
				{
					insideUnitSphere.y = -insideUnitSphere.y;
				}
				item.GetComponent<Rigidbody>()?.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);

				Sockets sockets = item.GetComponent<ItemDrop>().m_itemData.Data().GetOrCreate<Sockets>();

				float manyHp = highHp[biome];

				// between 0 and 1
				float hpFactor = (Mathf.Pow(Mathf.Clamp(character.GetMaxHealth() / manyHp, 0.125f, 8), 1 / 3f) - 0.5f) / 1.5f;

				// now, skew the probability distribution towards the hpFactor
				float worthFactor = 0;
				const int repetitions = 4;
				for (int i = 0; i < repetitions; ++i)
				{
					worthFactor += (Random.value > Jewelcrafting.lootSkew.Value / 100f ? Random.Range(0, hpFactor) : Random.Range(hpFactor, 1)) / repetitions;
				}

				int maxSockets = Jewelcrafting.maximumNumberSockets.Value;
				int worth = Mathf.CeilToInt(maxSockets * 6 * Mathf.Clamp01(worthFactor));

				int leastSockets = 1 + (worth - 1) / 6;
				int randomSockets() => Random.Range(leastSockets, Math.Min(worth, maxSockets + 1));
				int[] socketTiers = Enumerable.Repeat(1, Math.Max(randomSockets(), randomSockets())).ToArray();
				for (int i = socketTiers.Length; i < worth; ++i)
				{
					int index;
					do
					{
						index = Random.Range(0, socketTiers.Length);
					} while (socketTiers[index] == 6);
					++socketTiers[index];
				}

				List<GemType> tieredGems = new();
				GemType randomGemType()
				{
					if (tieredGems.Count == 0)
					{
						tieredGems.AddRange(GemStoneSetup.Gems.Where(kv => kv.Value.Count == 3).Select(kv => kv.Key));
					}

					int index = Random.Range(0, tieredGems.Count);
					GemType gem = tieredGems[index];
					tieredGems.RemoveAt(index);
					return gem;
				}

				sockets.socketedGems.Clear();
				for (int i = 0; i < socketTiers.Length; ++i)
				{
					// merged tier 2+2 is 4, merged 3+3 is 6; 5 does not exist
					if (socketTiers[i] == 5)
					{
						socketTiers[i] += Random.value > 0.5f ? 1 : -1;
					}

					bool isMergedGem = socketTiers[i] > 3 || (socketTiers[i] == 2 && Random.value > 0.8f);
					GemType primaryType = randomGemType();

					string gem;
					if (isMergedGem)
					{
						GemType secondaryType = randomGemType();
						if (secondaryType == primaryType)
						{
							secondaryType = randomGemType();
							tieredGems.Add(primaryType);
						}

						gem = MergedGemStoneSetup.mergedGems[primaryType][secondaryType][socketTiers[i] / 2 - 1].name;
					}
					else
					{
						gem = GemStoneSetup.Gems[primaryType][socketTiers[i] - 1].Prefab.name;
					}

					sockets.socketedGems.Add(new SocketItem(gem));
				}

				sockets.Save();
			}
		}
	}
}
