using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
[BepInIncompatibility("DasSauerkraut.Terraheim")]
[BepInDependency("randyknapp.mods.extendeditemdataframework")]
public partial class Jewelcrafting : BaseUnityPlugin
{
	public const string ModName = "Jewelcrafting";
	private const string ModVersion = "1.1.1";
	private const string ModGUID = "org.bepinex.plugins.jewelcrafting";

	public static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

	private static ConfigEntry<Toggle> serverConfigLocked = null!;
	public static SyncedConfigEntry<Toggle> useExternalYaml = null!;
	public static ConfigEntry<Toggle> socketSystem = null!;
	public static ConfigEntry<Toggle> inventorySocketing = null!;
	public static ConfigEntry<InteractBehaviour> inventoryInteractBehaviour = null!;
	public static ConfigEntry<int> breakChanceUnsocketSimple = null!;
	public static ConfigEntry<int> breakChanceUnsocketAdvanced = null!;
	public static ConfigEntry<int> breakChanceUnsocketPerfect = null!;
	public static ConfigEntry<int> breakChanceUnsocketMerged = null!;
	public static ConfigEntry<Toggle> visualEffects = null!;
	public static ConfigEntry<UniqueDrop> uniqueGemDropSystem = null!;
	public static ConfigEntry<int> uniqueGemDropChance = null!;
	public static ConfigEntry<Toggle> uniqueGemDropOnePerPlayer = null!;
	public static ConfigEntry<int> resourceReturnRate = null!;
	public static ConfigEntry<Toggle> badLuckRecipes = null!;
	public static ConfigEntry<int> badLuckCostSimpleOnyx = null!;
	public static ConfigEntry<int> badLuckCostSimpleSapphire = null!;
	public static ConfigEntry<int> badLuckCostSimpleEmerald = null!;
	public static ConfigEntry<int> badLuckCostSimpleSpinel = null!;
	public static ConfigEntry<int> badLuckCostSimpleRuby = null!;
	public static ConfigEntry<int> badLuckCostSimpleSulfur = null!;
	public static ConfigEntry<int> badLuckCostAdvancedOnyx = null!;
	public static ConfigEntry<int> badLuckCostAdvancedSapphire = null!;
	public static ConfigEntry<int> badLuckCostAdvancedEmerald = null!;
	public static ConfigEntry<int> badLuckCostAdvancedSpinel = null!;
	public static ConfigEntry<int> badLuckCostAdvancedRuby = null!;
	public static ConfigEntry<int> badLuckCostAdvancedSulfur = null!;
	public static ConfigEntry<int> gemDropChanceOnyx = null!;
	public static ConfigEntry<int> gemDropChanceSapphire = null!;
	public static ConfigEntry<int> gemDropChanceEmerald = null!;
	public static ConfigEntry<int> gemDropChanceSpinel = null!;
	public static ConfigEntry<int> gemDropChanceRuby = null!;
	public static ConfigEntry<int> gemDropChanceSulfur = null!;
	public static readonly ConfigEntry<int>[] crystalFusionBoxDropRate = new ConfigEntry<int>[FusionBoxSetup.Boxes.Length];
	public static readonly ConfigEntry<int>[] crystalFusionBoxMergeDuration = new ConfigEntry<int>[FusionBoxSetup.Boxes.Length];
	public static ConfigEntry<int> maximumNumberSockets = null!;
	public static ConfigEntry<int> gemRespawnRate = null!;
	public static ConfigEntry<int> upgradeChanceIncrease = null!;
	public static ConfigEntry<int> awarenessRange = null!;
	private static ConfigEntry<int> rigidDamageReduction = null!;
	private static ConfigEntry<uint> headhunterDuration = null!;
	private static ConfigEntry<int> headhunterDamage = null!;
	private static ConfigEntry<float> experienceGainedFactor = null!;
	public static ConfigEntry<int> magicRepairAmount = null!;
	private static ConfigEntry<int> aquaticDamageIncrease = null!;

	public static readonly Dictionary<int, ConfigEntry<int>> socketAddingChances = new();
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

	private readonly Dictionary<string, int[]> defaultBoxMergeChances = new()
	{
		{ "$jc_common_gembox", new[] { 75, 25, 0 } },
		{ "$jc_epic_gembox", new[] { 100, 50, 25 } },
		{ "$jc_legendary_gembox", new[] { 100, 75, 50 } }
	};

