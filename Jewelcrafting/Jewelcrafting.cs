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
using Jewelcrafting.GemEffects.Groups;
using LocalizationManager;
using ServerSync;
using SkillManager;
using UnityEngine;

namespace Jewelcrafting;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInIncompatibility("randyknapp.mods.epicloot")]
[BepInIncompatibility("DasSauerkraut.Terraheim")]
[BepInDependency("randyknapp.mods.extendeditemdataframework")]
[BepInDependency("org.bepinex.plugins.groups", BepInDependency.DependencyFlags.SoftDependency)]
public partial class Jewelcrafting : BaseUnityPlugin
{
	public const string ModName = "Jewelcrafting";
	private const string ModVersion = "1.1.19";
	private const string ModGUID = "org.bepinex.plugins.jewelcrafting";

	public static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

	private static ConfigEntry<Toggle> serverConfigLocked = null!;
	public static SyncedConfigEntry<Toggle> useExternalYaml = null!;
	public static ConfigEntry<Toggle> socketSystem = null!;
	public static ConfigEntry<Toggle> inventorySocketing = null!;
	public static ConfigEntry<Toggle> displayGemcursor = null!;
	public static ConfigEntry<Toggle> displaySocketBackground = null!;
	public static ConfigEntry<Toggle> colorItemName = null!;
	public static ConfigEntry<Unsocketing> allowUnsocketing = null!;
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
	public static readonly ConfigEntry<int>[] crystalFusionBoxDropRate = new ConfigEntry<int>[FusionBoxSetup.Boxes.Length];
	public static readonly ConfigEntry<float>[] crystalFusionBoxMergeActivityProgress = new ConfigEntry<float>[FusionBoxSetup.Boxes.Length];
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
	public static ConfigEntry<int> modersBlessingDuration = null!;
	public static ConfigEntry<int> modersBlessingCooldown = null!;
	public static ConfigEntry<int> gemBagSlots = null!;
	public static ConfigEntry<Toggle> gemBagAutofill = null!;
	public static ConfigEntry<KeyboardShortcut> advancedTooltipKey = null!;

	public static readonly Dictionary<int, ConfigEntry<int>> socketAddingChances = new();
	public static readonly Dictionary<GameObject, ConfigEntry<int>> gemDropChances = new();
	public static readonly CustomSyncedValue<List<string>> socketEffectDefinitions = new(configSync, "socket effects", new List<string>());

	private readonly Dictionary<string, int[]> defaultBoxMergeChances = new()
	{
		{ "$jc_common_gembox", new[] { 75, 25, 0 } },
		{ "$jc_epic_gembox", new[] { 100, 50, 25 } },
		{ "$jc_legendary_gembox", new[] { 100, 75, 50 } }
	};

	private readonly Dictionary<string, float[]> defaultBoxBossProgress = new()
	{
		{ "$enemy_eikthyr", new[] { 3f, 0, 0 } },
		{ "$enemy_gdking", new[] { 4f, 0.5f, 0 } },
		{ "$enemy_bonemass", new[] { 5f, 1f, 0.3f } },
		{ "$enemy_dragon", new[] { 7f, 3f, 0.7f } },
		{ "$enemy_goblinking", new[] { 10f, 5f, 1.5f } }
	};

	public static readonly Dictionary<string, ConfigEntry<float>> gemUpgradeChances = new();
	public static readonly Dictionary<string, ConfigEntry<int>[]> boxMergeChances = new();
	public static readonly Dictionary<string, ConfigEntry<float>[]> boxBossProgress = new();
	public static ConfigEntry<int> boxBossGemMergeChance = null!;

	public static Dictionary<Effect, List<EffectDef>> SocketEffects = new();
	public static readonly Dictionary<int, Dictionary<GemLocation, List<EffectPower>>> EffectPowers = new();
	public static Dictionary<Heightmap.Biome, Dictionary<GemType, float>> GemDistribution = new();
	public static List<string> configFilePaths = null!;

	private static Skill jewelcrafting = null!;

	public static GameObject swordFall = null!;
	public static StatusEffect gliding = null!;
	public static StatusEffect glidingDark = null!;
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
	public static SE_Stats lightningStart = null!;
	public static SE_Stats rootStart = null!;
	public static SE_Stats poisonStart = null!;
	public static SE_Stats iceStart = null!;
	public static SE_Stats fireStart = null!;
	public static SE_Stats friendshipStart = null!;
	public static SE_Stats friendship = null!;
	public static SE_Stats loneliness = null!;
	public static GameObject friendshipTether = null!;
	public static SE_Stats cowardice = null!;

