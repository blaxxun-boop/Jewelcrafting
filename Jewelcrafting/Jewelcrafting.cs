using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using Jewelcrafting.GemEffects;
using Jewelcrafting.Setup;
using Jewelcrafting.WorldBosses;
using LocalizationManager;
using ServerSync;
using SkillManager;
using TMPro;
using UnityEngine;

namespace Jewelcrafting;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInIncompatibility("randyknapp.mods.epicloot")]
[BepInIncompatibility("org.bepinex.plugins.valheim_plus")]
[BepInDependency("org.bepinex.plugins.groups", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.creaturelevelcontrol", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Azumatt.AzuExtendedPlayerInventory", BepInDependency.DependencyFlags.SoftDependency)]
public partial class Jewelcrafting : BaseUnityPlugin
{
	public const string ModName = "Jewelcrafting";
	private const string ModVersion = "1.5.32";
	private const string ModGUID = "org.bepinex.plugins.jewelcrafting";

	public static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

	private static ConfigEntry<Toggle> serverConfigLocked = null!;
	public static SyncedConfigEntry<Toggle> useExternalYaml = null!;
	public static ConfigEntry<Toggle> inventorySocketing = null!;
	public static ConfigEntry<Toggle> displayGemcursor = null!;
	public static ConfigEntry<Toggle> displaySocketBackground = null!;
	public static ConfigEntry<Toggle> colorItemName = null!;
	public static ConfigEntry<Unsocketing> allowUnsocketing = null!;
	public static ConfigEntry<InteractBehaviour> inventoryInteractBehaviour = null!;
	public static ConfigEntry<float> breakChanceUnsocketSimple = null!;
	public static ConfigEntry<float> breakChanceUnsocketAdvanced = null!;
	public static ConfigEntry<float> breakChanceUnsocketPerfect = null!;
	public static ConfigEntry<float> breakChanceUnsocketMerged = null!;
	public static ConfigEntry<Toggle> socketingItemsExperience = null!;
	public static ConfigEntry<Toggle> visualEffects = null!;
	public static ConfigEntry<UniqueDrop> uniqueGemDropSystem = null!;
	public static ConfigEntry<int> uniqueGemDropChance = null!;
	public static ConfigEntry<Toggle> uniqueGemDropOnePerPlayer = null!;
	public static ConfigEntry<int> uniqueGemDropChanceIncreasePerWorldLevel = null!;
	public static ConfigEntry<int> resourceReturnRate = null!;
	public static ConfigEntry<int> resourceReturnRateUpgrade = null!;
	public static ConfigEntry<int> resourceReturnRateDistance = null!;
	public static ConfigEntry<Toggle> badLuckRecipes = null!;
	public static readonly ConfigEntry<int>[] crystalFusionBoxDropRate = new ConfigEntry<int>[FusionBoxSetup.Boxes.Length];
	public static readonly ConfigEntry<float>[] crystalFusionBoxMergeActivityProgress = new ConfigEntry<float>[FusionBoxSetup.Boxes.Length];
	public static ConfigEntry<int> maximumNumberSockets = null!;
	public static ConfigEntry<Toggle> limitSocketsByTableLevel = null!;
	public static readonly ConfigEntry<int>[] maxSocketsTableLevel = new ConfigEntry<int>[4];
	public static ConfigEntry<int> gemRespawnRate = null!;
	public static ConfigEntry<int> upgradeChanceIncrease = null!;
	public static ConfigEntry<Toggle> additiveSkillBonus = null!;
	public static ConfigEntry<int> awarenessRange = null!;
	private static ConfigEntry<int> rigidDamageReduction = null!;
	private static ConfigEntry<uint> headhunterDuration = null!;
	private static ConfigEntry<int> headhunterDamage = null!;
	private static ConfigEntry<float> experienceGainedFactor = null!;
	private static ConfigEntry<int> experienceLoss = null!;
	private static ConfigEntry<int> warmthStaminaRegen = null!;
	public static ConfigEntry<int> magicRepairAmount = null!;
	private static ConfigEntry<int> aquaticDamageIncrease = null!;
	public static ConfigEntry<int> modersBlessingDuration = null!;
	public static ConfigEntry<int> modersBlessingCooldown = null!;
	public static ConfigEntry<int> guidanceCooldown = null!;
	public static ConfigEntry<int> legacyCooldown = null!;
	public static ConfigEntry<int> gemBagSlotsRows = null!;
	public static ConfigEntry<int> gemBagSlotsColumns = null!;
	public static ConfigEntry<Toggle> gemBagAutofill = null!;
	public static ConfigEntry<int> gemBoxSlotsRows = null!;
	public static ConfigEntry<int> gemBoxSlotsColumns = null!;
	public static ConfigEntry<KeyboardShortcut> advancedTooltipKey = null!;
	public static ConfigEntry<AdvancedTooltipMode> advancedTooltipMode = null!;
	public static ConfigEntry<Toggle> advancedTooltipAlwaysOn = null!;
	public static ConfigEntry<Toggle> gachaLocationIcon = null!;
	public static ConfigEntry<Toggle> gachaLocationActive = null!;
	public static ConfigEntry<int> bossSpawnTimer = null!;
	public static ConfigEntry<int> bossSpawnMinDistance = null!;
	public static ConfigEntry<int> bossSpawnMaxDistance = null!;
	public static ConfigEntry<int> bossSpawnBaseDistance = null!;
	public static ConfigEntry<int> bossTimeLimit = null!;
	public static ConfigEntry<int> bossCoinDrop = null!;
	public static ConfigEntry<float> eventBossSpawnChance = null!;
	private static ConfigEntry<GachaSetup.BalanceToggle> worldBossBalance = null!;
	private static ConfigEntry<float> worldBossHealth = null!;
	private static ConfigEntry<float> worldBossPunchDamage = null!;
	private static ConfigEntry<float> worldBossSmashDamage = null!;
	private static ConfigEntry<float> worldBossFireDamage = null!;
	private static ConfigEntry<float> worldBossFrostDamage = null!;
	private static ConfigEntry<float> worldBossPoisonDamage = null!;
	public static ConfigEntry<int> worldBossBonusWeaponDamage = null!;
	public static ConfigEntry<int> worldBossBonusBlockPower = null!;
	public static ConfigEntry<int> worldBossCountdownDisplayOffset = null!;
	public static ConfigEntry<Toggle> worldBossExploitProtectionHeal = null!;
	public static ConfigEntry<Toggle> worldBossExploitProtectionRangedShield = null!;
	public static ConfigEntry<float> defaultEventDuration = null!;
	public static ConfigEntry<int> frameOfChanceChance = null!;
	public static ConfigEntry<Toggle> mirrorMirrorImages = null!;
	public static ConfigEntry<Toggle> unsocketMirrorImages = null!;
	public static ConfigEntry<string> socketBlacklist = null!;
	public static ConfigEntry<string> mirrorBlacklist = null!;
	public static ConfigEntry<Toggle> gemstoneFormationParticles = null!;
	public static ConfigEntry<Toggle> wisplightGem = null!;
	public static ConfigEntry<Toggle> wishboneGem = null!;
	public static ConfigEntry<LootSystem> lootSystem = null!;
	public static ConfigEntry<Toggle> lootBeams = null!;
	public static readonly Dictionary<LootSystem, LootConfigs> lootConfigs = new();

	public class LootConfigs
	{
		public ConfigEntry<float> lootSkew = null!;
		public ConfigEntry<float> lootLowHpChance = null!;
		public ConfigEntry<float> lootDefaultChance = null!;
		public ConfigEntry<LootRestriction> lootRestriction = null!;
		public ConfigEntry<Toggle> unsocketDroppedItems = null!;
		public ConfigEntry<Toggle> addSocketToDroppedItem = null!;
	}

	public static ConfigEntry<int> gemChestMinGems = null!;
	public static ConfigEntry<int> gemChestMaxGems = null!;
	public static ConfigEntry<int> gemChestAllowedAmount = null!;
	public static ConfigEntry<int> equipmentChestMinItems = null!;
	public static ConfigEntry<int> equipmentChestMaxItems = null!;
	public static ConfigEntry<int> equipmentChestAllowedAmount = null!;
	public static ConfigEntry<Toggle> gemstoneFormations = null!;
	public static ConfigEntry<float> gemstoneFormationHealth = null!;
	public static ConfigEntry<Toggle> featherGliding = null!;
	public static ConfigEntry<int> featherGlidingBuff = null!;
	public static ConfigEntry<Toggle> asksvinRunning = null!;
	public static ConfigEntry<int> asksvinRunningBuff = null!;
	public static ConfigEntry<Toggle> ringSlot = null!;
	public static ConfigEntry<Toggle> necklaceSlot = null!;
	public static ConfigEntry<Toggle> splitSockets = null!;
	public static ConfigEntry<SocketCost> socketCost = null!;
	public static ConfigEntry<Toggle> pixelateTextures = null!;
	public static ConfigEntry<int> divinityOrbDropChance = null!;
	public static ConfigEntry<Toggle> randomPowerRanges = null!;
	public static ConfigEntry<Toggle> vanillaGemCrafting = null!;
	public static ConfigEntry<float> bigGemstoneFormationChance = null!;
	public static ConfigEntry<int> effectPowerStandardDeviation = null!;
	public static ConfigEntry<Toggle> gemReturnLockedGems = null!;
	public static ConfigEntry<Toggle> disableUniqueGemsInBase = null!;
	public static ConfigEntry<Toggle> gemDropBiomeDistribution = null!;
	public static ConfigEntry<Toggle> glidingFallDamagePrevention = null!;

