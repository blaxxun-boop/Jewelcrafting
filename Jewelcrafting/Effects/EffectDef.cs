using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

[Flags]
public enum GemLocation
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
	Weapon = 1 << 12,
	Tool = 1 << 13,
	Shield = 1 << 14,
	Utility = 1 << 15,
	All = 1 << 16,
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
	Tank,
	Paintolerance,
	Berserk,
	Ninja,
	Inconspicuous,
	Nimble,
	Mirror,
	Echo,
	Resonatingechoes,
	Resilience,
	Gourmet,
	Leadingwolf,
	Sharedhealing,
	Safehaven,
	Fleetinglife,
	Cowardice,
	Archerymentor,
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
	Lightningspeed,
	Rootedrevenge,
	Poisonousdrain,
	Icyprotection,
	Fierydoom,
	Togetherforever,
	Neveralone,
	Equilibrium,
	Eternalstudent,
	Timewarp,
	Carefulcutting
}

public enum Uniqueness
{
	None, // Not unique
	All, // Only one of all gems flagged by this
	Gem, // Only one of these gems
	Tier, // Only one gem of the same tier across all items
	Item // Only one gem in the same item
}

public struct EffectPower
{
	public Effect Effect;
	public float Power => (float)Config.GetType().GetFields().First().GetValue(Config);
	public object Config;
	public Uniqueness Unique;
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
		foreach (Type t in typeof(EffectDef).Assembly.GetTypes().Where(t => (t.Namespace ?? "").StartsWith(typeof(EffectDef).Namespace) || (t.Namespace ?? "").StartsWith("Jewelcrafting.SynergyEffects")))
		{
			RuntimeHelpers.RunClassConstructor(t.TypeHandle);
		}
	}

	public GemType Type;
	public GemLocation Slots;
	public Uniqueness Unique = Uniqueness.None;
	public object[] Power = Array.Empty<object>();

	public static readonly Dictionary<Effect, Type> ConfigTypes = new();

	public delegate string? OverrideDescription(Player player, ref float[] numbers);
	public static readonly Dictionary<Effect, OverrideDescription> DescriptionOverrides = new();

	public const GemLocation AllGemlocations = GemLocation.All - 1;
	public const GemLocation WeaponGemlocations = GemLocation.Sword | GemLocation.Knife | GemLocation.Club | GemLocation.Polearm | GemLocation.Spear | GemLocation.Axe;
	
	private static readonly Dictionary<string, Uniqueness> ValidUniquenesses = new(((Uniqueness[])Enum.GetValues(typeof(Uniqueness))).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	private static readonly Dictionary<string, GemLocation> ValidGemLocations = new(((GemLocation[])Enum.GetValues(typeof(GemLocation))).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	public static readonly Dictionary<string, GemType> ValidGemTypes = new(((GemType[])Enum.GetValues(typeof(GemType))).Where(t => t != GemType.Cyan || global::Groups.API.IsLoaded()).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	public static readonly Dictionary<string, Effect> ValidEffects = new(((Effect[])Enum.GetValues(typeof(Effect))).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	private static readonly Dictionary<string, Heightmap.Biome> ValidBiomes = new(((Heightmap.Biome[])Enum.GetValues(typeof(Heightmap.Biome))).Where(b => b is not Heightmap.Biome.None or Heightmap.Biome.BiomesMax or Heightmap.Biome.Ocean).ToDictionary(i => Regex.Replace(i.ToString(), "(?!^)([A-Z])", " $1"), i => i), StringComparer.InvariantCultureIgnoreCase);

	public static readonly Dictionary<GemType, string> GemTypeNames = ValidGemTypes.ToDictionary(kv => kv.Value, kv => kv.Key);
	public static readonly Dictionary<Effect, string> EffectNames = ValidEffects.ToDictionary(kv => kv.Value, kv => kv.Key);

	public static Dictionary<string, object?> castDictToStringDict(Dictionary<object, object?> dict) => new(dict.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value), StringComparer.InvariantCultureIgnoreCase);

	public struct ParseResult
	{
		public Dictionary<Heightmap.Biome, Dictionary<GemType, float>> gemDistribution;
		public Dictionary<Effect, List<EffectDef>> effects;
		public Dictionary<string, SynergyDef> Synergy;
	}

	public static ParseResult Parse(object? rootDictObj, out List<string> errors)
	{
		Dictionary<Effect, List<EffectDef>> effects = new();
		Dictionary<Heightmap.Biome, Dictionary<GemType, float>> gemDistribution = new();
		Dictionary<string, SynergyDef> synergies = new();
		ParseResult configurationResult = new() { gemDistribution = gemDistribution, effects = effects, Synergy = synergies };
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
						if (ValidGemTypes.ContainsKey(gem))
						{
							effectDef.Type = ValidGemTypes[gem];
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
						if (ValidGemLocations.ContainsKey(slot))
						{
							effectDef.Slots |= ValidGemLocations[slot];
						}
						else
						{
							errorList.Add($"A '{slot}' is not a valid location for a gem. Valid locations are: '{string.Join("', '", ValidGemLocations.Keys)}'. Alternatively specify 'none' to drop all slots for this gem color. {errorLocation}");
						}
					}
				}
				else
				{
					errorList.Add($"An effect definition must contain a key 'slot' for determining which slots the gem may be assigned to. {errorLocation}");
					return;
				}

				int tiers = GemStoneSetup.Gems[effectDef.Type].Count;
				effectDef.Power = new object[tiers];

				if (!ConfigTypes.TryGetValue(ValidEffects[effect], out Type configType))
				{
					configType = typeof(DefaultPower);
				}
				Dictionary<string, FieldInfo> configFields = new(configType.GetFields().ToDictionary(f => Regex.Replace(f.Name, "(?!^)([A-Z])", " $1"), f => f), StringComparer.InvariantCultureIgnoreCase);
				if (effectDef.Slots != 0 && configFields.Count > 0)
				{
					if (HasKey("power"))
					{
						for (int i = 0; i < tiers; ++i)
						{
							effectDef.Power[i] = Activator.CreateInstance(configType);
						}

						foreach (FieldInfo field in configFields.Values)
						{
							if (field.GetCustomAttribute<OptionalPowerAttribute>() is { } optional)
							{
								for (int i = 0; i < tiers; ++i)
								{
									field.SetValue(effectDef.Power[i], optional.DefaultValue);
								}
							}
						}

						bool ParseTiers(object? powerObj, FieldInfo? field = null)
						{
							bool hasField = field is not null;
							string fieldLocation = field is not null ? $" for the key '{field.Name}'" : "";
							field ??= configFields.First().Value;
							if (powerObj is Dictionary<object, object?> powersList && !hasField)
							{
								if (configFields.Count != powersList.Count)
								{
									errorList.Add($"There are missing configurable values for this effect. Specify values for all of {string.Join(", ", configFields.Keys)}. {errorLocation}");
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
										if (float.TryParse(powerNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out float power))
										{
											field.SetValue(effectDef.Power[i], power);
										}
										else
										{
											errorList.Add($"The {i}. power{fieldLocation} is not a number. Got unexpected '{powerNumber}'. {errorLocation}");
											return false;
										}
									}
									else
									{
										errorList.Add($"The {i}. power{fieldLocation} is not a number. Got unexpected {powerTiers[i]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
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
										if (float.TryParse(powerNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out float power))
										{
											field.SetValue(effectDef.Power[0], power);
										}
										else
										{
											errorList.Add($"The power{fieldLocation} is not a number. Got unexpected '{powerNumber}'. {errorLocation}");
											return false;
										}
									}
									else
									{
										errorList.Add($"The power{fieldLocation} is not a number. Got unexpected {powerObj?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
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
					for (int i = 0; i < effectList.Count; i++)
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

		public Loader()
		{
			instance = this;
		}

		public List<string> ErrorCheck(object? yaml)
		{
			Parse(yaml, out List<string> errors);
			return errors;
		}

		public List<string> ProcessConfig(string key, object? yaml)
		{
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
			foreach (string key in parsed.Keys.Where(k => k != "" && k != "Groups" && k != "Synergies" && !k.StartsWith("/")).ToArray())
			{
				parsed.Remove(key);
			}
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
							if (def.Slots != 0)
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

			Jewelcrafting.SocketEffects = socketEffects;
			Jewelcrafting.GemDistribution = gemDistribution;
			Jewelcrafting.EffectPowers.Clear();
			foreach (KeyValuePair<Effect, List<EffectDef>> kv in Jewelcrafting.SocketEffects)
			{
				foreach (EffectDef def in kv.Value)
				{
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
								Config = def.Power[i],
								Unique = def.Unique,
							};
							foreach (GemLocation location in (GemLocation[])Enum.GetValues(typeof(GemLocation)))
							{
								if ((def.Slots & location) == location)
								{
									if (!power.TryGetValue(location, out List<EffectPower> effectPowers))
									{
										effectPowers = power[location] = new List<EffectPower>();
									}
									effectPowers.Add(effectPower);
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
				TrackEquipmentChanges.CalculateEffects();
			}
		}

		public string FilePattern => "Jewelcrafting.Sockets*.yml";
		public string EditButtonName => Localization.instance.Localize("$jc_edit_socket_yaml_config");
		public CustomSyncedValue<List<string>> FileData => Jewelcrafting.socketEffectDefinitions;
		public bool Enabled => Jewelcrafting.socketSystem.Value == Jewelcrafting.Toggle.On;
	}
}
