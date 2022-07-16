using System.Collections.Generic;
using ItemManager;
using Jewelcrafting.GemEffects;
using UnityEngine;

namespace Jewelcrafting;

public static class VisualEffectSetup
{
	public static readonly Dictionary<Skills.SkillType, GameObject> redGemEffects = new();
	public static readonly Dictionary<Skills.SkillType, GameObject> blueGemEffects = new();
	public static readonly Dictionary<Skills.SkillType, GameObject> greenGemEffects = new();
	public static readonly Dictionary<Skills.SkillType, GameObject> blackGemEffects = new();
	public static readonly Dictionary<Skills.SkillType, GameObject> yellowGemEffects = new();
	public static readonly Dictionary<Skills.SkillType, GameObject> purpleGemEffects = new();

	public static readonly Dictionary<ItemDrop.ItemData.ItemType, GameObject> redArmorEffects = new();
	public static readonly Dictionary<ItemDrop.ItemData.ItemType, GameObject> blueArmorEffects = new();
	public static readonly Dictionary<ItemDrop.ItemData.ItemType, GameObject> greenArmorEffects = new();
	public static readonly Dictionary<ItemDrop.ItemData.ItemType, GameObject> blackArmorEffects = new();
	public static readonly Dictionary<ItemDrop.ItemData.ItemType, GameObject> yellowArmorEffects = new();
	public static readonly Dictionary<ItemDrop.ItemData.ItemType, GameObject> purpleArmorEffects = new();

