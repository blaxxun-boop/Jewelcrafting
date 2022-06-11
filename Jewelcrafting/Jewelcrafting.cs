using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using Jewelcrafting.GemEffects;
using LocalizationManager;
using ServerSync;
using SkillManager;
using UnityEngine;

namespace Jewelcrafting;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInIncompatibility("randyknapp.mods.epicloot")]
[BepInDependency("randyknapp.mods.extendeditemdataframework")]
public partial class Jewelcrafting : BaseUnityPlugin
{
	public const string ModName = "Jewelcrafting";
	private const string ModVersion = "1.0.0";
	private const string ModGUID = "org.bepinex.plugins.jewelcrafting";

	public static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

	private static ConfigEntry<Toggle> serverConfigLocked = null!;
	public static ConfigEntry<Toggle> socketSystem = null!;
	public static ConfigEntry<UniqueDrop> uniqueGemDropSystem = null!;
	public static ConfigEntry<int> uniqueGemDropChance = null!;
	public static ConfigEntry<Toggle> uniqueGemDropOnePerPlayer = null!;
	public static ConfigEntry<int> chanceToAddSocket = null!;
	public static ConfigEntry<int> gemDropChanceOnyx = null!;
	public static ConfigEntry<int> gemDropChanceSapphire = null!;
	public static ConfigEntry<int> gemDropChanceEmerald = null!;
	public static ConfigEntry<int> gemDropChanceSpinel = null!;
	public static ConfigEntry<int> gemDropChanceRuby = null!;
	public static ConfigEntry<int> gemDropChanceSulfur = null!;
	public static ConfigEntry<int> maximumNumberSockets = null!;
	public static ConfigEntry<int> gemRespawnRate = null!;
	public static ConfigEntry<int> upgradeChanceIncrease = null!;
	private static ConfigEntry<float> experienceGainedFactor = null!;
	
	public static readonly Dictionary<GameObject, ConfigEntry<int>> gemDropChances = new();
	public static readonly CustomSyncedValue<List<string>> socketEffectDefinitions = new(configSync, "socket effects", new List<string>());

	private readonly Dictionary<string, float> defaultGemUpgradeChances = new()
	{
		{ "$jc_black_socket", 30f },
		{ "$jc_adv_black_socket", 20f },
		{ "$jc_perfect_black_socket", 10f },
		{ "$jc_blue_socket", 30f },
		{ "$jc_adv_blue_socket", 20f },
		{ "$jc_perfect_blue_socket", 10f },
		{ "$jc_green_socket", 30f },
		{ "$jc_adv_green_socket", 20f },
		{ "$jc_perfect_green_socket", 10f },
		{ "$jc_purple_socket", 30f },
		{ "$jc_adv_purple_socket", 20f },
		{ "$jc_perfect_purple_socket", 10f },
		{ "$jc_red_socket", 30f },
		{ "$jc_adv_red_socket", 20f },
		{ "$jc_perfect_red_socket", 10f },
		{ "$jc_yellow_socket", 30f },
		{ "$jc_adv_yellow_socket", 20f },
		{ "$jc_perfect_yellow_socket", 10f }
	};

	public static readonly Dictionary<string, ConfigEntry<float>> gemUpgradeChances = new();
	public static Dictionary<Effect, List<EffectDef>> SocketEffects = new();
	public static readonly Dictionary<int, Dictionary<GemLocation, EffectPower>> EffectPowers = new();
	public static Dictionary<Heightmap.Biome, Dictionary<GemType, float>> GemDistribution = new();
	//private static SpriteAtlas slotIconAtlas = null!;
	//public static readonly Dictionary<GemLocation, Sprite> slotIcons = new();
	public static List<string> configFilePaths = null!;

	private static Skill jewelcrafting = null!;

	public static GameObject swordFall = null!;
	public static StatusEffect gliding = null!;
	public static SE_Stats lightningSpeed = null!;
	public static SE_Stats rootedRevenge = null!;
	public static SE_Stats poisonousDrain = null!;
	public static GameObject poisonousDrainCloud = null!;
	public static SE_Stats icyProtection = null!;
	public static SE_Stats fieryDoom = null!;
	public static GameObject fieryDoomExplosion = null!;

	private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
	{
		ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

		SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
		syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

		return configEntry;
	}

	private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

	public enum Toggle
	{
		On = 1,
		Off = 0
	}

	public enum UniqueDrop
	{
		Disabled = 0,
		TrulyUnique = 1,
		Custom = 2
	}

	[PublicAPI]
	public class ConfigurationManagerAttributes
	{
		public int? Order;
		public bool? HideSettingName;
		public bool? HideDefaultButton;
		public Action<ConfigEntryBase>? CustomDrawer;
	}
	
