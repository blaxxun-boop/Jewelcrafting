using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting;

public static class GemCursor
{
	private struct CursorInfo
	{
		public Texture2D cursorTexture;
		public Vector2 cursorHotspot;
	}

	[Flags]
	public enum CursorState
	{
		None = 0,
		Socketing = 1,
		Crafting = 2
	}

	private static CursorInfo lastCursor;
	private static Texture2D gemCursor = null!;
	private static CursorState CursorActive = CursorState.None;

	[HarmonyPatch(typeof(Cursor), nameof(Cursor.SetCursor), typeof(Texture2D), typeof(Vector2), typeof(CursorMode))]
	private static class CacheLastCursor
	{
		private static void Postfix(Texture2D texture, Vector2 hotspot)
		{
			if (texture != gemCursor)
			{
				lastCursor.cursorTexture = texture;
				lastCursor.cursorHotspot = hotspot;
			}
		}
	}

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private static class CacheVanillaCursor
	{
		private static void Postfix()
		{
			lastCursor.cursorTexture = Resources.FindObjectsOfTypeAll<Texture2D>().First(s => s.name == "cursor" && s.isReadable);
			lastCursor.cursorHotspot = Vector2.zero;
			gemCursor = Utils.loadTexture("gem_cursor.png");
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
	private static class ReplaceCursorGemcuttersTable
	{
		private static void Postfix()
		{
			if (Player.m_localPlayer.GetCurrentCraftingStation() is { } craftingStation && craftingStation && global::Utils.GetPrefabName(craftingStation.gameObject) == BuildingPiecesSetup.gemcuttersTable.name)
			{
				SetCursor(CursorState.Crafting);
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
	private static class SwitchCursorBack
	{
		private static void Postfix() => ResetCursor(CursorState.Crafting);
	}

	public static void SetCursor(CursorState state)
	{
		if (Jewelcrafting.displayGemcursor.Value == Jewelcrafting.Toggle.On)
		{
			Cursor.SetCursor(gemCursor, Vector2.zero, CursorMode.Auto);
			CursorActive |= state;
		}
	}

	public static void ResetCursor(CursorState state)
	{
		CursorActive &= ~state;
		if (CursorActive == CursorState.None)
		{
			Cursor.SetCursor(lastCursor.cursorTexture, lastCursor.cursorHotspot, CursorMode.Auto);
		}
	}
}
