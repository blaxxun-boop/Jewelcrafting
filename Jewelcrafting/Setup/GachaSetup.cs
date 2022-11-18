using System.Collections.Generic;
using HarmonyLib;
using ItemManager;
using LocationManager;
using UnityEngine;

namespace Jewelcrafting.WorldBosses;

public static class GachaSetup
{
	public static GameObject gachaCoins = null!;
	public static GameObject skeletonWindow = null!;
	public static readonly HashSet<string> worldBossBonusItems = new();

	public static void initializeGacha(AssetBundle assets)
	{
		gachaCoins = PrefabManager.RegisterPrefab(assets, "JC_Gacha_Coins", true);

		void RegisterWorldBossBonusItem(string name)
		{
			Item weapon = new(assets, name);
			weapon.Crafting.Add(CraftingTable.Forge, 1);
			weapon.RequiredItems.Add("SwordCheat", 1);
			weapon.RequiredUpgradeItems.Add("JC_Gacha_Coins", 5);
			worldBossBonusItems.Add(name);
		}
		RegisterWorldBossBonusItem("JC_Reaper_Axe");
		RegisterWorldBossBonusItem("JC_Reaper_Bow");
		RegisterWorldBossBonusItem("JC_Reaper_Pickaxe");
		RegisterWorldBossBonusItem("JC_Reaper_Sword");

		skeletonWindow = assets.LoadAsset<GameObject>("JC_Gacha_Window");

		GameObject location = assets.LoadAsset<GameObject>("JC_Gacha_Location");
		foreach (Container container in location.transform.GetComponentsInChildren<Container>())
		{
			Utils.ConvertComponent<GachaChest, Container>(container.gameObject);
		}
		GameObject containerPrefab = PrefabManager.RegisterPrefab(assets, "Jewelcrafting_Chest");
		Utils.ConvertComponent<GachaChest, Container>(containerPrefab);

		location.transform.Find("GemStone").gameObject.AddComponent<GemStoneInteract>();

		_ = new LocationManager.Location(location)
		{
			MapIconSprite = gachaCoins.GetComponent<ItemDrop>().m_itemData.GetIcon(),
			ShowMapIcon = Jewelcrafting.gachaLocationIcon.Value == Jewelcrafting.Toggle.On ? ShowIcon.Explored : ShowIcon.Never,
			Biome = Heightmap.Biome.Meadows,
			SpawnDistance = new Range(1000, 10000),
			SpawnAltitude = new Range(10, 200),
			Count = 5,
			Unique = true,
			Prioritize = true
		};
	}

	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class AddStoneToPickaxe
	{
		private static void Postfix(ObjectDB __instance)
		{
			if (__instance.GetItemPrefab("PickaxeIron") is { } vanillaPickaxe && __instance.GetItemPrefab("JC_Reaper_Pickaxe") is { } jewelcraftingPickaxe)
			{
				jewelcraftingPickaxe.GetComponent<ItemDrop>().m_itemData.m_shared.m_spawnOnHitTerrain = vanillaPickaxe.GetComponent<ItemDrop>().m_itemData.m_shared.m_spawnOnHitTerrain;
			}
		}
	}
}
