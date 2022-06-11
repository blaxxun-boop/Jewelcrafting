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

		piece = new BuildPiece(assets, "op_transmution_table");
		piece.RequiredItems.Add("Wood", 10, true);
		piece.RequiredItems.Add("Flint", 10, true);
		piece.Category.Add(BuildPieceCategory.Crafting);
	}
}