	public static readonly Dictionary<int, ConfigEntry<int>> socketAddingChances = new();
	public static readonly Dictionary<GameObject, ConfigEntry<float>> gemDropChances = new();
	public static readonly CustomSyncedValue<List<string>> socketEffectDefinitions = new(configSync, "socket effects", new List<string>());

	private readonly Dictionary<string, int[]> defaultBoxMergeChances = new()
	{
		{ "$jc_common_gembox", new[] { 90, 40, 10 } },
		{ "$jc_epic_gembox", new[] { 100, 70, 35 } },
		{ "$jc_legendary_gembox", new[] { 100, 90, 65 } },
	};

	private readonly Dictionary<string, float[]> defaultBoxBossProgress = new()
	{
		{ "$enemy_eikthyr", new[] { 12f, 0.5f, 0 } },
		{ "$enemy_gdking", new[] { 15f, 2f, 0.5f } },
		{ "$enemy_bonemass", new[] { 20f, 4f, 1.5f } },
		{ "$enemy_dragon", new[] { 28f, 12f, 3f } },
		{ "$enemy_goblinking", new[] { 40f, 20f, 6f } },
		{ "$enemy_seekerqueen", new[] { 55f, 30f, 9f } },
		{ "$jc_crystal_frost_reaper", new[] { 57f, 31f, 10f } },
		{ "$jc_crystal_flame_reaper", new[] { 57f, 31f, 10f } },
		{ "$jc_crystal_soul_reaper", new[] { 57f, 31f, 10f } },
		{ "$enemy_fader", new[] { 60f, 33f, 11f } },
	};

	public static readonly Dictionary<string, ConfigEntry<float>> gemUpgradeChances = new();
	public static readonly Dictionary<string, ConfigEntry<int>[]> boxMergeChances = new();
	public static readonly Dictionary<string, ConfigEntry<int>> boxSelfMergeChances = new();
	public static readonly Dictionary<string, ConfigEntry<float>[]> boxBossProgress = new();
	public static ConfigEntry<int> boxBossGemMergeChance = null!;

	public static Dictionary<Effect, List<EffectDef>> SocketEffects = new();
	public static readonly Dictionary<int, Dictionary<GemLocation, List<EffectPower>>> EffectPowers = new();
	public static readonly HashSet<GemType> GemsUsingPowerRanges = new();
	public static Dictionary<Heightmap.Biome, Dictionary<GemType, float>> GemDistribution = new();
	public static List<string> configFilePaths = null!;
	public static readonly List<SynergyDef> Synergies = new();

	public static readonly HashSet<string> PrefabBlacklist = new();

	public const int maxNumberOfSockets = 10;

	private static Skill jewelcrafting = null!;

	public static Jewelcrafting self = null!;

