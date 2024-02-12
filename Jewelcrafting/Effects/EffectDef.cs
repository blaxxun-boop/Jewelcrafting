using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Jewelcrafting.LootSystem;
using Jewelcrafting.WorldBosses;
using ServerSync;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

[Flags]
public enum GemLocation : ulong
{
	Head = 1 << 0,
	Cloak = 1 << 1,
	Legs = 1 << 2,
	Chest = 1 << 3,
	Sword = 1 << 5,
	Knife = 1 << 6,
	Club = 1 << 7,
	Polearm = 1 << 8,
	Spear = 1 << 9,
	Axe = 1 << 10,
	Bow = 1 << 11,
	Crossbow = 1 << 12,
	Weapon = 1 << 13,
	ElementalMagic = 1 << 14,
	BloodMagic = 1 << 15,
	Magic = 1 << 16,
	Tool = 1 << 17,
	Shield = 1 << 18,
	Utility = 1 << 19,
	All = 1 << 20,
}

public enum Effect
{
	Sprinter,
	Defender,
	Firestarter,
	Pyromaniac,
	Iceheart,
	Snakebite,
	Vitality,
	Regeneration,
	Shadowhit,
	Powerrecovery,
	Endlessarrows,
	Endlessbolts,
	Explorer,
	Student,
	Glider,
	Unfazed,
	Necromancer,
	Parrymaster,
	Comfortable,
	Avoidance,
	Magnetic,
	Hercules,
	Vampire,
	Bloodthirsty,
	Masterarcher,
	Masterarbalist,
	Tank,
	Paintolerance,
	Berserk,
	Frenzy,
	Ninja,
	Inconspicuous,
	Nimble,
	Mirror,
	Echo,
	Momentum,
	Ricochet,
	Elementalchaos,
	Resonatingechoes,
	Daring,
	Resilience,
	Gourmet,
	Leadingwolf,
	Sharedhealing,
	Safehaven,
	Fleetinglife,
	Cowardice,
	Archerymentor,
	Arbalistmentor,
	Dedicatedtank,
	Stealtharcher,
	Mercifuldeath,
	Opportunity,
	Turtleshell,
	Turtleembrace,
	Marathon,
	Unbreakable,
	Energetic,
	Mountaingoat,
	Glowingspirit,
	Dungeonguide,
	Lightningspeed,
	Rootedrevenge,
	Poisonousdrain,
	Icyprotection,
	Fierydoom,
	Apotheosis,
	Togetherforever,
	Neveralone,
	Equilibrium,
	Eternalstudent,
	Timewarp,
	Carefulcutting,
	Magicalbargain,
	Eitrsurge,
	Lifeguard,
	Preciousblood,
	Perforation,
	Thunderclap,
	Fade,
	Wisplight,
	Wishbone,
}

public enum Uniqueness
{
	None, // Not unique
	All, // Only one of all gems flagged by this
	Gem, // Only one of these gems
	Tier, // Only one gem of the same tier across all items
	Item, // Only one gem in the same item
}

public struct EffectPower
{
	public Effect Effect;
	public float MinPower => (float)MinConfig.GetType().GetFields().First().GetValue(MinConfig);
	public float MaxPower => (float)MaxConfig.GetType().GetFields().First().GetValue(MaxConfig);
	public object MinConfig;
	public object MaxConfig;
	public Uniqueness Unique;
	public GemType Type;
	public GemLocation Location;
}

[PublicAPI]
public struct DefaultPower
{
	[AdditivePower] public float Power;
}

[PublicAPI]
public class EffectDef
{
	static EffectDef()
	{
		foreach (Type t in typeof(EffectDef).Assembly.GetTypes().Where(t => (t.Namespace ?? "").StartsWith(typeof(EffectDef).Namespace!, StringComparison.Ordinal) || (t.Namespace ?? "").StartsWith("Jewelcrafting.SynergyEffects", StringComparison.Ordinal)))
		{
			RuntimeHelpers.RunClassConstructor(t.TypeHandle);
		}
	}

	public GemType Type;
	public GemLocation Slots;
	public List<string> Items = new();
	public Uniqueness Unique = Uniqueness.None;
	public object[] MinPower = Array.Empty<object>();
	public object[] MaxPower = Array.Empty<object>();
	public bool UsesPowerRanges = false;

	public static readonly Dictionary<Effect, Type> ConfigTypes = new();

	public delegate string? OverrideDescription(Player player, ref string[] numbers);

