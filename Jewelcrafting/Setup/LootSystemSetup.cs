using HarmonyLib;
using ItemDataManager;
using ItemManager;
using UnityEngine;

namespace Jewelcrafting.Setup;

public static class LootSystemSetup
{
	public static GameObject lootBeam = null!;
	public static GameObject[] gemChests = null!;
	public static GameObject[] equipmentChests = null!;

	public static void initializeLootSystem(AssetBundle assets)
	{
		lootBeam = assets.LoadAsset<GameObject>("JC_Loot_Effect");
		gemChests = new[]
		{
			new Item(assets, "JC_Blue_Chest")
			{
				Configurable = Configurability.Disabled,
			}.Prefab,
			new Item(assets, "JC_Purple_Chest")
			{
				Configurable = Configurability.Disabled,
			}.Prefab,
			new Item(assets, "JC_Orange_Chest")
			{
				Configurable = Configurability.Disabled,
			}.Prefab,
		};
		
		equipmentChests = new[]
		{
			new Item(assets, "JC_Blue_Item_Chest")
			{
				Configurable = Configurability.Disabled,
			}.Prefab,
			new Item(assets, "JC_Purple_Item_Chest")
			{
				Configurable = Configurability.Disabled,
			}.Prefab,
			new Item(assets, "JC_Orange_Item_Chest")
			{
				Configurable = Configurability.Disabled,
			}.Prefab,
		};

		foreach (GameObject[] objects in new[] { gemChests, equipmentChests })
		{
			attachBeam(objects[0], Color.cyan);
			attachBeam(objects[1], Color.magenta);
			attachBeam(objects[2], new Color(1, 0.6f, 0));
		}
	}

	private static void attachBeam(GameObject item, Color color)
	{
		GameObject beam = Object.Instantiate(lootBeam);
		// ReSharper disable once Unity.InstantiateWithoutParent
		beam.transform.SetParent(item.transform, false);
		ParticleSystem.MainModule mainModule = beam.transform.Find("Beam").GetComponent<ParticleSystem>().main;
		mainModule.startColor = color;
	}

	[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
	public static class AddLootBeam
	{
		public static void Postfix(ItemDrop __instance)
		{
			if (Jewelcrafting.lootBeams.Value == Jewelcrafting.Toggle.On)
			{
				if (__instance.m_itemData.Data().Get<Sockets>() is { } sockets)
				{
					attachBeam(__instance.gameObject, SocketsBackground.ItemColor(sockets));
				}
			}
		}
	}
}
