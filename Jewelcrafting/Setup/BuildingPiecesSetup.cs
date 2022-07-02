using PieceManager;
using UnityEngine;

namespace Jewelcrafting;

public static class BuildingPiecesSetup
{
	public static void initializeBuildingPieces(AssetBundle assets)
	{
		BuildPiece piece = new(assets, "Odins_Stone_Transmuter");
		piece.RequiredItems.Add("Uncut_Green_Stone", 10, true);
		piece.RequiredItems.Add("Uncut_Black_Stone", 10, true);
		piece.RequiredItems.Add("Uncut_Purple_Stone", 10, true);
		piece.RequiredItems.Add("Uncut_Blue_Stone", 10, true);
		piece.Category.Add(BuildPieceCategory.Crafting);
		Utils.ConvertComponent<OpenCompendium, StationExtension>(piece.Prefab);

		piece = new BuildPiece(assets, "op_transmution_table");
		piece.RequiredItems.Add("Wood", 10, true);
		piece.RequiredItems.Add("Flint", 10, true);
		piece.Category.Add(BuildPieceCategory.Crafting);
		
		piece = new BuildPiece(assets, "Odins_Jewelry_Box");
		piece.RequiredItems.Add("FineWood", 30, true);
		piece.RequiredItems.Add("IronNails", 15, true);
		piece.RequiredItems.Add("Obsidian", 4, true);
		piece.Category.Add(BuildPieceCategory.Crafting);
		piece.Prefab.AddComponent<RingInTheBox>();
	}
	
	private class RingInTheBox : MonoBehaviour
	{
		private float stationMaxDistance;
		
		public void Awake()
		{
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