	private readonly Dictionary<string, int[]> defaultBoxBossProgress = new()
	{
		{ "$enemy_eikthyr", new[] { 3, 1, 0 } },
		{ "$enemy_gdking", new[] { 5, 3, 0 } },
		{ "$enemy_bonemass", new[] { 7, 5, 1 } },
		{ "$enemy_dragon", new[] { 9, 7, 2 } },
		{ "$enemy_goblinking", new[] { 11, 9, 3 } }
	};

	public static readonly Dictionary<string, ConfigEntry<float>> gemUpgradeChances = new();
	public static readonly Dictionary<string, ConfigEntry<int>[]> boxMergeChances = new();
	public static readonly Dictionary<string, ConfigEntry<int>[]> boxBossProgress = new();

	public static Dictionary<Effect, List<EffectDef>> SocketEffects = new();
	public static readonly Dictionary<int, Dictionary<GemLocation, List<EffectPower>>> EffectPowers = new();
	public static Dictionary<Heightmap.Biome, Dictionary<GemType, float>> GemDistribution = new();
	//private static SpriteAtlas slotIconAtlas = null!;
	//public static readonly Dictionary<GemLocation, Sprite> slotIcons = new();
	public static List<string> configFilePaths = null!;

	private static Skill jewelcrafting = null!;

	public static GameObject swordFall = null!;
	public static StatusEffect gliding = null!;
	public static SE_Stats glowingSpirit = null!;
	public static GameObject glowingSpiritPrefab = null!;
	public static SE_Stats lightningSpeed = null!;
	public static SE_Stats rootedRevenge = null!;
	public static SE_Stats poisonousDrain = null!;
	public static GameObject poisonousDrainCloud = null!;
	public static SE_Stats icyProtection = null!;
	public static SE_Stats fieryDoom = null!;
	public static GameObject fieryDoomExplosion = null!;
	public static SE_Stats awareness = null!;
	public static GameObject heardIcon = null!;
	public static GameObject attackedIcon = null!;
	public static SE_Stats headhunter = null!;
	private static SE_Stats rigidFinger = null!;
	public static GameObject magicRepair = null!;
	public static SE_Stats aquatic = null!;
	public static GameObject lightningStart = null!;
	public static GameObject rootStart = null!;
	public static GameObject poisonStart = null!;
	public static GameObject iceStart = null!;
	public static GameObject fireStart = null!;

	private static Jewelcrafting self = null!;

