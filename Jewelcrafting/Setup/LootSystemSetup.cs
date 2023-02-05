using HarmonyLib;
using ItemDataManager;
using UnityEngine;

namespace Jewelcrafting.Setup;

public static class LootSystemSetup
{
	public static GameObject lootBeam = null!;
	
	public static void initializeLootSystem(AssetBundle assets)
	{
		lootBeam = assets.LoadAsset<GameObject>("JC_Loot_Effect");
	}

	[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
	public static class AddLootBeam
	{
		public static void Postfix(ItemDrop __instance)
		{
			if (Jewelcrafting.lootBeams.Value == Jewelcrafting.Toggle.On && __instance.m_itemData.Data().Get<Sockets>() is { } sockets)
			{
				GameObject beam = Object.Instantiate(lootBeam, __instance.transform);
				ParticleSystem.MainModule mainModule = beam.transform.Find("Beam").GetComponent<ParticleSystem>().main;
				mainModule.startColor = SocketsBackground.ItemColor(sockets);
			}
		}
	}
}