	private static Jewelcrafting self = null!;

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
		Off = 0
	}

	public enum UniqueDrop
	{
		Disabled = 0,
		TrulyUnique = 1,
		Custom = 2
	}

	public enum Unsocketing
	{
		Disabled = 0,
		UniquesOnly = 1,
		All = 2
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

	internal static Localization english = null!;

	public void Awake()
	{
		self = this;

		configFilePaths = new List<string> { Path.GetDirectoryName(Config.ConfigFilePath), Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) };

		Config.SaveOnConfigSet = false;

		Localizer.Load();
		english = new Localization();
		english.SetupLanguage("English");

		AssetBundle assets = PrefabManager.RegisterAssetBundle("jewelcrafting");
		AssetBundle compendiumAssets = PrefabManager.RegisterAssetBundle("jc_ui_additions");
		CompendiumDisplay.initializeCompendiumDisplay(compendiumAssets);

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
		displayGemcursor = config("2 - Socket System", "Display Gem Cursor", Toggle.On, "Changes the cursor to a gem, while interacting with the Gemcutters Table or socketing an item.", false);
		displaySocketBackground = config("2 - Socket System", "Display Socket Background", Toggle.On, "Changes the background of items that have sockets.", false);
		displaySocketBackground.SettingChanged += (_, _) => SocketsBackground.UpdateSocketBackground();
		colorItemName = config("2 - Socket System", "Color Item Names", Toggle.On, "Colors the name of items according to their socket levels.", false);
		useExternalYaml = configSync.AddConfigEntry(Config.Bind("2 - Socket System", "Use External YAML", Toggle.Off, new ConfigDescription("If set to on, the YAML file from your config folder will be used, to override gem effects configured inside of that file.", null, new ConfigurationManagerAttributes { Order = --order })));
		useExternalYaml.SourceConfig.SettingChanged += (_, _) => ConfigLoader.reloadConfigFile();
		badLuckRecipes = config("2 - Socket System", "Bad Luck Recipes", Toggle.On, new ConfigDescription("Enables or disables the bad luck recipes of all gems.", null, new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropSystem = config("2 - Socket System", "Drop System for Unique Gems", UniqueDrop.TrulyUnique, new ConfigDescription("Disabled: Unique Gems do not drop.\nTruly Unique: The first kill of each boss grants one Unique Gem.\nCustom: Lets you configure a drop chance and rate.", null, new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropChance = config("2 - Socket System", "Drop Chance for Unique Gems", 30, new ConfigDescription("Drop chance for Unique Gems. Has no effect, if the drop system is not set to custom.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		uniqueGemDropOnePerPlayer = config("2 - Socket System", "Drop one Gem per Player", Toggle.On, new ConfigDescription("If bosses should drop one Unique Gem per player. Has no effect, if the drop system is not set to custom.", null, new ConfigurationManagerAttributes { Order = --order }));
		allowUnsocketing = config("2 - Socket System", "Gems can be removed from items", Unsocketing.All, new ConfigDescription("All: All gems can be removed from items.\nUnique Only: Only unique gems can be removed from items.\nDisabled: No gems can be removed from items.\nDoes not affect gems without an effect.", null, new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketSimple = config("2 - Socket System", "Simple Gem Break Chance", 0, new ConfigDescription("Chance to break a simple gem when trying to remove it from a socket. Does not affect gems without an effect.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketAdvanced = config("2 - Socket System", "Advanced Gem Break Chance", 0, new ConfigDescription("Chance to break an advanced gem when trying to remove it from a socket. Does not affect gems without an effect.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketPerfect = config("2 - Socket System", "Perfect Gem Break Chance", 0, new ConfigDescription("Chance to break a perfect gem when trying to remove it from a socket. Does not affect gems without an effect.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		breakChanceUnsocketMerged = config("2 - Socket System", "Merged Gem Break Chance", 0, new ConfigDescription("Chance to break a merged gem when trying to remove it from a socket. Does not affect gems without an effect.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		resourceReturnRate = config("2 - Socket System", "Percentage Recovered", 0, new ConfigDescription("Percentage of items to be recovered, when an item breaks while trying to add a socket to it.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxDropRate[0] = config("3 - Fusion Box", "Drop rate for Fusion Box", 200, new ConfigDescription("Drop rate for the Common Crystal Fusion Box. Format is 1:x. The chance is further increased by creature health. Use 0 to disable the drop.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxDropRate[1] = config("3 - Fusion Box", "Drop rate for Blessed Fusion Box", 500, new ConfigDescription("Drop rate for the Blessed Crystal Fusion Box. Format is 1:x. The chance is further increased by creature health. Use 0 to disable the drop.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxDropRate[2] = config("3 - Fusion Box", "Drop rate for Celestial Fusion Box", 1000, new ConfigDescription("Drop rate for the Celestial Crystal Fusion Box. Format is 1:x. The chance is further increased by creature health. Use 0 to disable the drop.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxMergeActivityProgress[0] = config("3 - Fusion Box", "Activity reward for Fusion Box", 1.5f, new ConfigDescription("Progress for the Common Crystal Fusion Box per minute of activity.", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxMergeActivityProgress[1] = config("3 - Fusion Box", "Activity reward for Blessed Fusion Box", 0.7f, new ConfigDescription("Progress for the Blessed Crystal Fusion Box per minute of activity", null, new ConfigurationManagerAttributes { Order = --order }));
		crystalFusionBoxMergeActivityProgress[2] = config("3 - Fusion Box", "Activity reward for Celestial Fusion Box", 0.3f, new ConfigDescription("Progress for the Celestial Crystal Fusion Box per minute of activity.", null, new ConfigurationManagerAttributes { Order = --order }));
		maximumNumberSockets = config("2 - Socket System", "Maximum number of Sockets", 3, new ConfigDescription("Maximum number of sockets on each item.", new AcceptableValueRange<int>(1, 5), new ConfigurationManagerAttributes { Order = --order }));
		maximumNumberSockets.SettingChanged += (_, _) => SocketsBackground.CalculateColors();
		gemRespawnRate = config("2 - Socket System", "Gemstone Respawn Time", 100, new ConfigDescription("Respawn time for raw gemstones in ingame days. Use 0 to disable respawn.", null, new ConfigurationManagerAttributes { Order = --order }));
		upgradeChanceIncrease = config("4 - Other", "Success Chance Increase", 15, new ConfigDescription("Success chance increase at jewelcrafting skill level 100.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --order }));
		experienceGainedFactor = config("4 - Other", "Skill Experience Gain Factor", 1f, new ConfigDescription("Factor for experience gained for the jewelcrafting skill.", new AcceptableValueRange<float>(0.01f, 5f), new ConfigurationManagerAttributes { Order = --order }));
		experienceGainedFactor.SettingChanged += (_, _) => jewelcrafting.SkillGainFactor = experienceGainedFactor.Value;
		jewelcrafting.SkillGainFactor = experienceGainedFactor.Value;
		gemBagSlots = config("4 - Other", "Jewelers Bag Slots", 16, new ConfigDescription("Space in a Jewelers Bag. Changing this value does not affect existing bags.", new AcceptableValueRange<int>(4, 32), new ConfigurationManagerAttributes { Order = --order }));
		gemBagAutofill = config("4 - Other", "Jewelers Bag Autofill", Toggle.Off, new ConfigDescription("If set to on, gems will be added into a Jewelers Bag automatically on pickup.", null, new ConfigurationManagerAttributes { Order = --order }), false);
		advancedTooltipKey = config("4 - Other", "Advanced Tooltip Key", new KeyboardShortcut(KeyCode.LeftAlt), new ConfigDescription("Key to hold while hovering an item with sockets, to display the advanced tooltip.", null, new ConfigurationManagerAttributes { Order = --order }), false);

		awarenessRange = config("Ruby Necklace of Awareness", "Detection Range", 30, new ConfigDescription("Creature detection range for the Ruby Necklace of Awareness.", new AcceptableValueRange<int>(1, 50)));
		rigidDamageReduction = config("Sturdy Spinel Ring", "Damage Reduction", 5, new ConfigDescription("Damage reduction for the Sturdy Spinel Ring.", new AcceptableValueRange<int>(0, 100)));
		headhunterDuration = config("Emerald Headhunter Ring", "Effect Duration", 20U, new ConfigDescription("Effect duration for the Emerald Headhunter Ring."));
		headhunterDamage = config("Emerald Headhunter Ring", "Damage Increase", 30, new ConfigDescription("Damage increase for the Emerald Headhunter Ring effect.", new AcceptableValueRange<int>(0, 100)));
		magicRepairAmount = config("Emerald Necklace of Magic Repair", "Repair Amount", 5, new ConfigDescription("Durability restoration per minute for the Emerald Necklace of Magic Repair effect.", new AcceptableValueRange<int>(0, 100)));
		aquaticDamageIncrease = config("Aquatic Sapphire Necklace", "Damage Increase", 10, new ConfigDescription("Damage increase while wearing the Aquatic Sapphire Necklace and being wet.", new AcceptableValueRange<int>(0, 100)));
		modersBlessingDuration = config("Ring of Moders Sapphire Blessing", "Effect Duration", 15, new ConfigDescription("Effect duration in seconds for the Ring of Moder's Sapphire Blessing."));
		modersBlessingCooldown = config("Ring of Moders Sapphire Blessing", "Effect Cooldown", 60, new ConfigDescription("Effect cooldown in seconds for the Ring of Moder's Sapphire Blessing."));

		void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
		{
			setter(config.Value);
			config.SettingChanged += (_, _) => setter(config.Value);
		}

		MiscSetup.initializeMisc(assets);
		BuildingPiecesSetup.initializeBuildingPieces(assets);
		GemStoneSetup.initializeGemStones(assets);
		DestructibleSetup.initializeDestructibles(assets);
		JewelrySetup.initializeJewelry(assets);
		VisualEffectSetup.initializeVisualEffects(assets);
		MergedGemStoneSetup.initializeMergedGemStones(assets);
		FusionBoxSetup.initializeFusionBoxes(assets);
		ConfigLoader.LoadBuiltinConfig();
		SocketsBackground.CalculateColors();

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
		boxBossGemMergeChance = config("3 - Fusion Box", $"Merge Chance boss gems in {english.Localize("$jc_legendary_gembox")}", 100, new ConfigDescription($"Success chance while merging two boss gems in a {Localization.instance.Localize("$jc_legendary_gembox")}", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = --boxMergeOrder }));

		foreach (KeyValuePair<string, float[]> kv in defaultBoxBossProgress)
		{
			AddBossBoxProgressConfig(kv.Key, kv.Value);
		}

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);

		if (!ExtendedItemDataFramework.ExtendedItemDataFramework.Enabled)
		{
			throw new Exception("ExtendedItemDataFramework config is disabled. Fix it, then restart the game.");
		}

		swordFall = PrefabManager.RegisterPrefab(assets, "JC_Buff_FX_9");
		gliding = assets.LoadAsset<SE_Stats>("JCGliding");
		glidingDark = assets.LoadAsset<SE_Stats>("SE_DarkWings");
		glowingSpirit = assets.LoadAsset<SE_Stats>("SE_Crystal_Magelight");
		glowingSpiritPrefab = PrefabManager.RegisterPrefab(assets, "JC_Crystal_Magelight");
		glowingSpiritPrefab.AddComponent<GlowingSpirit.OrbDestroy>();
		lightningSpeed = Utils.ConvertStatusEffect<LightningSpeed.LightningSpeedEffect>(assets.LoadAsset<SE_Stats>("JC_Electric_Wings_SE"));
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
		lightningStart = assets.LoadAsset<SE_Stats>("SE_VFX_Start_Purple");
		rootStart = assets.LoadAsset<SE_Stats>("SE_VFX_Start_Brown");
		poisonStart = assets.LoadAsset<SE_Stats>("SE_VFX_Start_Green");
		iceStart = assets.LoadAsset<SE_Stats>("SE_VFX_Start_Blue");
		fireStart = assets.LoadAsset<SE_Stats>("SE_VFX_Start_Red");
		friendshipStart = assets.LoadAsset<SE_Stats>("SE_VFX_Start_Purple");
		friendship = Utils.ConvertStatusEffect<TogetherForever.TogetherForeverEffect>(assets.LoadAsset<SE_Stats>("SE_Friendship_Group"));
		loneliness = Utils.ConvertStatusEffect<TogetherForever.LonelinessEffect>(assets.LoadAsset<SE_Stats>("SE_Loneliness_Group"));
		friendshipTether = assets.LoadAsset<GameObject>("VFX_FriendLine_Render");
		friendshipTether.AddComponent<FriendshipTether>();
		cowardice = assets.LoadAsset<SE_Stats>("SE_Cowardice");

		SocketsBackground.background = assets.LoadAsset<GameObject>("JC_ItemBackground");

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

		Config.SaveOnConfigSet = true;
		Config.Save();
	}

	private static void AddBossBoxProgressConfig(string name, float[] progress)
	{
		Regex regex = new("['[\"\\]]");

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
