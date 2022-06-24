using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class GlowingSpirit
{
	private static GameObject? lightOrb;

	static GlowingSpirit()
	{
		TrackEquipmentChanges.OnEffectRecalc += () =>
		{
			if (Player.m_localPlayer.GetEffect(Effect.Glowingspirit) == 0)
			{
				Player.m_localPlayer.m_seman.RemoveStatusEffect(Jewelcrafting.glowingSpirit);
			}
		};
	}

	[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.OnEvening))]
	private static class SummonSpirit
	{
		private static void Postfix()
		{
			if (Player.m_localPlayer.GetEffect(Effect.Glowingspirit) > 0)
			{
				Jewelcrafting.glowingSpirit.m_ttl = Player.m_localPlayer.GetEffect(Effect.Glowingspirit) * 60;
				Player.m_localPlayer.m_seman.AddStatusEffect(Jewelcrafting.glowingSpirit, true);
				if (!lightOrb)
				{
					lightOrb = Object.Instantiate(Jewelcrafting.glowingSpiritPrefab);
					lightOrb.transform.position = Player.m_localPlayer.transform.position;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Update))]
	private static class MoveSpirit
	{
		private static bool orbArrived = false;
		private static float duration = 2f;

		private static void Postfix(Player __instance)
		{
			if (__instance != Player.m_localPlayer || !lightOrb || orbArrived)
			{
				return;
			}

			if (!__instance.m_seman.HaveStatusEffect(Jewelcrafting.glowingSpirit.name))
			{
				ZNetScene.instance.Destroy(lightOrb);
				lightOrb = null;
				return;
			}
			
			orbArrived = true;
			Vector3 offset = __instance.transform.position.y > 4500 ? new Vector3(Random.Range(-1f, 1f), Random.Range(-2f, 0.5f), Random.Range(-1f, 1f)) : new Vector3(Random.Range(-3f, 3f), Random.Range(-1.5f, 1.5f), Random.Range(-3f, 3f));
			Vector3 newPosition = __instance.transform.position + offset;
			duration = Random.Range(1f, 3f);

			__instance.StartCoroutine(MoveOrb(newPosition));
		}

		private static IEnumerator MoveOrb(Vector3 newPosition)
		{
			if (lightOrb is null)
			{
				yield break;
			}
			
			float timer = 0f;
			Vector3 startPosition = lightOrb.transform.position;
			if (global::Utils.DistanceXZ(startPosition, newPosition) > 5f)
			{
				duration = 0.5f;
			}
			while (timer < duration && lightOrb)
			{
				timer += Time.deltaTime;
				float tmpTimer = timer / duration;
				tmpTimer = (float)Math.Pow(tmpTimer, 3) * (tmpTimer * (6f * tmpTimer - 15f) + 10f);
				lightOrb.transform.position = Vector3.Lerp(startPosition, newPosition, tmpTimer);

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
				if (Player.m_localPlayer && Player.m_localPlayer.GetEffect(Effect.Glowingspirit) > 0)
				{
					lightOrb = Instantiate(Jewelcrafting.glowingSpiritPrefab);
					lightOrb.transform.position = Player.m_localPlayer.transform.position;
				}
				else
				{
					lightOrb = null;
				}
			}
		}
	}
}
