using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Jewelcrafting.WorldBosses;
using UnityEngine;

namespace Jewelcrafting;

public static class BossDied
{
	[HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
	public static class SetBossFlag
	{
		public static bool firstKill = false;

		private static void Prefix(Character __instance)
		{
			if (Jewelcrafting.uniqueGemDropSystem.Value == Jewelcrafting.UniqueDrop.TrulyUnique || Jewelcrafting.uniqueGemDropSystem.Value == Jewelcrafting.UniqueDrop.GuaranteedFirst)
			{
				firstKill = !BossKilled(__instance);
			}
		}

		private static bool BossKilled(Character boss) => ZoneSystem.instance.GetGlobalKey(boss.m_defeatSetGlobalKey);
	}

	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
	private static class AddGemDrop
	{
		[HarmonyPriority(Priority.VeryLow - 1)]
		private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
		{
			if (__instance.m_character.IsBoss())
			{
				if (__instance.m_character.m_nview.GetZDO().GetLong("Jewelcrafting World Boss") > 0)
				{
					List<Player> nearbyPlayers = new();
					Player.GetPlayersInRange(__instance.m_character.transform.position, 100, nearbyPlayers);
					foreach (Player player in nearbyPlayers)
					{
						player.m_nview.InvokeRPC("Jewelcrafting GachaCoin Receive", Jewelcrafting.bossCoinDrop.Value, __instance.m_character.transform.position);
					}
				}

				if (Jewelcrafting.uniqueGemDropSystem.Value != Jewelcrafting.UniqueDrop.Disabled && GemStones.bossToGem.TryGetValue(global::Utils.GetPrefabName(__instance.gameObject), out GameObject bossDrop))
				{
					if ((Jewelcrafting.uniqueGemDropSystem.Value == Jewelcrafting.UniqueDrop.TrulyUnique || Jewelcrafting.uniqueGemDropSystem.Value == Jewelcrafting.UniqueDrop.GuaranteedFirst) && SetBossFlag.firstKill)
					{
						__result.Add(new KeyValuePair<GameObject, int>(bossDrop, 1));
					}
					else if (Jewelcrafting.uniqueGemDropSystem.Value != Jewelcrafting.UniqueDrop.TrulyUnique && Random.value < Jewelcrafting.uniqueGemDropChance.Value / 100f + CreatureLevelControl.API.GetWorldLevel() * Jewelcrafting.uniqueGemDropChanceIncreasePerWorldLevel.Value / 100f)
					{
						__result.Add(new KeyValuePair<GameObject, int>(bossDrop, Jewelcrafting.uniqueGemDropOnePerPlayer.Value == Jewelcrafting.Toggle.On ? Player.GetPlayersInRangeXZ(__instance.m_character.transform.position, 100) : 1));
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
	private static class AddBoxProgress
	{
		private static void Postfix(Character __instance)
		{
			if (!__instance.IsBoss())
			{
				return;
			}

			List<Player> nearbyPlayers = new();
			Player.GetPlayersInRange(__instance.transform.position, 50f, nearbyPlayers);

			foreach (Player p in nearbyPlayers)
			{
				p.m_nview.InvokeRPC("Jewelcrafting Box BossDied", __instance.m_name);
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
	private static class AddBoxProgressEvent
	{
		private static void Postfix(Player __instance)
		{
			if (__instance.m_nview is { } netView)
			{
				netView.Register<string>("Jewelcrafting Box BossDied", BossDied);
				netView.Register<int, Vector3>("Jewelcrafting GachaCoin Receive", (_, amount, bossPosition) => CoinReceive(__instance, amount, bossPosition));
			}
		}

		private static void BossDied(long sender, string bossName)
		{
			if (Jewelcrafting.boxBossProgress.TryGetValue(bossName, out ConfigEntry<float>[] configs))
			{
				FusionBoxSetup.IncreaseBoxProgress(configs.Select(c => c.Value));
			}
		}

		private static void CoinReceive(Player player, int amount, Vector3 bossPosition)
		{
			if (player.GetInventory().CanAddItem(GachaSetup.gachaCoins, amount))
			{
				player.GetInventory().AddItem(GachaSetup.gachaCoins, amount);
				player.ShowPickupMessage(GachaSetup.gachaCoins.GetComponent<ItemDrop>().m_itemData, amount);
			}
			else
			{
				GachaSetup.gachaCoins.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = GachaSetup.gachaCoins;
				ItemDrop.DropItem(GachaSetup.gachaCoins.GetComponent<ItemDrop>().m_itemData, amount, bossPosition, Quaternion.identity);
			}
		}
	}
}