	private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
	{
		ConfigEntry<T> configEntry = self.Config.Bind(group, name, value, description);

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

	public enum InteractBehaviour
	{
		Disabled = 0,
		Hovering = 1,
		Enabled = 2
	}

	[PublicAPI]
	public class ConfigurationManagerAttributes
	{
		public int? Order;
		public bool? HideSettingName;
		public bool? HideDefaultButton;
		public string? DispName;
		public Action<ConfigEntryBase>? CustomDrawer;
	}

	private static readonly Localization english = new();

	public void Awake()
	{
		self = this;

		configFilePaths = new List<string> { Path.GetDirectoryName(Config.ConfigFilePath), Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) };

		Localizer.Load();
		english.SetupLanguage("English");

		AssetBundle assets = PrefabManager.RegisterAssetBundle("jewelcrafting");

		jewelcrafting = new Skill("Jewelcrafting", assets.LoadAsset<Sprite>("jewelcutting"));
		jewelcrafting.Name.Alias("jc_jewelcrafting_skill_name");
		jewelcrafting.Description.Alias("jc_jewelcrafting_skill_description");
		jewelcrafting.Configurable = false;

		serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
		configSync.AddLockingConfigEntry(serverConfigLocked);
		// Socket System
		socketSystem = config("2 - Socket System", "Socket System", Toggle.On, "Enables or disables the socket system.");
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

		int order = 0;

		config("2 - Socket System", "YAML Editor Anchor", 0, new ConfigDescription("Just ignore this.", null, new ConfigurationManagerAttributes { HideSettingName = true, HideDefaultButton = true, CustomDrawer = DrawYamlEditorButton }), false);
		inventorySocketing = config("2 - Socket System", "Inventory Socketing", Toggle.On, "If enabled, you can press the interact key to change gems in your items from your inventory. If disabled, you have to use the Gemcutters Table, to change the gems in your items.");
		inventoryInteractBehaviour = config("2 - Socket System", "Interact Behaviour", InteractBehaviour.Hovering, "Disabled: Interact key is disabled, while the inventory is open.\nHovering: Interact key is disabled, while hovering an item with at least one socket.\nEnabled: Interact key is enabled. You will have to use the Gemcutters Table, to socket your items.", false);
		visualEffects = config("2 - Socket System", "Particle Effects", Toggle.On, "Enables or disables the particle effects for perfect gems.", false);
		visualEffects.SettingChanged += (_, _) =>
		{
			foreach (ItemDrop item in Resources.FindObjectsOfTypeAll<ItemDrop>())
			{
				if (visualEffects.Value == Toggle.On)
				{
					VisualEffects.DisplayEffectOnItemDrop.Postfix(item);
				}
				else
				{
					VisualEffects.DisplayEffectOnItemDrop.RemoveEffects(item);
				}
			}
		};
		useExternalYaml = configSync.AddConfigEntry(Config.Bind("2 - Socket System", "Use External YAML", Toggle.Off, new ConfigDescription("If set to on, the YAML file from your config folder will be used, to override gem effects configured inside of that file.", null, new ConfigurationManagerAttributes { Order = --order })));
		useExternalYaml.SourceConfig.SettingChanged += (_, _) => ConfigLoader.reloadConfigFile();
		badLuckRecipes = config("2 - Socket System", "Bad Luck Recipes", Toggle.On, new ConfigDescription("Enables or disables the bad luck recipes of all gems.", null, new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropSystem = config("2 - Socket System", "Drop System for Unique Gems", UniqueDrop.TrulyUnique, new ConfigDescription("Disabled: Unique Gems do not drop.\nTruly Unique: The first kill of each boss grants one Unique Gem.\nCustom: Lets you configure a drop chance and rate.", null, new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropChance = config("2 - Socket System", "Drop Chance for Unique Gems", 30, new ConfigDescription("Drop chance for Unique Gems. Has no effect, if the drop system is not set to custom.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropOnePerPlayer = config("2 - Socket System", "Drop one Gem per Player", Toggle.On, new ConfigDescription("If bosses should drop one Unique Gem per player. Has no effect, if the drop system is not set to custom.", null, new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketSimple = config("2 - Socket System", "Simple Gem Break Chance", 0, new ConfigDescription("Chance to break a simple gem when trying to remove it from a socket.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketAdvanced = config("2 - Socket System", "Advanced Gem Break Chance", 0, new ConfigDescription("Chance to break an advanced gem when trying to remove it from a socket.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketPerfect = config("2 - Socket System", "Perfect Gem Break Chance", 0, new ConfigDescription("Chance to break a perfect gem when trying to remove it from a socket.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketMerged = config("2 - Socket System", "Merged Gem Break Chance", 0, new ConfigDescription("Chance to break a merged gem when trying to remove it from a socket.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		resourceReturnRate = config("2 - Socket System", "Percentage Recovered", 0, new ConfigDescription("Percentage of items to be recovered, when an item breaks while trying to add a socket to it.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostSimpleOnyx = config("2 - Socket System", "Bad Luck Cost Simple Onyx", 12, new ConfigDescription("Onyx shards required to craft a Simple Onyx.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostSimpleSapphire = config("2 - Socket System", "Bad Luck Cost Simple Sapphire", 12, new ConfigDescription("Sapphire shards required to craft a Simple Sapphire.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostSimpleEmerald = config("2 - Socket System", "Bad Luck Cost Simple Emerald", 12, new ConfigDescription("Emerald shards required to craft a Simple Emerald.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostSimpleSpinel = config("2 - Socket System", "Bad Luck Cost Simple Spinel", 12, new ConfigDescription("Spinel shards required to craft a Simple Spinel.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostSimpleRuby = config("2 - Socket System", "Bad Luck Cost Simple Ruby", 12, new ConfigDescription("Ruby shards required to craft a Simple Ruby.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostSimpleSulfur = config("2 - Socket System", "Bad Luck Cost Simple Sulfur", 12, new ConfigDescription("Sulfur shards required to craft a Simple Sulfur.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostAdvancedOnyx = config("2 - Socket System", "Bad Luck Cost Advanced Onyx", 35, new ConfigDescription("Onyx shards required to craft an Advanced Onyx.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostAdvancedSapphire = config("2 - Socket System", "Bad Luck Cost Advanced Sapphire", 35, new ConfigDescription("Sapphire shards required to craft an Advanced Sapphire.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostAdvancedEmerald = config("2 - Socket System", "Bad Luck Cost Advanced Emerald", 35, new ConfigDescription("Emerald shards required to craft an Advanced Emerald.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostAdvancedSpinel = config("2 - Socket System", "Bad Luck Cost Advanced Spinel", 35, new ConfigDescription("Spinel shards required to craft an Advanced Spinel.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostAdvancedRuby = config("2 - Socket System", "Bad Luck Cost Advanced Ruby", 35, new ConfigDescription("Ruby shards required to craft an Advanced Ruby.", null, new ConfigurationManagerAttributes { Order = --order }));
		badLuckCostAdvancedSulfur = config("2 - Socket System", "Bad Luck Cost Advanced Sulfur", 35, new ConfigDescription("Sulfur shards required to craft an Advanced Sulfur.", null, new ConfigurationManagerAttributes { Order = --order }));
		gemDropChanceOnyx = config("2 - Socket System", "Drop chance for Onyx Gemstones", 2, new ConfigDescription("Chance to drop an onyx gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		gemDropChanceSapphire = config("2 - Socket System", "Drop chance for Sapphire Gemstones", 2, new ConfigDescription("Chance to drop a sapphire gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		gemDropChanceEmerald = config("2 - Socket System", "Drop chance for Emerald Gemstones", 2, new ConfigDescription("Chance to drop an emerald gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		gemDropChanceSpinel = config("2 - Socket System", "Drop chance for Spinel Gemstones", 2, new ConfigDescription("Chance to drop a spinel gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		gemDropChanceRuby = config("2 - Socket System", "Drop chance for Ruby Gemstones", 2, new ConfigDescription("Chance to drop a ruby gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		gemDropChanceSulfur = config("2 - Socket System", "Drop chance for Sulfur Gemstones", 2, new ConfigDescription("Chance to drop a sulfur gemstone when killing creatures.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxDropRate[0] = config("3 - Fusion Box", "Drop rate for Fusion Box", 200, new ConfigDescription("Drop rate for the Common Crystal Fusion Box. Format is 1:x. The chance is further increased by creature health. Rate is for base 100 HP. Use 0 to disable the drop.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxDropRate[1] = config("3 - Fusion Box", "Drop rate for Blessed Fusion Box", 500, new ConfigDescription("Drop rate for the Blessed Crystal Fusion Box. Format is 1:x. The chance is further increased by creature health. Rate is for base 100 HP. Use 0 to disable the drop.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxDropRate[2] = config("3 - Fusion Box", "Drop rate for Celestial Fusion Box", 1000, new ConfigDescription("Drop rate for the Celestial Crystal Fusion Box. Format is 1:x. The chance is further increased by creature health. Rate is for base 100 HP. Use 0 to disable the drop.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxMergeDuration[0] = config("3 - Fusion Box", "Merge Duration for Fusion Box", 15, new ConfigDescription("Ingame days required for the Common Crystal Fusion Box to finish merging.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxMergeDuration[1] = config("3 - Fusion Box", "Merge Duration for Blessed Fusion Box", 40, new ConfigDescription("Ingame days required for the Blessed Crystal Fusion Box to finish merging", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxMergeDuration[2] = config("3 - Fusion Box", "Merge Duration for Celestial Fusion Box", 75, new ConfigDescription("Ingame days required for the Celestial Crystal Fusion Box to finish merging.", null, new ConfigurationManagerAttributes { Order = --order }));
		maximumNumberSockets = config("2 - Socket System", "Maximum number of Sockets", 3, new ConfigDescription("Maximum number of sockets on each item.", new AcceptableValueRange<int>(1, 5), new ConfigurationManagerAttributes { Order = --order }));
		gemRespawnRate = config("2 - Socket System", "Gemstone Respawn Time", 100, new ConfigDescription("Respawn time for raw gemstones in ingame days. Use 0 to disable respawn.", null, new ConfigurationManagerAttributes { Order = --order }));
		upgradeChanceIncrease = config("4 - Other", "Success Chance Increase", 15, new ConfigDescription("Success chance increase at jewelcrafting skill level 100.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		experienceGainedFactor = config("4 - Other", "Skill Experience Gain Factor", 1f, new ConfigDescription("Factor for experience gained for the jewelcrafting skill.", new AcceptableValueRange<float>(0.01f, 5f), new ConfigurationManagerAttributes { Order = --order }));
		experienceGainedFactor.SettingChanged += (_, _) => jewelcrafting.SkillGainFactor = experienceGainedFactor.Value;
		jewelcrafting.SkillGainFactor = experienceGainedFactor.Value;

		awarenessRange = config("Ruby Necklace of Awareness", "Detection Range", 30, new ConfigDescription("Creature detection range for the Ruby Necklace of Awareness.", new AcceptableValueRange<int>(1, 50)));
		rigidDamageReduction = config("Sturdy Spinel Ring", "Damage Reduction", 5, new ConfigDescription("Damage reduction for the Sturdy Spinel Ring.", new AcceptableValueRange<int>(0, 100)));
		headhunterDuration = config("Emerald Headhunter Ring", "Effect Duration", 20U, new ConfigDescription("Effect duration for the Emerald Headhunter Ring."));
		headhunterDamage = config("Emerald Headhunter Ring", "Damage Increase", 30, new ConfigDescription("Damage increase for the Emerald Headhunter Ring effect.", new AcceptableValueRange<int>(0, 100)));
		magicRepairAmount = config("Emerald Necklace of Magic Repair", "Repair Amount", 5, new ConfigDescription("Durability restoration per minute for the Emerald Necklace of Magic Repair effect.", new AcceptableValueRange<int>(0, 100)));
		aquaticDamageIncrease = config("Aquatic Sapphire Necklace", "Damage Increase", 10, new ConfigDescription("Damage increase while wearing the Aquatic Sapphire Necklace and being wet.", new AcceptableValueRange<int>(0, 100)));

		void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
		{
			setter(config.Value);
			config.SettingChanged += (_, _) => setter(config.Value);
		}

		BuildingPiecesSetup.initializeBuildingPieces(assets);
		GemStoneSetup.initializeGemStones(assets);
		DestructibleSetup.initializeDestructibles(assets);
		JewelrySetup.initializeJewelry(assets);
		VisualEffectSetup.initializeVisualEffects(assets);
		MergedGemStoneSetup.initializeMergedGemStones(assets);
		FusionBoxSetup.initializeFusionBoxes(assets);

		int upgradeOrder = 0;
		foreach (GemDefinition gem in GemStoneSetup.Gems.Values.SelectMany(g => g).Where(g => g.DefaultUpgradeChance > 0))
		{
			gemUpgradeChances.Add(gem.Name, config("Socket Upgrade Chances", english.Localize(gem.Name), gem.DefaultUpgradeChance, new ConfigDescription($"Success chance while trying to create {Localization.instance.Localize(gem.Name)}.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = --upgradeOrder, DispName = Localization.instance.Localize(gem.Name) })));
		}

		int socketAddingOrder = 0;
		for (int i = 0; i < 5; ++i)
		{
			socketAddingChances.Add(i, config("Socket Adding Chances", $"{i + 1}. Socket", 50, new ConfigDescription($"Success chance while trying to add the {i + 1}. Socket.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --socketAddingOrder })));
		}

		string[] boxMergeCategory = { "simple", "advanced", "perfect" };
		int boxMergeOrder = 0;
		foreach (KeyValuePair<string, int[]> kv in defaultBoxMergeChances)
		{
			boxMergeChances.Add(kv.Key, kv.Value.Select((chance, i) => config("3 - Fusion Box", $"Merge Chance {boxMergeCategory[i]} gems in {english.Localize(kv.Key)}", chance, new ConfigDescription($"Success chance while merging two {boxMergeCategory[i]} gems in a {Localization.instance.Localize(kv.Key)}", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --boxMergeOrder }))).ToArray());
		}

		foreach (KeyValuePair<string, int[]> kv in defaultBoxBossProgress)
		{
			AddBossBoxProgressConfig(kv.Key, kv.Value);
		}

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);

		swordFall = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_9");
		gliding = assets.LoadAsset<SE_Stats>("JCGliding");
		glowingSpirit = assets.LoadAsset<SE_Stats>("SE_Crystal_Magelight");
		glowingSpiritPrefab = PrefabManager.RegisterPrefab(assets, "JC_Crystal_Magelight");
		glowingSpiritPrefab.AddComponent<GlowingSpirit.OrbDestroy>();
		lightningSpeed = Utils.ConvertStatusEffect<LightningSpeed.LightningSpeedEffect>(assets.LoadAsset<SE_Stats>("JC_Electric_Wings_SE"));
		lightningSpeed.m_damageModifier = 0.5f;
		poisonousDrain = assets.LoadAsset<SE_Stats>("SE_Boss_2");
		poisonousDrainCloud = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_2");
		rootedRevenge = assets.LoadAsset<SE_Stats>("SE_Boss_3");
		icyProtection = assets.LoadAsset<SE_Stats>("SE_Boss_4");
		fieryDoom = assets.LoadAsset<SE_Stats>("SE_Boss_5");
		fieryDoomExplosion = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_3");
		awareness = assets.LoadAsset<SE_Stats>("JC_SE_Necklace_Red");
		heardIcon = assets.LoadAsset<GameObject>("JC_Eyeball_Obj");
		attackedIcon = assets.LoadAsset<GameObject>("JC_Alert_Obj");
		headhunter = assets.LoadAsset<SE_Stats>("JC_Se_Ring_Green");
		rigidFinger = assets.LoadAsset<SE_Stats>("JC_Se_Ring_Purple");
		magicRepair = PrefabManager.RegisterPrefab(assets, "VFX_Buff_Green");
		aquatic = assets.LoadAsset<SE_Stats>("JC_Se_Necklace_Blue");
		lightningStart = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Purple");
		rootStart = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Brown");
		poisonStart = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Green");
		iceStart = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Blue");
		fireStart = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Red");

		SetCfgValue(value => rigidFinger.m_damageModifier = 1 - value / 100f, rigidDamageReduction);
		SetCfgValue(value => headhunter.m_damageModifier = 1 + value / 100f, headhunterDamage);
		SetCfgValue(value => headhunter.m_ttl = value, headhunterDuration);
		SetCfgValue(value => aquatic.m_damageModifier = 1 + value / 100f, aquaticDamageIncrease);

		Necromancer.skeleton = PrefabManager.RegisterPrefab(assets, "JC_Skeleton");

		PrefabManager.RegisterPrefab(assets, "JC_Electric_Wings");
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
		PrefabManager.RegisterPrefab(assets, "vfx_puff_small");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_1");

		Localizer.AddPlaceholder("jc_electric_wings_description", "power", rigidDamageReduction);
		Localizer.AddPlaceholder("jc_ring_purple_description", "power", rigidDamageReduction);
		Localizer.AddPlaceholder("jc_se_ring_purple_description", "power", rigidDamageReduction);
		Localizer.AddPlaceholder("jc_ring_green_description", "power", headhunterDamage);
		Localizer.AddPlaceholder("jc_ring_green_description", "duration", headhunterDuration);
		Localizer.AddPlaceholder("jc_se_ring_green_description", "power", headhunterDamage);
		Localizer.AddPlaceholder("jc_se_necklace_blue_description", "power", aquaticDamageIncrease);

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

	private static void AddBossBoxProgressConfig(string name, int[] progress)
	{
		Regex regex = new("['[\"\\]]");
		
		boxBossProgress.Add(name, progress.Select((chance, i) => config("3 - Fusion Box", $"Boss Progress {regex.Replace(english.Localize(name), "")} for {english.Localize(FusionBoxSetup.Boxes[i].GetComponent<ItemDrop>().m_itemData.m_shared.m_name)}", chance, new ConfigDescription($"Progress applied to {english.Localize(FusionBoxSetup.Boxes[i].GetComponent<ItemDrop>().m_itemData.m_shared.m_name)} when killing {regex.Replace(english.Localize(name), "")}", null, new ConfigurationManagerAttributes { Order = -boxBossProgress.Count * 3 - i - 1000 }))).ToArray());
	}

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private static class AddBossBoxProgressConfigs
	{
		private static void Postfix(ZNetScene __instance)
		{
			foreach (GameObject prefab in __instance.m_prefabs)
			{
				if (prefab.GetComponent<Character>() is { } character && character.IsBoss() && !boxBossProgress.ContainsKey(character.m_name))
				{
					AddBossBoxProgressConfig(character.m_name, new[] { 0, 0, 0 });
				}
			}
		}
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

	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class AddStatusEffects
	{
		private static void Prefix(ObjectDB __instance)
		{
			__instance.m_StatusEffects.Add(headhunter);
		}
	}
}
