using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ItemManager;
using Jewelcrafting.GemEffects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jewelcrafting;

public static class DestructibleSetup
{
	private static readonly Dictionary<GemType, GameObject> destructibles = new();
	private static GameObject gemSpawner = null!;

	public static void initializeDestructibles(AssetBundle assets)
	{
		destructibles.Add(GemType.Black, PrefabManager.RegisterPrefab(assets, "Raw_Black_Gemstone"));
		destructibles.Add(GemType.Blue, PrefabManager.RegisterPrefab(assets, "Raw_Blue_Gemstone"));
		destructibles.Add(GemType.Green, PrefabManager.RegisterPrefab(assets, "Raw_Green_Gemstone"));
		destructibles.Add(GemType.Purple, PrefabManager.RegisterPrefab(assets, "Raw_Purple_Gemstone"));
		destructibles.Add(GemType.Red, PrefabManager.RegisterPrefab(assets, "Raw_Red_Gemstone"));
		destructibles.Add(GemType.Yellow, PrefabManager.RegisterPrefab(assets, "Raw_Yellow_Gemstone"));

		foreach (GameObject destructible in destructibles.Values)
		{
			destructible.AddComponent<DestructibleGem>();
		}
	}

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private static class RegisterGemSpawner
	{
		private static void Prefix(ZNetScene __instance)
		{
			GameObject gemSpawnerContainer = new("JewelCrafting GemStoneSpawner Container") { hideFlags = HideFlags.HideInHierarchy };
			gemSpawnerContainer.SetActive(false);

			gemSpawner = new GameObject("GemstoneSpawner");
			gemSpawner.transform.SetParent(gemSpawnerContainer.transform);
			gemSpawner.AddComponent<GemSpawner>();
			ZNetView netView = gemSpawner.AddComponent<ZNetView>();
			netView.m_persistent = true;
			netView.m_type = ZDO.ObjectType.Terrain;
			gemSpawner.AddComponent<SphereCollider>().radius = 0.4f; // prevent spawning other things on top

			__instance.m_prefabs.Add(gemSpawner);
		}
	}

	public class DestructibleGem : MonoBehaviour
	{
		private void Start()
		{
			if (Jewelcrafting.socketSystem.Value == Jewelcrafting.Toggle.Off)
			{
				gameObject.SetActive(false);
			}
		}
	}

	public class GemSpawner : MonoBehaviour
	{
		private ZNetView netView = null!;
		private static readonly List<GemSpawner> activeSpawners = new();
		private Heightmap.Biome biome;

		public void Awake()
		{
			netView = GetComponent<ZNetView>();
			biome = Heightmap.FindBiome(transform.position);

			if (netView.GetZDO().GetFloat("spawn random", -1) == -1)
			{
				float random = Random.value;
				if (activeSpawners.FirstOrDefault(s => Vector3.Distance(s.transform.position, transform.position) < 2) is { } neighbor)
				{
					random = neighbor.netView.GetZDO().GetFloat("spawn random");
				}
				netView.GetZDO().Set("spawn random", random);
			}

			activeSpawners.Add(this);
		}

		public void Start()
		{
			Destroy(GetComponent<SphereCollider>());
			InvokeRepeating(nameof(CheckSpawn), 0f, 5f);
		}

		public void CheckSpawn()
		{
			ZDO zdo = netView.GetZDO();
			if (!zdo.IsOwner())
			{
				return;
			}

			ZDOID gemId = zdo.GetZDOID("spawn gem");
			if (!Jewelcrafting.GemDistribution.TryGetValue(biome, out Dictionary<GemType, float> gemChances))
			{
				return;
			}

			GameObject? prefab = null;
			float random = netView.GetZDO().GetFloat("spawn random");
			foreach (KeyValuePair<GemType, float> kv in gemChances)
			{
				if (kv.Value > random)
				{
					prefab = destructibles[kv.Key];
					break;
				}
				random -= kv.Value;
			}

			if (gemId != ZDOID.None)
			{
				if (ZNetScene.instance.FindInstance(gemId) is { } existingDestructible)
				{
					if (global::Utils.GetPrefabName(existingDestructible) == prefab?.name)
					{
						return;
					}
					ZNetScene.instance.Destroy(existingDestructible);
					zdo.Set("spawn gem", ZDOID.None);
					if (prefab is null)
					{
						return;
					}
				}
				else
				{
					zdo.Set("destruction time", ZNet.instance.GetTime().Ticks);
					zdo.Set("spawn gem", ZDOID.None);
					return;
				}
			}
			else
			{
				if (prefab is null)
				{
					return;
				}

				long lastDestruction = netView.GetZDO().GetLong("destruction time");
				if (lastDestruction != 0 && (ZNet.instance.GetTime() - new DateTime(lastDestruction)).TotalSeconds < Jewelcrafting.gemRespawnRate.Value * EnvMan.instance.m_dayLengthSec)
				{
					return;
				}
			}

			if (Physics.SphereCast(transform.position + Vector3.down * 10, 0.4f, Vector3.up, out _, 12f, ZoneSystem.instance.m_blockRayMask))
			{
				return;
			}
			
			GameObject destructible = Instantiate(prefab, transform.position, Quaternion.Euler(0, Random.Range(0, 360), 0));
			zdo.Set("spawn gem", destructible.GetComponent<ZNetView>().GetZDO().m_uid);
		}

		private void OnDestroy()
		{
			activeSpawners.Remove(this);
		}
	}

	[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.ValidateVegetation))]
	public class AddDestructiblesToZoneSystem
	{
		public static void Prefix(ZoneSystem __instance)
		{
			__instance.m_vegetation.Add(new ZoneSystem.ZoneVegetation
			{
				m_biome = Heightmap.Biome.BiomesMax,
				m_groupRadius = 6f,
				m_groupSizeMin = 2,
				m_groupSizeMax = 6,
				m_minAltitude = 0,
				m_max = 2,
				m_prefab = gemSpawner
			});
		}
	}
}
