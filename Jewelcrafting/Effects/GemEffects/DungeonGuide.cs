﻿using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class DungeonGuide
{
	private static GameObject? lightOrb;

	static DungeonGuide()
	{
		API.OnEffectRecalc += () =>
		{
			if (Player.m_localPlayer.GetEffect(Effect.Dungeonguide) == 0)
			{
				Player.m_localPlayer.m_seman.RemoveStatusEffect(GemEffectSetup.dungeonGuide);
			}
		};
	}

	[HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
	private static class SummonGuide
	{
		private static void Postfix(Vector3 pos, bool __result)
		{
			if (!__result)
			{
				return;
			}
			
			if (Player.m_localPlayer.GetEffect(Effect.Dungeonguide) > 0 && pos.y > 3500)
			{
				GemEffectSetup.dungeonGuide.m_ttl = Player.m_localPlayer.GetEffect(Effect.Dungeonguide) * 60;
				Player.m_localPlayer.m_seman.AddStatusEffect(GemEffectSetup.dungeonGuide, true);
				if (!lightOrb)
				{
					lightOrb = Object.Instantiate(GemEffectSetup.dungeonGuidePrefab);
					lightOrb.transform.position = Player.m_localPlayer.transform.position;
				}
			}
			else
			{
				Player.m_localPlayer.m_seman.RemoveStatusEffect(GemEffectSetup.dungeonGuide);
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Update))]
	public static class MoveGuide
	{
		public static bool orbArrived = false;
		private static float duration = 2f;

		private static void Postfix(Player __instance)
		{
			if (__instance != Player.m_localPlayer || !lightOrb || orbArrived)
			{
				return;
			}

			if (!__instance.m_seman.HaveStatusEffect(GemEffectSetup.dungeonGuide.name.GetStableHashCode()))
			{
				ZNetScene.instance.Destroy(lightOrb);
				lightOrb = null;
				return;
			}
			
			orbArrived = true;
			Vector3 offset = __instance.transform.position.y > 3500 ? new Vector3(Random.Range(-1f, 1f), Random.Range(-2f, 0.5f), Random.Range(-1f, 1f)) : new Vector3(Random.Range(-3f, 3f), Random.Range(-1.5f, 1.5f), Random.Range(-3f, 3f));
			Vector3 newPosition = __instance.transform.position + offset;
			duration = Random.Range(1f, 3f);

			lightOrb!.GetComponent<OrbDestroy>().StartCoroutine(MoveOrb(lightOrb, newPosition));
		}

		private static IEnumerator MoveOrb(GameObject orb, Vector3 newPosition)
		{
			float timer = 0f;
			Vector3 startPosition = orb.transform.position;
			if (global::Utils.DistanceXZ(startPosition, newPosition) > 5f)
			{
				duration = 0.5f;
			}
			while (timer < duration)
			{
				timer += Time.deltaTime;
				float tmpTimer = timer / duration;
				tmpTimer = (float)Math.Pow(tmpTimer, 3) * (tmpTimer * (6f * tmpTimer - 15f) + 10f);
				orb.transform.position = Vector3.Lerp(startPosition, newPosition, tmpTimer);

				yield return null;
			}
			
			yield return new WaitForSeconds(Random.Range(0.3f, 1f));
			orbArrived = false;
		}
	}

	public class OrbDestroy : MonoBehaviour
	{
		private ZDO zdo = null!;
		
		private void Awake()
		{
			zdo = GetComponent<ZNetView>().GetZDO();
		}

		private void OnDestroy()
		{
			if (zdo.IsValid() && zdo.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zdo);
				if (Player.m_localPlayer && Player.m_localPlayer.m_seman.HaveStatusEffect(GemEffectSetup.dungeonGuide.name.GetStableHashCode()))
				{
					lightOrb = Instantiate(GemEffectSetup.dungeonGuidePrefab);
					lightOrb.transform.position = Player.m_localPlayer.transform.position;
				}
				else
				{
					lightOrb = null;
				}

				MoveGuide.orbArrived = false;
			}
		}
	}
}
