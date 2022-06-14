using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jewelcrafting;

public static class GenerateVegetationSpawners
{
	public static void RPC_GenerateVegetation(ZRpc? peer)
	{
		if (Utils.isAdmin(peer))
		{
			ZoneSystem.instance.StartCoroutine(GenerateVegetation(peer));
		}
	}

	private static IEnumerator GenerateVegetation(ZRpc? peer)
	{
		ZoneSystem zoneSystem = ZoneSystem.instance;
		int spawnerPrefab = DestructibleSetup.gemSpawner.name.GetStableHashCode();
		List<Vector2i> zones = new(zoneSystem.m_generatedZones);
		ZNet.instance.RemotePrint(peer, $"Starting to generate destructible gems for {zones.Count} zones. This can take a long time.");
		int zoneNum = 0;
		foreach (Vector2i zone in zones)
		{
			List<ZDO> zdos = new();
			ZDOMan.instance.FindObjects(zone, zdos);

			if (zdos.All(z => z.m_prefab != spawnerPrefab))
			{
				bool oldForceDisableTerrainOps = TerrainOp.m_forceDisableTerrainOps;
				TerrainOp.m_forceDisableTerrainOps = true;

				List<GameObject> tempSpawnedObjects = new();
				ZNetView.StartGhostInit();
				Dictionary<ZDO, ZNetView> zdoDict = ZNetScene.instance.m_instances;
				tempSpawnedObjects.AddRange(zdos.Where(z => !zdoDict.ContainsKey(z)).Select(ZNetScene.instance.CreateObject));
				ZNetView.FinishGhostInit();

				List<ZoneSystem.ZoneVegetation> originalVegetation = zoneSystem.m_vegetation;
				zoneSystem.m_vegetation = new List<ZoneSystem.ZoneVegetation>();
				DestructibleSetup.AddDestructiblesToZoneSystem.Prefix(zoneSystem);

				Vector3 zonePos = zoneSystem.GetZonePos(zone);
				GameObject root = Object.Instantiate(zoneSystem.m_zonePrefab, zonePos, Quaternion.identity);
				tempSpawnedObjects.Add(root);
				zoneSystem.PlaceVegetation(zone, zonePos, root.transform, root.GetComponentInChildren<Heightmap>(), new List<ZoneSystem.ClearArea>(), ZoneSystem.SpawnMode.Ghost, tempSpawnedObjects);

				foreach (GameObject tempObj in tempSpawnedObjects)
				{
					if (!tempObj)
					{
						continue;
					}

					if (tempObj.GetComponent<ZNetView>() is { } netView && netView.GetZDO() is { } zdo)
					{
						netView.ResetZDO();
						zdoDict.Remove(zdo);
					}
					Object.Destroy(tempObj);
				}

				zoneSystem.m_vegetation = originalVegetation;
				TerrainOp.m_forceDisableTerrainOps = oldForceDisableTerrainOps;
			}

			if (++zoneNum % 100 == 0)
			{
				ZNet.instance.RemotePrint(peer, $"Gem destructibles creation: Processed {zoneNum}/{zones.Count} zones");
			}
			yield return null;
		}

		ZNet.instance.RemotePrint(peer, "Destructible gems have been generated for this world.");
	}
}
