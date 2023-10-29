using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using JetBrains.Annotations;
using UnityEngine;
#if ! API
using System.Diagnostics;
using System.Linq;
using ItemDataManager;
using Jewelcrafting.GemEffects;
using Jewelcrafting.WorldBosses;
using LocalizationManager;
using YamlDotNet.Serialization;
using Jewelcrafting.LootSystem;
using Debug = UnityEngine.Debug;
#endif

namespace Jewelcrafting;

[PublicAPI]
public static class API
{
	public static event Action? OnEffectRecalc;

	public static bool IsLoaded()
	{
#if API
		return false;
#else
		return true;
#endif
	}

	internal static void InvokeEffectRecalc() => OnEffectRecalc?.Invoke();

#if ! API
	private static GameObject CreateNecklaceFromTemplate(string colorName, MaterialColor color)
	{
		GameObject necklace = JewelrySetup.CreateNecklaceFromTemplate(colorName, color);
		MarkJewelry(necklace);
		return necklace;
	}
#endif

	public static GameObject CreateNecklaceFromTemplate(string colorName, Color color)
	{
#if ! API
		return CreateNecklaceFromTemplate(colorName, new MaterialColor { Color = color });
#else
		return null!;
#endif
	}

	public static GameObject CreateNecklaceFromTemplate(string colorName, Material material)
	{
#if ! API
		return CreateNecklaceFromTemplate(colorName, new MaterialColor { Material = material });
#else
		return null!;
#endif
	}

#if ! API
	private static GameObject CreateRingFromTemplate(string colorName, MaterialColor color)
	{
		GameObject ring = JewelrySetup.CreateRingFromTemplate(colorName, color);
		MarkJewelry(ring);
		return ring;
	}
#endif

	public static GameObject CreateRingFromTemplate(string colorName, Color color)
	{
#if ! API
		return CreateRingFromTemplate(colorName,new MaterialColor { Color = color });
#else
		return null!;
#endif
	}

	public static GameObject CreateRingFromTemplate(string colorName, Material material)
	{
#if ! API
		return CreateRingFromTemplate(colorName, new MaterialColor { Material = material });
#else
		return null!;
#endif
	}

	public static void MarkJewelry(GameObject jewelry)
	{
#if ! API
		JewelrySetup.MarkJewelry(jewelry);
#endif
	}

#if ! API
	private static List<GameObject> AddGems(string type, string colorName, MaterialColor color)
	{
		if (string.Equals(colorName, "White", StringComparison.InvariantCultureIgnoreCase))
		{
			throw new Exception($"{colorName} is a reserved color.");
		}
		List<GameObject> objects = new()
		{
			AddShardFromTemplate(type, colorName, color),
			RegisterUncut(type, colorName, AddUncutFromTemplate(type, colorName, color)),
			AddDestructibleFromTemplate(type, colorName, color),
		};
		objects.AddRange(AddTieredGemFromTemplate(type, colorName, color));
		return objects;
	}
#endif

	public static void AddGems(string type, string colorName, Color color)
	{
#if ! API
		AddGems(type, colorName, new MaterialColor { Color = color });
#endif
	}

	public static List<GameObject> AddGems(string type, string colorName, Material material, Color color)
	{
#if ! API
		return AddGems(type, colorName, new MaterialColor { Material = material, Color = color });
#else
		return null!;
#endif
	}

#if ! API
	private static GameObject AddDestructibleFromTemplate(string type, string colorName, MaterialColor color)
	{
		if (!GemStoneSetup.uncutGems.ContainsKey((GemType)colorName.GetStableHashCode()))
		{
			throw new Exception($"A destructible for {colorName} must be registered after the uncut gemstone");
		}

		GameObject prefab = DestructibleSetup.CreateDestructibleFromTemplate(DestructibleSetup.customDestructiblePrefab, colorName, color);
		Localizer.AddText(prefab.GetComponent<HoverText>().m_text.Substring(1), type + " Formation");
		AddDestructible(prefab, colorName);

		string assemblyName = new StackTrace().GetFrame(1).GetMethod().DeclaringType!.Assembly.GetName().Name;
		EffectDef.Loader.instance.parsed.Add($"/{assemblyName}/{type}.yml", new EffectDef.ParseResult
		{
			effects = new Dictionary<Effect, List<EffectDef>>(),
			Synergy = new Dictionary<string, SynergyDef>(),
			gemDistribution = EffectDef.Loader.instance.DefaultConfig.gemDistribution.ToDictionary(kv => kv.Key, _ => new Dictionary<GemType, float>
				{ { (GemType)colorName.GetStableHashCode(), 0.04f } }),
			equipmentDrops = new EquipmentDropDef(),
			gemDrops = new GemDropDef(),
			Prizes = new List<Prizes>(),
		});

		return prefab;
	}
#endif

