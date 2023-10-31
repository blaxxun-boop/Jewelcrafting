using BepInEx.Configuration;
using HarmonyLib;
using PieceManager;
using SkillManager;
using UnityEngine;

namespace Jewelcrafting;

public static class BuildingPiecesSetup
{
	public static GameObject gemcuttersTable = null!;
	private static GameObject astralCutter = null!;

	public static void initializeBuildingPieces(AssetBundle assets)
	{
		BuildPiece piece = new(assets, "Odins_Stone_Transmuter");
		piece.RequiredItems.Add("Uncut_Green_Stone", 10, true);
		piece.RequiredItems.Add("Uncut_Black_Stone", 10, true);
		piece.RequiredItems.Add("Uncut_Purple_Stone", 10, true);
		piece.RequiredItems.Add("Uncut_Blue_Stone", 10, true);
		piece.Category.Set(BuildPieceCategory.Crafting);
		Utils.ConvertComponent<OpenCompendium, StationExtension>(piece.Prefab);

		piece = new BuildPiece(assets, "op_transmution_table");
		piece.RequiredItems.Add("Wood", 10, true);
		piece.RequiredItems.Add("Flint", 10, true);
		piece.Category.Set(BuildPieceCategory.Crafting);
		gemcuttersTable = piece.Prefab;
		
		piece = new BuildPiece(assets, "Odins_Jewelry_Box");
		piece.RequiredItems.Add("FineWood", 30, true);
		piece.RequiredItems.Add("IronNails", 15, true);
		piece.RequiredItems.Add("Obsidian", 4, true);
		piece.Category.Set(BuildPieceCategory.Crafting);
		piece.Prefab.AddComponent<RingInTheBox>();
		
		piece = new BuildPiece(assets, "JC_Gemstone_Furnace");
		piece.RequiredItems.Add("Thunderstone", 1, true);
		piece.RequiredItems.Add("SurtlingCore", 5, true);
		piece.RequiredItems.Add("Bronze", 10, true);
		piece.Category.Set(BuildPieceCategory.Crafting);
		astralCutter = piece.Prefab;
	}

	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	private static class AddFuelItem
	{
		private static void Postfix(ObjectDB __instance)
		{
			astralCutter.GetComponent<Smelter>().m_fuelItem = __instance.GetItemPrefab("Coal")?.GetComponent<ItemDrop>();
		}
	}

	[HarmonyPatch(typeof(Smelter), nameof(Smelter.GetItemConversion))]
	private static class AddChanceToBreakOrUpgrade
	{
		private static void Postfix(Smelter __instance, ref Smelter.ItemConversion? __result)
		{
			if (__result is not null && Jewelcrafting.gemUpgradeChances.TryGetValue(__result.m_to.m_itemData.m_shared.m_name, out ConfigEntry<float> upgradeChance) && Random.value > upgradeChance.Value / 100f * (1 + Jewelcrafting.upgradeChanceIncrease.Value / 100f * __instance.m_nview.GetZDO().GetFloat("Jewelcrafting SkillLevel")))
			{
				__result = new Smelter.ItemConversion
				{
					m_from = __result.m_from,
					m_to = GemStones.gemToShard[__result.m_to.name].GetComponent<ItemDrop>(),
				};
			}
		}
	}
	
	[HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
	private static class StoreSkillLevel
	{
		private static void Postfix(Smelter __instance, bool __result)
		{
			if (__result)
			{
				__instance.m_nview.InvokeRPC("Jewelcrafting SkillLevel", Player.m_localPlayer.GetSkillFactor("Jewelcrafting"));
			}
		}
	}

	[HarmonyPatch(typeof(Smelter), nameof(Smelter.Awake))]
	private static class AddRPCs
	{
		private static void Postfix(Smelter __instance)
		{
			__instance.m_nview.Register<float>("Jewelcrafting SkillLevel", (_, skill) =>
			{
				if (__instance.m_nview.IsOwner())
				{
					__instance.m_nview.GetZDO().Set("Jewelcrafting SkillLevel", skill);
				}
			});
		}
	}
	
	private class RingInTheBox : MonoBehaviour
	{
		private float stationMaxDistance;
		
		public void Awake()
		{
			GetComponent<WearNTear>().m_onDestroyed += GetComponentInChildren<ItemStand>().OnDestroyed;
			stationMaxDistance = GetComponent<StationExtension>().m_maxStationDistance;
		}

		public void Update()
		{
			if (GetComponent<ZNetView>()?.GetZDO() is { } zdo)
			{
				GetComponent<StationExtension>().m_maxStationDistance = zdo.GetString("item") == "" ? 0 : stationMaxDistance;
				transform.Find("_enabled").gameObject.SetActive(zdo.GetString("item") != "");
			}
		}
	}
}
