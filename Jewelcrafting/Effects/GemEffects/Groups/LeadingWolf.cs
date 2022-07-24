using System.Collections.Generic;
using System.Linq;
using Groups;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects.Groups;

public static class LeadingWolf
{
	static LeadingWolf()
	{
		EffectDef.ConfigTypes.Add(Effect.Leadingwolf, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.GetJogSpeedFactor))]
	private class IncreaseJogSpeed
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += IncreaseMovementSpeed(__instance) / 100f;
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.GetRunSpeedFactor))]
	private class IncreaseRunSpeed
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += IncreaseMovementSpeed(__instance) / 100f;
		}
	}

	private static float IncreaseMovementSpeed(Player self)
	{
		List<Player> nearbyPlayers = Utils.GetNearbyGroupMembers(self, 20, true);
		if (nearbyPlayers.Count <= 1)
		{
			return 0;
		}

		float activeFactor = 0;
		bool hasGroupLeader = false; 
		foreach (Player player in nearbyPlayers)
		{
			float factor = player.GetEffect(Effect.Leadingwolf);
			if (factor > 0)
			{
				if (hasGroupLeader)
				{
					return 0;
				}
				hasGroupLeader = true;
				
				if (player == self)
				{
					float totalDist(Vector3 translate = new()) => nearbyPlayers.Where(p => p != self).Sum(p => Vector3.Distance(self.transform.position + translate, p.transform.position));
                    if (totalDist() < totalDist(self.GetMoveDir()))
                    {
                        activeFactor = factor;
                    }
				}
				else if (Vector3.Distance(self.transform.position, player.transform.position) > Vector3.Distance(self.transform.position + self.GetMoveDir(), player.transform.position))
                {
                    activeFactor = factor;
                }
			}
		}

		return activeFactor;
	}
}