	public static GameObject AddDestructibleFromTemplate(string type, string colorName, Color color)
	{
#if ! API
		return AddDestructibleFromTemplate(type, colorName, new MaterialColor { Color = color });
#else
		return null!;
#endif
	}
	
	public static GameObject AddDestructibleFromTemplate(string type, string colorName, Material material)
	{
#if ! API
		return AddDestructibleFromTemplate(type, colorName, new MaterialColor { Material = material });
#else
		return null!;
#endif
	}

#if ! API
	private static GameObject AddUncutFromTemplate(string type, string colorName, MaterialColor color)
	{
		GameObject prefab = GemStoneSetup.CreateUncutFromTemplate(GemStoneSetup.customUncutGemPrefab, colorName, color);

		ItemDrop.ItemData.SharedData shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		Localizer.AddText(shared.m_name.Substring(1), type + " Gemstone");
		Localizer.AddText(shared.m_description.Substring(1), $"A {colorName} gemstone, ready to be cut at a Gemcutters Table.");

		return prefab;
	}
#endif
	
	public static GameObject AddUncutFromTemplate(string type, string colorName, Color color)
	{
#if ! API
		return AddUncutFromTemplate(type, colorName, new MaterialColor { Color = color });
#else
		return null!;
#endif
	}
	
	public static GameObject AddUncutFromTemplate(string type, string colorName, Material material)
	{
#if ! API
		return AddUncutFromTemplate(type, colorName, new MaterialColor { Material = material });
#else
		return null!;
#endif
	}

#if ! API
	private static GameObject RegisterUncut(string type, string colorName, GameObject uncutGem)
	{
		AddUncutGem(uncutGem, colorName, Jewelcrafting.config("Gem Drops", $"Drop chance for {type} Gemstones", 1.5f, new ConfigDescription($"Chance to drop an {type.ToLower()} gemstone when killing creatures.", new AcceptableValueRange<float>(0, 100))));
		return uncutGem;
	}
#endif
	
	public static GameObject AddAndRegisterUncutFromTemplate(string type, string colorName, Color color)
	{
#if ! API
		return RegisterUncut(type, colorName, AddUncutFromTemplate(type, colorName, color));
#else
		return null!;
#endif
	}

	public static GameObject AddAndRegisterUncutFromTemplate(string type, string colorName, Material material)
	{
#if ! API
		return RegisterUncut(type, colorName, AddUncutFromTemplate(type, colorName, material));
#else
		return null!;
#endif
	}

#if ! API
	private static GameObject AddShardFromTemplate(string type, string colorName, MaterialColor color)
	{
		GameObject prefab = GemStoneSetup.CreateShardFromTemplate(GemStoneSetup.customGemShardPrefab, colorName, color);
		AddShard(prefab, colorName);

		ItemDrop.ItemData.SharedData shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		Localizer.AddText(shared.m_name.Substring(1), type + " Shard");
		Localizer.AddText(shared.m_description.Substring(1), $"A {colorName} gemstone, which can be socketed into an equipment piece, to unlock the power within.");

		return prefab;
	}
#endif
		
	public static GameObject AddShardFromTemplate(string type, string colorName, Color color)
	{
#if ! API
		return AddShardFromTemplate(type, colorName, new MaterialColor { Color = color });
#else
		return null!;
#endif
	}
	
	public static GameObject AddShardFromTemplate(string type, string colorName, Material material)
	{
#if ! API
		return AddShardFromTemplate(type, colorName, new MaterialColor { Material = material });
#else
		return null!;
#endif
	}
	
#if ! API
	private static GameObject[] AddTieredGemFromTemplate(string type, string colorName, MaterialColor color)
	{
			GameObject[] prefabs = new GameObject[GemStoneSetup.customGemTierPrefabs.Length];
        	Localizer.AddText($"jc_merged_gemstone_{colorName.Replace(" ", "_").ToLower()}", type);
        	for (int tier = 0; tier < prefabs.Length; ++tier)
        	{
        		GameObject prefab = GemStoneSetup.CreateGemFromTemplate(GemStoneSetup.customGemTierPrefabs[tier], colorName, color, tier);
    
        		ItemDrop.ItemData.SharedData shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
        		Localizer.AddText(shared.m_name.Substring(1), tier switch { 0 => "Simple", 1 => "Advanced", _ => "Perfect" } + " " + type);
        		Localizer.AddText(shared.m_description.Substring(1), $"A {colorName} gemstone, which can be socketed into an equipment piece, to unlock the power within.");
    
        		GemStoneSetup.RegisterTieredGemItem(prefab, colorName, tier);
        		AddGem(prefab, colorName);
        		prefabs[tier] = prefab;
        	}
    
        	GemType gemType = (GemType)colorName.GetStableHashCode();
        	MergedGemStoneSetup.mergedGems[gemType] = new Dictionary<GemType, GameObject[]>();
        	foreach (KeyValuePair<GemType, MaterialColor> other in GemStoneSetup.Colors)
        	{
        		MergedGemStoneSetup.CreateMergedGemStone(new KeyValuePair<GemType, MaterialColor>(gemType, color), other);
        		MergedGemStoneSetup.CreateMergedGemStone(other, new KeyValuePair<GemType, MaterialColor>(gemType, color));
        	}
        	GemStoneSetup.Colors.Add(gemType, color);
    
        	return prefabs;
	}
#endif