	public static readonly Dictionary<Effect, OverrideDescription> DescriptionOverrides = new();

	public const GemLocation AllGemlocations = GemLocation.All - 1;
	public const GemLocation WeaponGemlocations = GemLocation.Sword | GemLocation.Knife | GemLocation.Club | GemLocation.Polearm | GemLocation.Spear | GemLocation.Axe;
	public const GemLocation MagicGemlocations = GemLocation.BloodMagic | GemLocation.ElementalMagic;

	private static readonly Dictionary<string, Uniqueness> ValidUniquenesses = new(((Uniqueness[])Enum.GetValues(typeof(Uniqueness))).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	private static readonly Dictionary<string, GemLocation> ValidGemLocations = new(((GemLocation[])Enum.GetValues(typeof(GemLocation))).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	public static readonly Dictionary<string, GemType> ValidGemTypes = new(((GemType[])Enum.GetValues(typeof(GemType))).Where(t => t != GemType.Cyan || global::Groups.API.IsLoaded()).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	public static readonly Dictionary<string, Effect> ValidEffects = new(((Effect[])Enum.GetValues(typeof(Effect))).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	public static Dictionary<string, Heightmap.Biome> ValidBiomes => new(((Heightmap.Biome[])Enum.GetValues(typeof(Heightmap.Biome))).Where(b => b is not Heightmap.Biome.None).ToDictionary(i => Regex.Replace(i.ToString(), "(?!^)([A-Z])", " $1"), i => i), StringComparer.InvariantCultureIgnoreCase);

	public static readonly Dictionary<GemType, string> GemTypeNames = ValidGemTypes.ToDictionary(kv => kv.Value, kv => kv.Key);
	public static readonly Dictionary<Effect, string> EffectNames = ValidEffects.ToDictionary(kv => kv.Value, kv => kv.Key);

	public static Dictionary<string, object?> castDictToStringDict(Dictionary<object, object?> dict) => new(dict.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value), StringComparer.InvariantCultureIgnoreCase);

	public struct ParseResult
	{
		public Dictionary<Heightmap.Biome, Dictionary<GemType, float>> gemDistribution = new();
		public Dictionary<Effect, List<EffectDef>> effects = new();
		public Dictionary<string, SynergyDef> Synergy = new();
		public GemDropDef gemDrops = new();
		public EquipmentDropDef equipmentDrops = new();
		public Dictionary<Heightmap.Biome, Dictionary<int, Dictionary<string, int>?>> SocketCosts = new();
		public List<Prizes> Prizes = new();
		public List<string> prizeBlacklist = new();

		public ParseResult()
		{
		}
	}

	public static ParseResult Parse(object? rootDictObj, out List<string> errors)
	{
		Dictionary<Effect, List<EffectDef>> effects = new();
		Dictionary<Heightmap.Biome, Dictionary<GemType, float>> gemDistribution = new();
		Dictionary<string, SynergyDef> synergies = new();
		List<Prizes> prizes = new();
		List<string> prizeBlacklist = new();
		ParseResult configurationResult = new() { gemDistribution = gemDistribution, effects = effects, Synergy = synergies, Prizes = prizes, prizeBlacklist = prizeBlacklist };
		errors = new List<string>();

		if (rootDictObj is not Dictionary<object, object?> rootDict)
		{
			if (rootDictObj is not null)
			{
				errors.Add($"All top-level keys must be a mapping. Got unexpected {rootDictObj.GetType()}.");
			}
			return configurationResult;
		}

		List<string> errorList = errors;
		foreach (KeyValuePair<string, object?> rootDictKv in castDictToStringDict(rootDict))
		{
			string effect = rootDictKv.Key;
			if (rootDictKv.Value is Dictionary<object, object?> synergyDict && synergyDict.ContainsKey("conditions"))
			{
				if (SynergyDef.Parse(rootDictKv.Key, castDictToStringDict(synergyDict), errors) is { } synergy)
				{
					synergies.Add(rootDictKv.Key, synergy);
				}

				continue;
			}

			if (rootDictKv.Value is Dictionary<object, object?> prizeDict && prizeDict.ContainsKey("prizes"))
			{
				if (GachaDef.Parse(rootDictKv.Key, castDictToStringDict(prizeDict), errors) is { } prize)
				{
					prizes.Add(prize);
				}

				continue;
			}

			if (rootDictKv.Key == "global blacklist")
			{
				if (GachaDef.ParseBlacklist(rootDictKv.Value, errors) is { } blacklist)
				{
					prizeBlacklist.AddRange(blacklist);
				}
				continue;
			}

			if (rootDictKv.Key == "gem drops")
			{
				if (ChestDrops.Parse(rootDictKv.Value, errors) is { } drops)
				{
					configurationResult.gemDrops = drops;
				}
				continue;
			}

			if (rootDictKv.Key == "equipment")
			{
				if (EquipmentDrops.Parse(rootDictKv.Value, errors) is { } drops)
				{
					configurationResult.equipmentDrops = drops;
				}
				continue;
			}

			if (rootDictKv.Key == "socket cost")
			{
				if (Socketing.Parse(rootDictKv.Value, errors) is { } socketCosts)
				{
					configurationResult.SocketCosts = socketCosts;
				}
				continue;
			}

			if (!ValidEffects.ContainsKey(effect))
			{
				effect = effect.Replace(" ", "");
			}
			if (!ValidEffects.ContainsKey(effect))
			{
				if (rootDictKv.Value is "unset")
				{
					synergies.Add(rootDictKv.Key, new SynergyDef());
				}

				if (effect != "gems")
				{
					errors.Add($"'{rootDictKv.Key}' is not a valid effect name. Valid effects are: '{string.Join("', '", ValidEffects.Keys)}'. There also can be a 'gems' section to define gem spawn chances.");
					continue;
				}

				if (rootDictKv.Value is Dictionary<object, object?> gemBiomeDict)
				{
					bool IsDestructibleGemType(GemType type) => GemStoneSetup.shardColors.ContainsKey(type);

					foreach (KeyValuePair<string, object?> biomeKv in castDictToStringDict(gemBiomeDict))
					{
						if (ValidBiomes.TryGetValue(biomeKv.Key, out Heightmap.Biome biome))
						{
							string errorLocation = $"Found in definition for biome {biomeKv.Key} of 'gems' section.";
							Dictionary<GemType, float> spawnChance = new();

							if (biomeKv.Value is Dictionary<object, object?> gemDict)
							{
								foreach (KeyValuePair<string, object?> gemKv in castDictToStringDict(gemDict))
								{
									if (ValidGemTypes.TryGetValue(gemKv.Key, out GemType gemType) && IsDestructibleGemType(gemType))
									{
										if (gemKv.Value is string stringChance && float.TryParse(stringChance, NumberStyles.Float, CultureInfo.InvariantCulture, out float chance) && chance is >= 0 and <= 1)
										{
											spawnChance[gemType] = chance;
										}
										else
										{
											errorList.Add($"The gem spawn chance of {gemKv.Key} must be a number between 0 and 1. Got unexpected {(gemKv.Value is string stringValue ? $"'{stringValue}'" : gemKv.Value?.GetType().ToString() ?? "empty string (null)")}. {errorLocation}");
										}
									}
									else
									{
										errorList.Add($"'{gemKv.Key}' is not a valid type for a destructible gem. Valid destructible gem types are: '{string.Join("', '", ValidGemTypes.Keys.Where(k => IsDestructibleGemType(ValidGemTypes[k])))}'. {errorLocation}");
									}
								}
							}
							else if (biomeKv.Value is string biomeChance && float.TryParse(biomeChance, NumberStyles.Float, CultureInfo.InvariantCulture, out float chance) && chance is >= 0 and <= 1)
							{
								foreach (GemType gemType in ValidGemTypes.Values.Where(IsDestructibleGemType))
								{
									spawnChance[gemType] = chance;
								}
							}
							else
							{
								errors.Add($"Gem chances are defined individually as a mapping of gem type and chance, or a global chance for all gems expressed by a number between 0 and 1. Got unexpected {(biomeKv.Value is string stringValue ? $"'{stringValue}'" : biomeKv.Value?.GetType().ToString() ?? "empty string (null)")}. {errorLocation}");
							}

							gemDistribution[biome] = spawnChance;
						}
						else
						{
							errors.Add($"Found invalid biome '{biomeKv.Key}' in 'gems' section. Valid biomes are: '{string.Join("', '", ValidBiomes.Keys)}'.");
						}
					}
				}
				else
				{
					errors.Add($"The gems section expects a mapping of biomes to gem types to chance. Got unexpected {rootDictKv.Value?.GetType().ToString() ?? "empty string (null)"}.");
				}

				continue;
			}

			void AddEffectDef(Dictionary<string, object?> effectDict, int? index = null)
			{
				HashSet<string> knownKeys = new(StringComparer.InvariantCultureIgnoreCase);
				string errorLocation = $"Found in{(index is not null ? $" {index}." : "")} effect definition for effect '{effect}'.";

				EffectDef effectDef = new();

				bool HasKey(string key)
				{
					knownKeys.Add(key);
					return effectDict.ContainsKey(key);
				}

				if (HasKey("gem"))
				{
					if (effectDict["gem"] is string gem)
					{
						if (ValidGemTypes.TryGetValue(gem, out GemType type))
						{
							effectDef.Type = type;
						}
						else
						{
							errorList.Add($"'{gem}' is not a valid type for a gem. Valid gem types are: '{string.Join("', '", ValidGemTypes.Keys)}'. {errorLocation}");
							return;
						}
					}
					else
					{
						errorList.Add($"A gem type must be a string. Got unexpected {effectDict["gem"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
						return;
					}
				}
				else
				{
					errorList.Add($"An effect definition must contain a key 'gem' for determining which gem to assign this effect to. {errorLocation}");
					return;
				}

				if (HasKey("slot"))
				{
					List<string> slots = new();
					if (effectDict["slot"] is string singleSlot)
					{
						if (!string.Equals(singleSlot, "none", StringComparison.InvariantCultureIgnoreCase))
						{
							slots.Add(singleSlot);
						}
					}
					else if (effectDict["slot"] is List<object?> slotList)
					{
						foreach (object? slotObj in slotList)
						{
							if (slotObj is string slot)
							{
								slots.Add(slot);
							}
							else
							{
								errorList.Add($"The slot list must contain only strings. Got unexpected {slotObj?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
							}
						}
					}
					else
					{
						errorList.Add($"Slot must be a list of slots or a string. Got unexpected {effectDict["slot"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
						return;
					}
					foreach (string slot in slots)
					{
						if (ValidGemLocations.TryGetValue(slot, out GemLocation location))
						{
							effectDef.Slots |= location;
						}
						else
						{
							errorList.Add($"A '{slot}' is not a valid location for a gem. Valid locations are: '{string.Join("', '", ValidGemLocations.Keys)}'. Alternatively specify 'none' to drop all slots for this gem color. {errorLocation}");
						}
					}
				}
				else if (HasKey("item"))
				{
					List<string> items = new();
					if (effectDict["item"] is string singleItem)
					{
						if (!string.Equals(singleItem, "none", StringComparison.InvariantCultureIgnoreCase))
						{
							items.Add(singleItem);
						}
					}
					else if (effectDict["item"] is List<object?> itemList)
					{
						foreach (object? itemObj in itemList)
						{
							if (itemObj is string item)
							{
								items.Add(item);
							}
							else
							{
								errorList.Add($"The item list must contain only strings. Got unexpected {itemObj?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
							}
						}
					}
					else
					{
						errorList.Add($"Item must be a list of items or a string. Got unexpected {effectDict["item"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
						return;
					}
					effectDef.Items = items;
				}
				else
				{
					errorList.Add($"An effect definition must contain a key 'slot' or 'item' for determining which slots or items the gem may be assigned to. {errorLocation}");
					return;
				}

				int tiers = GemStoneSetup.Gems.TryGetValue(effectDef.Type, out List<GemDefinition> gemDefinitions) ? gemDefinitions.Count : 1;
				effectDef.MinPower = new object[tiers];
				effectDef.MaxPower = new object[tiers];

				if (!ConfigTypes.TryGetValue(ValidEffects[effect], out Type configType))
				{
					configType = typeof(DefaultPower);
				}
				Dictionary<string, FieldInfo> configFields = new(configType.GetFields().ToDictionary(f => Regex.Replace(f.Name, "(?!^)([A-Z])", " $1"), f => f), StringComparer.InvariantCultureIgnoreCase);
				if ((effectDef.Slots != 0 || effectDef.Items.Count != 0) && configFields.Count > 0)
				{
					if (HasKey("power"))
					{
						for (int i = 0; i < tiers; ++i)
						{
							effectDef.MinPower[i] = Activator.CreateInstance(configType);
							effectDef.MaxPower[i] = Activator.CreateInstance(configType);
						}

						foreach (FieldInfo field in configFields.Values)
						{
							if (field.GetCustomAttribute<OptionalPowerAttribute>() is { } optional)
							{
								for (int i = 0; i < tiers; ++i)
								{
									field.SetValue(effectDef.MinPower[i], optional.DefaultValue);
									field.SetValue(effectDef.MaxPower[i], optional.DefaultValue);
								}
							}
						}

						bool ParseTiers(object? powerObj, FieldInfo? field = null)
						{
							bool ParsePower(string powerString, int tier)
							{
								if (float.TryParse(powerString, NumberStyles.Float, CultureInfo.InvariantCulture, out float power))
								{
									field.SetValue(effectDef.MinPower[tier], power);
									field.SetValue(effectDef.MaxPower[tier], power);

									return true;
								}

								string[] split = powerString.Split('-');
								if (split.Length == 2 && float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float minPower) && float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float maxPower))
								{
									field.SetValue(effectDef.MinPower[tier], minPower);
									field.SetValue(effectDef.MaxPower[tier], maxPower);
									if (minPower != maxPower)
									{
										effectDef.UsesPowerRanges = true;
									}

									return true;
								}

								return false;
							}

							bool hasField = field is not null;
							string fieldLocation = field is not null ? $" for the key '{field.Name}'" : "";
							field ??= configFields.First().Value;
							if (powerObj is Dictionary<object, object?> powersList && !hasField)
							{
								if (configFields.Count != powersList.Count)
								{
									errorList.Add($"There are missing configurable values for this effect. Specify values for all of {string.Join(", ", configFields.Keys)}. Found values for {string.Join(", ", castDictToStringDict(powersList).Keys)}. {errorLocation}");
									return false;
								}

								foreach (KeyValuePair<string, object?> kv in castDictToStringDict(powersList))
								{
									if (!configFields.TryGetValue(kv.Key, out field))
									{
										errorList.Add($"'{kv.Key}' is not a valid configuration key for the powers. Specify values for all of {string.Join(", ", configFields.Keys)}. {errorLocation}");
										return false;
									}

									if (!ParseTiers(kv.Value, field))
									{
										return false;
									}
								}
							}
							else if (!hasField && configFields.Count(kv => kv.Value.GetCustomAttribute<OptionalPowerAttribute>() is null) > 1)
							{
								errorList.Add($"There are multiple configurable values for this effect. Specify values for all of {string.Join(", ", configFields.Keys)}. {errorLocation}");
								return false;
							}
							else if (powerObj is List<object?> powerTiers && powerTiers.Count == tiers)
							{
								for (int i = 0; i < powerTiers.Count; ++i)
								{
									if (powerTiers[i] is string powerNumber)
									{
										if (!ParsePower(powerNumber, i))
										{
											errorList.Add($"The {i+1}. power{fieldLocation} is not a number or range of two numbers. Got unexpected '{powerNumber}'. {errorLocation}");
											return false;
										}
									}
									else
									{
										errorList.Add($"The {i+1}. power{fieldLocation} is not a number or range of two numbers. Got unexpected {powerTiers[i]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
										return false;
									}
								}
							}
							else
							{
								if (tiers == 1)
								{
									if (powerObj is string powerNumber)
									{
										if (!ParsePower(powerNumber, 0))
										{
											errorList.Add($"The power{fieldLocation} is not a number or range of two numbers. Got unexpected '{powerNumber}'. {errorLocation}");
											return false;
										}
									}
									else
									{
										errorList.Add($"The power{fieldLocation} is not a number or range of two numbers. Got unexpected {powerObj?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
										return false;
									}
								}
								else
								{
									errorList.Add($"The power must be a list of exactly {tiers} numbers denoting the strength of the effect. Got unexpected {powerObj?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
									return false;
								}
							}

							return true;
						}

						if (!ParseTiers(effectDict["power"]))
						{
							return;
						}
					}
					else
					{
						errorList.Add($"An effect definition must contain a key 'power' for determining what strength the effect has. {errorLocation}");
						return;
					}
				}

				if (HasKey("unique"))
				{
					if (effectDict["unique"] is string uniqueStr && ValidUniquenesses.TryGetValue(uniqueStr, out Uniqueness uniqueness))
					{
						effectDef.Unique = uniqueness;
					}
					else
					{
						errorList.Add($"'{effectDict["unique"] as string ?? effectDict["unique"]?.GetType().ToString() ?? "empty string (null)"}' is not a valid identifier for the unique flag. Valid locations are: '{string.Join("', '", ValidUniquenesses.Keys)}'. {errorLocation}");
					}
				}

				if (!effects.TryGetValue(ValidEffects[effect], out List<EffectDef> effectDefs))
				{
					effectDefs = effects[ValidEffects[effect]] = new List<EffectDef>();
				}
				effectDefs.Add(effectDef);

				errorList.AddRange(from key in effectDict.Keys where !knownKeys.Contains(key) select $"An effect definition may not contain a key '{key}'. {errorLocation}");
			}

			switch (rootDictKv.Value)
			{
				case "unset":
				case List<object?> { Count: 0 }:
					effects[ValidEffects[effect]] = new List<EffectDef>();
					break;
				case List<object?> effectList:
				{
					for (int i = 0; i < effectList.Count; ++i)
					{
						if (effectList[i] is Dictionary<object, object?> effectDict)
						{
							AddEffectDef(castDictToStringDict(effectDict), i);
						}
						else
						{
							errors.Add($"Effect definitions must be a mapping of effect properties. Got unexpected {effectList[i]?.GetType().ToString() ?? "empty string (null)"} for effect '{effect}'.");
						}
					}
					break;
				}
				case Dictionary<object, object?> effectDict:
					AddEffectDef(castDictToStringDict(effectDict));
					break;
				default:
					errors.Add($"Effects must either contain an effect definition, which must be a mapping of effect properties, a list of effect definitions, or the string 'unset' to remove the effect altogether. Got unexpected {rootDictKv.Value?.GetType().ToString() ?? "empty string (null)"} for effect '{effect}'.");
					break;
			}
		}

		return configurationResult;
	}

	public class Loader : ConfigLoader.Loader
	{
		public static Loader instance = null!;

		public readonly Dictionary<string, ParseResult> parsed = new();
		public ParseResult DefaultConfig => parsed[""];
		private readonly HashSet<string> globalConfigs = new();

		public Loader()
		{
			instance = this;
		}

		public List<string> ErrorCheck(object? yaml)
		{
			ParseResult result = Parse(yaml, out List<string> errors);
			if (ObjectDB.instance)
			{
				Utils.ReloadItemNameMap();
				ValidateRuntime(result, errors);
			}
			return errors;
		}

		public List<string> ProcessConfig(string key, object? yaml, bool global = false)
		{
			if (global)
			{
				globalConfigs.Add(key);
			}
			
			ParseResult result = Parse(yaml, out List<string> errors);

			Dictionary<GemType, GemLocation> usedSlots = new();
			foreach (KeyValuePair<Effect, List<EffectDef>> effect in result.effects)
			{
				foreach (EffectDef def in effect.Value)
				{
					usedSlots.TryGetValue(def.Type, out GemLocation usedLocations);
					usedSlots[def.Type] = usedLocations | def.Slots;
				}
			}

			foreach (KeyValuePair<Effect, List<EffectDef>> effect in result.effects)
			{
				foreach (EffectDef def in effect.Value)
				{
					usedSlots.TryGetValue(def.Type, out GemLocation usedLocations);
					if ((def.Slots & GemLocation.Weapon) != 0)
					{
						def.Slots |= WeaponGemlocations & ~usedLocations;
					}
					if ((def.Slots & GemLocation.Magic) != 0)
					{
						def.Slots |= MagicGemlocations & ~usedLocations;
					}
					if ((def.Slots & GemLocation.All) != 0)
					{
						def.Slots |= AllGemlocations & ~usedLocations;
					}
				}
			}

			parsed[key] = result;
			return errors;
		}

		public void Reset()
		{
			foreach (string key in parsed.Keys.Where(k => !globalConfigs.Contains(k) && !k.StartsWith("/")).ToArray())
			{
				parsed.Remove(key);
			}
		}

		public void ValidateRuntime(List<string> warnings)
		{
			Utils.ReloadItemNameMap();

			foreach (ParseResult result in parsed.Values)
			{
				ValidateRuntime(result, warnings);
			}
		}

		private void ValidateRuntime(ParseResult result, List<string> warnings)
		{
			foreach (KeyValuePair<Effect, List<EffectDef>> effectKv in result.effects)
			{
				foreach (EffectDef def in effectKv.Value)
				{
					foreach (string item in def.Items)
					{
						if (Utils.GetItem(item) is null)
						{
							warnings.Add($"Tried to apply effect {effectKv.Key.ToString()} to an item {item} which does not exist. Skipping.");
						}
					}
				}
			}

			GachaDef.ValidatePrizes(result.Prizes, warnings);
		}

		public void ApplyConfig()
		{
			Dictionary<Effect, List<EffectDef>> socketEffects = new();
			Dictionary<Heightmap.Biome, Dictionary<GemType, float>> gemDistribution = new();
			foreach (ParseResult parse in parsed.Values)
			{
				foreach (KeyValuePair<Effect, List<EffectDef>> effectKv in parse.effects)
				{
					if (effectKv.Value.Count == 0)
					{
						socketEffects.Remove(effectKv.Key);
					}
					else
					{
						if (!socketEffects.TryGetValue(effectKv.Key, out List<EffectDef> gems))
						{
							gems = socketEffects[effectKv.Key] = new List<EffectDef>();
						}
						foreach (EffectDef def in effectKv.Value)
						{
							gems.RemoveAll(g => def.Type == g.Type);
							if (def.Slots != 0 || def.Items.Count > 0)
							{
								gems.Add(def);
							}
						}
					}
				}

				foreach (KeyValuePair<Heightmap.Biome, Dictionary<GemType, float>> biomeKv in parse.gemDistribution)
				{
					if (!gemDistribution.TryGetValue(biomeKv.Key, out Dictionary<GemType, float> gems))
					{
						gems = gemDistribution[biomeKv.Key] = new Dictionary<GemType, float>();
					}
					foreach (KeyValuePair<GemType, float> kv in biomeKv.Value)
					{
						gems[kv.Key] = kv.Value;
					}
				}
			}

			// Ensure the sum for any given gemtype is at most 1
			foreach (Dictionary<GemType, float> gems in gemDistribution.Values)
			{
				float sum = gems.Values.Sum();
				if (sum > 1)
				{
					foreach (GemType gemType in gems.Keys.ToArray())
					{
						gems[gemType] /= sum;
					}
				}
			}

			SynergyDef.Apply(parsed.Values.SelectMany(v => v.Synergy).GroupBy(kv => kv.Key).Select(kv => kv.Last().Value));

			if (parsed.Values.LastOrDefault(p => p.Prizes.Count > 0) is { Prizes: not null } result)
			{
				GachaDef.Apply(result.Prizes, result.prizeBlacklist);
			}

			Dictionary<Heightmap.Biome, EquipmentDropBiome> equipmentDropBiomes = ValidBiomes.Values.ToDictionary(b => b, _ => new EquipmentDropBiome
			{
				lowHp = 0,
				highHp = 0,
				resourceMap = new List<string>(),
			});
			foreach (KeyValuePair<Heightmap.Biome, EquipmentDropBiome> dropKv in parsed.Values.SelectMany(p => p.equipmentDrops.biomeConfig))
			{
				EquipmentDropBiome dropBiome = equipmentDropBiomes[dropKv.Key];
				if (dropKv.Value.lowHp is { } lowHp)
				{
					dropBiome.lowHp = lowHp;
				}
				if (dropKv.Value.highHp is { } highHp)
				{
					dropBiome.highHp = highHp;
				}
				if (dropKv.Value.resourceMap is { } resources)
				{
					dropBiome.resourceMap = resources;
				}
			}
			EquipmentDrops.Apply(new EquipmentDropDef
			{
				biomeOrder = parsed.Values.Last(p => p.equipmentDrops.biomeOrder.Count > 0).equipmentDrops.biomeOrder,
				biomeConfig = equipmentDropBiomes,
				blacklist = parsed.Values.Last(p => p.equipmentDrops.blacklist is not null).equipmentDrops.blacklist,
			});

			Dictionary<Heightmap.Biome, Dictionary<int, Dictionary<string, int>?>> socketCosts = new();
			foreach (KeyValuePair<Heightmap.Biome, Dictionary<int, Dictionary<string, int>?>> biomeCostKv in parsed.Values.SelectMany(p => p.SocketCosts))
			{
				socketCosts[biomeCostKv.Key] = biomeCostKv.Value;
			}
			Socketing.Apply(socketCosts);

			Dictionary<Heightmap.Biome, GemDropBiome> gemDropBiomes = ValidBiomes.Values.ToDictionary(b => b, _ => new GemDropBiome
			{
				lowHp = 0,
				highHp = 0,
			});
			foreach (KeyValuePair<Heightmap.Biome, GemDropBiome> dropKv in parsed.Values.SelectMany(p => p.gemDrops.biomeConfig))
			{
				GemDropBiome dropBiome = gemDropBiomes[dropKv.Key];
				if (dropKv.Value.lowHp is { } lowHp)
				{
					dropBiome.lowHp = lowHp;
				}
				if (dropKv.Value.highHp is { } highHp)
				{
					dropBiome.highHp = highHp;
				}
				if (dropKv.Value.distribution is { } distribution)
				{
					dropBiome.distribution = distribution;
				}
			}
			foreach (KeyValuePair<Heightmap.Biome, GemDropBiome> dropKv in gemDropBiomes)
			{
				dropKv.Value.distribution ??= gemDistribution.TryGetValue(dropKv.Key, out Dictionary<GemType, float> distribution) ? distribution : new Dictionary<GemType, float>();
			}
			ChestDrops.Apply(new GemDropDef
			{
				biomeConfig = gemDropBiomes,
				blacklist = parsed.Values.Last(p => p.gemDrops.blacklist is not null).gemDrops.blacklist,
			});

			Jewelcrafting.SocketEffects = socketEffects;
			Jewelcrafting.GemDistribution = gemDistribution;
			Jewelcrafting.EffectPowers.Clear();
			Jewelcrafting.GemsUsingPowerRanges.Clear();
			foreach (KeyValuePair<Effect, List<EffectDef>> kv in Jewelcrafting.SocketEffects)
			{
				foreach (EffectDef def in kv.Value)
				{
					if ((def.Type == GemType.Wisplight && Jewelcrafting.wisplightGem.Value == Jewelcrafting.Toggle.Off) || (def.Type == GemType.Wishbone && Jewelcrafting.wishboneGem.Value == Jewelcrafting.Toggle.Off))
					{
						continue;
					}

					if (def.UsesPowerRanges)
					{
						Jewelcrafting.GemsUsingPowerRanges.Add(def.Type);
					}

					void ApplyToGems(List<GameObject> gems)
					{
						for (int i = 0; i < gems.Count; ++i)
						{
							int hash = gems[i].name.GetStableHashCode();
							if (!Jewelcrafting.EffectPowers.TryGetValue(hash, out Dictionary<GemLocation, List<EffectPower>> power))
							{
								power = Jewelcrafting.EffectPowers[hash] = new Dictionary<GemLocation, List<EffectPower>>();
							}
							EffectPower effectPower = new()
							{
								Effect = kv.Key,
								MinConfig = def.MinPower[i],
								MaxConfig = def.MaxPower[i],
								Unique = def.Unique,
								Type = def.Type,
							};
							foreach (GemLocation location in (GemLocation[])Enum.GetValues(typeof(GemLocation)))
							{
								if ((def.Slots & location) == location)
								{
									if (!power.TryGetValue(location, out List<EffectPower> effectPowers))
									{
										effectPowers = power[location] = new List<EffectPower>();
									}
									effectPowers.Add(effectPower with { Location = location });
								}
							}
							foreach (string item in def.Items)
							{
								if (Utils.GetItem(item) is { } itemdrop)
								{
									GemLocation location = Utils.GetItemGemLocation(itemdrop.m_itemData);
									if (!power.TryGetValue(location, out List<EffectPower> effectPowers))
									{
										effectPowers = power[location] = new List<EffectPower>();
									}
									effectPowers.Add(effectPower with { Location = location });
								}
							}
						}
					}

					ApplyToGems(GemStoneSetup.Gems[def.Type].Select(g => g.Prefab).ToList());
					foreach (KeyValuePair<GemType, Dictionary<GemType, GameObject[]>> mergedGem in MergedGemStoneSetup.mergedGems)
					{
						foreach (KeyValuePair<GemType, GameObject[]> mergedKv in mergedGem.Value)
						{
							if (mergedKv.Key == def.Type || mergedGem.Key == def.Type)
							{
								ApplyToGems(mergedKv.Value.ToList());
							}
						}
					}
				}
			}

			if (Player.m_localPlayer)
			{
				TrackEquipmentChanges.CalculateEffects(Player.m_localPlayer);
			}
		}

		public string FilePattern => "Jewelcrafting.*.yml";
		public string EditButtonName => Localization.instance.Localize("$jc_edit_socket_yaml_config");
		public CustomSyncedValue<List<string>> FileData => Jewelcrafting.socketEffectDefinitions;
		public bool Enabled => true;
	}
}
