using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jewelcrafting.GemEffects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting.WorldBosses;

public class Prize
{
	public float Chance = float.NaN;
	public readonly string Item;
	public readonly List<string> Sockets = new();

	public Prize(string item) => Item = item;
}

public class Prizes
{
	public string Name = "default";
	public float RotationDays = 0;
	private float durationDays = 0;
	public float DurationDays
	{
		get => durationDays == -1 ? Jewelcrafting.defaultEventDuration.Value : durationDays;
		set => durationDays = value;
	}
	public DateTime StartDate = DateTime.MinValue;
	public DateTime EndDate = DateTime.MaxValue;
	public readonly List<Prize> prizes = new();
	public readonly HashSet<string> blackList = new();
}

public static class GachaDef
{
	private static List<Prizes> prizesList = new();
	private static readonly Dictionary<string, ItemDrop> items = new(StringComparer.InvariantCultureIgnoreCase);

	public static Prizes? Parse(string name, Dictionary<string, object?> prizesConfig, List<string> errorList)
	{
		Prizes prizes = new() { Name = name };

		string errorLocation = $"Found in prize definition '{name}'.";

		HashSet<string> knownKeys = new(StringComparer.InvariantCultureIgnoreCase);
		bool HasKey(string key)
		{
			knownKeys.Add(key);
			return prizesConfig.ContainsKey(key);
		}

		if (HasKey("days"))
		{
			if (prizesConfig["days"] is string daysString)
			{
				if (float.TryParse(daysString, NumberStyles.Float, CultureInfo.InvariantCulture, out float days))
				{
					prizes.RotationDays = days;
				}
				else
				{
					errorList.Add($"The days is not a number. Got unexpected '{daysString}'. {errorLocation}");
					return null;
				}
			}
			else
			{
				errorList.Add($"The days is not a number. Got unexpected {prizesConfig["days"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
				return null;
			}
		}

		if (HasKey("duration"))
		{
			if (prizesConfig["duration"] is string durationString)
			{
				if (durationString == "default")
				{
					prizes.DurationDays = -1;
				}
				else if (float.TryParse(durationString, NumberStyles.Float, CultureInfo.InvariantCulture, out float days))
				{
					prizes.DurationDays = days;
				}
				else
				{
					errorList.Add($"The duration is not 'default' or a number. Got unexpected '{durationString}'. {errorLocation}");
					return null;
				}
			}
			else
			{
				errorList.Add($"The duration is not a string. Got unexpected {prizesConfig["duration"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
				return null;
			}
		}

		void parseDate(string name, ref DateTime target)
		{
			if (HasKey(name))
			{
				switch (prizesConfig[name])
				{
					case Dictionary<object, object?> dateObjDict:
					{
						Dictionary<string, object?> dateDict = EffectDef.castDictToStringDict(dateObjDict);
						HashSet<string> knownDateKeys = new(StringComparer.InvariantCultureIgnoreCase);
						int? ParseInt(string field)
						{
							knownDateKeys.Add(field);
							if (dateDict.ContainsKey(field))
							{
								if (dateDict[field] is not string timeString)
								{
									errorList.Add($"The '{name}' date is not a valid date. {field} is not a number. Got unexpected {dateDict["field"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
								}
								else if (int.TryParse(timeString, out int time))
								{
									return time;
								}
								else
								{
									errorList.Add($"The '{name}' date is not a valid date. {field} is not a number. Got unexpected '{timeString}'. {errorLocation}");
								}
							}
							return null;
						}

						int year = ParseInt("year") ?? DateTime.Now.Year;
						int month = ParseInt("month") ?? DateTime.Now.Month;
						int day = ParseInt("day") ?? DateTime.Now.Day;
						int hour = ParseInt("hour") ?? 0;
						int minute = ParseInt("minute") ?? 0;
						int second = ParseInt("second") ?? 0;

						try
						{
							target = new DateTime(year, month, day, hour, minute, second);
						}
						catch (ArgumentOutOfRangeException)
						{
							errorList.Add($"The '{name}' date is not a valid date. Got invalid {year}-{month}-{day} {hour}:{minute}:{second}. {errorLocation}");
						}

						errorList.AddRange(from key in dateDict.Keys where !knownDateKeys.Contains(key) select $"A '{name}' date may not contain a key '{key}'. {errorLocation}");
						break;
					}
					case string dateString when DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date):
						target = date;
						break;
					case string dateString when int.TryParse(dateString, out int unix):
						target = DateTimeOffset.FromUnixTimeSeconds(unix).DateTime;
						break;
					case string dateString:
						errorList.Add($"The '{name}' date is not a date. Got unexpected '{dateString}'. {errorLocation}");
						break;
					default:
						errorList.Add($"The '{name}' date is not a date (as unix timestamp or mapping of year, month, day, hour, minute and second). Got unexpected {prizesConfig["days"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
						break;
				}
			}
		}

		parseDate("start", ref prizes.StartDate);
		parseDate("end", ref prizes.EndDate);

		if (HasKey("prizes"))
		{
			if (prizesConfig["prizes"] is Dictionary<object, object?> prizesDict)
			{
				foreach (KeyValuePair<string, object?> prizesKv in EffectDef.castDictToStringDict(prizesDict))
				{
					Prize prize = new(prizesKv.Key);

					if (prizesKv.Value is string prizeChance)
					{
						if (float.TryParse(prizeChance, NumberStyles.Float, CultureInfo.InvariantCulture, out float chance))
						{
							prize.Chance = chance;
						}
						else
						{
							errorList.Add($"The prize chance for prize item '{prize.Item}' is not a number denoting a chance. Got unexpected '{prizeChance}'. {errorLocation}");
							return null;
						}
					}
					else if (prizesKv.Value is Dictionary<object, object?> prizeObjDict)
					{
						string prizeErrorLocation = $"Found in prize config for item '{prize.Item}' of prize definition '{name}'.";

						Dictionary<string, object?> prizeDict = EffectDef.castDictToStringDict(prizeObjDict);
						HashSet<string> knownPrizeKeys = new(StringComparer.InvariantCultureIgnoreCase);
						bool HasPrizeKey(string key)
						{
							knownPrizeKeys.Add(key);
							return prizeDict.ContainsKey(key);
						}

						if (HasPrizeKey("chance"))
						{
							if (prizeDict["chance"] is string chanceString)
							{
								if (float.TryParse(chanceString, NumberStyles.Float, CultureInfo.InvariantCulture, out float chance))
								{
									prize.Chance = chance;
								}
								else
								{
									errorList.Add($"The chance is not a number. Got unexpected '{chanceString}'. {prizeErrorLocation}");
									return null;
								}
							}
							else
							{
								errorList.Add($"The chance is not a number. Got unexpected {prizeDict["chance"]?.GetType().ToString() ?? "empty string (null)"}. {prizeErrorLocation}");
								return null;
							}
						}

						if (HasPrizeKey("sockets"))
						{
							if (prizeDict["sockets"] is List<object?> socketsList)
							{
								foreach (object? socketObj in socketsList)
								{
									if (socketObj is string socket)
									{
										prize.Sockets.Add(socket);
									}
									else
									{
										errorList.Add($"Found something that is not a socket name in sockets list. Got unexpected {socketObj?.GetType().ToString() ?? "empty string (null)"}. {prizeErrorLocation}");
									}
								}
							}
							else
							{
								errorList.Add($"The chance is not a list of socket names. Got unexpected {prizeDict["sockets"]?.GetType().ToString() ?? "empty string (null)"}. {prizeErrorLocation}");
							}
						}

						errorList.AddRange(from key in prizeDict.Keys where !knownPrizeKeys.Contains(key) select $"A prize config may not contain a key '{key}'. {prizeErrorLocation}");
					}
					else if (prizesKv.Value is not null)
					{
						errorList.Add($"The configuration for prize item '{prize.Item}' is not a number denoting a chance not a full prize configuration mapping. Got unexpected {prizesKv.Value.GetType()}. {errorLocation}");
						return null;
					}

					prizes.prizes.Add(prize);
				}
			}
			else
			{
				errorList.Add($"The prizes are not a mapping of item to prize configuration. Got unexpected {prizesConfig["prizes"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
			}
		}

		double totalChances = 0;
		List<Prize> prizeWithoutChance = new();
		foreach (Prize prize in prizes.prizes)
		{
			if (float.IsNaN(prize.Chance))
			{
				prizeWithoutChance.Add(prize);
			}
			else
			{
				totalChances += prize.Chance;
			}
		}

		if (totalChances > 1.0001)
		{
			errorList.Add($"The sum of chances of the prizes must not be greater than 1. Found a total chance of {totalChances} for prize definition '{name}'.");
		}
		if (prizeWithoutChance.Count > 0)
		{
			float averageChance = (float)((1 - totalChances) / prizeWithoutChance.Count);
			foreach (Prize prize in prizeWithoutChance)
			{
				prize.Chance = averageChance;
			}
		}

		if (HasKey("blacklist"))
		{
			if (prizesConfig["blacklist"] is List<object?> socketsList)
			{
				foreach (object? socketObj in socketsList)
				{
					if (socketObj is string socket)
					{
						prizes.blackList.Add(socket);
					}
					else
					{
						errorList.Add($"Found something that is not a socket name in sockets blacklist. Got unexpected {socketObj?.GetType().ToString() ?? "empty string (null)"} in blacklist of prize definition '{name}'.");
					}
				}
			}
			else
			{
				errorList.Add($"The blacklist is not a list of socket names. Got unexpected {prizesConfig["blacklist"]?.GetType().ToString() ?? "empty string (null)"} in prize definition '{name}'.");
			}
		}

		errorList.AddRange(from key in prizesConfig.Keys where !knownKeys.Contains(key) select $"A prize definition may not contain a key '{key}'. {errorLocation}");

		return prizes;
	}

	public static void Apply(List<Prizes> prizeLists)
	{
		items.Clear();
		foreach (ItemDrop item in ObjectDB.instance.m_items.Select(p => p.GetComponent<ItemDrop>()).Where(c => c != null))
		{
			foreach (string name in prefabLocalizations(item))
			{
				items[name] = item;
			}
		}

		foreach (Prizes prizes in prizeLists)
		{
			List<Prize> invalidPrizes = new();
			foreach (Prize prize in prizes.prizes)
			{
				if (getItem(prize.Item) is { } item)
				{
					if (prize.Sockets.Count > 0 && !Utils.IsSocketableItem(item))
					{
						Debug.LogWarning($"Prize item {prize.Item} for prize definition '{prizes.Name}' has a Sockets list despite not being socketable. Ignoring.");
						invalidPrizes.Add(prize);
						continue;
					}

					foreach (string socket in prize.Sockets)
					{
						if (socket != "empty" && (getItem(socket) is not { } socketItem || !GemStones.socketableGemStones.Contains(socketItem.m_itemData.m_shared.m_name)))
						{
							Debug.LogWarning($"Socket {socket} for prize item {prize.Item} for prize definition '{prizes.Name}' does not exist. Ignoring.");
							invalidPrizes.Add(prize);
						}
					}
				}
				else
				{
					Debug.LogWarning($"Prize item {prize.Item} for prize definition '{prizes.Name}' does not exist. Ignoring.");
					invalidPrizes.Add(prize);
				}
			}
			prizes.prizes.RemoveAll(p => invalidPrizes.Contains(p));
		}

		prizesList = prizeLists;
	}

	private static List<string> prefabLocalizations(ItemDrop prefab) => new()
	{
		prefab.name.ToLower(),
		prefab.m_itemData.m_shared.m_name.ToLower(),
		Localization.instance.Localize(prefab.m_itemData.m_shared.m_name).ToLower(),
		Jewelcrafting.english.Localize(prefab.m_itemData.m_shared.m_name).ToLower()
	};

	public static ItemDrop? getItem(string name)
	{
		if (items.ContainsKey(name))
		{
			return items[name];
		}

		List<GameObject> gems = (name switch
		{
			"frame" => MiscSetup.framePrefabs,
			"simple gem" => GemStoneSetup.Gems.Values.Where(g => g.Count > 1).Select(g => g[0].Prefab),
			"advanced gem" => GemStoneSetup.Gems.Values.Where(g => g.Count > 1).Select(g => g[1].Prefab),
			"perfect gem" => GemStoneSetup.Gems.Values.Where(g => g.Count > 2).Select(g => g[2].Prefab),
			"simple merged gem" => MergedGemStoneSetup.mergedGems.Values.SelectMany(kv => kv.Values).Where(g => g.Length > 1).Select(g => g[0]),
			"advanced merged gem" => MergedGemStoneSetup.mergedGems.Values.SelectMany(kv => kv.Values).Where(g => g.Length > 1).Select(g => g[1]),
			"perfect merged gem" => MergedGemStoneSetup.mergedGems.Values.SelectMany(kv => kv.Values).Where(g => g.Length > 2).Select(g => g[2]),
			"boss gem" => GemStoneSetup.Gems.Values.Where(g => g.Count == 1).Select(g => g[0].Prefab),
			_ => Array.Empty<GameObject>()
		}).ToList();

		return gems.Count > 0 ? gems[Random.Range(0, gems.Count)].GetComponent<ItemDrop>() : null;
	}

	public static Prizes? ActivePrizes()
	{
		if (prizesList.FirstOrDefault(p => p.StartDate < DateTime.Now && p.EndDate > DateTime.Now && p.DurationDays <= 0 && p.Name != "default") is { } prizes)
		{
			return prizes;
		}

		List<Prizes> repeatingPrizes = prizesList.Where(p => p.DurationDays > 0).ToList();
		if (repeatingPrizes.Count > 0)
		{
			float totalDays = repeatingPrizes.Sum(p => p.DurationDays);
			long now = DateTimeOffset.Now.ToUnixTimeSeconds();
			double current = (double)now / 86400 % totalDays;
			foreach (Prizes p in prizesList)
			{
				p.StartDate = DateTimeOffset.FromUnixTimeSeconds(now - (long)(current * 86400)).UtcDateTime;
				current -= p.DurationDays;
				if (current <= 0)
				{
					return p;
				}
			}
			return repeatingPrizes.Last();
		}

		return prizesList.FirstOrDefault(p => p.Name == "default");
	}
}