	public static GameObject[] AddTieredGemFromTemplate(string type, string colorName, Color color)
	{
#if ! API
		return AddTieredGemFromTemplate(type, colorName, new MaterialColor { Color = color });
#else
		return null!;
#endif
	}

	public static GameObject[] AddTieredGemFromTemplate(string type, string colorName, Material material, Color color)
	{
#if ! API
		return AddTieredGemFromTemplate(type, colorName, new MaterialColor { Material = material, Color = color });
#else
		return null!;
#endif
	}

	public static void AddGem(GameObject prefab, string colorName)
	{
#if ! API
		EffectDef.ValidGemTypes[colorName] = (GemType)colorName.GetStableHashCode();
		EffectDef.GemTypeNames[(GemType)colorName.GetStableHashCode()] = colorName.Replace(" ", "_");
		GemStoneSetup.RegisterGem(prefab, (GemType)colorName.GetStableHashCode());
#endif
	}

	public static void AddShard(GameObject prefab, string colorName)
	{
#if ! API
		if (GemStoneSetup.Gems.ContainsKey((GemType)colorName.GetStableHashCode()))
		{
			throw new Exception($"A shard for {colorName} must be registered before any gems for {colorName}");
		}
		GemStoneSetup.RegisterShard(prefab, (GemType)colorName.GetStableHashCode());
#endif
	}

	public static void AddDestructible(GameObject prefab, string colorName)
	{
#if ! API
		DestructibleSetup.AddDestructible(prefab, (GemType)colorName.GetStableHashCode());
#endif
	}

	public static void AddUncutGem(GameObject prefab, string colorName, ConfigEntry<float>? dropChance = null)
	{
#if ! API
		GemStoneSetup.RegisterUncutGem(prefab, (GemType)colorName.GetStableHashCode(), dropChance);
#endif
	}

	// ReSharper disable once UnusedTypeParameter
	public static void AddGemEffect<T>(string name, string? englishDescription = null, string? englishDescriptionDetailed = null) where T : struct
	{
#if ! API
		EffectDef.ConfigTypes.Add((Effect)name.GetStableHashCode(), typeof(T));
		EffectDef.ValidEffects[name] = (Effect)name.GetStableHashCode();
		EffectDef.EffectNames[(Effect)name.GetStableHashCode()] = name.Replace(" ", "_");
		Localizer.AddText($"jc_effect_{name.Replace(" ", "_").ToLower()}", name);
		if (englishDescription is not null)
		{
			Localizer.AddText($"jc_effect_{name.Replace(" ", "_").ToLower()}_desc", englishDescription);
		}
		if (englishDescriptionDetailed is not null)
		{
			Localizer.AddText($"jc_effect_{name.Replace(" ", "_").ToLower()}_desc_detail", englishDescriptionDetailed);
		}
		Utils.zdoNames[(Effect)name.GetStableHashCode()] = "Jewelcrafting Socket " + name;
#endif
	}

	public static void AddGemConfig(string yaml)
	{
#if ! API
		string assemblyName = new StackTrace().GetFrame(1).GetMethod().DeclaringType!.Assembly.GetName().Name;
		List<string> errors = ConfigLoader.loaders.Single(l => l.GetType() == typeof(EffectDef.Loader)).ProcessConfig($"/{assemblyName}.yml", new DeserializerBuilder().Build().Deserialize<Dictionary<object, object>>(yaml));
		foreach (string error in errors)
		{
			Debug.LogError($"Error in config of Gem config specified by mod {assemblyName}: {error}");
		}
#endif
	}

	public static T GetEffectPower<T>(this Player player, string name) where T : struct
	{
#if ! API
		return player.GetEffect<T>((Effect)name.GetStableHashCode());
#else
		return default;
#endif
	}

