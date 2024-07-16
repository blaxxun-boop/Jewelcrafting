using System.Collections.Generic;
using BepInEx.Configuration;
using ItemManager;
using UnityEngine;
using UnityEngine.UI;
using Toggle = ItemManager.Toggle;

namespace Jewelcrafting;

public enum GemType
{
	Black = 1,
	Blue,
	Green,
	Purple,
	Red,
	Yellow,
	Orange,
	Cyan,
	Eikthyr,
	Elder,
	Bonemass,
	Moder,
	Yagluth,
	Queen,
	Fader,
	Group,
	Wisplight,
	Wishbone,
}

public struct GemDefinition
{
	public string Name;
	public GameObject Prefab;
}

public struct GemInfo
{
	public GemType Type;
	public int Tier;
}

public struct MaterialColor
{
	public Color Color;
	public Material? Material;
}

public static class GemStoneSetup
{
	public static GameObject SocketTooltip = null!;
	public static readonly Dictionary<GemType, List<GemDefinition>> Gems = new();
	public static readonly Dictionary<GemType, GameObject> shardColors = new();
	public static readonly Dictionary<GemType, GameObject> uncutGems = new();
	public static readonly Dictionary<string, GemInfo> GemInfos = new();

	public static readonly Dictionary<GemType, MaterialColor> Colors = new()
	{
		{ GemType.Black, new MaterialColor { Color = Color.black } },
		{ GemType.Blue, new MaterialColor { Color = Color.blue } },
		{ GemType.Red, new MaterialColor { Color = Color.red } },
		{ GemType.Yellow, new MaterialColor { Color = Color.yellow } },
		{ GemType.Green, new MaterialColor { Color = Color.green } },
		{ GemType.Purple, new MaterialColor { Color = Color.magenta } },
		{ GemType.Orange, new MaterialColor { Color = new Color(1, 0.6f, 0) } },
	};

	private static readonly Dictionary<GemType, string> materials = new()
	{
		{ GemType.Black, "StoneBlack" },
		{ GemType.Blue, "StoneBlue" },
		{ GemType.Green, "StoneGreen" },
		{ GemType.Orange, "StoneOrange" },
		{ GemType.Purple, "StonePurple" },
		{ GemType.Red, "StoneRed" },
		{ GemType.Yellow, "StoneYellow" },
	};

	public static readonly GameObject[] customGemTierPrefabs = new GameObject[3];
	public static GameObject customUncutGemPrefab = null!;
	public static GameObject customGemShardPrefab = null!;
	private static readonly int ShaderColorKey = Shader.PropertyToID("_Color");
	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	public static GameObject CreateItemFromTemplate(GameObject template, string colorName, string localizationName, MaterialColor materialColor)
	{
		GameObject prefab = Object.Instantiate(template, MergedGemStoneSetup.gemList.transform);
		prefab.name = template.name.Replace("Custom", colorName);
		ItemDrop.ItemData.SharedData shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		shared.m_name = $"${localizationName}";
		shared.m_description = $"${localizationName}_description";
		MeshRenderer colorMesh = prefab.transform.Find("attach/Custom_Color_Mesh").GetComponent<MeshRenderer>();
		if (materialColor.Material is { } material)
		{
			colorMesh.material = material;
		}
		else
		{
			colorMesh.material.SetColor(ShaderColorKey, materialColor.Color);
			colorMesh.material.SetColor(EmissionColor, materialColor.Color);
		}
		ItemSnapshots.SnapshotItems(prefab.GetComponent<ItemDrop>());
		return prefab;
	}

	public static GameObject CreateGemFromTemplate(GameObject template, string colorName, MaterialColor color, int tier) => CreateItemFromTemplate(template, colorName, $"jc_{tier switch { 1 => "adv_", 2 => "perfect_", _ => "" }}{colorName.Replace(" ", "_").ToLower()}_socket", color);
	public static GameObject CreateShardFromTemplate(GameObject template, string colorName, MaterialColor color) => CreateItemFromTemplate(template, colorName, $"jc_shattered_{colorName.Replace(" ", "_").ToLower()}_crystal", color);
	public static GameObject CreateUncutFromTemplate(GameObject template, string colorName, MaterialColor color) => CreateItemFromTemplate(template, colorName, $"jc_uncut_{colorName.Replace(" ", "_").ToLower()}_stone", color);

