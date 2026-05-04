using System.Collections.Generic;
using HarmonyLib;
using ItemManager;
using LocationManager;
using UnityEngine;

namespace Jewelcrafting.Setup;

public static class CaveSetup
{
	public static void initializeCaves(AssetBundle assets)
	{
		_ = new LocationManager.Location(assets, "JC_Crystal_Cave")
		{
			MapIconSprite = assets.LoadAsset<Sprite>("gemcave_icon.png"),
			ShowMapIcon = Jewelcrafting.gemCaveLocationIcon.Value == Jewelcrafting.Toggle.On ? ShowIcon.Always : ShowIcon.Never,
			Biome = Heightmap.Biome.All & ~Heightmap.Biome.Ocean & ~Heightmap.Biome.DeepNorth,
			SpawnDistance = new Range(1000, 10000),
			SpawnAltitude = new Range(ZoneSystem.c_WaterLevel, 10000),
			Count = 150,
			MinimumDistanceFromGroup = 500,
		};
		
		GameObject golem = PrefabManager.RegisterPrefab(assets, "JC_Crystal_Golem");
		CharacterDrop drops = golem.AddComponent<CharacterDrop>();
		foreach (GameObject uncutGem in GemStoneSetup.uncutGems.Values)
		{
			drops.m_drops.Add(new CharacterDrop.Drop
			{
				m_amountMin = 4,
				m_amountMax = 6,
				m_chance = 1,
				m_prefab = uncutGem,
			});
		}
	}
}
