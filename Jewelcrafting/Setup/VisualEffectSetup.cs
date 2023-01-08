using System.Collections.Generic;
using ItemManager;
using UnityEngine;
using static Jewelcrafting.VisualEffectCondition;

namespace Jewelcrafting;

public static class VisualEffectSetup
{
	public static readonly Dictionary<VisualEffectCondition, GameObject> redGemEffects = new();
	public static readonly Dictionary<VisualEffectCondition, GameObject> blueGemEffects = new();
	public static readonly Dictionary<VisualEffectCondition, GameObject> greenGemEffects = new();
	public static readonly Dictionary<VisualEffectCondition, GameObject> blackGemEffects = new();
	public static readonly Dictionary<VisualEffectCondition, GameObject> yellowGemEffects = new();
	public static readonly Dictionary<VisualEffectCondition, GameObject> purpleGemEffects = new();
	public static readonly Dictionary<VisualEffectCondition, GameObject> orangeGemEffects = new();
	public static readonly Dictionary<VisualEffectCondition, GameObject> cyanGemEffects = new();

	public static readonly Dictionary<GemType, GameObject> spearProjectiles = new();

	public static void initializeVisualEffects(AssetBundle assets)
	{
		spearProjectiles.Add(GemType.Red, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Spear_Pro"));
		spearProjectiles.Add(GemType.Blue, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Spear_Pro"));
		spearProjectiles.Add(GemType.Green, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Spear_Pro"));
		spearProjectiles.Add(GemType.Black, PrefabManager.RegisterPrefab(assets, "JC_Magnetic_Spear_Pro"));
		spearProjectiles.Add(GemType.Yellow, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Spear_Pro"));
		spearProjectiles.Add(GemType.Purple, PrefabManager.RegisterPrefab(assets, "JC_Berserk_Spear_Pro"));
		
		redGemEffects.Add(Swords, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Sword"));
		redGemEffects.Add(Axes, PrefabManager.RegisterPrefab(assets, "JC_Opportunity_Axe"));
		redGemEffects.Add(Axes | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_Opportunity_BAxe"));
		redGemEffects.Add(Knives, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Knife"));
		redGemEffects.Add(Spears, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Spear"));
		redGemEffects.Add(Clubs, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Mace"));
		redGemEffects.Add(Club, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Club"));
		redGemEffects.Add(Clubs | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Sledge"));
		redGemEffects.Add(Polearms, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Atgeir"));
		redGemEffects.Add(Blocking, PrefabManager.RegisterPrefab(assets, "JC_PainTolerance_Shield"));
		redGemEffects.Add(Blocking | Blackmetal, PrefabManager.RegisterPrefab(assets, "JC_PainTolerance_BShield"));
		redGemEffects.Add(Buckler, PrefabManager.RegisterPrefab(assets, "JC_PainTolerance_AShield"));
		redGemEffects.Add(Towershield, PrefabManager.RegisterPrefab(assets, "JC_PainTolerance_TShield"));
		redGemEffects.Add(Bows, PrefabManager.RegisterPrefab(assets, "JC_EndlessArrows_Bow"));
		redGemEffects.Add(FineWoodBow, PrefabManager.RegisterPrefab(assets, "JC_EndlessArrows_FineBow"));
		redGemEffects.Add(BowHuntsman, PrefabManager.RegisterPrefab(assets, "JC_EndlessArrows_HuntBow"));
		redGemEffects.Add(BowDraugrFang, PrefabManager.RegisterPrefab(assets, "JC_EndlessArrows_FangBow"));

		blueGemEffects.Add(Swords, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Sword"));
		blueGemEffects.Add(Axes, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Axe"));
		blueGemEffects.Add(Axes | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_BAxe"));
		blueGemEffects.Add(Knives, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Knife"));
		blueGemEffects.Add(Spears, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Spear"));
		blueGemEffects.Add(Clubs, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Mace"));
		blueGemEffects.Add(Club, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Club"));
		blueGemEffects.Add(Clubs | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Sledge"));
		blueGemEffects.Add(Polearms, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Atgeir"));
		blueGemEffects.Add(Blocking, PrefabManager.RegisterPrefab(assets, "JC_Unfazed_Shield"));
		blueGemEffects.Add(Blocking | Blackmetal, PrefabManager.RegisterPrefab(assets, "JC_Unfazed_BShield"));
		blueGemEffects.Add(Buckler, PrefabManager.RegisterPrefab(assets, "JC_Unfazed_AShield"));
		blueGemEffects.Add(Towershield, PrefabManager.RegisterPrefab(assets, "JC_Unfazed_TShield"));

		greenGemEffects.Add(Swords, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Sword"));
		greenGemEffects.Add(Axes, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Axe"));
		greenGemEffects.Add(Axes | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_BAxe"));
		greenGemEffects.Add(Knives, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Knife"));
		greenGemEffects.Add(Spears, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Spear"));
		greenGemEffects.Add(Clubs, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Mace"));
		greenGemEffects.Add(Club, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Club"));
		greenGemEffects.Add(Clubs | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Sledge"));
		greenGemEffects.Add(Polearms, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Atgeir"));
		greenGemEffects.Add(Bows, PrefabManager.RegisterPrefab(assets, "JC_Necromancer_Bow"));
		greenGemEffects.Add(FineWoodBow, PrefabManager.RegisterPrefab(assets, "JC_Necromancer_FineBow"));
		greenGemEffects.Add(BowHuntsman, PrefabManager.RegisterPrefab(assets, "JC_Necromancer_HuntBow"));
		greenGemEffects.Add(BowDraugrFang, PrefabManager.RegisterPrefab(assets, "JC_Necromancer_FangBow"));

		blackGemEffects.Add(Swords, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Sword"));
		blackGemEffects.Add(Axes, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Axe"));
		blackGemEffects.Add(Axes | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_BAxe"));
		blackGemEffects.Add(Knives, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Knife"));
		blackGemEffects.Add(Spears, PrefabManager.RegisterPrefab(assets, "JC_Magnetic_Spear"));
		blackGemEffects.Add(Clubs, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Mace"));
		blackGemEffects.Add(Club, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Club"));
		blackGemEffects.Add(Clubs | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Sledge"));
		blackGemEffects.Add(Polearms, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Atgeir"));
		blackGemEffects.Add(Blocking, PrefabManager.RegisterPrefab(assets, "JC_Tank_Shield"));
		blackGemEffects.Add(Blocking | Blackmetal, PrefabManager.RegisterPrefab(assets, "JC_Tank_BShield"));
		blackGemEffects.Add(Buckler, PrefabManager.RegisterPrefab(assets, "JC_Tank_AShield"));
		blackGemEffects.Add(Towershield, PrefabManager.RegisterPrefab(assets, "JC_Tank_TShield"));
		blackGemEffects.Add(Bows, PrefabManager.RegisterPrefab(assets, "JC_StealthArcher_Bow"));
		blackGemEffects.Add(FineWoodBow, PrefabManager.RegisterPrefab(assets, "JC_StealthArcher_FineBow"));
		blackGemEffects.Add(BowHuntsman, PrefabManager.RegisterPrefab(assets, "JC_StealthArcher_HuntBow"));
		blackGemEffects.Add(BowDraugrFang, PrefabManager.RegisterPrefab(assets, "JC_StealthArcher_FangBow"));
		blackGemEffects.Add(Pickaxes, PrefabManager.RegisterPrefab(assets, "JC_Pick_Frenzy"));
		blackGemEffects.Add(PickaxeIron, PrefabManager.RegisterPrefab(assets, "JC_Pick_Frenzy_Iron"));
		blackGemEffects.Add(Hammer, PrefabManager.RegisterPrefab(assets, "JC_Hammer_Frenzy"));
		blackGemEffects.Add(Hoe, PrefabManager.RegisterPrefab(assets, "JC_Hoe_Frenzy"));
		
		yellowGemEffects.Add(Swords, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Sword"));
		yellowGemEffects.Add(Axes, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Axe"));
		yellowGemEffects.Add(Axes | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_BAxe"));
		yellowGemEffects.Add(Knives, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Knife"));
		yellowGemEffects.Add(Spears, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Spear"));
		yellowGemEffects.Add(Clubs, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Mace"));
		yellowGemEffects.Add(Club, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Club"));
		yellowGemEffects.Add(Clubs | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Sledge"));
		yellowGemEffects.Add(Polearms, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Atgeir"));
		yellowGemEffects.Add(Blocking, PrefabManager.RegisterPrefab(assets, "JC_Avoidance_Shield"));
		yellowGemEffects.Add(Buckler, PrefabManager.RegisterPrefab(assets, "JC_Avoidance_AShield"));
		yellowGemEffects.Add(Towershield, PrefabManager.RegisterPrefab(assets, "JC_Avoidance_BShield"));
		yellowGemEffects.Add(Blocking | Blackmetal, PrefabManager.RegisterPrefab(assets, "JC_Avoidance_TShield"));
		yellowGemEffects.Add(Bows, PrefabManager.RegisterPrefab(assets, "JC_Echo_Bow"));
		yellowGemEffects.Add(FineWoodBow, PrefabManager.RegisterPrefab(assets, "JC_Echo_FineBow"));
		yellowGemEffects.Add(BowHuntsman, PrefabManager.RegisterPrefab(assets, "JC_Echo_HuntBow"));
		yellowGemEffects.Add(BowDraugrFang, PrefabManager.RegisterPrefab(assets, "JC_Echo_FangBow"));
		yellowGemEffects.Add(Pickaxes, PrefabManager.RegisterPrefab(assets, "JC_Pick_Energetic"));
		yellowGemEffects.Add(PickaxeIron, PrefabManager.RegisterPrefab(assets, "JC_PickIron_Energetic"));
		yellowGemEffects.Add(Hammer, PrefabManager.RegisterPrefab(assets, "JC_Hammer_Energetic"));
		yellowGemEffects.Add(Hoe, PrefabManager.RegisterPrefab(assets, "JC_Hoe_Energetic"));
		
		purpleGemEffects.Add(Swords, PrefabManager.RegisterPrefab(assets, "JC_Berserk_Sword"));
		purpleGemEffects.Add(Axes, PrefabManager.RegisterPrefab(assets, "JC_Berserk_Axe"));
		purpleGemEffects.Add(Axes | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_Berserk_BAxe"));
		purpleGemEffects.Add(Knives, PrefabManager.RegisterPrefab(assets, "JC_Berserk_Knife"));
		purpleGemEffects.Add(Spears, PrefabManager.RegisterPrefab(assets, "JC_Berserk_Spear"));
		purpleGemEffects.Add(Clubs, PrefabManager.RegisterPrefab(assets, "JC_Berserk_Mace"));
		purpleGemEffects.Add(Club, PrefabManager.RegisterPrefab(assets, "JC_Berserk_Club"));
		purpleGemEffects.Add(Clubs | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_Berserk_Sledge"));
		purpleGemEffects.Add(Polearms, PrefabManager.RegisterPrefab(assets, "JC_Berserk_Atgeir"));
		purpleGemEffects.Add(Blocking, PrefabManager.RegisterPrefab(assets, "JC_ParryMaster_Shield"));
		purpleGemEffects.Add(Buckler, PrefabManager.RegisterPrefab(assets, "JC_ParryMaster_AShield"));
		purpleGemEffects.Add(Towershield, PrefabManager.RegisterPrefab(assets, "JC_ParryMaster_BShield"));
		purpleGemEffects.Add(Blocking | Blackmetal, PrefabManager.RegisterPrefab(assets, "JC_ParryMaster_TShield"));
		purpleGemEffects.Add(Bows, PrefabManager.RegisterPrefab(assets, "JC_MasterArcher_Bow"));
		purpleGemEffects.Add(FineWoodBow, PrefabManager.RegisterPrefab(assets, "JC_MasterArcher_FineBow"));
		purpleGemEffects.Add(BowHuntsman, PrefabManager.RegisterPrefab(assets, "JC_MasterArcher_HuntBow"));
		purpleGemEffects.Add(BowDraugrFang, PrefabManager.RegisterPrefab(assets, "JC_MasterArcher_FangBow"));
		purpleGemEffects.Add(Pickaxes, PrefabManager.RegisterPrefab(assets, "JC_Pick_Unbreakable"));
		purpleGemEffects.Add(PickaxeIron, PrefabManager.RegisterPrefab(assets, "JC_PickIron_Unbreakable"));
		purpleGemEffects.Add(Hammer, PrefabManager.RegisterPrefab(assets, "JC_Hammer_Unbreakable"));
		purpleGemEffects.Add(Hoe, PrefabManager.RegisterPrefab(assets, "JC_Hoe_Unbreakable"));
		
		orangeGemEffects.Add(Knives, PrefabManager.RegisterPrefab(assets, "JC_Perforation_Knife"));
		orangeGemEffects.Add(Polearms, PrefabManager.RegisterPrefab(assets, "JC_ThunderClap_Atgeir"));
		
		cyanGemEffects.Add(Swords, PrefabManager.RegisterPrefab(assets, "JC_FleetingLife_Sword"));
		cyanGemEffects.Add(Axes, PrefabManager.RegisterPrefab(assets, "JC_FleetingLife_Axe"));
		cyanGemEffects.Add(Axes | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_FleetingLife_BAxe"));
		cyanGemEffects.Add(Knives, PrefabManager.RegisterPrefab(assets, "JC_FleetingLife_Knife"));
		cyanGemEffects.Add(Spears, PrefabManager.RegisterPrefab(assets, "JC_FleetingLife_Spear"));
		cyanGemEffects.Add(Clubs, PrefabManager.RegisterPrefab(assets, "JC_FleetingLife_Mace"));
		cyanGemEffects.Add(Club, PrefabManager.RegisterPrefab(assets, "JC_FleetingLife_Club"));
		cyanGemEffects.Add(Clubs | TwoHanded, PrefabManager.RegisterPrefab(assets, "JC_FleetingLife_Sledge"));
		cyanGemEffects.Add(Polearms, PrefabManager.RegisterPrefab(assets, "JC_FleetingLife_Atgeir"));
		cyanGemEffects.Add(Blocking, PrefabManager.RegisterPrefab(assets, "JC_DedicatedTank_Shield"));
		cyanGemEffects.Add(Buckler, PrefabManager.RegisterPrefab(assets, "JC_DedicatedTank_AShield"));
		cyanGemEffects.Add(Towershield, PrefabManager.RegisterPrefab(assets, "JC_DedicatedTank_BShield"));
		cyanGemEffects.Add(Blocking | Blackmetal, PrefabManager.RegisterPrefab(assets, "JC_DedicatedTank_TShield"));
		cyanGemEffects.Add(Bows, PrefabManager.RegisterPrefab(assets, "JC_ArcheryMentor_Bow"));
		cyanGemEffects.Add(BowHuntsman, PrefabManager.RegisterPrefab(assets, "JC_ArcheryMentor_Bow"));
		cyanGemEffects.Add(FineWoodBow, PrefabManager.RegisterPrefab(assets, "JC_ArcheryMentor_Bow"));
		cyanGemEffects.Add(BowDraugrFang, PrefabManager.RegisterPrefab(assets, "JC_ArcheryMentor_FangBow"));
	}
}
