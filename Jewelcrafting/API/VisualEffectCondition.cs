using System;

namespace Jewelcrafting;

[Flags]
public enum VisualEffectCondition : uint
{
	IsSkill = 0xFFF,
	Swords = 1,
	Knives = 2,
	Clubs = 3,
	Polearms = 4,
	Spears = 5,
	Blocking = 6,
	Axes = 7,
	Bows = 8,
	Unarmed = 11,
	Pickaxes = 12,
	WoodCutting = 13,
	Crossbows = 14,
	
	IsItem = 0xFF << 12,
	Helmet = 6 << 12,
	Chest = 7 << 12,
	Legs = 11 << 12,
	Hands = 12 << 12,
	Shoulder = 17 << 12,
	Tool = 19 << 12,

	GenericExtraAttributes = 0xFFu << 24, 
	Blackmetal = 1 << 30,
	TwoHanded = 1u << 31,

	SpecificExtraAttributes = 0xF << 20,
	Hammer = (1 << 20) | Tool,
	Hoe = (2 << 20) | Tool,
	Buckler = (1 << 20) | Blocking,
	Towershield = (2 << 20) | Blocking,
	FineWoodBow = (1 << 20) | Bows,
	BowHuntsman = (2 << 20) | Bows,
	BowDraugrFang = (3 << 20) | Bows,
	PickaxeIron = (1 << 20) | Pickaxes,
	Club = (1 << 20) | Clubs,
}
