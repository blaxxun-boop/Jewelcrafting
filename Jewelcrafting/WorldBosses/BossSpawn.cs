using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting.WorldBosses;

public static class BossSpawn
{
	private static readonly HashSet<int> playerBasePieces = new();
	private static readonly Dictionary<string, Location> locations = new();
	public static readonly Dictionary<string, Sprite> bossIcons = new();
	private static Text bossTimer = null!;

	public static void SetupBossSpawn()
	{
		foreach (KeyValuePair<string, Sprite> kv in bossIcons)
		{
			GameObject locationObject = new($"Jewelcrafting BossSpawn {kv.Key}");
			locationObject.transform.SetParent(MergedGemStoneSetup.gemList.transform);
			locations.Add(kv.Key, locationObject.AddComponent<Location>());
		}
	}

	[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
	private static class SpawnBossCheck
	{
		private static void Postfix(ZoneSystem __instance)
		{
			if (ZNet.instance.IsServer())
			{
				IEnumerator Check()
				{
					while (true)
					{
						if (Jewelcrafting.bossSpawnTimer.Value <= 0)
						{
							yield return new WaitForSeconds(1);
							continue;
						}

						int oldRemainingTime = int.MaxValue;
						int remainingTime = int.MaxValue - 1;
						while (oldRemainingTime > remainingTime || oldRemainingTime > 50)
						{
							List<KeyValuePair<Vector2i, ZoneSystem.LocationInstance>> locationsToRemove = new();

							foreach (KeyValuePair<Vector2i, ZoneSystem.LocationInstance> location in ZoneSystem.instance.m_locationInstances)
							{
								if (locations.ContainsValue(location.Value.m_location.m_location))
								{
									if (location.Value.m_position.y <= (int)ZNet.instance.GetTimeSeconds())
									{
										locationsToRemove.Add(location);
									}
								}
							}

							if (locationsToRemove.Count > 0)
							{
								foreach (KeyValuePair<Vector2i, ZoneSystem.LocationInstance> location in locationsToRemove)
								{
									List<ZDO> zdos = new();
									ZoneSystem.instance.m_locationInstances.Remove(location.Key);
									ZDOMan.instance.FindObjects(location.Key, zdos);

									double currentTime = ZNet.instance.GetTimeSeconds() + 5;
									foreach (ZDO zdo in zdos)
									{
										if (zdo.GetLong("Jewelcrafting World Boss", long.MaxValue) < currentTime)
										{
											zdo.SetOwner(ZDOMan.instance.GetMyID());
											ZDOMan.instance.DestroyZDO(zdo);
										}
									}
								}

								BroadcastMinimapUpdate();
							}

							yield return new WaitForSeconds(1);
							oldRemainingTime = remainingTime;
							if (Jewelcrafting.bossSpawnTimer.Value > 0)
							{
								remainingTime = Jewelcrafting.bossSpawnTimer.Value * 60 - (int)ZNet.instance.GetTimeSeconds() % (Jewelcrafting.bossSpawnTimer.Value * 60);
							}
						}
						SpawnBoss();
					}
					// ReSharper disable once IteratorNeverReturns
				}
				__instance.StartCoroutine(Check());
			}
		}
	}

	public static void BroadcastMinimapUpdate()
	{
		ZoneSystem.instance.SendLocationIcons(ZRoutedRpc.Everybody);

		if (Minimap.instance)
		{
			Minimap.instance.UpdateLocationPins(10);
		}

	}

	[HarmonyPatch(typeof(Character), nameof(Character.Start))]
	private static class RemoveBossIfMoved
	{
		private static void Postfix(Character __instance)
		{
			if (__instance.m_nview?.GetZDO() is { } zdo && zdo.GetLong("Jewelcrafting World Boss", long.MaxValue) < ZNet.instance.GetTimeSeconds())
			{
				IEnumerator DestroyCharacter()
				{
					yield return null;
					ZNetScene.instance.Destroy(__instance.gameObject);
				}
				__instance.StartCoroutine(DestroyCharacter());
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
	private static class AddRPCs
	{
		private static void Postfix(ZNet __instance, ZNetPeer peer)
		{
			if (__instance.IsServer())
			{
				peer.m_rpc.Register<int, int>("Jewelcrafting BossDied", (_, sectorX, sectorY) => HandleBossDeath(new Vector2i(sectorX, sectorY)));
			}
		}
	}

	private static void HandleBossDeath(Vector2i sector)
	{
		ZoneSystem.instance.m_locationInstances.Remove(sector);
		BroadcastMinimapUpdate();
	}

	[HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
	private static class RemoveOnDeath
	{
		private static void Prefix(Character __instance)
		{
			if (__instance.m_nview.GetZDO().GetLong("Jewelcrafting World Boss") > 0)
			{
				Vector2i sector = ZoneSystem.instance.GetZone(__instance.m_baseAI.m_spawnPoint);
				if (ZNet.instance.IsServer())
				{
					HandleBossDeath(sector);
				}
				else
				{
					ZNet.instance.GetServerPeer().m_rpc.Invoke("Jewelcrafting BossDied", sector.x, sector.y);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
	private static class InsertMinimapIcon
	{
		private static void Postfix(Minimap __instance)
		{
			foreach (KeyValuePair<string, Sprite> kv in bossIcons)
			{
				__instance.m_locationIcons.Add(new Minimap.LocationSpriteData { m_icon = kv.Value, m_name = locations[kv.Key].name });
			}

			bossTimer = Object.Instantiate(__instance.m_largeRoot.transform.Find("KeyHints/keyboard_hints/AddPin/Label"), __instance.m_largeRoot.transform).GetComponent<Text>();
			bossTimer.name = "Jewelcrafting Boss Timer";
			bossTimer.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
			bossTimer.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
			UpdateBossTimerPosition();
			UpdateBossTimerVisibility();
			IEnumerator Check()
			{
				while (true)
				{
					if (Jewelcrafting.bossSpawnTimer.Value > 0)
					{
						int nextBossSpawn = Jewelcrafting.bossSpawnTimer.Value * 60 - (int)ZNet.instance.GetTimeSeconds() % (Jewelcrafting.bossSpawnTimer.Value * 60) - 1;
						bossTimer.text = Localization.instance.Localize("$jc_gacha_world_boss_spawn", TimeSpan.FromSeconds(nextBossSpawn).ToString("c"));

						foreach (Minimap.PinData pin in __instance.m_pins.Where(p => bossIcons.ContainsValue(p.m_icon)))
						{
							pin.m_name = TimeSpan.FromSeconds((int)pin.m_pos.y - (int)ZNet.instance.GetTimeSeconds()).ToString("c");
							if (pin.m_NamePinData is null)
							{
								pin.m_NamePinData = new Minimap.PinNameData(pin);
								if (__instance.IsPointVisible(pin.m_pos, __instance.m_mapImageLarge))
								{
									__instance.CreateMapNamePin(pin, __instance.m_pinNameRootLarge);
								}
							}
							if (pin.m_NamePinData.PinNameGameObject)
							{
								pin.m_NamePinData.PinNameText.text = pin.m_name;
							}
						}
					}

					yield return new WaitForSeconds(1);
				}
				// ReSharper disable once IteratorNeverReturns
			}
			__instance.StartCoroutine(Check());
		}
	}

	public static void UpdateBossTimerVisibility()
	{
		if (bossTimer)
		{
			bossTimer.gameObject.SetActive(Jewelcrafting.bossSpawnTimer.Value > 0);
		}
	}

	public static void UpdateBossTimerPosition()
	{
		if (Minimap.instance)
		{
			RectTransform rect = (RectTransform)Minimap.instance.m_largeRoot.transform.Find("IconPanel").transform;
			Vector2 anchoredPosition = rect.anchoredPosition;
			bossTimer.GetComponent<RectTransform>().anchoredPosition = new Vector2(-anchoredPosition.x - 30, -anchoredPosition.y -5 - Jewelcrafting.worldBossCountdownDisplayOffset.Value);
			bossTimer.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
			bossTimer.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
			bossTimer.GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;
			bossTimer.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
		}
	}

	private static DateTime lastBossSpawn = DateTime.MinValue;

	public static void SpawnBoss()
	{
		if (lastBossSpawn != ZNet.instance.GetTime() && GetRandomSpawnPoint() is { } pos)
		{
			long despawnTime = ZNet.instance.GetTime().AddMinutes(Jewelcrafting.bossTimeLimit.Value).Ticks / 10000000L;

			string boss = bossIcons.Keys.ToList()[Random.Range(0, bossIcons.Count)];

			ZoneSystem.instance.RegisterLocation(new ZoneSystem.ZoneLocation
			{
				m_iconAlways = true,
				m_prefabName = locations[boss].name,
				m_location = locations[boss]
			}, pos with { y = despawnTime }, true);

			ZDO zdo = ZDOMan.instance.CreateNewZDO(pos);
			zdo.m_persistent = true;
			zdo.SetPrefab(boss.GetStableHashCode());
			zdo.Set("Jewelcrafting World Boss", despawnTime);

			lastBossSpawn = ZNet.instance.GetTime();

			BroadcastMinimapUpdate();
		}
	}

	[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_LocationIcons))]
	private static class UpdateLocationIcons
	{
		private static void Postfix()
		{
			if (Minimap.instance && Player.m_localPlayer?.IsDead() == false)
			{
				Minimap.instance.UpdateLocationPins(10);
			}
		}
	}

	private static Vector3? GetRandomSpawnPoint()
	{
		for (int i = 0; i < 10000; ++i)
		{
			retry:
			Vector2 randomPoint = Random.insideUnitCircle * Jewelcrafting.bossSpawnMaxDistance.Value;
			Vector3 point = new(randomPoint.x, 0, randomPoint.y);

			if (global::Utils.DistanceXZ(Vector3.zero, point) < Jewelcrafting.bossSpawnMinDistance.Value)
			{
				continue;
			}

			Heightmap.Biome biome = WorldGenerator.instance.GetBiome(point.x, point.z);
			float biomeHeight = WorldGenerator.instance.GetBiomeHeight(biome, point.x, point.z, out _);
			float forestFactor = Minimap.instance.GetMaskColor(point.x, point.z, biomeHeight, biome).r;

			if (biomeHeight < ZoneSystem.instance.m_waterLevel + 5 || forestFactor > 0.75 || (biome == Heightmap.Biome.AshLands && Random.Range(1, 6) != 1) || (biome == Heightmap.Biome.DeepNorth && Random.Range(1, 7) > 2))
			{
				continue;
			}

			int baseValue = 0;
			Vector2i sector = ZoneSystem.instance.GetZone(point);

			if (ZoneSystem.instance.m_locationInstances.ContainsKey(sector))
			{
				continue;
			}

			for (int j = 0; j < 10; ++j)
			{
				Vector2 circle = Random.insideUnitCircle * j;
				if (Mathf.Abs(biomeHeight - WorldGenerator.instance.GetBiomeHeight(biome, point.x + circle.x, point.z + circle.y, out _)) > 5)
				{
					goto retry;
				}
			}

			if (WorldGenerator.instance.GetBiomeArea(point) == Heightmap.BiomeArea.Edge)
			{
				continue;
			}
			
			List<ZDO> zdos = new();
			for (int y = -1; y <= 1; ++y)
			{
				for (int x = -1; x <= 1; ++x)
				{
					zdos.Clear();
					ZDOMan.instance.FindObjects(sector + new Vector2i(x, y), zdos);
					foreach (ZDO zdo in zdos)
					{
						if (playerBasePieces.Contains(zdo.m_prefab) && global::Utils.DistanceXZ(zdo.m_position, point) < Jewelcrafting.bossSpawnBaseDistance.Value)
						{
							++baseValue;
						}
					}
				}
			}
			if (baseValue > 1)
			{
				continue;
			}

			return point with { y = WorldGenerator.instance.GetHeight(point.x, point.z) };
		}

		return null;
	}

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private static class CachePrefabs
	{
		private static void Postfix(ZNetScene __instance)
		{
			foreach (GameObject prefab in __instance.m_prefabs.Where(prefab => prefab.GetComponent<EffectArea>()?.m_type == EffectArea.Type.PlayerBase))
			{
				playerBasePieces.Add(prefab.name.GetStableHashCode());
			}
		}
	}
}
