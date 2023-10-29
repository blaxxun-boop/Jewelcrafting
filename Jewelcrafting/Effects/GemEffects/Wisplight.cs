using System.Collections;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class Wisplight
{
	static Wisplight()
	{
		API.OnEffectRecalc += () =>
		{
			if (Jewelcrafting.wisplightGem.Value == Jewelcrafting.Toggle.On)
			{
				if (Player.m_localPlayer.GetEffect(Effect.Wisplight) == 0)
				{
					Player.m_localPlayer.m_seman.RemoveStatusEffect("Demister".GetStableHashCode());
				}
				else
				{
					Player.m_localPlayer.m_seman.AddStatusEffect("Demister".GetStableHashCode());
				}
			}
		};
	}
	
	private class SetForceField : MonoBehaviour
	{
		private ParticleSystemForceField forceField = null!;
		
		public void Awake()
		{
			forceField = transform.Find("effects/Particle System Force Field").GetComponent<ParticleSystemForceField>();
			
			if (GetComponent<ZNetView>().IsOwner())
			{
				GetComponent<ZNetView>().GetZDO().Set("Jewelcrafting Wisplight Owner", ZDOMan.instance.m_sessionID);
			}
		}

		public void Start()
		{
			long id = GetComponent<ZNetView>().GetZDO().GetLong("Jewelcrafting Wisplight Owner");
			if (Player.s_players.Find(p => p.GetZDOID().UserID == id) is { } player)
			{
				IEnumerator SetRange()
				{
					for (;;)
					{
						if (player.GetEffect(Effect.Wisplight) is { } effect and > 0)
						{
							forceField.endRange = effect;
						}
						yield return new WaitForSeconds(1);
					}
					// ReSharper disable once IteratorNeverReturns
				}
				StartCoroutine(SetRange());
			}
		}
	}

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private static class AddForceField
	{
		private static void Postfix(ZNetScene __instance)
		{
			GameObject wisplight = __instance.GetPrefab("demister_ball");
			if (!wisplight.GetComponent<SetForceField>())
			{
				wisplight.AddComponent<SetForceField>();
			}
		}
	}
}
