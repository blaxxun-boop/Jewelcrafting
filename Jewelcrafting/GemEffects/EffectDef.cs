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
	Weapon = Sword | Knife | Club | Polearm | Spear | Axe,
	Tool = 1 << 12,
	Shield = 1 << 13,
	Utility = 1 << 14,
	All = (1 << 15) - 1
}

public enum Effect
{
	Sprinter,
	Defender,
	Firestarter,
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
	Hercules,
	Vampire,
	Masterarcher,
	Tank,
	Paintolerance,
	Berserk,
	Ninja,
	Inconspicuous,
	Nimble,
	Mirror,
	Resilience,
	Gourmet,
	Stealtharcher,
	Mercifuldeath,
	Turtleshell,
	Marathon,
	Unbreakable,
	Glowingspirit,
	Lightningspeed,
	Rootedrevenge,
	Poisonousdrain,
	Icyprotection,
	Fierydoom
}

public enum GemType
{
	Black,
	Blue,
	Green,
	Purple,
	Red,
	Yellow,
	Eikthyr,
	Elder,
	Bonemass,
	Moder,
	Yagluth
}

public struct EffectPower
{
	public Effect Effect;
	public float Power => (float)Config.GetType().GetFields().First().GetValue(Config);
	public object Config;
	public bool Unique;
}

[AttributeUsage(AttributeTargets.Field)]
public abstract class PowerAttribute : Attribute
{
	public abstract float Add(float a, float b);
}

public class AdditivePowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => a + b;
}

// Use when doing 1 + effect / 100
public class MultiplicativePercentagePowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => ((1 + a / 100) * (1 + b / 100) - 1) * 100;
}

// Use when doing 1 - effect / 100 or when doing Random.Value < effect power
public class InverseMultiplicativePercentagePowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => (1 - (1 - a / 100) * (1 - b / 100)) * 100;
}

public class MinPowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => Mathf.Min(a, b);
}