	public static void RegisterGem(GameObject prefab, GemType color)
	{
		if (shardColors.TryGetValue(color, out GameObject shard))
		{
			GemStones.gemToShard.Add(prefab.name, shard);
		}

		if (!Gems.TryGetValue(color, out List<GemDefinition> colorGems))
		{
			colorGems = Gems[color] = new List<GemDefinition>();
		}
		prefab.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = prefab;
		string gemName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		GemStones.socketableGemStones.Add(gemName);
		colorGems.Add(new GemDefinition
		{
			Prefab = prefab,
			Name = gemName,
		});

		GemInfos[gemName] = new GemInfo
		{
			Type = color,
			Tier = colorGems.Count,
		};
	}

	public static void DisableGemColor(GemType color)
	{
		if (Gems.TryGetValue(color, out List<GemDefinition> gems))
		{
			foreach (GemDefinition gem in gems)
			{
				GemStones.socketableGemStones.Remove(gem.Name);
				GemInfos.Remove(gem.Name);
			}
			Gems.Remove(color);
		}
	}

	public static void RegisterShard(GameObject prefab, GemType color)
	{
		shardColors[color] = new Item(prefab) { Configurable = Configurability.Disabled }.Prefab;
	}

	public static void RegisterUncutGem(GameObject prefab, GemType color, ConfigEntry<float>? dropChance = null)
	{
		Item gemStone = new(prefab) { Configurable = Configurability.Disabled };
		uncutGems[color] = prefab;
		if (dropChance is not null)
		{
			Jewelcrafting.gemDropChances.Add(gemStone.Prefab, dropChance);
		}
	}

	public static void RegisterTieredGemItem(GameObject prefab, string colorName, int tier)
	{
		string gemName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
		Jewelcrafting.gemUpgradeChances.Add(gemName, Jewelcrafting.config("Socket Upgrade Chances", Jewelcrafting.english.Localize(gemName), tier switch { 0 => 33f, 1 => 23f, _ => 13f }, new ConfigDescription($"Success chance while trying to create {Localization.instance.Localize(gemName)}.", new AcceptableValueRange<float>(0f, 100f), new Jewelcrafting.ConfigurationManagerAttributes { DispName = Localization.instance.Localize(gemName) })));

		Item gemStone = new(prefab) { Configurable = Configurability.Full };
		switch (tier)
		{
			case 0:
				gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
				gemStone["GambleRecipe"].RequiredItems.Add($"Uncut_{colorName}_Stone", 1);
				gemStone["MassRecipe"].Crafting.Add("op_transmution_table", 1);
				gemStone["MassRecipe"].RequiredItems.Add($"Uncut_{colorName}_Stone", 5);
				gemStone["MassRecipe"].CraftAmount = 5;
				gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
				gemStone["BadLuckRecipe"].RequiredItems.Add($"Shattered_{colorName}_Crystal", Jewelcrafting.config("Bad Luck Protection", $"Bad Luck Cost Simple {colorName}", 12, new ConfigDescription($"{colorName} shards required to craft a Simple {colorName}.")));
				gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

				_ = new Conversion(gemStone)
				{
					Input = $"Uncut_{colorName}_Stone",
					Custom = "JC_Gemstone_Furnace",
				};

				break;
			case 1:
				gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
				gemStone["GambleRecipe"].RequiredItems.Add($"Simple_{colorName}_Socket", 1);
				gemStone["MassRecipe"].Crafting.Add("op_transmution_table", 1);
				gemStone["MassRecipe"].RequiredItems.Add($"Simple_{colorName}_Socket", 5);
				gemStone["MassRecipe"].CraftAmount = 5;
				gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
				gemStone["BadLuckRecipe"].RequiredItems.Add($"Shattered_{colorName}_Crystal", Jewelcrafting.config("Bad Luck Protection", $"Bad Luck Cost Advanced {colorName}", 35, new ConfigDescription($"{colorName} shards required to craft an Advanced {colorName}.")));
				gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

				_ = new Conversion(gemStone)
				{
					Input = $"Simple_{colorName}_Socket",
					Custom = "JC_Gemstone_Furnace",
				};

				break;
			case 2:
				gemStone.Crafting.Add("op_transmution_table", 2);
				gemStone.RequiredItems.Add($"Advanced_{colorName}_Socket", 1);
				break;
		}
	}