	public void Awake()
	{
		configFilePaths = new List<string> { Path.GetDirectoryName(Config.ConfigFilePath), Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) };

		Localizer.Load();

		AssetBundle assets = PrefabManager.RegisterAssetBundle("jewelcrafting");

		jewelcrafting = new Skill("Jewelcrafting", assets.LoadAsset<Sprite>("jewelcutting"));
		jewelcrafting.Name.Alias("jc_jewelcrafting_skill_name");
		jewelcrafting.Description.Alias("jc_jewelcrafting_skill_description");
		jewelcrafting.Configurable = false;
		
		serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
		configSync.AddLockingConfigEntry(serverConfigLocked);
		// Socket System
		socketSystem = config("2 - Socket System", "Socket System", Toggle.On, "Enables or disables the socket system. Cannot be enabled while Epic Loot is installed.");
		socketSystem.SettingChanged += (_, _) =>
		{
			if (socketSystem.Value == Toggle.On)
			{
				ConfigLoader.loaders.First(l => l.GetType() == typeof(EffectDef.Loader)).ApplyConfig();
			}
			else
			{
				SocketEffects.Clear();
				EffectPowers.Clear();
			}
			foreach (DestructibleSetup.DestructibleGem destructibleGem in FindObjectsOfType<DestructibleSetup.DestructibleGem>())
			{
				destructibleGem.gameObject.SetActive(socketSystem.Value == Toggle.On);
			}
		};
		config("2 - Socket System", "YAML Editor Anchor", 0, new ConfigDescription("Just ignore this.", null, new ConfigurationManagerAttributes { HideSettingName = true, HideDefaultButton = true, CustomDrawer = DrawYamlEditorButton }), false);
		uniqueGemDropSystem = config("2 - Socket System", "Drop System for Unique Gems", UniqueDrop.TrulyUnique, new ConfigDescription("Disabled: Unique Gems do not drop.\nTruly Unique: The first kill of each boss grants one Unique Gem.\nCustom: Lets you configure a drop chance and rate."));
		uniqueGemDropChance = config("2 - Socket System", "Drop Chance for Unique Gems", 30, new ConfigDescription("Drop chance for Unique Gems. Has no effect, if the drop system is not set to custom.", new AcceptableValueRange<int>(0, 100)));
		uniqueGemDropOnePerPlayer = config("2 - Socket System", "Drop one Gem per Player", Toggle.On, new ConfigDescription("If bosses should drop one Unique Gem per player. Has no effect, if the drop system is not set to custom."));
		chanceToAddSocket = config("2 - Socket System", "Chance to add a Socket", 50, new ConfigDescription("Chance to successfully add a socket to an item.", new AcceptableValueRange<int>(0, 100)));
		gemDropChanceOnyx = config("2 - Socket System", "Drop chance for Onyx Gemstones", 2, new ConfigDescription("Chance to drop an onyx gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100)));
		gemDropChanceSapphire = config("2 - Socket System", "Drop chance for Sapphire Gemstones", 2, new ConfigDescription("Chance to drop a sapphire gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100)));
		gemDropChanceEmerald = config("2 - Socket System", "Drop chance for Emerald Gemstones", 2, new ConfigDescription("Chance to drop an emerald gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100)));
		gemDropChanceSpinel = config("2 - Socket System", "Drop chance for Spinel Gemstones", 2, new ConfigDescription("Chance to drop a spinel gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100)));
		gemDropChanceRuby = config("2 - Socket System", "Drop chance for Ruby Gemstones", 2, new ConfigDescription("Chance to drop a ruby gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100)));
		gemDropChanceSulfur = config("2 - Socket System", "Drop chance for Sulfur Gemstones", 2, new ConfigDescription("Chance to drop a sulfur gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100)));
		maximumNumberSockets = config("2 - Socket System", "Maximum number of Sockets", 3, new ConfigDescription("Maximum number of sockets on each item.", new AcceptableValueRange<int>(1, 5)));
		gemRespawnRate = config("2 - Socket System", "Gemstone Respawn Time", 100, new ConfigDescription("Respawn time for raw gemstones in ingame days. Use 0 to disable respawn."));
		upgradeChanceIncrease = config("3 - Other", "Success Chance Increase", 15, new ConfigDescription("Success chance increase at jewelcrafting skill level 100.", new AcceptableValueRange<int>(0, 100)));
		experienceGainedFactor = config("3 - Other", "Skill Experience Gain Factor", 1f, new ConfigDescription("Factor for experience gained for the jewelcrafting skill.", new AcceptableValueRange<float>(0.01f, 5f)));
		experienceGainedFactor.SettingChanged += (_, _) => jewelcrafting.SkillGainFactor = experienceGainedFactor.Value;
		jewelcrafting.SkillGainFactor = experienceGainedFactor.Value;
		
		BuildingPiecesSetup.initializeBuildingPieces(assets);
		GemStoneSetup.initializeGemStones(assets);
		DestructibleSetup.initializeDestructibles(assets);

		int upgradeOrder = 0;
		foreach (GemDefinition gem in GemStoneSetup.Gems.Values.SelectMany(g => g).Where(g => g.DefaultUpgradeChance > 0))
		{
			gemUpgradeChances.Add(gem.Name, config("Socket Upgrade Chances", Localization.instance.Localize(gem.Name), gem.DefaultUpgradeChance, new ConfigDescription($"Success chance while trying to create {Localization.instance.Localize(gem.Name)}.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = --upgradeOrder })));
		}

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);

		swordFall = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_9");
		gliding = assets.LoadAsset<SE_Stats>("JCGliding");
		lightningSpeed = assets.LoadAsset<SE_Stats>("SE_Boss_1");
		lightningSpeed.m_damageModifier = 0.5f;
		poisonousDrain = assets.LoadAsset<SE_Stats>("SE_Boss_2");
		poisonousDrainCloud = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_2");
		rootedRevenge = assets.LoadAsset<SE_Stats>("SE_Boss_3");
		icyProtection = assets.LoadAsset<SE_Stats>("SE_Boss_4");
		fieryDoom = assets.LoadAsset<SE_Stats>("SE_Boss_5");
		fieryDoomExplosion = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_3");

		Necromancer.skeleton = PrefabManager.RegisterPrefab(assets, "JC_Skeleton");

		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_5");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_6");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_7");
		PrefabManager.RegisterPrefab(assets, "JCGliding");
		PrefabManager.RegisterPrefab(assets, "VFX_StoneRed");
		PrefabManager.RegisterPrefab(assets, "VFX_StoneBlack");
		PrefabManager.RegisterPrefab(assets, "sfx_build_table");
		PrefabManager.RegisterPrefab(assets, "sfx_crystal_craft");
		PrefabManager.RegisterPrefab(assets, "sfx_crystal_repair");
		PrefabManager.RegisterPrefab(assets, "sfx_destroy_table");
		PrefabManager.RegisterPrefab(assets, "sfx_stoneuse");
		PrefabManager.RegisterPrefab(assets, "sfx_rebirth");
		PrefabManager.RegisterPrefab(assets, "vfx_Destroy_Alchemy");
		PrefabManager.RegisterPrefab(assets, "sfx_buffed");
		PrefabManager.RegisterPrefab(assets, "vfx_crystal_destroyed");
		PrefabManager.RegisterPrefab(assets, "sfx_crystal_destroyed");
		PrefabManager.RegisterPrefab(assets, "vfx_potionhit");
		PrefabManager.RegisterPrefab(assets, "sfx_potion_smash");

		if (!File.Exists(Paths.ConfigPath + "/Jewelcrafting.Sockets.yml"))
		{
			File.WriteAllBytes(Paths.ConfigPath + "/Jewelcrafting.Sockets.yml", Utils.ReadEmbeddedFileBytes("GemEffects.Jewelcrafting.Sockets.yml"));
		}

		/*
		foreach (UnityEngine.Object asset in assets.LoadAllAssets())
		{
			Debug.Log($"{asset.name} ({asset.GetType()})");
		}
		
		slotIconAtlas = assets.LoadAsset<SpriteAtlas>("ppspriteatlas");
		slotIcons.Add(GemLocation.Bow, slotIconAtlas.GetSprite("bow"));
		slotIcons.Add(GemLocation.Chest, slotIconAtlas.GetSprite("chest"));
		slotIcons.Add(GemLocation.Cloak, slotIconAtlas.GetSprite("cape"));
		slotIcons.Add(GemLocation.Head, slotIconAtlas.GetSprite("head"));
		slotIcons.Add(GemLocation.Legs, slotIconAtlas.GetSprite("legs"));
		slotIcons.Add(GemLocation.Shield, slotIconAtlas.GetSprite("shield"));
		slotIcons.Add(GemLocation.Tool, slotIconAtlas.GetSprite("tool"));
		slotIcons.Add(GemLocation.Utility, slotIconAtlas.GetSprite("utility"));
		slotIcons.Add(GemLocation.Weapon, slotIconAtlas.GetSprite("weapon"));*/
	}

	[HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.AddPeer))]
	private static class AddRPCs
	{
		private static void Postfix(ZNetPeer peer)
		{
			if (ZNet.instance.IsServer())
			{
				peer.m_rpc.Register("Jewelcrafting GenerateVegetation", GenerateVegetationSpawners.RPC_GenerateVegetation);
			}
		}
	}
}
