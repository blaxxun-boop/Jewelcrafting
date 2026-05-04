using System.Collections.Generic;
using HarmonyLib;
using ItemDataManager;

namespace Jewelcrafting;

public static class KeepSockets
{
	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
	private static class CopySockets
	{
		private static void Prefix(InventoryGui __instance, ref KeyValuePair<Sockets?, int> __state)
		{
			if (__instance.m_craftRecipe is not null && __instance.m_craftUpgradeItem is null && Utils.IsSocketableItem(__instance.m_craftRecipe.m_item))
			{
				foreach (Piece.Requirement req in __instance.m_craftRecipe.m_resources)
				{
					if (req.m_amount == 1)
					{
						if (Player.m_localPlayer.GetInventory().GetItem(req.m_resItem.m_itemData.m_shared.m_name) is { } item && item.Data().Get<Sockets>() is { } sockets && __instance.m_craftRecipe.m_item.m_itemData.Data().Add<Sockets>() is { } newSockets)
						{
							int flags = 0;
							if (sockets.Info["SocketsLock"] is not null && newSockets.Info["SocketsLock"] is null)
							{
								newSockets.Info["SocketsLock"] = "";
								flags |= 1;
							}
							if (sockets.Info["SocketSlotsLock"] is not null && newSockets.Info["SocketSlotsLock"] is null)
							{
								newSockets.Info["SocketSlotsLock"] = "";
								flags |= 2;
							}
							newSockets.Value = sockets.Value;
							__state = new KeyValuePair<Sockets?, int>(newSockets, flags);
							return;
						}
					}
				}
			}
		}

		private static void Finalizer(ref KeyValuePair<Sockets?, int> __state)
		{
			if (__state.Key is { } sockets)
			{
				sockets.Info.Remove(sockets);
				if ((__state.Value & 1) != 0)
				{
					sockets.Info.Remove("SocketsLock");
				}
				if ((__state.Value & 2) != 0)
				{
					sockets.Info.Remove("SocketSlotsLock");
				}
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupUpgradeItem))]
	private static class HintCorruptedItemNotUpgradeable
	{
		private static void Postfix(InventoryGui __instance, ItemDrop.ItemData item)
		{
			if (item.Data()["Corrupted Item"] is not null)
			{
				__instance.m_itemCraftType.text = Localization.instance.Localize("$jc_corrupted_upgrade_attempt");
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
	private static class PreventCorruptedItemUpgradeDisplay
	{
		private static void Postfix(InventoryGui __instance)
		{
			if (__instance.m_selectedRecipe.Recipe && __instance.m_selectedRecipe.ItemData?.Data()["Corrupted Item"] is not null)
			{
				__instance.m_itemCraftType.text = "";
				__instance.m_minStationLevelIcon.gameObject.SetActive(false);
				__instance.m_craftButton.interactable = false;
				__instance.m_craftButton.GetComponent<UITooltip>().m_text = Localization.instance.Localize("$jc_corrupted_upgrade_attempt");
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
	private static class StopCorruptedItemUpgrade
	{
		private static bool Prefix(InventoryGui __instance) => !__instance.m_selectedRecipe.Recipe || __instance.m_selectedRecipe.ItemData?.Data()["Corrupted Item"] is null;
	}
}
