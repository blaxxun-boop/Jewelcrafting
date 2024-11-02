using System;
using System.Collections.Generic;
using System.Linq;
using Jewelcrafting.GemEffects;
using Jewelcrafting.LootSystem;

namespace Jewelcrafting;

public static class Socketing
{
	private static Dictionary<Heightmap.Biome, Dictionary<string, int>[]> SocketCosts = null!;
	public static Dictionary<Heightmap.Biome, Piece.Requirement[][]> SocketRequirements = null!;
	
	public static Dictionary<Heightmap.Biome, Dictionary<int, Dictionary<string, int>?>>? Parse(object? costs, List<string> errors)
	{
		if (costs is Dictionary<object, object?> costsDict)
		{
			Dictionary<Heightmap.Biome, Dictionary<int, Dictionary<string, int>?>> def = new();
			foreach (KeyValuePair<string, object?> biomeKv in EffectDef.castDictToStringDict(costsDict))
			{
				if (EffectDef.ValidBiomes.TryGetValue(biomeKv.Key, out Heightmap.Biome biome))
				{
					string errorLocation = $"in {biomeKv.Key} section in 'socket cost' section";
					if (biomeKv.Value is Dictionary<object, object?> biomeDict)
					{
						Dictionary<int, Dictionary<string, int>?> biomeCosts = new();
						foreach (KeyValuePair<string, object?> tierKv in EffectDef.castDictToStringDict(biomeDict))
						{
							if (int.TryParse(tierKv.Key, out int tier) && tier is >= 1 and <= Jewelcrafting.maxNumberOfSockets)
							{
								if (tierKv.Value is Dictionary<object, object?> tierDict)
								{
									Dictionary<string, int> tierCosts = new();
									foreach (KeyValuePair<string, object?> resourceKv in EffectDef.castDictToStringDict(tierDict))
									{
										if (resourceKv.Value is string numString && int.TryParse(numString, out int num) && num > 0)
										{
											tierCosts[resourceKv.Key] = num;
										}
										else
										{
											errors.Add($"The amount must be a positive number. Found unexpected key {resourceKv.Value?.GetType().ToString() ?? "empty string (null)"} for costs of socket {tier} {errorLocation}.");
										}
									}
									biomeCosts.Add(tier, tierCosts);
								}
								else if (tierKv.Value is "disabled")
								{
									biomeCosts.Add(tier, null);
								}
								else
								{
									errors.Add($"The resources must be either 'disabled' or a map of resources to the amount of resources. Found unexpected value {tierKv.Value?.GetType().ToString() ?? "empty string (null)"} for socket {tier} {errorLocation}.");
								}
							}
							else
							{
								errors.Add($"The per biome configuration must be a mapping of socket number (starting with 1, and lower than the max number of sockets possible: {Jewelcrafting.maxNumberOfSockets}) to a map of resources. Found unexpected key {tierKv.Key} {errorLocation}.");
							}
						}
						def.Add(biome, biomeCosts);
					}
					else
					{
						errors.Add($"The per biome configuration must be a mapping of socket number (starting with 1) to a map of resources. Got unexpected {biomeKv.Value?.GetType().ToString() ?? "empty string (null)"} {errorLocation}.");
					}
				}
				else
				{
					errors.Add($"Found invalid biome '{biomeKv.Key}' in 'socket cost' section. Valid keys are the biomes: '{string.Join("', '", EffectDef.ValidBiomes.Keys)}'.");
				}
			}
			return def;
		}

		errors.Add($"The 'socket cost' section must be a mapping of biomes to biome specific configuration, got unexpected {costs?.GetType().ToString() ?? "empty string (null)"}.");
		return null;
	}

	public static void Apply(Dictionary<Heightmap.Biome, Dictionary<int, Dictionary<string, int>?>> costs)
	{
		SocketCosts = costs.ToDictionary(kv => kv.Key, kv =>
		{
			Dictionary<string, int>[] resources = new Dictionary<string, int>[Jewelcrafting.maxNumberOfSockets];
			Dictionary<string, int> last = new();
			for (int i = 0; i < Jewelcrafting.maxNumberOfSockets; ++i)
			{
				if (kv.Value.TryGetValue(i + 1, out Dictionary<string, int>? value))
				{
					if (value is null)
					{
						Array.Resize(ref resources, i);
						break;
					}
					last = value;
				}
				resources[i] = last;
			}
			return resources;
		});
		SocketRequirements = null!;
	}

	public static void EnsureCostsCache()
	{
		EquipmentDrops.EnsureDropCache();
		SocketRequirements = SocketCosts.ToDictionary(kv => kv.Key, kv => kv.Value.Select(costs =>
		{
			List<Piece.Requirement> requirements = new();
			foreach (KeyValuePair<string, int> kv in costs)
			{
				if (Utils.GetItem(kv.Key) is { } item)
				{
					requirements.Add(new Piece.Requirement { m_amount = kv.Value, m_resItem = item });
				}
			}
			return requirements.ToArray();
		}).ToArray());
	}
}
