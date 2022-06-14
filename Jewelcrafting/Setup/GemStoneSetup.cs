using System.Collections.Generic;
using ItemManager;
using Jewelcrafting.GemEffects;
using UnityEngine;
using UnityEngine.UI;

namespace Jewelcrafting;

public struct GemDefinition
{
	public string Name;
	public GameObject Prefab;
	public float DefaultUpgradeChance;
}

public static class GemStoneSetup
{
	public static readonly List<string> lastConfigHashes = new()
	{
		"893E9532EC165DF4305EFE13974FC593"
	};
	
	public static GameObject SocketTooltip = null!;
	public static readonly Dictionary<GemType, List<GemDefinition>> Gems = new();
	public static readonly Dictionary<GemType, GameObject> shardColors = new();

	public static void initializeGemStones(AssetBundle assets)
	{
		Item AddGem(string prefab, GemType color, float defaultUpgradeChance = 0)
		{
			Item gemStone = new(assets, prefab) { Configurable = false };
			GemStones.socketableGemStones.Add(gemStone.Prefab);
			if (shardColors.TryGetValue(color, out GameObject shard))
			{
				GemStones.gemToShard.Add(gemStone.Prefab.name, shard);
			}

			if (!Gems.TryGetValue(color, out List<GemDefinition> colorGems))
			{
				colorGems = Gems[color] = new List<GemDefinition>();
			}
			colorGems.Add(new GemDefinition
			{
				DefaultUpgradeChance = defaultUpgradeChance,
				Prefab = gemStone.Prefab,
				Name = gemStone.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name,
			});

			return gemStone;
		}

		SocketTooltip = assets.LoadAsset<GameObject>("CrystalText");
		GemStones.emptySocketSprite = SocketTooltip.transform.Find("Bkg (1)/TrannyHoles/Transmute_1").GetComponent<Image>().sprite;

		Item gemStone = new(assets, "Uncut_Black_Stone");
		Jewelcrafting.gemDropChances.Add(gemStone.Prefab, Jewelcrafting.gemDropChanceOnyx);
		shardColors[GemType.Black] = new Item(assets, "Shattered_Black_Crystal").Prefab;

		gemStone = AddGem("Simple_Black_Socket", GemType.Black, 30f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Uncut_Black_Stone", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Black_Crystal", Jewelcrafting.badLuckCostSimpleOnyx);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Advanced_Black_Socket", GemType.Black, 20f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Simple_Black_Socket", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Black_Crystal", Jewelcrafting.badLuckCostAdvancedOnyx);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Perfect_Black_Socket", GemType.Black, 10f);
		gemStone.Crafting.Add("op_transmution_table", 2);
		gemStone.RequiredItems.Add("Advanced_Black_Socket", 1);
		gemStone.RecipeIsActive = Jewelcrafting.socketSystem;

		gemStone = new Item(assets, "Uncut_Blue_Stone");
		Jewelcrafting.gemDropChances.Add(gemStone.Prefab, Jewelcrafting.gemDropChanceSapphire);
		shardColors[GemType.Blue] = new Item(assets, "Shattered_Blue_Crystal").Prefab;