	public static void initializeGemStones(AssetBundle assets)
	{
		SocketTooltip = assets.LoadAsset<GameObject>("CrystalText");
		GemStones.emptySocketSprite = SocketTooltip.transform.Find("Bkg (1)/TrannyHoles/Transmute_Text_1/Border/Transmute_1").GetComponent<Image>().sprite;

		Transform toCopy = SocketTooltip.transform.Find("Bkg (1)/TrannyHoles/Transmute_Text_1");
		Transform toParent = SocketTooltip.transform.Find("Bkg (1)/TrannyHoles");
		for (int i = 6; i <= Jewelcrafting.maxNumberOfSockets; ++i)
		{
			GameObject newSlot = Object.Instantiate(toCopy.gameObject);
			newSlot.name = $"Transmute_Text_{i}";
			// ReSharper disable once Unity.InstantiateWithoutParent
			newSlot.transform.SetParent(toParent, false);
			newSlot.gameObject.SetActive(false);
		}

		customGemTierPrefabs[0] = assets.LoadAsset<GameObject>("Simple_Custom_Socket");
		customGemTierPrefabs[1] = assets.LoadAsset<GameObject>("Advanced_Custom_Socket");
		customGemTierPrefabs[2] = assets.LoadAsset<GameObject>("Perfect_Custom_Socket");
		customUncutGemPrefab = assets.LoadAsset<GameObject>("Uncut_Custom_Stone");
		customGemShardPrefab = assets.LoadAsset<GameObject>("Shattered_Custom_Crystal");

		foreach (KeyValuePair<GemType, string> kv in materials)
		{
			Colors[kv.Key] = Colors[kv.Key] with { Material = assets.LoadAsset<Material>(kv.Value) };
		}

		if (Groups.API.IsLoaded())
		{
			Colors.Add(GemType.Cyan, new MaterialColor { Color = Color.cyan });
		}

		Jewelcrafting.gemDropBiomeDistribution = Jewelcrafting.config("Gem Drops", "Use biome distribution", Jewelcrafting.Toggle.Off, new ConfigDescription("If on, gem drops will follow the biome distribution defined in the YAML."));
		foreach (KeyValuePair<GemType, MaterialColor> gemType in Colors)
		{
			string shardAssetName = customGemShardPrefab.name.Replace("Custom", gemType.Key.ToString());
			RegisterShard(assets.Contains(shardAssetName) ? assets.LoadAsset<GameObject>(shardAssetName) : CreateShardFromTemplate(customGemShardPrefab, gemType.Key.ToString(), gemType.Value), gemType.Key);
			string uncutGemShardName = customUncutGemPrefab.name.Replace("Custom", gemType.Key.ToString());
			string name = Jewelcrafting.english.Localize($"$jc_merged_gemstone_{gemType.Key.ToString().ToLower()}");
			RegisterUncutGem(assets.Contains(uncutGemShardName) ? assets.LoadAsset<GameObject>(uncutGemShardName) : CreateUncutFromTemplate(customUncutGemPrefab, gemType.Key.ToString(), gemType.Value), gemType.Key, Jewelcrafting.config("Gem Drops", $"Drop chance for {name} Gemstones", 1.5f, new ConfigDescription($"Chance to drop {name} gemstones when killing creatures.", new AcceptableValueRange<float>(0, 100))));

			for (int tier = 0; tier < customGemTierPrefabs.Length; ++tier)
			{
				string tieredGemAssetName = customGemTierPrefabs[tier].name.Replace("Custom", gemType.Key.ToString());
				GameObject prefab = assets.Contains(tieredGemAssetName) ? assets.LoadAsset<GameObject>(tieredGemAssetName) : CreateGemFromTemplate(customGemTierPrefabs[tier], gemType.Key.ToString(), gemType.Value, tier);
				RegisterTieredGemItem(prefab, gemType.Key.ToString(), tier);
				RegisterGem(prefab, gemType.Key);
			}
		}

		Item AddGem(string prefab, GemType color)
		{
			Item gemStone = new(assets, prefab) { Configurable = Configurability.Disabled };
			RegisterGem(gemStone.Prefab, color);
			return gemStone;
		}

		Item gemStone = AddGem("Boss_Crystal_7", GemType.Eikthyr);
		GemStones.bossToGem.Add("Eikthyr", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_1", GemType.Elder);
		GemStones.bossToGem.Add("gd_king", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_2", GemType.Bonemass);
		GemStones.bossToGem.Add("Bonemass", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_4", GemType.Moder);
		GemStones.bossToGem.Add("Dragon", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_5", GemType.Yagluth);
		GemStones.bossToGem.Add("GoblinKing", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_3", GemType.Queen);
		GemStones.bossToGem.Add("SeekerQueen", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_8", GemType.Fader);
		GemStones.bossToGem.Add("Fader", gemStone.Prefab);

		if (Groups.API.IsLoaded())
		{
			gemStone = AddGem("Friendship_Group_Gem", GemType.Group);
			GemStones.bossToGem.Add("Friendship", gemStone.Prefab);
		}
	}
}
