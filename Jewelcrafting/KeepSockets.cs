using HarmonyLib;
using ItemDataManager;

namespace Jewelcrafting;

public static class KeepSockets
{
	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
	private static class CopySockets
	{
		private static void Prefix(InventoryGui __instance, ref Sockets? __state)
		{
			if (__instance.m_craftRecipe is not null && __instance.m_craftUpgradeItem is null && Utils.IsSocketableItem(__instance.m_craftRecipe.m_item))
			{
				foreach (Piece.Requirement req in __instance.m_craftRecipe.m_resources)
				{
					if (req.m_amount == 1)
					{
						if (Player.m_localPlayer.GetInventory().GetItem(req.m_resItem.m_itemData.m_shared.m_name) is { } item && item.Data().Get<Sockets>() is { } sockets && __instance.m_craftRecipe.m_item.m_itemData.Data().Add<Sockets>() is { } newSockets)
						{
							newSockets.Value = sockets.Value;
							__state = newSockets;
							return;
						}
					}
				}
			}
		}

		private static void Finalizer(ref Sockets? __state) => __state?.Info.Remove(__state);
	}
}
