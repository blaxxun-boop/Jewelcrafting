using System;
using System.Collections.Generic;
using HarmonyLib;
using ItemDataManager;
using Jewelcrafting.GemEffects;
using Jewelcrafting.WorldBosses;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jewelcrafting;

public static class TerminalCommands
{
	[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
	public class AddChatCommands
	{
		private static void Postfix()
		{
			_ = new Terminal.ConsoleCommand("Jewelcrafting", "Manages the Jewelcrafting commands.", (Terminal.ConsoleEvent)(args =>
			{
				if (Jewelcrafting.configSync is { IsAdmin: false, IsSourceOfTruth: false })
				{
					args.Context.AddString("You are not an admin on this server.");
					return;
				}

				switch (args.Length)
				{
					case >= 2 when args[1] == "generate":
					{
						if (ZNet.instance.GetServerPeer() is { } peer)
						{
							peer.m_rpc.Invoke("Jewelcrafting GenerateVegetation");
						}
						else
						{
							GenerateVegetationSpawners.RPC_GenerateVegetation(null);
						}

						return;
					}
					case >= 2 when args[1] == "worldboss":
					{
						if (ZNet.instance.GetServerPeer() is { } peer)
						{
							peer.m_rpc.Invoke("Jewelcrafting SpawnBoss");
						}
						else
						{
							BossSpawn.SpawnBoss();
						}

						return;
					}
					case >= 4 when args[1] == "spawn":
					{
						if (ObjectDB.instance.GetItemPrefab(args[2]) is { } item)
						{
							Transform transform = Player.m_localPlayer.transform;
							GameObject go = Object.Instantiate(item, transform.position + transform.forward * 2f + Vector3.up, Quaternion.identity);
							Sockets sockets = go.GetComponent<ItemDrop>().m_itemData.Data().GetOrCreate<Sockets>();
							sockets.socketedGems.Clear();
							for (int i = 3; i < args.Length; ++i)
							{
								if (args[i].ToLower() == "empty")
								{
									sockets.socketedGems.Add(new SocketItem(""));
									continue;
								}

								GemType? GemType(string arg)
								{
									if (arg.Length > 0 && EffectDef.ValidGemTypes.TryGetValue(char.ToUpper(arg[0]) + arg.Substring(1), out GemType type))
									{
										return type;
									}
									return null;
								}

								void AddSocket<T>(IList<T> list, Func<T, GameObject> get)
								{
									if (i + 1 < args.Length && int.TryParse(args[i + 1], out int tier))
									{
										++i;
										--tier;
									}
									else
									{
										tier = 0;
									}
									if (tier < list.Count)
									{
										sockets.socketedGems.Add(new SocketItem(get(list[tier]).name));
									}
								}

								string[] parts = args[i].Split('-');
								if (parts.Length == 1 && GemType(args[i]) is { } type && GemStoneSetup.Gems.TryGetValue(type, out List<GemDefinition> gems))
								{
									AddSocket(gems, def => def.Prefab);
								}
								else if (parts.Length == 2 && GemType(parts[0]) is { } part1 && GemType(parts[1]) is { } part2 && MergedGemStoneSetup.mergedGems.TryGetValue(part1, out Dictionary<GemType, GameObject[]> outer) && outer.TryGetValue(part2, out GameObject[] mergedGems))
								{
									AddSocket(mergedGems, gem => gem);
								}
							}
							if (sockets.socketedGems.Count == 0)
							{
								go.GetComponent<ItemDrop>().m_itemData.Data().Remove<Sockets>();
							}
						}

						return;
					}
				}

				args.Context.AddString("Jewelcrafting console commands - use 'Jewelcrafting' followed by one of the following options.");
				args.Context.AddString("generate - generates raw crystals in this world.");
				args.Context.AddString("worldboss - immediately spawns a random world boss.");
				args.Context.AddString("spawn - spawns an item with sockets. Example: 'spawn SwordSilver green 3 blue-red 1 empty' spawns a silver sword with a tier 3 green gem, a tier 1 merged blue-red gem and one empty socket.");
			}), optionsFetcher: () => new List<string> { "generate", "worldboss", "spawn" });
		}
	}
}
