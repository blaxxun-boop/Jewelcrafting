using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Awareness
{
	private static GameObject heardIcon = null!;
	private static GameObject attackedIcon = null!;

	[HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
	private static class AddAwarenessIcon
	{
		private static void Postfix(Hud __instance)
		{
			heardIcon = Object.Instantiate(GemEffectSetup.heardIcon, __instance.m_rootObject.transform);
			Vector3 localPosition = __instance.m_staggerProgress.transform.localPosition;
			heardIcon.transform.localPosition = localPosition + Vector3.right * 30;
			attackedIcon = Object.Instantiate(GemEffectSetup.attackedIcon, __instance.m_rootObject.transform);
			attackedIcon.transform.localPosition = localPosition + Vector3.right * 30;
		}
	}

	[HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
	private static class DisplayIcon
	{
		private static void Postfix()
		{
			if (Player.m_localPlayer?.m_seman.HaveStatusEffect(GemEffectSetup.awareness.name) != true)
			{
				attackedIcon.gameObject.SetActive(false);
				heardIcon.gameObject.SetActive(false);
				return;
			}

			Vector3 playerPos = Player.m_localPlayer.transform.position;

			if (Character.m_characters.Any(c => Vector3.Distance(playerPos, c.transform.position) < Jewelcrafting.awarenessRange.Value && c.GetComponent<MonsterAI>()?.IsAlerted() == true))
			{
				attackedIcon.gameObject.SetActive(true);
				heardIcon.gameObject.SetActive(false);
			}
			else if (Character.m_characters.Any(c => Vector3.Distance(playerPos, c.transform.position) < Jewelcrafting.awarenessRange.Value && c.GetComponent<MonsterAI>()?.HaveTarget() == true))
			{
				attackedIcon.gameObject.SetActive(false);
				heardIcon.gameObject.SetActive(true);
			}
			else
			{
				attackedIcon.gameObject.SetActive(false);
				heardIcon.gameObject.SetActive(false);
			}
		}
	}
}
