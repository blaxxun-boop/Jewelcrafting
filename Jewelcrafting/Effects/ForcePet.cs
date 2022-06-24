using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public class ForcePet : MonoBehaviour
{
	private static readonly List<string> possiblePets = new();
	
	private ZNetView netView = null!;
	
	public void Awake()
	{
		netView = GetComponent<ZNetView>();
		if (netView.GetZDO()?.GetFloat("Jewelcrafting Pet TTL") > 0)
		{
			ConvertToPet();
		}
	}

	public void MakePet(float ttl)
	{
		if (netView.GetZDO() is { } zdo)
		{
			zdo.Set("Jewelcrafting Pet TTL", ttl + (float)ZNet.instance.m_netTime);
			zdo.Set("tamed", true);
			
			GetComponent<Character>().m_tamed = true;
			ConvertToPet();
		}
	}

	private void ConvertToPet()
	{
		if (GetComponent<Tameable>() is null)
		{
			gameObject.AddComponent<Tameable>();
		}
		gameObject.AddComponent<CharacterTimedDestruction>().Trigger(netView.GetZDO().GetFloat("Jewelcrafting Pet TTL") - (float)ZNet.instance.m_netTime);
		Destroy(GetComponent<CharacterDrop>());
	}

	public static void RegisterPet(string name) => possiblePets.Add(name);

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private static class RegisterPets
	{
		[HarmonyPriority(Priority.VeryLow)]
		private static void Postfix(ZNetScene __instance)
		{
			foreach (string pet in possiblePets)
			{
				__instance.GetPrefab(pet).AddComponent<ForcePet>();
			}
		}
	}
}
