using System.Collections.Generic;
using HarmonyLib;

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
				if (!Jewelcrafting.configSync.IsAdmin && !Jewelcrafting.configSync.IsSourceOfTruth)
				{
					args.Context.AddString("You are not an admin on this server.");
					return;
				}

				if (args.Length >= 2 && args[1] == "generate")
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
				
				args.Context.AddString("Jewelcrafting console commands - use 'Jewelcrafting' followed by one of the following options.");
				args.Context.AddString("generate - generates raw crystals in this world.");
			}), optionsFetcher: () => new List<string> { "generate" });
		}
	}
}
