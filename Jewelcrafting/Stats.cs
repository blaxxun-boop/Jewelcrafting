using System.Linq;
using JetBrains.Annotations;
using StatManager;

namespace Jewelcrafting;

[PublicAPI]
public static class Stats
{
	public static readonly Stat gemsCut = new("Gemstones Cut Success");
	public static readonly Stat cutsFailed = new("Gemstones Cut Failed");
	public static readonly Stat simpleGemsCut = new("Simple Gemstones Cut Success");
	public static readonly Stat simpleCutsFailed = new("Simple Gemstones Cut Failed");
	public static readonly Stat advancedGemsCut = new("Advanced Gemstones Cut Success");
	public static readonly Stat advancedCutsFailed = new("Advanced Gemstones Cut Failed");
	public static readonly Stat perfectGemsCut = new("Perfect Gemstones Cut Success");
	public static readonly Stat perfectCutsFailed = new("Perfect Gemstones Cut Failed");
	public static readonly Stat[] tieredGemsCut = { simpleGemsCut, advancedGemsCut, perfectGemsCut };
	public static readonly Stat[] tieredCutsFailed = { simpleCutsFailed, advancedCutsFailed, perfectCutsFailed };
	public static readonly Stat fusionsCompleted = new("Gem Fusion Success");
	public static readonly Stat fusionsFailed = new("Gem Fusion Failed");
	public static readonly Stat boxFusionsCompleted = new("Box Fusion Success");
	public static readonly Stat boxFusionsFailed = new("Box Fusion Failed");
	public static readonly Stat commonFusionCompleted = new("Gem Fusion Common Success");
	public static readonly Stat commonFusionCompletedSimple = new("Gem Fusion Common Success Simple");
	public static readonly Stat commonFusionCompletedAdvanced = new("Gem Fusion Common Success Advanced");
	public static readonly Stat commonFusionCompletedPerfect = new("Gem Fusion Common Success Perfect");
	public static readonly Stat commonFusionCompletedFusion = new("Gem Fusion Common Success Fusion");
	public static readonly Stat commonFusionFailed = new("Gem Fusion Common Failed");
	public static readonly Stat commonFusionFailedSimple = new("Gem Fusion Common Failed Simple");
	public static readonly Stat commonFusionFailedAdvanced = new("Gem Fusion Common Failed Advanced");
	public static readonly Stat commonFusionFailedPerfect = new("Gem Fusion Common Failed Perfect");
	public static readonly Stat commonFusionFailedFusion = new("Gem Fusion Common Failed Fusion");
	public static readonly Stat epicFusionCompleted = new("Gem Fusion Epic Success");
	public static readonly Stat epicFusionCompletedSimple = new("Gem Fusion Epic Success Simple");
	public static readonly Stat epicFusionCompletedAdvanced = new("Gem Fusion Epic Success Advanced");
	public static readonly Stat epicFusionCompletedPerfect = new("Gem Fusion Epic Success Perfect");
	public static readonly Stat epicFusionCompletedFusion = new("Gem Fusion Epic Success Fusion");
	public static readonly Stat epicFusionFailed = new("Gem Fusion Epic Failed");
	public static readonly Stat epicFusionFailedSimple = new("Gem Fusion Epic Failed Simple");
	public static readonly Stat epicFusionFailedAdvanced = new("Gem Fusion Epic Failed Advanced");
	public static readonly Stat epicFusionFailedPerfect = new("Gem Fusion Epic Failed Perfect");
	public static readonly Stat epicFusionFailedFusion = new("Gem Fusion Epic Failed Fusion");
	public static readonly Stat legendaryFusionCompleted = new("Gem Fusion Legendary Success");
	public static readonly Stat legendaryFusionCompletedSimple = new("Gem Fusion Legendary Success Simple");
	public static readonly Stat legendaryFusionCompletedAdvanced = new("Gem Fusion Legendary Success Advanced");
	public static readonly Stat legendaryFusionCompletedPerfect = new("Gem Fusion Legendary Success Perfect");
	public static readonly Stat legendaryFusionCompletedBoss = new("Gem Fusion Legendary Success Boss");
	public static readonly Stat legendaryFusionFailed = new("Gem Fusion Legendary Failed");
	public static readonly Stat legendaryFusionFailedSimple = new("Gem Fusion Legendary Failed Simple");
	public static readonly Stat legendaryFusionFailedAdvanced = new("Gem Fusion Legendary Failed Advanced");
	public static readonly Stat legendaryFusionFailedPerfect = new("Gem Fusion Legendary Failed Perfect");
	public static readonly Stat legendaryFusionFailedBoss = new("Gem Fusion Legendary Failed Boss");
	public static readonly Stat[] tieredFusionCompleted = { commonFusionCompleted, epicFusionCompleted, legendaryFusionCompleted };
	public static readonly Stat[] tieredFusionFailed = { commonFusionFailed, epicFusionFailed, legendaryFusionFailed };
	public static readonly Stat[][] tieredFusionTiersCompleted = {
		new [] { commonFusionCompletedSimple, commonFusionCompletedAdvanced, commonFusionCompletedPerfect },
		new [] { epicFusionCompletedSimple, epicFusionCompletedAdvanced, epicFusionCompletedPerfect },
		new [] { legendaryFusionCompletedSimple, legendaryFusionCompletedAdvanced, legendaryFusionCompletedPerfect },
	};
	public static readonly Stat[][] tieredFusionTiersFailed = {
		new [] { commonFusionFailedSimple, commonFusionFailedAdvanced, commonFusionFailedPerfect },
		new [] { epicFusionFailedSimple, epicFusionFailedAdvanced, epicFusionFailedPerfect },
		new [] { legendaryFusionFailedSimple, legendaryFusionFailedAdvanced, legendaryFusionFailedPerfect },
	};
	public static readonly Stat gemsBroken = new("Gems Broken Unsocket");
	public static readonly Stat socketAddSuccess = new("Sockets Added (Gemcutter Table)");
	public static readonly Stat[] socketAddSuccessSlot = Enumerable.Range(1, 10).Select(i => new Stat($"Sockets Added {i}. Socket (Gemcutter Table)")).ToArray();
	public static readonly Stat socketAddFailure = new("Socket Adding Failed (Gemcutter Table)");
	public static readonly Stat[] socketAddFailureSlot = Enumerable.Range(1, 10).Select(i => new Stat($"Socket Adding Failed {i}. Socket (Gemcutter Table)")).ToArray();
	public static readonly Stat chanceFramesUsed = new("Chance Frames Used");
	public static readonly Stat chanceFrameSocketsAdded = new("Sockets Added (Chance Frames)");
	public static readonly Stat chanceFrameSocketsLoss = new("Sockets Removed (Chance Frames)");
	public static readonly Stat chaosFramesUsed = new("Chaos Frames Used");
	public static readonly Stat chaosFrameSocketsAdded = new("Sockets Added With Chaos Frames");
	public static readonly Stat chaosFrameSocketsLoss = new("Sockets Lost With Chaos Frames");
	public static readonly Stat gemsDuplicated = new("Gems Mirrored (Blessed Mirror)");
	public static readonly Stat itemsDuplicated = new("Items Mirrored (Celestial Mirror)");
	public static readonly Stat destructiblesDestroyed = new("Gemstone Formations Mined");
	public static readonly Stat gemsDroppedDestructible = new("Gems Dropped (Gemstone Formation)");
	public static readonly Stat gemsDroppedCreature = new("Gems Dropped (Creature)");
	public static readonly Stat socketedEquipmentDropped = new("Socketed Equipment Drops");
	public static readonly Stat worldBossKills = new("World Boss Kills");
	public static readonly Stat deathByWorldBoss = new("Death By World Boss");
	public static readonly Stat gachaCoinsUsed = new("Celestial Chests Blessed");
	public static readonly Stat gachaMainPrizesWon = new("Mystical Gemstone Main Prizes Won");
	public static readonly Stat celestialItemUpgrades = new("Celestial Item Upgrades");
}