		gemStone = AddGem("Simple_Blue_Socket", GemType.Blue, 30f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Uncut_Blue_Stone", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Blue_Crystal", Jewelcrafting.badLuckCostSimpleSapphire);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Advanced_Blue_Socket", GemType.Blue, 20f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Simple_Blue_Socket", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Blue_Crystal", Jewelcrafting.badLuckCostAdvancedSapphire);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Perfect_Blue_Socket", GemType.Blue, 10f);
		gemStone.Crafting.Add("op_transmution_table", 2);
		gemStone.RequiredItems.Add("Advanced_Blue_Socket", 1);
		gemStone.RecipeIsActive = Jewelcrafting.socketSystem;

		gemStone = new Item(assets, "Uncut_Green_Stone");
		Jewelcrafting.gemDropChances.Add(gemStone.Prefab, Jewelcrafting.gemDropChanceEmerald);
		shardColors[GemType.Green] = new Item(assets, "Shattered_Green_Crystal").Prefab;

		gemStone = AddGem("Simple_Green_Socket", GemType.Green, 30f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Uncut_Green_Stone", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Green_Crystal", Jewelcrafting.badLuckCostSimpleEmerald);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Advanced_Green_Socket", GemType.Green, 20f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Simple_Green_Socket", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Green_Crystal", Jewelcrafting.badLuckCostAdvancedEmerald);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Perfect_Green_Socket", GemType.Green, 10f);
		gemStone.Crafting.Add("op_transmution_table", 2);
		gemStone.RequiredItems.Add("Advanced_Green_Socket", 1);
		gemStone.RecipeIsActive = Jewelcrafting.socketSystem;

		gemStone = new Item(assets, "Uncut_Purple_Stone");
		Jewelcrafting.gemDropChances.Add(gemStone.Prefab, Jewelcrafting.gemDropChanceSpinel);
		shardColors[GemType.Purple] = new Item(assets, "Shattered_Purple_Crystal").Prefab;

		gemStone = AddGem("Simple_Purple_Socket", GemType.Purple, 30f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Uncut_Purple_Stone", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Purple_Crystal", Jewelcrafting.badLuckCostSimpleSpinel);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Advanced_Purple_Socket", GemType.Purple, 20f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Simple_Purple_Socket", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Purple_Crystal", Jewelcrafting.badLuckCostAdvancedSpinel);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Perfect_Purple_Socket", GemType.Purple, 10f);
		gemStone.Crafting.Add("op_transmution_table", 2);
		gemStone.RequiredItems.Add("Advanced_Purple_Socket", 1);
		gemStone.RecipeIsActive = Jewelcrafting.socketSystem;

		gemStone = new Item(assets, "Uncut_Red_Stone");
		Jewelcrafting.gemDropChances.Add(gemStone.Prefab, Jewelcrafting.gemDropChanceRuby);
		shardColors[GemType.Red] = new Item(assets, "Shattered_Red_Crystal").Prefab;

		gemStone = AddGem("Simple_Red_Socket", GemType.Red, 30f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Uncut_Red_Stone", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Red_Crystal", Jewelcrafting.badLuckCostSimpleRuby);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Advanced_Red_Socket", GemType.Red, 20f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Simple_Red_Socket", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Red_Crystal", Jewelcrafting.badLuckCostAdvancedRuby);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Perfect_Red_Socket", GemType.Red, 10f);
		gemStone.Crafting.Add("op_transmution_table", 2);
		gemStone.RequiredItems.Add("Advanced_Red_Socket", 1);
		gemStone.RecipeIsActive = Jewelcrafting.socketSystem;

		gemStone = new Item(assets, "Uncut_Yellow_Stone");
		Jewelcrafting.gemDropChances.Add(gemStone.Prefab, Jewelcrafting.gemDropChanceSulfur);
		shardColors[GemType.Yellow] = new Item(assets, "Shattered_Yellow_Crystal").Prefab;

		gemStone = AddGem("Simple_Yellow_Socket", GemType.Yellow, 30f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Uncut_Yellow_Stone", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Yellow_Crystal", Jewelcrafting.badLuckCostSimpleSulfur);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Advanced_Yellow_Socket", GemType.Yellow, 20f);
		gemStone["GambleRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["GambleRecipe"].RequiredItems.Add("Simple_Yellow_Socket", 1);
		gemStone["GambleRecipe"].RecipeIsActive = Jewelcrafting.socketSystem;
		gemStone["BadLuckRecipe"].Crafting.Add("op_transmution_table", 1);
		gemStone["BadLuckRecipe"].RequiredItems.Add("Shattered_Yellow_Crystal", Jewelcrafting.badLuckCostAdvancedSulfur);
		gemStone["BadLuckRecipe"].RecipeIsActive = Jewelcrafting.badLuckRecipes;

		gemStone = AddGem("Perfect_Yellow_Socket", GemType.Yellow, 10f);
		gemStone.Crafting.Add("op_transmution_table", 2);
		gemStone.RequiredItems.Add("Advanced_Yellow_Socket", 1);
		gemStone.RecipeIsActive = Jewelcrafting.socketSystem;

		gemStone = AddGem("Boss_Crystal_1", GemType.Elder);
		GemStones.bossToGem.Add("gd_king", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_2", GemType.Bonemass);
		GemStones.bossToGem.Add("Bonemass", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_4", GemType.Moder);
		GemStones.bossToGem.Add("Dragon", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_5", GemType.Yagluth);
		GemStones.bossToGem.Add("GoblinKing", gemStone.Prefab);
		gemStone = AddGem("Boss_Crystal_7", GemType.Eikthyr);
		GemStones.bossToGem.Add("Eikthyr", gemStone.Prefab);
	}
}
