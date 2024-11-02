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
	public static readonly Dictionary<string, BalanceConfig> celestialItemsConfigs = new();

	public struct BalanceConfig
	{
		public Item item;
		public ItemDrop.ItemData.SharedData plains;
		public ItemDrop.ItemData.SharedData mistlands;
		public ItemDrop.ItemData.SharedData ashlands;
	}

	public enum BalanceToggle
	{
		Plains,
		Mistlands,
		Ashlands,
		Custom,
	}

	public static void initializeGacha(AssetBundle assets)
	{
		gachaCoins = PrefabManager.RegisterPrefab(assets, "JC_Gacha_Coins", true);

		GameObject RegisterWorldBossBonusItem(string name)
		{
			Item weapon = new(assets, name);
			weapon.Crafting.Add(CraftingTable.Forge, 1);
			weapon.RequiredUpgradeItems.Add("JC_Gacha_Coins", 5);
			worldBossBonusItems.Add(name);
			celestialItemsConfigs.Add(name, new BalanceConfig { item = weapon, plains = Utils.Clone(weapon.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared), mistlands = Utils.Clone(weapon.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared), ashlands = Utils.Clone(weapon.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared) });

			return weapon.Prefab;
		}
		RegisterWorldBossBonusItem("JC_Reaper_Axe");
		celestialItemsConfigs["JC_Reaper_Axe"].mistlands.m_damages.m_slash = 120;
		celestialItemsConfigs["JC_Reaper_Axe"].mistlands.m_damages.m_chop = 70;
		celestialItemsConfigs["JC_Reaper_Axe"].mistlands.m_damagesPerLevel.m_slash = 5;
		celestialItemsConfigs["JC_Reaper_Axe"].mistlands.m_damagesPerLevel.m_chop = 3;
		celestialItemsConfigs["JC_Reaper_Axe"].mistlands.m_maxDurability = 200;
		celestialItemsConfigs["JC_Reaper_Axe"].ashlands.m_damages.m_slash = 140;
		celestialItemsConfigs["JC_Reaper_Axe"].ashlands.m_damages.m_chop = 90;
		celestialItemsConfigs["JC_Reaper_Axe"].ashlands.m_damagesPerLevel.m_slash = 5;
		celestialItemsConfigs["JC_Reaper_Axe"].ashlands.m_damagesPerLevel.m_chop = 3;
		celestialItemsConfigs["JC_Reaper_Axe"].ashlands.m_maxDurability = 225;
		GameObject bow = RegisterWorldBossBonusItem("JC_Reaper_Bow");
		bow.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_bowDraw = true;
		bow.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_drawDurationMin = 2.5f;
		bow.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_drawAnimationState = "bow_aim";
		bow.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_projectileBursts = 2;
		bow.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_burstInterval = 0.05f;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_damages.m_pierce = 36;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_damages.m_poison = 0;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_damages.m_frost = 0;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_damages.m_spirit = 2.5f;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_attackForce = 25;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_attack = Utils.Clone(celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_attack);
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_attack.m_drawStaminaDrain = 14;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_damagesPerLevel.m_pierce = 2;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_damagesPerLevel.m_spirit = 2.5f;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_damagesPerLevel.m_frost = 0;
		celestialItemsConfigs["JC_Reaper_Bow"].mistlands.m_damagesPerLevel.m_poison = 0;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_damages.m_pierce = 42;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_damages.m_poison = 0;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_damages.m_frost = 6;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_damages.m_spirit = 0;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_attackForce = 25;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_attack = Utils.Clone(celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_attack);
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_attack.m_drawStaminaDrain = 14;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_damagesPerLevel.m_pierce = 0;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_damagesPerLevel.m_spirit = 6;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_damagesPerLevel.m_frost = 0;
		celestialItemsConfigs["JC_Reaper_Bow"].ashlands.m_damagesPerLevel.m_poison = 0;
		RegisterWorldBossBonusItem("JC_Reaper_Pickaxe");
		celestialItemsConfigs["JC_Reaper_Pickaxe"].mistlands.m_toolTier = 3;
		celestialItemsConfigs["JC_Reaper_Pickaxe"].mistlands.m_damages.m_pierce = 49;
		celestialItemsConfigs["JC_Reaper_Pickaxe"].mistlands.m_maxDurability = 400;
		celestialItemsConfigs["JC_Reaper_Pickaxe"].ashlands.m_toolTier = 3;
		celestialItemsConfigs["JC_Reaper_Pickaxe"].ashlands.m_damages.m_pierce = 57;
		celestialItemsConfigs["JC_Reaper_Pickaxe"].ashlands.m_maxDurability = 450;
		RegisterWorldBossBonusItem("JC_Reaper_Sword");
		celestialItemsConfigs["JC_Reaper_Sword"].mistlands.m_damages.m_slash = 75;
		celestialItemsConfigs["JC_Reaper_Sword"].mistlands.m_damages.m_frost = 40;
		celestialItemsConfigs["JC_Reaper_Sword"].mistlands.m_damagesPerLevel.m_frost = 6;
		celestialItemsConfigs["JC_Reaper_Sword"].mistlands.m_damagesPerLevel.m_spirit = 5;
		celestialItemsConfigs["JC_Reaper_Sword"].mistlands.m_attack = Utils.Clone(celestialItemsConfigs["JC_Reaper_Sword"].mistlands.m_attack);
		celestialItemsConfigs["JC_Reaper_Sword"].mistlands.m_attack.m_attackStamina = 14;
		celestialItemsConfigs["JC_Reaper_Sword"].mistlands.m_blockPower = 48;
		celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_damages.m_slash = 145;
		celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_damages.m_fire = 10;
		celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_damages.m_frost = 0;
		celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_damagesPerLevel.m_frost = 0;
		celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_damagesPerLevel.m_fire = 6;
		celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_damagesPerLevel.m_slash = 5;
		celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_attack = Utils.Clone(celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_attack);
		celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_attack.m_attackStamina = 16;
		celestialItemsConfigs["JC_Reaper_Sword"].ashlands.m_blockPower = 57;
		RegisterWorldBossBonusItem("JC_Reaper_Battleaxe");
		celestialItemsConfigs["JC_Reaper_Battleaxe"].mistlands.m_damages.m_slash = 130;
		celestialItemsConfigs["JC_Reaper_Battleaxe"].mistlands.m_movementModifier = -0.1f;
		celestialItemsConfigs["JC_Reaper_Battleaxe"].plains.m_movementModifier = -0.1f;
		celestialItemsConfigs["JC_Reaper_Battleaxe"].ashlands.m_damages.m_slash = 150;
		celestialItemsConfigs["JC_Reaper_Battleaxe"].ashlands.m_movementModifier = -0.1f;
		celestialItemsConfigs["JC_Reaper_Battleaxe"].ashlands.m_movementModifier = -0.1f;
		RegisterWorldBossBonusItem("JC_Reaper_TH_Mace");
		celestialItemsConfigs["JC_Reaper_TH_Mace"].mistlands.m_damages.m_blunt = 145;
		celestialItemsConfigs["JC_Reaper_TH_Mace"].mistlands.m_blockPower = 52;
		celestialItemsConfigs["JC_Reaper_TH_Mace"].mistlands.m_attackForce = 230;
		celestialItemsConfigs["JC_Reaper_TH_Mace"].plains.m_attackForce = 230;
		celestialItemsConfigs["JC_Reaper_TH_Mace"].ashlands.m_damages.m_blunt = 165;
		celestialItemsConfigs["JC_Reaper_TH_Mace"].ashlands.m_blockPower = 64;
		celestialItemsConfigs["JC_Reaper_TH_Mace"].ashlands.m_attackForce = 230;
		celestialItemsConfigs["JC_Reaper_TH_Mace"].ashlands.m_attackForce = 230;
		RegisterWorldBossBonusItem("JC_Reaper_TH_Sword");
		celestialItemsConfigs["JC_Reaper_TH_Sword"].mistlands.m_damages.m_slash = 150;
		celestialItemsConfigs["JC_Reaper_TH_Sword"].ashlands.m_damages.m_slash = 170;
		celestialItemsConfigs["JC_Reaper_TH_Sword"].ashlands.m_blockPower = 64;
		RegisterWorldBossBonusItem("JC_Reaper_Spear");
		celestialItemsConfigs["JC_Reaper_Spear"].mistlands.m_damages.m_pierce = 115;
		celestialItemsConfigs["JC_Reaper_Spear"].mistlands.m_blockPower = 48;
		celestialItemsConfigs["JC_Reaper_Spear"].mistlands.m_attack = Utils.Clone(celestialItemsConfigs["JC_Reaper_Spear"].mistlands.m_attack);
		celestialItemsConfigs["JC_Reaper_Spear"].mistlands.m_attack.m_attackStamina = 16;
		celestialItemsConfigs["JC_Reaper_Spear"].mistlands.m_backstabBonus = 3.5f;
		celestialItemsConfigs["JC_Reaper_Spear"].plains.m_backstabBonus = 3.5f;
		celestialItemsConfigs["JC_Reaper_Spear"].ashlands.m_damages.m_pierce = 135;
		celestialItemsConfigs["JC_Reaper_Spear"].ashlands.m_blockPower = 57;
		celestialItemsConfigs["JC_Reaper_Spear"].ashlands.m_attack = Utils.Clone(celestialItemsConfigs["JC_Reaper_Spear"].ashlands.m_attack);
		celestialItemsConfigs["JC_Reaper_Spear"].ashlands.m_backstabBonus = 3.5f;
		RegisterWorldBossBonusItem("JC_Reaper_Shield");
		celestialItemsConfigs["JC_Reaper_Shield"].plains.m_blockPower = 80;
		celestialItemsConfigs["JC_Reaper_Shield"].ashlands.m_blockPower = 114;
		RegisterWorldBossBonusItem("JC_Reaper_Knife");
		celestialItemsConfigs["JC_Reaper_Knife"].plains.m_backstabBonus = 6.5f;
		celestialItemsConfigs["JC_Reaper_Knife"].mistlands.m_backstabBonus = 6.5f;
		celestialItemsConfigs["JC_Reaper_Knife"].mistlands.m_damages.m_pierce = 41;
		celestialItemsConfigs["JC_Reaper_Knife"].mistlands.m_damages.m_slash = 41;
		celestialItemsConfigs["JC_Reaper_Knife"].mistlands.m_backstabBonus = 6.5f;
		celestialItemsConfigs["JC_Reaper_Knife"].ashlands.m_damages.m_pierce = 50;
		celestialItemsConfigs["JC_Reaper_Knife"].ashlands.m_damages.m_slash = 50;
		RegisterWorldBossBonusItem("JC_Lightning_Staff");
		celestialItemsConfigs["JC_Lightning_Staff"].plains.m_attack = Utils.Clone(celestialItemsConfigs["JC_Lightning_Staff"].mistlands.m_attack);
		celestialItemsConfigs["JC_Lightning_Staff"].plains.m_attack.m_attackEitr = 45;
		celestialItemsConfigs["JC_Lightning_Staff"].ashlands.m_attack = Utils.Clone(celestialItemsConfigs["JC_Lightning_Staff"].mistlands.m_attack);
		celestialItemsConfigs["JC_Lightning_Staff"].ashlands.m_attack.m_attackEitr = 25;
		RegisterWorldBossBonusItem("JC_Poison_Staff");
		celestialItemsConfigs["JC_Poison_Staff"].plains.m_attack = Utils.Clone(celestialItemsConfigs["JC_Poison_Staff"].mistlands.m_attack);
		celestialItemsConfigs["JC_Poison_Staff"].plains.m_attack.m_attackEitr = 45;
		celestialItemsConfigs["JC_Poison_Staff"].ashlands.m_attack = Utils.Clone(celestialItemsConfigs["JC_Poison_Staff"].mistlands.m_attack);
		celestialItemsConfigs["JC_Poison_Staff"].ashlands.m_attack.m_attackEitr = 25;
		RegisterWorldBossBonusItem("JC_Shield_Tower");
		celestialItemsConfigs["JC_Shield_Tower"].plains.m_blockPower = 104;
		celestialItemsConfigs["JC_Shield_Tower"].plains.m_movementModifier = -0.1f;
		celestialItemsConfigs["JC_Shield_Tower"].mistlands.m_blockPower = 122;
		celestialItemsConfigs["JC_Shield_Tower"].mistlands.m_movementModifier = -0.1f;
		celestialItemsConfigs["JC_Shield_Tower"].ashlands.m_blockPower = 140;
		celestialItemsConfigs["JC_Shield_Tower"].ashlands.m_movementModifier = -0.1f;
		RegisterWorldBossBonusItem("JC_Shield_Buckler");
		celestialItemsConfigs["JC_Shield_Buckler"].plains.m_blockPower = 60;
		celestialItemsConfigs["JC_Shield_Buckler"].plains.m_timedBlockBonus = 2.7f;
		celestialItemsConfigs["JC_Shield_Buckler"].mistlands.m_timedBlockBonus = 2.7f;
		celestialItemsConfigs["JC_Shield_Buckler"].ashlands.m_blockPower = 96;
		celestialItemsConfigs["JC_Shield_Buckler"].ashlands.m_timedBlockBonus = 2.7f;
		RegisterWorldBossBonusItem("JC_Reaper_Crossbow");
		celestialItemsConfigs["JC_Reaper_Crossbow"].plains.m_damages.m_pierce = 150;
		celestialItemsConfigs["JC_Reaper_Crossbow"].ashlands.m_damages.m_pierce = 220;
		RegisterWorldBossBonusItem("JC_Reaper_Mace");
		celestialItemsConfigs["JC_Reaper_Mace"].plains.m_damages.m_blunt = 95;
		celestialItemsConfigs["JC_Reaper_Mace"].mistlands.m_damages.m_blunt = 115;
		celestialItemsConfigs["JC_Reaper_Mace"].ashlands.m_damages.m_blunt = 135;

		skeletonWindow = assets.LoadAsset<GameObject>("JC_Gacha_Window");

		GameObject location = assets.LoadAsset<GameObject>("JC_Gacha_Location");
		location.AddComponent<LocationHider>();
		foreach (Container container in location.transform.GetComponentsInChildren<Container>())
		{
			Utils.ConvertComponent<GachaChest, Container>(container.gameObject);
			container.gameObject.AddComponent<LocationHider>();
		}
		GameObject containerPrefab = PrefabManager.RegisterPrefab(assets, "Jewelcrafting_Chest");
		Utils.ConvertComponent<GachaChest, Container>(containerPrefab);
		containerPrefab.AddComponent<LocationHider>();

		location.transform.Find("GemStone").gameObject.AddComponent<GemStoneInteract>();

		_ = new LocationManager.Location(location)
		{
			MapIconSprite = gachaCoins.GetComponent<ItemDrop>().m_itemData.GetIcon(),
			ShowMapIcon = Jewelcrafting.gachaLocationIcon.Value == Jewelcrafting.Toggle.On ? ShowIcon.Explored : ShowIcon.Never,
			Biome = Heightmap.Biome.Meadows,
			SpawnDistance = new Range(1000, 3000),
			SpawnAltitude = new Range(10, 200),
			Count = 5,
			Unique = true,
			Prioritize = true,
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

	public class LocationHider : MonoBehaviour
	{
		private void Start()
		{
			if (Jewelcrafting.gachaLocationActive.Value == Jewelcrafting.Toggle.Off)
			{
				gameObject.SetActive(false);
			}
		}
	}
}