	public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
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
		Off = 0,
	}

	public enum UniqueDrop
	{
		Disabled = 0,
		TrulyUnique = 1,
		Custom = 2,
		GuaranteedFirst = 3,
	}

	public enum Unsocketing
	{
		Disabled = 0,
		UniquesOnly = 1,
		All = 2,
	}

	public enum InteractBehaviour
	{
		Disabled = 0,
		Hovering = 1,
		Enabled = 2,
	}

	public enum AdvancedTooltipMode
	{
		General = 0,
		Detailed = 1,
	}

	[Flags]
	public enum LootSystem
	{
		GemDrops = 1,
		EquipmentDrops = 2,
		GemChests = 4,
		EquipmentChests = 8,
	}

	public enum LootRestriction
	{
		None = 0,
		KnownStation = 1,
		KnownRecipe = 2,
	}

	public enum SocketCost
	{
		ItemMayBreak,
		CostsItems,
		BreakOrCost,
		BreakAndCost,
	}

	[PublicAPI]
	public class ConfigurationManagerAttributes
	{
		public int? Order;
		public bool? Browsable;
		public bool? HideSettingName;
		public bool? HideDefaultButton;
		public string? DispName;
		public Action<ConfigEntryBase>? CustomDrawer;
	}

	private static object? configManager;

	private static void reloadConfigDisplay() => configManager?.GetType().GetMethod("BuildSettingList")!.Invoke(configManager, Array.Empty<object>());

	[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
	private static class FetchConfigManager
	{
		private static void Prefix()
		{
			Assembly? bepinexConfigManager = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");

			Type? configManagerType = bepinexConfigManager?.GetType("ConfigurationManager.ConfigurationManager");
			configManager = configManagerType == null ? null : BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(configManagerType);
		}
	}

	internal static Localization english = null!;

	private AssetBundle assets = null!;

	public void Awake()
	{
		self = this;

		configFilePaths = [Path.GetDirectoryName(Config.ConfigFilePath)!, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!];

		Config.SaveOnConfigSet = false;

		APIManager.Patcher.Patch();
		RuntimeHelpers.RunClassConstructor(typeof(Stats).TypeHandle);
		Localizer.Load();
		english = new Localization();
		english.SetupLanguage("English");

		assets = PrefabManager.RegisterAssetBundle("jewelcrafting");
		AssetBundle compendiumAssets = PrefabManager.RegisterAssetBundle("jc_ui_additions");
		CompendiumDisplay.initializeCompendiumDisplay(compendiumAssets);

		jewelcrafting = new Skill("Jewelcrafting", assets.LoadAsset<Sprite>("jewelcutting"));
		jewelcrafting.Name.Alias("jc_jewelcrafting_skill_name");
		jewelcrafting.Description.Alias("jc_jewelcrafting_skill_description");
		jewelcrafting.Configurable = false;

		serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
		configSync.AddLockingConfigEntry(serverConfigLocked);
		// Socket System
		int order = 0;

		config("2 - Socket System", "YAML Editor Anchor", 0, new ConfigDescription("Just ignore this.", null, new ConfigurationManagerAttributes { HideSettingName = true, HideDefaultButton = true, CustomDrawer = DrawYamlEditorButton }), false);
		inventorySocketing = config("2 - Socket System", "Inventory Socketing", Toggle.On, "If enabled, you can press the interact key to change gems in your items from your inventory. If disabled, you have to use the Gemcutters Table, to change the gems in your items.");
		socketCost = config("2 - Socket System", "Socket Cost", SocketCost.ItemMayBreak, "Item May Break: If adding a socket to the item fails, the item will be destroyed.\nCosts Items: Adding sockets to an item costs other items. Use the Jewelcrafting.SocketCosts.yml to configure this.\nBreak Or Cost: Successfully adding a socket spends the resources required for that socket. Failing to add a socket breaks the item.\nBreak And Cost: Successfully adding a socket spends the resources required for that socket. Failing to add a socket breaks the item and spends the resources.");
		effectPowerStandardDeviation = config("2 - Socket System", "Effect Power Deviation", 0, new ConfigDescription("Can be used to apply a standard deviation in percent to all gem effects. E.g. putting 30 here, will turn an effect power of 10 into 7 - 13. Use 0 to disable this.", new AcceptableValueRange<int>(0, 75)));
		effectPowerStandardDeviation.SettingChanged += (_, _) => ConfigLoader.TryReapplyConfig();
		randomPowerRanges = config("2 - Socket System", "Random Power Ranges", Toggle.On, "If enabled, the effect powers for power ranges on gems are different for each effect on the gem.");
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
		displayGemcursor = config("2 - Socket System", "Display Gem Cursor", Toggle.On, "Changes the cursor to a gem, while interacting with the Gemcutters Table or socketing an item.", false);
		displaySocketBackground = config("2 - Socket System", "Display Socket Background", Toggle.On, "Changes the background of items that have sockets.", false);
		displaySocketBackground.SettingChanged += (_, _) => SocketsBackground.UpdateSocketBackground();
		colorItemName = config("2 - Socket System", "Color Item Names", Toggle.On, "Colors the name of items according to their socket levels.", false);
		useExternalYaml = configSync.AddConfigEntry(Config.Bind("2 - Socket System", "Use External YAML", Toggle.Off, new ConfigDescription("If set to on, external YAML files will be loaded from your config folder. These can be used, to highly fine tune Jewelcrafting to your liking.", null, new ConfigurationManagerAttributes { Order = --order })));
		useExternalYaml.SourceConfig.SettingChanged += (_, _) => ConfigLoader.reloadConfigFile();
		badLuckRecipes = config("2 - Socket System", "Bad Luck Recipes", Toggle.On, new ConfigDescription("Enables or disables the bad luck recipes of all gems.", null, new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropSystem = config("2 - Socket System", "Drop System for Unique Gems", UniqueDrop.TrulyUnique, new ConfigDescription("Disabled: Unique Gems do not drop.\nTruly Unique: The first kill of each boss grants one Unique Gem.\nCustom: Lets you configure a drop chance and rate.\nGuaranteed First: The first kill of each boss grants one Unique Gem. After that, the custom drop chance is used.", null, new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropChance = config("2 - Socket System", "Drop Chance for Unique Gems", 30, new ConfigDescription("Drop chance for Unique Gems. Has no effect, if the drop system is not set to custom.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropChanceIncreasePerWorldLevel = config("2 - Socket System", "Drop Chance Increase per World Level", 0, new ConfigDescription("If you have Creature Level & Loot Control installed, you can use this setting to increase the drop chance of Unique Gems per World Level. Additive, not multiplicative.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropOnePerPlayer = config("2 - Socket System", "Drop one Gem per Player", Toggle.On, new ConfigDescription("If bosses should drop one Unique Gem per player. Has no effect, if the drop system is not set to custom.", null, new ConfigurationManagerAttributes { Order = --order }));
		allowUnsocketing = config("2 - Socket System", "Gems can be removed from items", Unsocketing.All, new ConfigDescription("All: All gems can be removed from items.\nUnique Only: Only unique gems can be removed from items.\nDisabled: No gems can be removed from items.\nDoes not affect gems without an effect.", null, new ConfigurationManagerAttributes { Order = --order }));
		gemReturnLockedGems = config("2 - Socket System", "Return locked gems", Toggle.Off, new ConfigDescription("If on, locked gems will be returned to the player, if the item breaks while adding a socket to it.", null, new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketSimple = config("2 - Socket System", "Simple Gem Break Chance", 0f, new ConfigDescription("Chance to break a simple gem when trying to remove it from a socket. Does not affect gems without an effect.", new AcceptableValueRange<float>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketAdvanced = config("2 - Socket System", "Advanced Gem Break Chance", 0f, new ConfigDescription("Chance to break an advanced gem when trying to remove it from a socket. Does not affect gems without an effect.", new AcceptableValueRange<float>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketPerfect = config("2 - Socket System", "Perfect Gem Break Chance", 0f, new ConfigDescription("Chance to break a perfect gem when trying to remove it from a socket. Does not affect gems without an effect.", new AcceptableValueRange<float>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketMerged = config("2 - Socket System", "Merged Gem Break Chance", 0f, new ConfigDescription("Chance to break a merged gem when trying to remove it from a socket. Does not affect gems without an effect.", new AcceptableValueRange<float>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		resourceReturnRate = config("2 - Socket System", "Percentage Recovered", 50, new ConfigDescription("Percentage of items to be recovered, when an item breaks while trying to add a socket to it. This only considers the base cost of the item.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		resourceReturnRateUpgrade = config("2 - Socket System", "Percentage Recovered Upgrades", 100, new ConfigDescription("Percentage of items to be recovered, when an item breaks while trying to add a socket to it. This only considers the upgrade cost of the item.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		resourceReturnRateDistance = config("2 - Socket System", "Maximum Distance for Item Recovery", 0, new ConfigDescription("Maximum distance between the position where the item has been crafted and the position where the item has been destroyed to recover non-teleportable resources. This can be used, to prevent people from crafting items from metal, taking them through a portal and destroying them on the other side, to teleport the metal. Setting this to 0 disables this.", null, new ConfigurationManagerAttributes { Order = --order }));
		maximumNumberSockets = config("2 - Socket System", "Maximum number of Sockets", 3, new ConfigDescription("Maximum number of sockets on each item.", new AcceptableValueRange<int>(1, maxNumberOfSockets), new ConfigurationManagerAttributes { Order = --order }));
		maximumNumberSockets.SettingChanged += (_, _) =>
		{
			SocketsBackground.CalculateColors();
			JewelrySetup.SetPurpleRingSockets();
		};
		limitSocketsByTableLevel = config("2 - Socket System", "Limit number of Sockets", Toggle.Off, new ConfigDescription("If on, the number of sockets that can be added to an item by a player is limited by the level of the Gemcutters Table.", null, new ConfigurationManagerAttributes { Order = --order }));
		for (int i = 0; i < maxSocketsTableLevel.Length; ++i)
		{
			maxSocketsTableLevel[i] = config("2 - Socket System", $"Socket limit table level {i + 1}", i + 1, new ConfigDescription($"Sets the maximum number of sockets that can be added to an item at a level {i + 1} Gemcutters Table, if Limit number of Sockets is on.", new AcceptableValueRange<int>(1, maxNumberOfSockets), new ConfigurationManagerAttributes { Order = --order }));
		}
		gemRespawnRate = config("2 - Socket System", "Gemstone Respawn Time", 100, new ConfigDescription("Respawn time for raw gemstones in ingame days. Use 0 to disable respawn.", null, new ConfigurationManagerAttributes { Order = --order }));
		socketingItemsExperience = config("2 - Socket System", "Adding Sockets grants Experience", Toggle.On, new ConfigDescription("If off, adding sockets to items does not grant Jewelcrafting experience anymore. This can be used, to prevent people from crafting cheap items and socketing them, to level up the skill.", null, new ConfigurationManagerAttributes { Order = --order }));
		socketBlacklist = config("6 - Other", "Socketing Blacklist", "", new ConfigDescription("Comma separated list of prefabs that cannot be socketed.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxDropRate[0] = config("3 - Fusion Box", "Drop rate for Fusion Box", 200, new ConfigDescription("Drop rate for the Common Crystal Fusion Box. Format is 1:x. The chance is further increased by creature health. Use 0 to disable the drop.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxDropRate[1] = config("3 - Fusion Box", "Drop rate for Blessed Fusion Box", 500, new ConfigDescription("Drop rate for the Blessed Crystal Fusion Box. Format is 1:x. The chance is further increased by creature health. Use 0 to disable the drop.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxDropRate[2] = config("3 - Fusion Box", "Drop rate for Celestial Fusion Box", 1000, new ConfigDescription("Drop rate for the Celestial Crystal Fusion Box. Format is 1:x. The chance is further increased by creature health. Use 0 to disable the drop.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxMergeActivityProgress[0] = config("3 - Fusion Box", "Activity reward for Fusion Box", 3f, new ConfigDescription("Progress for the Common Crystal Fusion Box per minute of activity.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxMergeActivityProgress[1] = config("3 - Fusion Box", "Activity reward for Blessed Fusion Box", 2f, new ConfigDescription("Progress for the Blessed Crystal Fusion Box per minute of activity", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxMergeActivityProgress[2] = config("3 - Fusion Box", "Activity reward for Celestial Fusion Box", 1f, new ConfigDescription("Progress for the Celestial Crystal Fusion Box per minute of activity.", null, new ConfigurationManagerAttributes { Order = --order }));
		gachaLocationActive = config("4 - World Boss", "Gacha Location", Toggle.On, new ConfigDescription("Can be used to disable the mystical gemstone location. Be aware that this means you can no longer turn in your gacha coins for rewards.", null, new ConfigurationManagerAttributes { Order = --order }));
		gachaLocationActive.SettingChanged += (_, _) =>
		{
			foreach (GachaSetup.LocationHider hider in FindObjectsOfType<GachaSetup.LocationHider>(true))
			{
				hider.gameObject.SetActive(gachaLocationActive.Value == Toggle.On);
			}
		};
		gachaLocationIcon = config("4 - World Boss", "Location Icon", Toggle.On, new ConfigDescription("Display the map icon of the mystical gemstone.", null, new ConfigurationManagerAttributes { Order = --order }));
		gachaLocationIcon.SettingChanged += (_, _) => ZoneSystem.instance.GetLocation("JC_Gacha_Location").m_iconPlaced = gachaLocationIcon.Value == Toggle.On;
		bossSpawnTimer = config("4 - World Boss", "Time between Boss Spawns", 120, new ConfigDescription("Time in minutes between boss spawns. Set this to 0, to disable boss spawns.", null, new ConfigurationManagerAttributes { Order = --order }));
		bossSpawnTimer.SettingChanged += (_, _) => BossSpawn.UpdateBossTimerVisibility();
		eventBossSpawnChance = config("4 - World Boss", "Event Boss Spawn Chance", 10f, new ConfigDescription("Chance for the Frozen Triumvirate event boss to spawn. Set this to 0, to disable the spawn.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = --order }));
		bossSpawnMinDistance = config("4 - World Boss", "Minimum Distance Boss Spawns", 1000, new ConfigDescription("Minimum distance from the center of the map for boss spawns.", null, new ConfigurationManagerAttributes { Order = --order }));
		bossSpawnMaxDistance = config("4 - World Boss", "Maximum Distance Boss Spawns", 10000, new ConfigDescription("Maximum distance from the center of the map for boss spawns.", null, new ConfigurationManagerAttributes { Order = --order }));
		bossSpawnBaseDistance = config("4 - World Boss", "Base Distance Boss Spawns", 50, new ConfigDescription("Minimum distance to player build structures for boss spawns.", null, new ConfigurationManagerAttributes { Order = --order }));
		bossTimeLimit = config("4 - World Boss", "Time Limit", 60, new ConfigDescription("Time in minutes before world bosses despawn.", null, new ConfigurationManagerAttributes { Order = --order }));
		bossCoinDrop = config("4 - World Boss", "Coins per Boss Kill", 5, new ConfigDescription("Number of Celestial Coins dropped by bosses per player.", new AcceptableValueRange<int>(0, 20), new ConfigurationManagerAttributes { Order = --order }));
		worldBossBalance = config("4 - World Boss", "Balance", GachaSetup.BalanceToggle.Ashlands, new ConfigDescription("Balancing to use for the world bosses.", null, new ConfigurationManagerAttributes { Order = --order }));
		List<ConfigurationManagerAttributes> worldBossCustomAttributes = new();
		worldBossBalance.SettingChanged += (o, e) =>
		{
			foreach (ConfigurationManagerAttributes attributes in worldBossCustomAttributes)
			{
				attributes.Browsable = worldBossBalance.Value == GachaSetup.BalanceToggle.Custom;
			}
			reloadConfigDisplay();
			WorldBossCustomChanged(o, e);
		};
		worldBossHealth = config("4 - World Boss", "Boss Health", BossSetup.ashlandsConfigs.health, new ConfigDescription("Balancing to use for the world bosses.", null, WorldBossAttribute()));
		worldBossHealth.SettingChanged += WorldBossCustomChanged;
		worldBossPunchDamage = config("4 - World Boss", "Punch Damage", BossSetup.ashlandsConfigs.punchBlunt, new ConfigDescription("Basic attack damage dealt by world bosses.", null, WorldBossAttribute()));
		worldBossPunchDamage.SettingChanged += WorldBossCustomChanged;
		worldBossSmashDamage = config("4 - World Boss", "Smash Damage", BossSetup.ashlandsConfigs.smashBlunt, new ConfigDescription("Smash attack damage dealt by world bosses.", null, WorldBossAttribute()));
		worldBossSmashDamage.SettingChanged += WorldBossCustomChanged;
		worldBossFireDamage = config("4 - World Boss", "Fire Damage", BossSetup.ashlandsConfigs.aoeFire, new ConfigDescription("Fire damage dealt by world bosses.", null, WorldBossAttribute()));
		worldBossFireDamage.SettingChanged += WorldBossCustomChanged;
		worldBossFrostDamage = config("4 - World Boss", "Frost Damage", BossSetup.ashlandsConfigs.aoeFrost, new ConfigDescription("Frost damage dealt by world bosses.", null, WorldBossAttribute()));
		worldBossFrostDamage.SettingChanged += WorldBossCustomChanged;
		worldBossPoisonDamage = config("4 - World Boss", "Poison Damage", BossSetup.ashlandsConfigs.aoePoison, new ConfigDescription("Poison damage dealt by world bosses.", null, WorldBossAttribute()));
		worldBossPoisonDamage.SettingChanged += WorldBossCustomChanged;
		worldBossBonusWeaponDamage = config("4 - World Boss", "Celestial Weapon Bonus Damage", 10, new ConfigDescription("Bonus damage taken by world bosses when hit with a celestial weapon.", null, new ConfigurationManagerAttributes { Order = --order }));
		worldBossBonusBlockPower = config("4 - World Boss", "Celestial Shield Bonus Power", 10, new ConfigDescription("Additional damage blocked by a celestial shield when hit by a world boss..", null, new ConfigurationManagerAttributes { Order = --order }));
		worldBossCountdownDisplayOffset = config("4 - World Boss", "Countdown Display Offset", 0, new ConfigDescription("Offset for the world boss countdown display on the world map. Increase this, to move the display down, to prevent overlapping with other mods.", null, new ConfigurationManagerAttributes { Order = --order }), false);
		worldBossCountdownDisplayOffset.SettingChanged += (_, _) => BossSpawn.UpdateBossTimerPosition();
		worldBossExploitProtectionHeal = config("4 - World Boss", "Range Check Healing", Toggle.Off, new ConfigDescription("If on and no player has been in melee range of the world boss for more than 10 seconds, it will start to heal.", null, new ConfigurationManagerAttributes { Order = --order }));
		worldBossExploitProtectionRangedShield = config("4 - World Boss", "Ranged Shield", Toggle.Off, new ConfigDescription("If on each consecutive ranged attack against the world boss will cause it to take 10% less damage, up to 90% reduced damage taken. Melee attacks remove one stack.", null, new ConfigurationManagerAttributes { Order = --order }));
		defaultEventDuration = config("4 - World Boss", "Default Event Duration", 1f, new ConfigDescription("Default duration for each event in days.", null, new ConfigurationManagerAttributes { Order = --order }));
		lootSystem = config("5 - Loot System", "Loot System", LootSystem.GemDrops, new ConfigDescription("Loot system to be used for Jewelcrafting.\nGem Drops: Creatures have a chance to drop uncut gems.\nEquipment Drops: Creatures have a chance to drop Equipment items with sockets and gems.\nGem Chests: Creatures have a chance to drop a chest with some gems, so you can choose one.\nEquipment Chests: Creatures have a chance to drop a chest with some equipment, so you can choose one piece.", null, new ConfigurationManagerAttributes { Order = --order }));
		lootBeams = config("5 - Loot System", "Loot Beams", Toggle.On, new ConfigDescription("Loot beams for dropped items.", null, new ConfigurationManagerAttributes { Order = --order }), false);
		lootBeams.SettingChanged += (_, _) =>
		{
			foreach (ItemDrop item in ItemDrop.s_instances)
			{
				if (lootBeams.Value == Toggle.On)
				{
					LootSystemSetup.AddLootBeam.Postfix(item);
				}
				else if (item.transform.Find(LootSystemSetup.lootBeam.name + "(Clone)") is { } beam)
				{
					Destroy(beam);
				}
			}
		};
		foreach (LootSystem system in (LootSystem[])Enum.GetValues(typeof(LootSystem)))
		{
			lootConfigs[system] = new LootConfigs
			{
				lootSkew = config("5 - Loot System", $"{system} - Drop Skew", 20f, new ConfigDescription("Can be used to skew the worth of loot that is dropped.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = --order })),
				lootDefaultChance = config("5 - Loot System", $"{system} - Drop Chance", 20f, new ConfigDescription("Chance for loot to be dropped.", new AcceptableValueRange<float>(0, 100), new ConfigurationManagerAttributes { Order = --order })),
				lootLowHpChance = config("5 - Loot System", $"{system} - Drop Low HP Chance", 7f, new ConfigDescription("Chance for loot to be dropped from low HP creatures.", new AcceptableValueRange<float>(0, 100), new ConfigurationManagerAttributes { Order = --order })),
				lootRestriction = config("5 - Loot System", $"{system} - Loot Restriction", LootRestriction.KnownRecipe, new ConfigDescription("None: No restrictions for item drops.\nKnown Station: You can only drop items that can be crafted on crafting stations that you know.\nKnown Recipe: You can only drop items that you know the recipe of.", null, new ConfigurationManagerAttributes { Order = --order })),
			};
			if ((system & (LootSystem.EquipmentDrops | LootSystem.EquipmentChests)) != 0)
			{
				lootConfigs[system].unsocketDroppedItems = config("5 - Loot System", $"{system} - Unsocket Dropped Items", Toggle.Off, new ConfigDescription("If on, gems can be removed from dropped equipment items.", null, new ConfigurationManagerAttributes { Order = --order }));
				lootConfigs[system].addSocketToDroppedItem = config("5 - Loot System", $"{system} - Socket Dropped Items", Toggle.Off, new ConfigDescription("If on, new sockets can be added to dropped equipment items.", null, new ConfigurationManagerAttributes { Order = --order }));
			}
		}
		gemChestMinGems = config("5 - Loot System", "Minimum Gems Gem Chest", 2, new ConfigDescription("Minimum amount of gems inside a dropped gem chest. Needs to be less than the maximum.", new AcceptableValueRange<int>(2, 8), new ConfigurationManagerAttributes { Order = --order }));
		gemChestMinGems.SettingChanged += (_, _) =>
		{
			if (gemChestMinGems.Value > gemChestMaxGems.Value)
			{
				gemChestMinGems.Value = gemChestMaxGems.Value;
			}
		};
		gemChestMaxGems = config("5 - Loot System", "Maximum Gems Gem Chest", 3, new ConfigDescription("Maximum amount of gems inside a dropped gem chest. Needs to be more than the minimum.", new AcceptableValueRange<int>(2, 8), new ConfigurationManagerAttributes { Order = --order }));
		gemChestMaxGems.SettingChanged += (_, _) =>
		{
			if (gemChestMaxGems.Value < gemChestMinGems.Value)
			{
				gemChestMaxGems.Value = gemChestMinGems.Value;
			}
		};
		gemChestAllowedAmount = config("5 - Loot System", "Allowed Gems Gem Chest", 1, new ConfigDescription("Sets how many gems players may take from a single gem chest.", new AcceptableValueRange<int>(1, 8), new ConfigurationManagerAttributes { Order = --order }));
		equipmentChestMinItems = config("5 - Loot System", "Minimum Items Equipment Chest", 2, new ConfigDescription("Minimum amount of items inside a dropped equipment chest. Needs to be less than the maximum.", new AcceptableValueRange<int>(2, 8), new ConfigurationManagerAttributes { Order = --order }));
		equipmentChestMinItems.SettingChanged += (_, _) =>
		{
			if (equipmentChestMinItems.Value > equipmentChestMaxItems.Value)
			{
				equipmentChestMinItems.Value = equipmentChestMaxItems.Value;
			}
		};
		equipmentChestMaxItems = config("5 - Loot System", "Maximum Items Equipment Chest", 3, new ConfigDescription("Maximum amount of items inside a dropped equipment chest. Needs to be more than the minimum.", new AcceptableValueRange<int>(2, 8), new ConfigurationManagerAttributes { Order = --order }));
		equipmentChestMaxItems.SettingChanged += (_, _) =>
		{
			if (equipmentChestMaxItems.Value < equipmentChestMinItems.Value)
			{
				equipmentChestMaxItems.Value = equipmentChestMinItems.Value;
			}
		};
		equipmentChestAllowedAmount = config("5 - Loot System", "Allowed Items Equipment Chest", 1, new ConfigDescription("Sets how many items players may take from a single equipment chest.", new AcceptableValueRange<int>(1, 8), new ConfigurationManagerAttributes { Order = --order }));
		upgradeChanceIncrease = config("6 - Other", "Success Chance Increase", 15, new ConfigDescription("Success chance increase at jewelcrafting skill level 100.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		additiveSkillBonus = config("6 - Other", "Additive Skill Bonus", Toggle.Off, new ConfigDescription("If on, the skill bonus from Jewelcrafting is additive instead of multiplicative. Not recommended, since it makes the skill insanely strong.", null, new ConfigurationManagerAttributes { Order = --order }));
		experienceGainedFactor = config("6 - Other", "Skill Experience Gain Factor", 1f, new ConfigDescription("Factor for experience gained for the jewelcrafting skill.", new AcceptableValueRange<float>(0.01f, 5f), new ConfigurationManagerAttributes { Order = --order }));
		experienceGainedFactor.SettingChanged += (_, _) => jewelcrafting.SkillGainFactor = experienceGainedFactor.Value;
		jewelcrafting.SkillGainFactor = experienceGainedFactor.Value;
		experienceLoss = config("6 - Other", "Skill Experience Loss", 0, new ConfigDescription("How much experience to lose in the jewelcrafting skill on death.", new AcceptableValueRange<int>(0, 100)));
		experienceLoss.SettingChanged += (_, _) => jewelcrafting.SkillLoss = experienceLoss.Value;
		jewelcrafting.SkillLoss = experienceLoss.Value;
		gemBagSlotsRows = config("6 - Other", "Jewelers Bag Slot Rows", 2, new ConfigDescription("Rows in a Jewelers Bag. Changing this value does not affect existing bags.", new AcceptableValueRange<int>(1, 4), new ConfigurationManagerAttributes { Order = --order }));
		gemBagSlotsRows.SettingChanged += (_, _) => MiscSetup.UpdateGemBagSize();
		gemBagSlotsColumns = config("6 - Other", "Jewelers Bag Columns", 8, new ConfigDescription("Columns in a Jewelers Bag. Changing this value does not affect existing bags.", new AcceptableValueRange<int>(1, 8), new ConfigurationManagerAttributes { Order = --order }));
		gemBagSlotsColumns.SettingChanged += (_, _) => MiscSetup.UpdateGemBagSize();
		gemBagAutofill = config("6 - Other", "Jewelers Bag Autofill", Toggle.On, new ConfigDescription("If set to on, gems will be added into a Jewelers Bag automatically on pickup.", null, new ConfigurationManagerAttributes { Order = --order }), false);
		gemBoxSlotsRows = config("6 - Other", "Jewelers Box Slot Rows", 2, new ConfigDescription("Rows in a Jewelers Box. Changing this value does not affect existing boxes.", new AcceptableValueRange<int>(1, 4), new ConfigurationManagerAttributes { Order = --order }));
		gemBoxSlotsRows.SettingChanged += (_, _) =>
		{
			MiscSetup.jewelryBag.ReadInventory().m_height = gemBoxSlotsRows.Value;
			MiscSetup.jewelryBag.Save();
		};
		gemBoxSlotsColumns = config("6 - Other", "Jewelers Box Columns", 2, new ConfigDescription("Columns in a Jewelers Box. Changing this value does not affect existing boxes.", new AcceptableValueRange<int>(1, 8), new ConfigurationManagerAttributes { Order = --order }));
		gemBoxSlotsColumns.SettingChanged += (_, _) =>
		{
			MiscSetup.jewelryBag.ReadInventory().m_width = gemBoxSlotsColumns.Value;
			MiscSetup.jewelryBag.Save();
		};
		frameOfChanceChance = config("6 - Other", "Frame of Chance chance", 50, new ConfigDescription("Chance to add a socket instead of losing one when applying equipment to a frame of chance.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		mirrorBlacklist = config("6 - Other", "Celestial Mirror Blacklist", "", new ConfigDescription("Comma separated list of prefabs that cannot be duplicated with a celestial mirror.", null, new ConfigurationManagerAttributes { Order = --order }));
		mirrorMirrorImages = config("6 - Other", "Mirror Mirror Images", Toggle.Off, new ConfigDescription("If on, mirror images can be mirrored again.", null, new ConfigurationManagerAttributes { Order = --order }));
		unsocketMirrorImages = config("6 - Other", "Unsocket Mirror Images", Toggle.Off, new ConfigDescription("If on, gems can be removed from mirror images.", null, new ConfigurationManagerAttributes { Order = --order }));
		advancedTooltipKey = config("6 - Other", "Advanced Tooltip Key", new KeyboardShortcut(KeyCode.LeftAlt), new ConfigDescription("Key to hold while hovering an item with sockets, to display the advanced tooltip.", null, new ConfigurationManagerAttributes { Order = --order }), false);
		advancedTooltipMode = config("6 - Other", "Advanced Tooltip Details", AdvancedTooltipMode.General, new ConfigDescription("How detailed the advanced tooltip should be.", null, new ConfigurationManagerAttributes { Order = --order }), false);
		advancedTooltipAlwaysOn = config("6 - Other", "Always Display Advanced Tooltip", Toggle.Off, new ConfigDescription("If on, the advanced tooltip is always displayed, instead of the name of the effect.", null, new ConfigurationManagerAttributes { Order = --order }), false);
		gemstoneFormationParticles = config("6 - Other", "Gemstone Formation Particles", Toggle.On, new ConfigDescription("You can use this to disable the particles around gemstone formations in the world. If you think this will improve your performance, you will be disappointed. Let me tell you a thing or two about performance. If you are one of those people that still look at their instance count and worry about it being too high, please stop doing that. There are 'good' instances and 'bad' instances. You can have a million good instances and still get 100 FPS and you can have a single bad instance and your FPS drop to 5. So, if you want to improve your performance, get rid of the bad instances in your base. Creatures? Bad instances. Crops? Bad instances. Building pieces? Okayish instances. Gemstone formation particles? Good instances.\n\nIf you need an indicator for the performance of an area, how about looking at your FPS, instead of some arbitrary instance count?", null, new ConfigurationManagerAttributes { Order = --order }), false);
		gemstoneFormationParticles.SettingChanged += (_, _) =>
		{
			foreach (GameObject destructible in DestructibleSetup.destructibles.Values)
			{
				SetActive(destructible);
			}
			foreach (DestructibleSetup.GemSpawner spawner in DestructibleSetup.GemSpawner.activeSpawners)
			{
				ZDOID gemId = spawner.netView.GetZDO().GetZDOID("spawn gem");
				if (gemId != ZDOID.None && ZNetScene.instance.FindInstance(gemId) is { } existingDestructible)
				{
					SetActive(existingDestructible);
				}
			}

			return;

			void SetActive(GameObject destructible)
			{
				if (destructible.transform.Find("Orbs") is { } orbs)
				{
					orbs.gameObject.SetActive(gemstoneFormationParticles.Value == Toggle.On);
				}
			}
		};
		wisplightGem = config("6 - Other", "Wisplight Gem", Toggle.On, new ConfigDescription("If on, the Wisplight is a gem for utility items, instead of a utility item itself.", null, new ConfigurationManagerAttributes { Order = --order }));
		wisplightGem.SettingChanged += (_, _) =>
		{
			GemStones.ApplyWisplightGem();
			ConfigLoader.TryReapplyConfig();
			if (wisplightGem.Value == Toggle.Off)
			{
				Player.m_localPlayer?.m_seman?.RemoveStatusEffect("Demister".GetStableHashCode());
			}
			if (ObjectDB.instance)
			{
				if (Player.m_localPlayer is { } player && player && player.m_utilityItem?.m_shared.m_name == "$item_demister")
				{
					player.UnequipItem(player.m_utilityItem);
				}
				Inventory[] inventories = Player.s_players.Select(p => p.GetInventory()).Concat(FindObjectsOfType<Container>().Select(c => c.GetInventory())).Where(c => c is not null).ToArray();
				foreach (ItemDrop.ItemData itemdata in ObjectDB.instance.m_items.Select(p => p.GetComponent<ItemDrop>()).Where(c => c && c.GetComponent<ZNetView>()).Concat(ItemDrop.s_instances).Select(i => i.m_itemData).Concat(inventories.SelectMany(i => i.GetAllItems())))
				{
					if (itemdata.m_shared.m_name == "$item_demister")
					{
						itemdata.m_shared.m_itemType = wisplightGem.Value == Toggle.On ? ItemDrop.ItemData.ItemType.Material : ItemDrop.ItemData.ItemType.Utility;
					}
				}
			}
			if (Player.m_localPlayer)
			{
				API.InvokeEffectRecalc();
			}
		};
		wishboneGem = config("6 - Other", "Wishbone Gem", Toggle.On, new ConfigDescription("If on, the Wishbone is a gem for utility items, instead of a utility item itself.", null, new ConfigurationManagerAttributes { Order = --order }));
		wishboneGem.SettingChanged += (_, _) =>
		{
			GemStones.ApplyWishboneGem();
			ConfigLoader.TryReapplyConfig();
			if (wishboneGem.Value == Toggle.Off)
			{
				Player.m_localPlayer?.m_seman?.RemoveStatusEffect("Wishbone".GetStableHashCode());
			}
			if (ObjectDB.instance)
			{
				if (Player.m_localPlayer is { } player && player && player.m_utilityItem.m_shared.m_name == "$item_wishbone")
				{
					player.UnequipItem(player.m_utilityItem);
				}
				Inventory[] inventories = Player.s_players.Select(p => p.GetInventory()).Concat(FindObjectsOfType<Container>().Select(c => c.GetInventory())).Where(c => c is not null).ToArray();
				foreach (ItemDrop.ItemData itemdata in ObjectDB.instance.m_items.Select(p => p.GetComponent<ItemDrop>()).Where(c => c && c.GetComponent<ZNetView>()).Concat(ItemDrop.s_instances).Select(i => i.m_itemData).Concat(inventories.SelectMany(i => i.GetAllItems())))
				{
					if (itemdata.m_shared.m_name == "$item_wishbone")
					{
						itemdata.m_shared.m_itemType = wishboneGem.Value == Toggle.On ? ItemDrop.ItemData.ItemType.Material : ItemDrop.ItemData.ItemType.Utility;
					}
				}
			}
			if (Player.m_localPlayer)
			{
				API.InvokeEffectRecalc();
			}
		};
		gemstoneFormations = config("6 - Other", "Gemstone Formations", Toggle.On, new ConfigDescription("Can be used to disable the gemstone formations in the world.", null, new ConfigurationManagerAttributes { Order = --order }));
		bigGemstoneFormationChance = config("6 - Other", "Giant Gemstone Formation Chance", 3f, new ConfigDescription("Chance for giant gemstone formations to spawn.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = --order }));
		gemstoneFormationHealth = config("6 - Other", "Gemstone Formation Health", 20f, new ConfigDescription("Sets the health of the gemstone formations found in the world.", null, new ConfigurationManagerAttributes { Order = --order }));
		gemstoneFormationHealth.SettingChanged += (_, _) =>
		{
			foreach (GameObject destructible in DestructibleSetup.destructibles.Values)
			{
				if (DestructibleSetup.hpModifiableTypes.Contains(destructible.name))
				{
					destructible.GetComponent<Destructible>().m_health = gemstoneFormationHealth.Value;
				}
			}
			foreach (DestructibleSetup.GemSpawner spawner in DestructibleSetup.GemSpawner.activeSpawners)
			{
				if (spawner.netView.GetZDO()?.GetZDOID("spawn gem") is { } gemId && gemId != ZDOID.None && ZNetScene.instance.FindInstance(gemId) is { } existingDestructible && DestructibleSetup.hpModifiableTypes.Contains(global::Utils.GetPrefabName(existingDestructible)))
				{
					existingDestructible.GetComponent<Destructible>().m_health = (existingDestructible.GetComponent<DestructibleSetup.ScaledDestructible>()?.destructibleDrops ?? 1) * gemstoneFormationHealth.Value;
				}
			}
		};
		featherGliding = config("6 - Other", "Feather Fall", Toggle.On, new ConfigDescription("Can be used to disable Feather Fall on the Feather Cape and replace it with a buff for Gliding instead.", null, new ConfigurationManagerAttributes { Order = --order }));
		featherGliding.SettingChanged += ToggleFeatherFall;
		featherGlidingBuff = config("6 - Other", "Feather Fall Buff", 30, new ConfigDescription("Increases the duration of Gliding. Percentage. Only active, when Feather Fall is disabled.", null, new ConfigurationManagerAttributes { Order = --order }));
		asksvinRunning = config("6 - Other", "Wind Run", Toggle.On, new ConfigDescription("Can be used to disable Wind Run on the Asksvin Cloak and replace it with a buff for Windwalk instead.", null, new ConfigurationManagerAttributes { Order = --order }));
		asksvinRunning.SettingChanged += ToggleWindRun;
		asksvinRunningBuff = config("6 - Other", "Wind Run Buff", 30, new ConfigDescription("Increases the effect of Windwalk. Percentage. Only active, when Wind Run is disabled.", null, new ConfigurationManagerAttributes { Order = --order }));
		ringSlot = config("6 - Other", "Ring Slot", Toggle.On, new ConfigDescription("If on, the Jewelcrafting rings do not go into the utility slot, but get a special dedicated ring slot.", null, new ConfigurationManagerAttributes { Order = --order }));
		ringSlot.SettingChanged += (_, _) => Visual.HandleSettingChanged(ringSlot);
		Visual.HandleSettingChanged(ringSlot);
		necklaceSlot = config("6 - Other", "Necklace Slot", Toggle.On, new ConfigDescription("If on, the Jewelcrafting necklaces do not go into the utility slot, but get a special dedicated necklace slot.", null, new ConfigurationManagerAttributes { Order = --order }));
		necklaceSlot.SettingChanged += (_, _) => Visual.HandleSettingChanged(necklaceSlot);
		Visual.HandleSettingChanged(necklaceSlot);
		splitSockets = config("6 - Other", "Split Sockets", Toggle.On, new ConfigDescription("If on, the sockets in utility, necklace and ring slots are split, if multiple items are equipped.", null, new ConfigurationManagerAttributes { Order = --order }));
		splitSockets.SettingChanged += (_, _) =>
		{
			if (Player.m_localPlayer)
			{
				TrackEquipmentChanges.CalculateEffects(Player.m_localPlayer);
			}
		};
		pixelateTextures = config("6 - Other", "Pixelate Textures", Toggle.Off, new ConfigDescription("If on, all Jewelcrafting textures will be slightly pixelate. Some people think this makes it look more vanilla. Needs a reload of the objects visuals to take effect, e.g. by leaving the area or unequipping items.", null, new ConfigurationManagerAttributes { Order = --order }), false);
		divinityOrbDropChance = config("6 - Other", "Divinity Orb Drop Chance", 5, new ConfigDescription("Chance to obtain an Orb of Divinity from a treasure chest. Has no effect, if there are no gem effect powers that use value ranges.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		vanillaGemCrafting = config("6 - Other", "Vanilla Gem Crafting", Toggle.On, new ConfigDescription("If on, the recipes for crafting vanilla gems are enabled.", null, new ConfigurationManagerAttributes { Order = --order }));
		vanillaGemCrafting.SettingChanged += (_, _) =>
		{
			foreach (Recipe recipe in MiscSetup.vanillaGemCraftingRecipes)
			{
				recipe.m_enabled = vanillaGemCrafting.Value == Toggle.On;
			}
		};
		disableUniqueGemsInBase = config("6 - Other", "Unique gems in base", Toggle.On, new ConfigDescription("If off, unique gems cannot trigger while in whatever Valheim considers to be a player base.", null, new ConfigurationManagerAttributes { Order = --order }), false);
		glidingFallDamagePrevention = config("6 - Other", "Gliding Status Fall Damage Prevention", Toggle.On, new ConfigDescription("If on, the gliding status prevents fall damage. If off, only actually gliding prevents fall damage.", null, new ConfigurationManagerAttributes { Order = --order }));

		warmthStaminaRegen = config("Ruby Ring of Warmth", "Stamina Regen", 10, new ConfigDescription("Stamina regen increase for the Ruby Ring of Warmth effect.", new AcceptableValueRange<int>(0, 100)));
		awarenessRange = config("Ruby Necklace of Awareness", "Detection Range", 30, new ConfigDescription("Creature detection range for the Ruby Necklace of Awareness.", new AcceptableValueRange<int>(1, 50)));
		rigidDamageReduction = config("Sturdy Spinel Ring", "Damage Reduction", 5, new ConfigDescription("Damage reduction for the Sturdy Spinel Ring.", new AcceptableValueRange<int>(0, 100)));
		headhunterDuration = config("Emerald Headhunter Ring", "Effect Duration", 20U, new ConfigDescription("Effect duration for the Emerald Headhunter Ring."));
		headhunterDamage = config("Emerald Headhunter Ring", "Damage Increase", 30, new ConfigDescription("Damage increase for the Emerald Headhunter Ring effect.", new AcceptableValueRange<int>(0, 100)));
		magicRepairAmount = config("Emerald Necklace of Magic Repair", "Repair Amount", 5, new ConfigDescription("Durability restoration per minute for the Emerald Necklace of Magic Repair effect.", new AcceptableValueRange<int>(0, 100)));
		aquaticDamageIncrease = config("Aquatic Sapphire Necklace", "Damage Increase", 10, new ConfigDescription("Damage increase while wearing the Aquatic Sapphire Necklace and being wet.", new AcceptableValueRange<int>(0, 100)));
		modersBlessingDuration = config("Ring of Moders Sapphire Blessing", "Effect Duration", 15, new ConfigDescription("Effect duration in seconds for the Ring of Moder's Sapphire Blessing."));
		modersBlessingCooldown = config("Ring of Moders Sapphire Blessing", "Effect Cooldown", 60, new ConfigDescription("Effect cooldown in seconds for the Ring of Moder's Sapphire Blessing."));
		guidanceCooldown = config("Spinel Necklace of Guidance", "Effect Cooldown", 30, new ConfigDescription("Effect cooldown in seconds for the Spinel Necklace of Guidance."));
		legacyCooldown = config("Ring of Onyx Legacy", "Effect Cooldown", 30, new ConfigDescription("Effect cooldown in seconds for the Ring of Onyx Legacy."));

		GemEffectSetup.initializeGemEffect(assets);
		MiscSetup.initializeMisc(assets);
		BuildingPiecesSetup.initializeBuildingPieces(assets);
		GemStoneSetup.initializeGemStones(assets);
		DestructibleSetup.initializeDestructibles(assets);
		MergedGemStoneSetup.initializeMergedGemStones(assets);
		JewelrySetup.initializeJewelry(assets);
		VisualEffectSetup.initializeVisualEffects(assets);
		FusionBoxSetup.initializeFusionBoxes(assets);
		Synergy.initializeSynergy(assets);
		ConfigLoader.LoadBuiltinConfig();
		GachaSetup.initializeGacha(assets);
		BossSetup.initializeBosses(assets);
		LootSystemSetup.initializeLootSystem(assets);
		SocketsBackground.CalculateColors();

		WorldBossCustomChanged(null, null);

		foreach (GachaSetup.BalanceConfig balanceConfig in GachaSetup.celestialItemsConfigs.Values)
		{
			ConfigEntry<GachaSetup.BalanceToggle> toggle = config(english.Localize(balanceConfig.item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name), "Balance", GachaSetup.BalanceToggle.Ashlands, "");
			void Apply()
			{
				if (toggle.Value == GachaSetup.BalanceToggle.Custom)
				{
					balanceConfig.item.ToggleConfigurationVisibility(Configurability.Full);
				}
				else
				{
					balanceConfig.item.ToggleConfigurationVisibility(Configurability.Recipe | Configurability.Drop);
					ItemDrop.ItemData.SharedData sharedData = toggle.Value switch
					{
						GachaSetup.BalanceToggle.Ashlands => balanceConfig.ashlands,
						GachaSetup.BalanceToggle.Mistlands => balanceConfig.mistlands,
						GachaSetup.BalanceToggle.Plains => balanceConfig.plains,
						_ => throw new ArgumentOutOfRangeException(),
					};
					balanceConfig.item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared = Utils.Clone(sharedData);
					balanceConfig.item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack = Utils.Clone(sharedData.m_attack);

					if (ObjectDB.instance)
					{
						Inventory[] inventories = Player.s_players.Select(p => p.GetInventory()).Concat(FindObjectsOfType<Container>().Select(c => c.GetInventory())).Where(c => c is not null).ToArray();
						foreach (ItemDrop.ItemData itemdata in ObjectDB.instance.m_items.Select(p => p.GetComponent<ItemDrop>()).Where(c => c && c.GetComponent<ZNetView>()).Concat(ItemDrop.s_instances).Select(i => i.m_itemData).Concat(inventories.SelectMany(i => i.GetAllItems())))
						{
							if (itemdata.m_shared.m_name == sharedData.m_name)
							{
								itemdata.m_shared = Utils.Clone(sharedData);
								itemdata.m_shared.m_attack = Utils.Clone(sharedData.m_attack);
							}
						}
					}
				}
			}
			Apply();
			toggle.SettingChanged += (_, _) => Apply();
		}

		int socketAddingOrder = 0;
		for (int i = 0; i < maxNumberOfSockets; ++i)
		{
			socketAddingChances.Add(i, config("Socket Adding Chances", $"{i + 1}. Socket", i <= 5 ? 80 - i * 10 : 55 - i * 5, new ConfigDescription($"Success chance while trying to add the {i + 1}. Socket.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --socketAddingOrder })));
		}

		string[] boxMergeCategory = { "simple", "advanced", "perfect" };
		int boxMergeOrder = 0;
		foreach (KeyValuePair<string, int[]> kv in defaultBoxMergeChances)
		{
			boxMergeChances.Add(kv.Key, kv.Value.Select((chance, i) => config("3 - Fusion Box", $"Merge Chance {boxMergeCategory[i]} gems in {english.Localize(kv.Key)}", chance, new ConfigDescription($"Success chance while merging two {boxMergeCategory[i]} gems in a {Localization.instance.Localize(kv.Key)}", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --boxMergeOrder }))).ToArray());
			if (kv.Key != "$jc_legendary_gembox")
			{
				boxSelfMergeChances.Add(kv.Key, config("3 - Fusion Box", $"Merge Chance {english.Localize(kv.Key)}", 100, new ConfigDescription($"Success chance while merging two {Localization.instance.Localize(kv.Key)} in another {Localization.instance.Localize(kv.Key)}. 0 to disable merging of these gem boxes.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --boxMergeOrder })));
			}
		}
		boxBossGemMergeChance = config("3 - Fusion Box", $"Merge Chance boss gems in {english.Localize("$jc_legendary_gembox")}", 100, new ConfigDescription($"Success chance while merging two boss gems in a {Localization.instance.Localize("$jc_legendary_gembox")}", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --boxMergeOrder }));

		foreach (KeyValuePair<string, float[]> kv in defaultBoxBossProgress)
		{
			AddBossBoxProgressConfig(kv.Key, kv.Value);
		}

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);

		SocketsBackground.background = assets.LoadAsset<GameObject>("JC_ItemBackground");

		SetCfgValue(value => GemEffectSetup.rigidFinger.m_damageModifier = 1 - value / 100f, rigidDamageReduction);
		SetCfgValue(value => GemEffectSetup.headhunter.m_damageModifier = 1 + value / 100f, headhunterDamage);
		SetCfgValue(value => GemEffectSetup.headhunter.m_ttl = value, headhunterDuration);
		SetCfgValue(value => GemEffectSetup.aquatic.m_damageModifier = 1 + value / 100f, aquaticDamageIncrease);
		SetCfgValue(value => GemEffectSetup.warmth.m_staminaRegenMultiplier = 1 + value / 100f, warmthStaminaRegen);

		Necromancer.skeleton = PrefabManager.RegisterPrefab(assets, "JC_Skeleton");

		PrefabManager.RegisterPrefab(assets, "JC_Electric_Wings");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_5");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_6");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_7");
		PrefabManager.RegisterPrefab(assets, "JCGliding");
		PrefabManager.RegisterPrefab(assets, "JC_DarkWings");
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
		PrefabManager.RegisterPrefab(assets, "sfx_start_buff");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Purple");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Brown");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Green");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Blue");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Red");
		PrefabManager.RegisterPrefab(assets, "sfx_start_buff_2");
		PrefabManager.RegisterPrefab(assets, "VFX_Group_Loneliness");
		PrefabManager.RegisterPrefab(assets, "VFX_FriendLine_Render");
		PrefabManager.RegisterPrefab(assets, "VFX_Hearts_Start");
		PrefabManager.RegisterPrefab(assets, "sfx_reaper_offering");
		PrefabManager.RegisterPrefab(assets, "VFX_Crystal_Explosion_Red");
		PrefabManager.RegisterPrefab(assets, "VFX_Crystal_Explosion_Blue");
		PrefabManager.RegisterPrefab(assets, "VFX_Crystal_Explosion_Green");
		PrefabManager.RegisterPrefab(assets, "sfx_reaper_idle");
		PrefabManager.RegisterPrefab(assets, "vfx_reaper_hit");
		PrefabManager.RegisterPrefab(assets, "sfx_reaper_attack");
		PrefabManager.RegisterPrefab(assets, "sfx_reaper_death");
		PrefabManager.RegisterPrefab(assets, "VFX_Boss_Death");
		PrefabManager.RegisterPrefab(assets, "sfx_reaper_attack_hit");
		PrefabManager.RegisterPrefab(assets, "vfx_reaper_destroyed");
		PrefabManager.RegisterPrefab(assets, "sfx_reaper_hurt");
		PrefabManager.RegisterPrefab(assets, "sfx_reaper_rock_destroyed");
		PrefabManager.RegisterPrefab(assets, "JC_Boss_Projectile_Fire");
		PrefabManager.RegisterPrefab(assets, "JC_Boss_Projectile_Frost");
		PrefabManager.RegisterPrefab(assets, "JC_Boss_Projectile_Poison");
		PrefabManager.RegisterPrefab(assets, "sfx_reaper_alert");
		PrefabManager.RegisterPrefab(assets, "sfx_open_box");
		PrefabManager.RegisterPrefab(assets, "SFX_Reaper_Bow_Draw");
		PrefabManager.RegisterPrefab(assets, "SFX_Reaper_Bow_Fire");
		PrefabManager.RegisterPrefab(assets, "SFX_Reaper_Weapon_Blocked");
		PrefabManager.RegisterPrefab(assets, "SFX_Reaper_Weapon_Hit");
		PrefabManager.RegisterPrefab(assets, "SFX_Reaper_Weapon_Swing");
		PrefabManager.RegisterPrefab(assets, "VFX_Reaper_Bow_Fire");
		PrefabManager.RegisterPrefab(assets, "VFX_Reaper_Weapon_CamShake");
		PrefabManager.RegisterPrefab(assets, "VFX_Reaper_Weapon_Hit");
		PrefabManager.RegisterPrefab(assets, "SFX_Arrow_Explosion");
		PrefabManager.RegisterPrefab(assets, "vfx_reaper_water_surface");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Apotheosis");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Start_Black");
		PrefabManager.RegisterPrefab(assets, "JC_Marked_Effect");
		PrefabManager.RegisterPrefab(assets, "JC_Marked_Explode");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Fade");
		PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_Fade_End");
		PrefabManager.RegisterPrefab(assets, "JC_Reaper_Spear_Pro");
		PrefabManager.RegisterPrefab(assets, "JC_Purple_Neck_Coins");
		PrefabManager.RegisterPrefab(assets, "JC_Purple_Neck_Gems");
		PrefabManager.RegisterPrefab(assets, "JC_Staff_Lightning_Pro");
		PrefabManager.RegisterPrefab(assets, "FX_Lightning_Staff_Explosion");
		PrefabManager.RegisterPrefab(assets, "JC_Staff_Poison_Pro");
		PrefabManager.RegisterPrefab(assets, "FX_Poison_Staff_Explosion");
		PrefabManager.RegisterPrefab(assets, "vfx_jc_crossbow");
		PrefabManager.RegisterPrefab(assets, "JC_Black_Ring_Haldor");
		PrefabManager.RegisterPrefab(assets, "JC_Black_Ring_Hildir");
		PrefabManager.RegisterPrefab(assets, "JC_Black_Ring_Quest");
		PrefabManager.RegisterPrefab(assets, "vfx_asksvin_spawn");
		PrefabManager.RegisterPrefab(assets, "JC_Start_FX_Asksvin");

		Localizer.AddPlaceholder("jc_ring_red_description", "regen", warmthStaminaRegen);
		Localizer.AddPlaceholder("jc_se_ring_red_description", "regen", warmthStaminaRegen);
		Localizer.AddPlaceholder("jc_ring_purple_description", "power", rigidDamageReduction);
		Localizer.AddPlaceholder("jc_se_ring_purple_description", "power", rigidDamageReduction);
		Localizer.AddPlaceholder("jc_ring_green_description", "power", headhunterDamage);
		Localizer.AddPlaceholder("jc_ring_green_description", "duration", headhunterDuration);
		Localizer.AddPlaceholder("jc_se_ring_green_description", "power", headhunterDamage);
		Localizer.AddPlaceholder("jc_se_necklace_blue_description", "power", aquaticDamageIncrease);
		Localizer.AddPlaceholder("jc_ring_blue_description", "duration", modersBlessingDuration);
		Localizer.AddPlaceholder("jc_ring_blue_description", "cooldown", modersBlessingCooldown);
		Localizer.AddPlaceholder("jc_se_ring_blue_description", "duration", modersBlessingDuration);
		Localizer.AddPlaceholder("jc_se_ring_blue_description", "cooldown", modersBlessingCooldown);
		Localizer.AddPlaceholder("jc_reaper_sword_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_axe_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_bow_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_pickaxe_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_battleaxe_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_th_mace_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_th_sword_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_spear_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_knife_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_lightning_staff_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_poison_staff_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_crossbow_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_mace_description", "power", worldBossBonusWeaponDamage);
		Localizer.AddPlaceholder("jc_reaper_shield_description", "power", worldBossBonusBlockPower);
		Localizer.AddPlaceholder("jc_shield_buckler_description", "power", worldBossBonusBlockPower);
		Localizer.AddPlaceholder("jc_shield_tower_description", "power", worldBossBonusBlockPower);
		Localizer.AddPlaceholder("jc_blue_chest_description", "allowed", gemChestAllowedAmount);
		Localizer.AddPlaceholder("jc_purple_chest_description", "allowed", gemChestAllowedAmount);
		Localizer.AddPlaceholder("jc_orange_chest_description", "allowed", gemChestAllowedAmount);
		Localizer.AddPlaceholder("jc_blue_item_chest_description", "allowed", equipmentChestAllowedAmount);
		Localizer.AddPlaceholder("jc_purple_item_chest_description", "allowed", equipmentChestAllowedAmount);
		Localizer.AddPlaceholder("jc_orange_item_chest_description", "allowed", equipmentChestAllowedAmount);

		Config.SaveOnConfigSet = true;
		Config.Save();

		BossSpawn.SetupBossSpawn();

		return;

		ConfigurationManagerAttributes WorldBossAttribute()
		{
			ConfigurationManagerAttributes attributes = new() { Order = --order, Browsable = worldBossBalance.Value == GachaSetup.BalanceToggle.Custom };
			worldBossCustomAttributes.Add(attributes);
			return attributes;
		}

		void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
		{
			setter(config.Value);
			config.SettingChanged += (_, _) => setter(config.Value);
		}

		void WorldBossCustomChanged(object? o, EventArgs? e)
		{
			switch (worldBossBalance.Value)
			{
				case GachaSetup.BalanceToggle.Plains:
					BossSetup.ApplyBalanceConfig(BossSetup.plainsConfigs);
					break;
				case GachaSetup.BalanceToggle.Mistlands:
					BossSetup.ApplyBalanceConfig(BossSetup.mistlandsConfigs);
					break;
				case GachaSetup.BalanceToggle.Ashlands:
					BossSetup.ApplyBalanceConfig(BossSetup.ashlandsConfigs);
					break;
				case GachaSetup.BalanceToggle.Custom:
					BossSetup.ApplyBalanceConfig(new BossSetup.BalanceConfig
					{
						health = worldBossHealth.Value,
						punchBlunt = worldBossPunchDamage.Value,
						smashBlunt = worldBossSmashDamage.Value,
						aoeFire = worldBossFireDamage.Value,
						aoeFrost = worldBossFrostDamage.Value,
						aoePoison = worldBossPoisonDamage.Value,
					});
					break;
			}
		}
	}

	private static void ToggleFeatherFall(object sender, EventArgs e)
	{
		if (ObjectDB.instance && ObjectDB.instance.GetItemPrefab("CapeFeather") is { } cape && ObjectDB.instance.GetStatusEffect("SlowFall".GetStableHashCode()) is { } statusEffect)
		{
			StatusEffect? value = featherGliding.Value == Toggle.Off ? null : statusEffect;
			Item.ApplyToAllInstances(cape, data => data.m_shared.m_equipStatusEffect = value);

			if (Player.m_localPlayer is { } player && player.m_shoulderItem?.m_shared.m_name == "$item_cape_feather")
			{
				if (featherGliding.Value == Toggle.On)
				{
					player.GetSEMan().AddStatusEffect(statusEffect);
				}
				else
				{
					player.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
		}
	}
	
	private static void ToggleWindRun(object sender, EventArgs e)
	{
		if (ObjectDB.instance && ObjectDB.instance.GetItemPrefab("CapeAsksvin") is { } cape && ObjectDB.instance.GetStatusEffect("WindRun".GetStableHashCode()) is { } statusEffect)
		{
			StatusEffect? value = asksvinRunning.Value == Toggle.Off ? null : statusEffect;
			Item.ApplyToAllInstances(cape, data => data.m_shared.m_equipStatusEffect = value);

			if (Player.m_localPlayer is { } player && player.m_shoulderItem?.m_shared.m_name == "$item_cape_asksvin")
			{
				if (asksvinRunning.Value == Toggle.On)
				{
					player.GetSEMan().AddStatusEffect(statusEffect);
				}
				else
				{
					player.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
		}
	}

	private static void AddBossBoxProgressConfig(string name, float[] progress)
	{
		Regex regex = new("""['\["\]]""");

		boxBossProgress.Add(name, progress.Select((chance, i) => config("3 - Fusion Box", $"Boss Progress {regex.Replace(english.Localize(name), "")} {english.Localize(FusionBoxSetup.Boxes[i].GetComponent<ItemDrop>().m_itemData.m_shared.m_name)}", chance, new ConfigDescription($"Progress applied to {english.Localize(FusionBoxSetup.Boxes[i].GetComponent<ItemDrop>().m_itemData.m_shared.m_name)} when killing {regex.Replace(english.Localize(name), "")}", null, new ConfigurationManagerAttributes { Order = -boxBossProgress.Count * 3 - i - 1000 }))).ToArray());
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
					AddBossBoxProgressConfig(character.m_name, new[] { 0f, 0f, 0f });
				}
			}
		}
	}

	[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
	private static class FixupSocketTooltipFont
	{
		private static bool initialized = false;

		private static void Postfix(FejdStartup __instance)
		{
			if (initialized)
			{
				initialized = true;
				return;
			}

			if (__instance.transform.Find("StartGame/Panel/JoinPanel/serverCount")?.GetComponent<TextMeshProUGUI>() is { } vanilla)
			{
				foreach (TMP_Text tmpText in GemStoneSetup.SocketTooltip.GetComponentsInChildren<TMP_Text>(true))
				{
					tmpText.font = vanilla.font;
					tmpText.textWrappingMode = TextWrappingModes.Normal;
				}
			}

			english.SetupLanguage("English");
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
				peer.m_rpc.Register("Jewelcrafting SpawnBoss", _ => BossSpawn.SpawnBoss());
			}
		}
	}

	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class AddStatusEffects
	{
		private static void Prefix(ObjectDB __instance)
		{
			__instance.m_StatusEffects.Add(GemEffectSetup.headhunter);
			__instance.m_StatusEffects.Add(GemEffectSetup.fireBossDebuff);
			__instance.m_StatusEffects.Add(GemEffectSetup.frostBossDebuff);
			__instance.m_StatusEffects.Add(GemEffectSetup.poisonBossDebuff);
		}

		private static void Postfix()
		{
			ToggleFeatherFall(null!, null!);
			ToggleWindRun(null!, null!);
		}
	}

	[HarmonyPatch(typeof(ItemStand), nameof(ItemStand.CanAttach))]
	private static class MakeGemsAttachableToItemStand
	{
		private static bool CanAttachGem(ItemStand itemStand, ItemDrop.ItemData item)
		{
			return itemStand.m_supportedTypes.Contains(ItemDrop.ItemData.ItemType.Misc) && Utils.ItemAllowedInGemBag(item);
		}
		
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
		{
			List<CodeInstruction> instructions = instructionsEnumerable.ToList();
			instructions.InsertRange(instructions.Count - 1, new []
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MakeGemsAttachableToItemStand), nameof(CanAttachGem))),
				new CodeInstruction(OpCodes.Or),
			});
			return instructions;
		}
	}
}
