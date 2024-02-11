using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CreatureLevelControl;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class Daring
{
	static Daring()
	{
		EffectDef.ConfigTypes.Add(Effect.Daring, typeof(Config));
		API.OnEffectRecalc += OnEffectRecalc;
	}

	private static void OnEffectRecalc()
	{
		if (Player.m_localPlayer.GetEffect(Effect.Daring) > 0)
		{
			Player.m_localPlayer.StartCoroutine(CreatureLevelUp(Player.m_localPlayer));
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public readonly float Power;
	}

	private static IEnumerator CheckDaringPresent(Character character)
	{
		yield return null;

		List<Player> players = new();
		while (!string.IsNullOrEmpty(character.m_nview.GetZDO()?.GetString("Jewelcrafting Daring")))
		{
			if (character.m_nview.IsOwner())
			{
				List<string> daring = character.m_nview.GetZDO().GetString("Jewelcrafting Daring").Split(',').ToList();

				players.Clear();
				Player.GetPlayersInRange(character.transform.position, SpawnSystem.m_spawnDistanceMax, players);

				HashSet<string> daringPlayers = new(players.Where(player => player.GetEffect(Effect.Daring) > 0).Select(player => player.GetZDOID().UserID.ToString()));

				int reduceLevel = 0;
				foreach (string player in daring.Where(player => !daringPlayers.Contains(player)).ToArray())
				{
					daring.Remove(player);
					++reduceLevel;
				}
				if (reduceLevel > 0)
				{
					character.SetLevel(character.GetLevel() - reduceLevel);
					character.m_nview.InvokeRPC(ZNetView.Everybody, "Jewelcrafting Daring SyncLevel", character.GetLevel());
					character.m_nview.GetZDO().Set("Jewelcrafting Daring", string.Join(",", daring));
				}
			}
			yield return new WaitForSeconds(1);
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Awake))]
	private class RegisterRPC
	{
		private static void Postfix(Character __instance)
		{
			if (__instance.m_nview.GetZDO() is null)
			{
				return;
			}

			__instance.m_nview.Register("Jewelcrafting Daring", sender =>
			{
				if (!CreatureLevelControl.API.IsEnabled() && __instance.GetLevel() >= 3)
				{
					return;
				}

				string defined = __instance.m_nview.GetZDO().GetString("Jewelcrafting Daring");
				if (string.IsNullOrEmpty(defined))
				{
					defined = sender.ToString();
					__instance.StartCoroutine(CheckDaringPresent(__instance));
				}
				else if (!defined.Split(',').Contains(sender.ToString()))
				{
					defined += $",{sender}";
				}
				else
				{
					return;
				}
				if (__instance.m_nview.IsOwner())
				{
					__instance.SetLevel(__instance.GetLevel() + 1);
					__instance.m_nview.InvokeRPC(ZNetView.Everybody, "Jewelcrafting Daring SyncLevel", __instance.GetLevel());
					__instance.m_nview.GetZDO().Set("Jewelcrafting Daring", defined);
				}
			});
			__instance.m_nview.Register<int>("Jewelcrafting Daring SyncLevel", (_, level) => __instance.SetLevel(level));
			if (!string.IsNullOrEmpty(__instance.m_nview.GetZDO().GetString("Jewelcrafting Daring")))
			{
				__instance.StartCoroutine(CheckDaringPresent(__instance));
			}
		}
	}

	private static int coroutineId = 0;

	private static IEnumerator CreatureLevelUp(Player player)
	{
		int coroutine = ++coroutineId;
		while (coroutine == coroutineId && player.GetEffect(Effect.Daring) > 0)
		{
			if (!player.IsDead())
			{
				List<Character> characters = Character.s_characters.Where(c => !c.IsPlayer() && !c.IsTamed() && Vector3.Distance(player.transform.position, c.transform.position) < SpawnSystem.m_spawnDistanceMax).ToList();
				foreach (Character character in characters)
				{
					if (CreatureLevelControl.API.IsLoaded() && (CreatureExtraEffect)character.m_nview.GetZDO().GetInt("CL&LC effect") == CreatureExtraEffect.Splitting)
					{
						continue;
					}

					if (!character.m_nview.GetZDO().GetString("Jewelcrafting Daring").Split(',').Contains(ZDOMan.instance.m_sessionID.ToString()) && (CreatureLevelControl.API.IsEnabled() || character.GetLevel() < 3))
					{
						Random.State state = Random.state;
						Random.InitState((int)(player.GetZDOID().ID + character.GetZDOID().ID));
						if (Random.value < player.GetEffect(Effect.Daring) / 100f / Math.Max(Player.GetPlayersInRangeXZ(character.transform.position, SpawnSystem.m_spawnDistanceMax), 1))
						{
							// Ensure owner is in proximity of Character
							if (!character.m_nview.IsOwner() && (Player.s_players.FirstOrDefault(player => player.GetZDOID().UserID == character.m_nview.GetZDO().GetOwner()) is not { } owner || Vector3.Distance(character.transform.position, owner.transform.position) > SpawnSystem.m_spawnDistanceMax + 20))
							{
								character.m_nview.ClaimOwnership();
							}

							character.m_nview.InvokeRPC(ZNetView.Everybody, "Jewelcrafting Daring");
						}
						Random.state = state;
					}
				}
			}
			yield return new WaitForSeconds(1);
		}
		// ReSharper disable once IteratorNeverReturns
	}
}
