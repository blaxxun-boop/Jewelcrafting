using System.Collections.Generic;
using HarmonyLib;
using Jewelcrafting.WorldBosses;

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
				}

				args.Context.AddString("Jewelcrafting console commands - use 'Jewelcrafting' followed by one of the following options.");
				args.Context.AddString("generate - generates raw crystals in this world.");
				args.Context.AddString("worldboss - immediately spawns a random world boss.");
			}), optionsFetcher: () => new List<string> { "generate", "worldboss" });
		}
	}
}