	[PublicAPI]
	public class GemInfo
	{
		public readonly string gemPrefab;
		public readonly Sprite gemSprite;
		public readonly Dictionary<string, float> gemEffects;

		public GemInfo(string gemPrefab, Sprite gemSprite, Dictionary<string, float> gemEffects)
		{
			this.gemPrefab = gemPrefab;
			this.gemSprite = gemSprite;
			this.gemEffects = gemEffects;
		}
	}

	public static List<GemInfo?> GetGems(ItemDrop.ItemData item)
	{
		List<GemInfo?> gems = new();
#if ! API
		if (item.Data().Get<Socketable>() is { } sockets and not Box { progress: >= 100 } and not SocketBag)
		{
			GemLocation location = Utils.GetGemLocation(item.m_shared);
			IEnumerable<EffectPower> effects(string socket) => Jewelcrafting.EffectPowers.TryGetValue(socket.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(location, out List<EffectPower> effectPowers) ? effectPowers : Enumerable.Empty<EffectPower>();
			foreach (string socket in sockets.socketedGems.Select(i => i.Name))
			{
				if (socket == "" || ObjectDB.instance.GetItemPrefab(socket) is not { } prefab)
				{
					gems.Add(null);
				}
				else
				{
					gems.Add(new GemInfo(prefab.name, prefab.GetComponent<ItemDrop>().m_itemData.GetIcon(), effects(socket).ToDictionary(e => EffectDef.EffectNames[e.Effect].Replace("_", " "), e => e.Power)));
				}
			}
		}
#endif
		return gems;
	}

	public static bool SetGems(ItemDrop.ItemData item, List<GemInfo?> gems)
	{
#if ! API
		if (!Utils.IsSocketableItem(item))
		{
			return false;
		}
		Socketable socketable = item.Data().Get<Socketable>() ?? item.Data().Add<Sockets>()!;
		socketable.socketedGems.Clear();
		foreach (GemInfo? gem in gems)
		{
			socketable.socketedGems.Add(gem is null ? new SocketItem("") : new SocketItem(gem.gemPrefab));
		}

		return true;
#else
		return false;
#endif
	}

	public static Sprite GetSocketBorder()
	{
#if ! API
		return GemStones.emptySocketSprite;
#else
		return null!;
#endif
	}

	public static GameObject GetGemcuttersTable()
	{
#if ! API
		return BuildingPiecesSetup.gemcuttersTable;
#else
		return null!;
#endif
	}

	public static void AddParticleEffect(string prefabName, GameObject effect, VisualEffectCondition displayCondition)
	{
#if ! API
		if (VisualEffects.attachEffectPrefabs.TryGetValue(prefabName, out Dictionary<VisualEffectCondition, GameObject>? prefab))
		{
			prefab.Add(displayCondition, effect);
		}
		else
		{
			VisualEffects.attachEffectPrefabs[prefabName] = new Dictionary<VisualEffectCondition, GameObject>
				{ { displayCondition, effect } };
		}
#endif
	}

	public static void SetSocketsLock(ItemDrop.ItemData item, bool enabled)
	{
#if ! API
		if (enabled)
		{
			item.Data()["SocketsLock"] = "";
		}
		else
		{
			item.Data().Remove("SocketsLock");
		}
#endif
	}
	
	public delegate bool GemBreakHandler(ItemDrop.ItemData? container, ItemDrop.ItemData gem, int count = 1);
	public delegate bool ItemBreakHandler(ItemDrop.ItemData? container);

	public static void OnGemBreak(GemBreakHandler callback)
	{
#if ! API
		GemStones.GemBreakHandlers.Add(callback);
#endif
	}
	
	public static void OnItemBreak(ItemBreakHandler callback)
	{
#if ! API
		GemStones.ItemBreakHandlers.Add(callback);
#endif
	}

	public delegate bool ItemMirroredHandler(ItemDrop.ItemData? item);
	
	public static void OnItemMirrored(ItemMirroredHandler callback)
	{
#if ! API
		GemStones.ItemMirroredHandlers.Add(callback);
#endif
	}

	public static bool IsJewelryEquipped(Player player, string prefabName)
	{
#if ! API
		int hash = prefabName.GetStableHashCode();
		if (player.m_visEquipment.m_currentUtilityItemHash == hash)
		{
			return true;
		}

		return Visual.visuals.TryGetValue(player.m_visEquipment, out Visual visual) && (visual.currentFingerItemHash == hash || visual.currentNeckItemHash == hash);
#else
		return false;
#endif
	}
}