public class MaxPowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => Mathf.Max(a, b);
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
		foreach (Type t in typeof(EffectDef).Assembly.GetTypes().Where(t => t.Namespace == typeof(EffectDef).Namespace))
		{
			RuntimeHelpers.RunClassConstructor(t.TypeHandle);
		}
	}

	public GemType Type;
	public GemLocation Slots;
	public bool Unique = false;
	public object[] Power = Array.Empty<object>();

	public static readonly Dictionary<Effect, Type> ConfigTypes = new();

	private static readonly Dictionary<string, GemLocation> ValidGemLocations = new(((GemLocation[])Enum.GetValues(typeof(GemLocation))).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	private static readonly Dictionary<string, GemType> ValidGemTypes = new(((GemType[])Enum.GetValues(typeof(GemType))).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	private static readonly Dictionary<string, Effect> ValidEffects = new(((Effect[])Enum.GetValues(typeof(Effect))).ToDictionary(i => i.ToString(), i => i), StringComparer.InvariantCultureIgnoreCase);
	private static readonly Dictionary<string, Heightmap.Biome> ValidBiomes = new(((Heightmap.Biome[])Enum.GetValues(typeof(Heightmap.Biome))).Where(b => b is not Heightmap.Biome.None or Heightmap.Biome.BiomesMax or Heightmap.Biome.Ocean).ToDictionary(i => Regex.Replace(i.ToString(), "(?!^)([A-Z])", " $1"), i => i), StringComparer.InvariantCultureIgnoreCase);

	private static Dictionary<string, object?> castDictToStringDict(Dictionary<object, object?> dict) => new(dict.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value), StringComparer.InvariantCultureIgnoreCase);

	public static KeyValuePair<Dictionary<Heightmap.Biome, Dictionary<GemType, float>>, Dictionary<Effect, List<EffectDef>>> Parse(object? rootDictObj, out List<string> errors)
	{
		Dictionary<Effect, List<EffectDef>> effects = new();
		Dictionary<Heightmap.Biome, Dictionary<GemType, float>> gemDistribution = new();
		KeyValuePair<Dictionary<Heightmap.Biome, Dictionary<GemType, float>>, Dictionary<Effect, List<EffectDef>>> configurationResult = new(gemDistribution, effects);
		errors = new List<string>();

		if (rootDictObj is not Dictionary<object, object?> rootDict)
		{
			errors.Add($"All top-level keys must be a mapping. Got unexpected {rootDictObj?.GetType().ToString() ?? "empty string (null)"}.");
			return configurationResult;
		}

		List<string> errorList = errors;
		bool ParseBool(object? inputObj, string error)
		{
			if (inputObj is not string input)
			{
				errorList.Add(string.Format(error, $"Got unexpected {inputObj?.GetType().ToString() ?? "empty string (null)"}, expecting true or false."));
				return false;
			}

			string[] falsy = { "0", "false", "off", "no", "nope", "nah", "-", "hell no", "pls dont", "lol no" };
			string[] truthy = { "1", "true", "on", "yes", "yep", "yeah", "+", "hell yeah", "ok", "okay", "k" };
			if (truthy.Contains(input.ToLower()))
			{
				return true;
			}

			if (!falsy.Contains(input.ToLower()))
			{
				errorList.Add(string.Format(error, $"Got unexpected '{input}', expecting true or false."));
			}

			return false;
		}

		foreach (KeyValuePair<string, object?> rootDictKv in castDictToStringDict(rootDict))
		{
			string effect = rootDictKv.Key.Replace(" ", "");
			if (!ValidEffects.ContainsKey(effect))
			{
				if (effect != "gems")
				{
					errors.Add($"'{effect}' is not a valid effect name. Valid effects are: '{string.Join("', '", ValidEffects.Keys)}'. There also can be a 'gems' section to define gem spawn chances.");
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
							else
							{
								errors.Add($"Gem chances are defined individually as a mapping of gem type and chance. Got unexpected {biomeKv.Value?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
							}

							if (spawnChance.Values.Sum() - 0.00001f > 1)
							{
								errors.Add($"The sum of all gem chances must not exceed 1. {errorLocation}");
								continue;
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
						slots.Add(singleSlot);
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
							errorList.Add($"A '{slot}' is not a valid location for a gem. Valid locations are: '{string.Join("', '", ValidGemLocations.Keys)}'. {errorLocation}");
						}
					}
					if (effectDef.Slots == 0)
					{
						errorList.Add($"An effect definition must contain at least one location the gem can be inserted into. {errorLocation}");
						return;
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
				if (configFields.Count > 0)
				{
					if (HasKey("power"))
					{
						for (int i = 0; i < tiers; ++i)
						{
							effectDef.Power[i] = Activator.CreateInstance(configType);
						}

						bool ParseTiers(object? powerObj, FieldInfo? field = null)
						{
							bool hasField = field is not null;
							string fieldLocation = field is not null ? $" for the key '{field.Name}'" : "";
							field ??= configFields.First().Value;
							if (powerObj is Dictionary<object, object?> powersList && !hasField)
							{
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
							else if (!hasField && configFields.Count > 1)
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
					effectDef.Unique = ParseBool(effectDict["unique"], $"The unique flag must be a boolean. {{0}} {errorLocation}");
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
					errors.Add($"Effects must either contain an effect definition, which must be a mapping of effect properties, or a list of effect definitions. Got unexpected {rootDictKv.Value?.GetType().ToString() ?? "empty string (null)"} for effect '{effect}'.");
					break;
			}
		}

		return configurationResult;
	}

	public class Loader : ConfigLoader.Loader
	{
		private Dictionary<Effect, List<EffectDef>> SocketEffects = new();
		private Dictionary<Heightmap.Biome, Dictionary<GemType, float>> GemDistribution = new();

		public List<string> ErrorCheck(object? yaml)
		{
			Parse(yaml, out List<string> errors);
			return errors;
		}

		public List<string> ProcessConfig(object? yaml)
		{
			KeyValuePair<Dictionary<Heightmap.Biome, Dictionary<GemType, float>>, Dictionary<Effect, List<EffectDef>>> result = Parse(yaml, out List<string> errors);
			GemDistribution = result.Key;
			SocketEffects = result.Value;
			return errors;
		}

		public void ApplyConfig()
		{
			Jewelcrafting.SocketEffects = new Dictionary<Effect, List<EffectDef>>(SocketEffects);
			Jewelcrafting.GemDistribution = new Dictionary<Heightmap.Biome, Dictionary<GemType, float>>(GemDistribution);
			Jewelcrafting.EffectPowers.Clear();
			foreach (KeyValuePair<Effect, List<EffectDef>> kv in Jewelcrafting.SocketEffects)
			{
				foreach (EffectDef def in kv.Value)
				{
					List<GemDefinition> gems = GemStoneSetup.Gems[def.Type];
					for (int i = 0; i < gems.Count; ++i)
					{
						GemDefinition gem = gems[i];
						int hash = gem.Prefab.name.GetStableHashCode();
						if (!Jewelcrafting.EffectPowers.TryGetValue(hash, out Dictionary<GemLocation, EffectPower> power))
						{
							power = Jewelcrafting.EffectPowers[hash] = new Dictionary<GemLocation, EffectPower>();
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
								power[location] = effectPower;
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