	public static void initializeVisualEffects(AssetBundle assets)
	{
		redGemEffects.Add(Skills.SkillType.Swords, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Sword"));
		redGemEffects.Add(Skills.SkillType.Axes, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Axe"));
		redGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Axes), PrefabManager.RegisterPrefab(assets, "JC_FireParticles_BAxe"));
		redGemEffects.Add(Skills.SkillType.Knives, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Knife"));
		redGemEffects.Add(Skills.SkillType.Spears, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Spear"));
		redGemEffects.Add(Skills.SkillType.Clubs, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Mace"));
		redGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Clubs), PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Sledge"));
		redGemEffects.Add(Skills.SkillType.Polearms, PrefabManager.RegisterPrefab(assets, "JC_FireParticles_Atgeir"));
		redGemEffects.Add(Skills.SkillType.Blocking, PrefabManager.RegisterPrefab(assets, "JC_PainTolerance_Shield"));
		redGemEffects.Add(VisualEffects.Blackmetal(Skills.SkillType.Blocking), PrefabManager.RegisterPrefab(assets, "JC_PainTolerance_BShield"));
		redGemEffects.Add(VisualEffects.Buckler(), PrefabManager.RegisterPrefab(assets, "JC_PainTolerance_AShield"));
		redGemEffects.Add(VisualEffects.Towershield(), PrefabManager.RegisterPrefab(assets, "JC_PainTolerance_TShield"));
		redGemEffects.Add(Skills.SkillType.Bows, PrefabManager.RegisterPrefab(assets, "JC_EndlessArrows_Bow"));
		redGemEffects.Add(VisualEffects.FineWoodBow(), PrefabManager.RegisterPrefab(assets, "JC_EndlessArrows_FineBow"));
		redGemEffects.Add(VisualEffects.BowHuntsman(), PrefabManager.RegisterPrefab(assets, "JC_EndlessArrows_HuntBow"));
		redGemEffects.Add(VisualEffects.BowDraugrFang(), PrefabManager.RegisterPrefab(assets, "JC_EndlessArrows_FangBow"));

		blueGemEffects.Add(Skills.SkillType.Swords, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Sword"));
		blueGemEffects.Add(Skills.SkillType.Axes, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Axe"));
		blueGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Axes), PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_BAxe"));
		blueGemEffects.Add(Skills.SkillType.Knives, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Knife"));
		blueGemEffects.Add(Skills.SkillType.Spears, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Spear"));
		blueGemEffects.Add(Skills.SkillType.Clubs, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Mace"));
		blueGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Clubs), PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Sledge"));
		blueGemEffects.Add(Skills.SkillType.Polearms, PrefabManager.RegisterPrefab(assets, "JC_FrostParticles_Atgeir"));
		blueGemEffects.Add(Skills.SkillType.Blocking, PrefabManager.RegisterPrefab(assets, "JC_Unfazed_Shield"));
		blueGemEffects.Add(VisualEffects.Blackmetal(Skills.SkillType.Blocking), PrefabManager.RegisterPrefab(assets, "JC_Unfazed_BShield"));
		blueGemEffects.Add(VisualEffects.Buckler(), PrefabManager.RegisterPrefab(assets, "JC_Unfazed_AShield"));
		blueGemEffects.Add(VisualEffects.Towershield(), PrefabManager.RegisterPrefab(assets, "JC_Unfazed_TShield"));

		greenGemEffects.Add(Skills.SkillType.Swords, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Sword"));
		greenGemEffects.Add(Skills.SkillType.Axes, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Axe"));
		greenGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Axes), PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_BAxe"));
		greenGemEffects.Add(Skills.SkillType.Knives, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Knife"));
		greenGemEffects.Add(Skills.SkillType.Spears, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Spear"));
		greenGemEffects.Add(Skills.SkillType.Clubs, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Mace"));
		greenGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Clubs), PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Sledge"));
		greenGemEffects.Add(Skills.SkillType.Polearms, PrefabManager.RegisterPrefab(assets, "JC_PoisonParticles_Atgeir"));
		greenGemEffects.Add(Skills.SkillType.Bows, PrefabManager.RegisterPrefab(assets, "JC_Necromancer_Bow"));
		greenGemEffects.Add(VisualEffects.FineWoodBow(), PrefabManager.RegisterPrefab(assets, "JC_Necromancer_FineBow"));
		greenGemEffects.Add(VisualEffects.BowHuntsman(), PrefabManager.RegisterPrefab(assets, "JC_Necromancer_HuntBow"));
		greenGemEffects.Add(VisualEffects.BowDraugrFang(), PrefabManager.RegisterPrefab(assets, "JC_Necromancer_FangBow"));

		blackGemEffects.Add(Skills.SkillType.Swords, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Sword"));
		blackGemEffects.Add(Skills.SkillType.Axes, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Axe"));
		blackGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Axes), PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_BAxe"));
		blackGemEffects.Add(Skills.SkillType.Knives, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Knife"));
		blackGemEffects.Add(Skills.SkillType.Spears, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Spear"));
		blackGemEffects.Add(Skills.SkillType.Clubs, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Mace"));
		blackGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Clubs), PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Sledge"));
		blackGemEffects.Add(Skills.SkillType.Polearms, PrefabManager.RegisterPrefab(assets, "JC_ShadowParticles_Atgeir"));
		blackGemEffects.Add(Skills.SkillType.Blocking, PrefabManager.RegisterPrefab(assets, "JC_Tank_Shield"));
		blackGemEffects.Add(VisualEffects.Blackmetal(Skills.SkillType.Blocking), PrefabManager.RegisterPrefab(assets, "JC_Tank_BShield"));
		blackGemEffects.Add(VisualEffects.Buckler(), PrefabManager.RegisterPrefab(assets, "JC_Tank_AShield"));
		blackGemEffects.Add(VisualEffects.Towershield(), PrefabManager.RegisterPrefab(assets, "JC_Tank_TShield"));
		blackGemEffects.Add(Skills.SkillType.Bows, PrefabManager.RegisterPrefab(assets, "JC_StealthArcher_Bow"));
		blackGemEffects.Add(VisualEffects.FineWoodBow(), PrefabManager.RegisterPrefab(assets, "JC_StealthArcher_FineBow"));
		blackGemEffects.Add(VisualEffects.BowHuntsman(), PrefabManager.RegisterPrefab(assets, "JC_StealthArcher_HuntBow"));
		blackGemEffects.Add(VisualEffects.BowDraugrFang(), PrefabManager.RegisterPrefab(assets, "JC_StealthArcher_FangBow"));

		yellowGemEffects.Add(Skills.SkillType.Swords, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Sword"));
		yellowGemEffects.Add(Skills.SkillType.Axes, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Axe"));
		yellowGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Axes), PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_BAxe"));
		yellowGemEffects.Add(Skills.SkillType.Knives, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Knife"));
		yellowGemEffects.Add(Skills.SkillType.Spears, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Spear"));
		yellowGemEffects.Add(Skills.SkillType.Clubs, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Mace"));
		yellowGemEffects.Add(VisualEffects.TwoHanded(Skills.SkillType.Clubs), PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Sledge"));
		yellowGemEffects.Add(Skills.SkillType.Polearms, PrefabManager.RegisterPrefab(assets, "JC_VampireParticles_Atgeir"));
		yellowGemEffects.Add(Skills.SkillType.Blocking, PrefabManager.RegisterPrefab(assets, "JC_Avoidance_Shield"));
		yellowGemEffects.Add(VisualEffects.Buckler(), PrefabManager.RegisterPrefab(assets, "JC_Avoidance_AShield"));
		yellowGemEffects.Add(VisualEffects.Towershield(), PrefabManager.RegisterPrefab(assets, "JC_Avoidance_BShield"));
		yellowGemEffects.Add(VisualEffects.Blackmetal(Skills.SkillType.Blocking), PrefabManager.RegisterPrefab(assets, "JC_Avoidance_TShield"));
		yellowGemEffects.Add(Skills.SkillType.Bows, PrefabManager.RegisterPrefab(assets, "JC_Echo_Bow"));
		yellowGemEffects.Add(VisualEffects.FineWoodBow(), PrefabManager.RegisterPrefab(assets, "JC_Echo_FineBow"));
		yellowGemEffects.Add(VisualEffects.BowHuntsman(), PrefabManager.RegisterPrefab(assets, "JC_Echo_HuntBow"));
		yellowGemEffects.Add(VisualEffects.BowDraugrFang(), PrefabManager.RegisterPrefab(assets, "JC_Echo_FangBow"));
		yellowGemEffects.Add(Skills.SkillType.Pickaxes, PrefabManager.RegisterPrefab(assets, "JC_Pick_Energetic"));
		yellowGemEffects.Add(VisualEffects.PickaxeIron(), PrefabManager.RegisterPrefab(assets, "JC_PickIron_Energetic"));
		
		yellowArmorEffects.Add(VisualEffects.Hammer(), PrefabManager.RegisterPrefab(assets, "JC_Hammer_Energetic"));
		yellowArmorEffects.Add(VisualEffects.Hoe(), PrefabManager.RegisterPrefab(assets, "JC_Hoe_Energetic"));
		
		purpleGemEffects.Add(Skills.SkillType.Blocking, PrefabManager.RegisterPrefab(assets, "JC_ParryMaster_Shield"));
		purpleGemEffects.Add(VisualEffects.Buckler(), PrefabManager.RegisterPrefab(assets, "JC_ParryMaster_AShield"));
		purpleGemEffects.Add(VisualEffects.Towershield(), PrefabManager.RegisterPrefab(assets, "JC_ParryMaster_BShield"));
		purpleGemEffects.Add(VisualEffects.Blackmetal(Skills.SkillType.Blocking), PrefabManager.RegisterPrefab(assets, "JC_ParryMaster_TShield"));
		purpleGemEffects.Add(Skills.SkillType.Bows, PrefabManager.RegisterPrefab(assets, "JC_MasterArcher_Bow"));
		purpleGemEffects.Add(VisualEffects.FineWoodBow(), PrefabManager.RegisterPrefab(assets, "JC_MasterArcher_FineBow"));
		purpleGemEffects.Add(VisualEffects.BowHuntsman(), PrefabManager.RegisterPrefab(assets, "JC_MasterArcher_HuntBow"));
		purpleGemEffects.Add(VisualEffects.BowDraugrFang(), PrefabManager.RegisterPrefab(assets, "JC_MasterArcher_FangBow"));
		purpleGemEffects.Add(Skills.SkillType.Pickaxes, PrefabManager.RegisterPrefab(assets, "JC_Pick_Unbreakable"));
		purpleGemEffects.Add(VisualEffects.PickaxeIron(), PrefabManager.RegisterPrefab(assets, "JC_PickIron_Unbreakable"));

		purpleArmorEffects.Add(VisualEffects.Hammer(), PrefabManager.RegisterPrefab(assets, "JC_Hammer_Unbreakable"));
		purpleArmorEffects.Add(VisualEffects.Hoe(), PrefabManager.RegisterPrefab(assets, "JC_Hoe_Unbreakable"));
	}
}
