using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using ItemManager;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting;

public static class DestructibleSetup
{
	public static readonly Dictionary<GemType, GameObject> destructibles = new();
	public static readonly HashSet<string> hpModifiableTypes = new();
	public static GameObject gemSpawner = null!;
	public static GameObject customDestructiblePrefab = null!;

	public static void AddDestructible(GameObject prefab, GemType type)
	{
		PrefabManager.RegisterPrefab(prefab);
		destructibles.Add(type, prefab);
		if (Regex.IsMatch(prefab.name, "Raw_.*_Gemstone"))
		{
			hpModifiableTypes.Add(prefab.name);
			prefab.GetComponent<Destructible>().m_health = Jewelcrafting.gemstoneFormationHealth.Value;
		}

		prefab.GetComponent<DropOnDestroyed>().m_dropWhenDestroyed.m_drops = new List<DropTable.DropData>
		{
			new()
			{
				m_item = GemStoneSetup.uncutGems[type],
				m_weight = 1,
				m_stackMin = 1,
				m_stackMax = 1,
			},
		};

		if (prefab.transform.Find("Orbs") is { } orbs)
		{
			orbs.gameObject.SetActive(Jewelcrafting.gemstoneFormationParticles.Value == Jewelcrafting.Toggle.On);
		}
	}

	public static GameObject CreateDestructibleFromTemplate(GameObject template, string type, MaterialColor materialColor)
	{
		GameObject prefab = Object.Instantiate(template, MergedGemStoneSetup.gemList.transform);
		prefab.name = template.name.Replace("Custom", type);
		prefab.GetComponent<HoverText>().m_text = $"$jc_raw_{type.ToLower()}_gemstone";
		foreach (MeshRenderer renderer in prefab.transform.Find("collider").GetComponentsInChildren<MeshRenderer>())
		{
			if (materialColor.Material is { } material)
			{
				renderer.material = material;
			}
			else
			{
				renderer.material.color = materialColor.Color;
			}
		}
		return prefab;
	}

	public static void initializeDestructibles(AssetBundle assets)
	{
		customDestructiblePrefab = assets.LoadAsset<GameObject>("Raw_Custom_Gemstone");

		foreach (KeyValuePair<GemType, MaterialColor> color in GemStoneSetup.Colors)
		{
			string destructibleAssetName = customDestructiblePrefab.name.Replace("Custom", color.Key.ToString());
			AddDestructible(assets.Contains(destructibleAssetName) ? assets.LoadAsset<GameObject>(destructibleAssetName) : CreateDestructibleFromTemplate(customDestructiblePrefab, color.Key.ToString(), color.Value), color.Key);
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
			gemSpawner.AddComponent<SphereCollider>().radius = 1.5f; // prevent spawning other things on top

			__instance.m_prefabs.Add(gemSpawner);
		}
	}
	
	public class GemSpawner : MonoBehaviour
	{
		public ZNetView netView = null!;
		public static readonly List<GemSpawner> activeSpawners = new();
		private Heightmap.Biome biome;

		public void Awake()
		{
			netView = GetComponent<ZNetView>();
			if (!netView)
			{
				return;
			}

			biome = Heightmap.FindBiome(transform.position);

			if (netView.GetZDO().GetFloat("spawn random", -1) == -1)
			{
				float random = Random.value;
				if (activeSpawners.FirstOrDefault(s => Vector3.Distance(s.transform.position, transform.position) < 3) is { } neighbor)
				{
					random = neighbor.netView.GetZDO().GetFloat("spawn random");
				}
				netView.GetZDO().Set("spawn random", random);
			}

			activeSpawners.Add(this);
		}

		public void Start()
		{
			if (netView)
			{
				Destroy(GetComponent<SphereCollider>());
				InvokeRepeating(nameof(CheckSpawn), netView.GetZDO()?.GetLong("destruction time") > 0 ? 5 : 0, 5);
			}
		}

		public void CheckSpawn()
		{
			ZDO zdo = netView.GetZDO();
			if (!zdo.IsOwner())
			{
				return;
			}

			ZDOID gemId = zdo.GetZDOID("spawn gem");

			GameObject? prefab = null;
			if (Jewelcrafting.gemstoneFormations.Value == Jewelcrafting.Toggle.On)
			{
				if (!Jewelcrafting.GemDistribution.TryGetValue(biome, out Dictionary<GemType, float> gemChances))
				{
					return;
				}

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
				if (lastDestruction != 0 && (Jewelcrafting.gemRespawnRate.Value == 0 || (ZNet.instance.GetTime() - new DateTime(lastDestruction)).TotalSeconds < Jewelcrafting.gemRespawnRate.Value * EnvMan.instance.m_dayLengthSec))
				{
					return;
				}
			}

			if (Physics.SphereCast(transform.position + Vector3.down * 10, 1.5f, Vector3.up, out _, 12f, ZoneSystem.instance.m_blockRayMask) || EffectArea.IsPointInsideArea(transform.position, EffectArea.Type.PlayerBase, 30f))
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
				m_biome = (Heightmap.Biome)(-1),
				m_groupRadius = 6f,
				m_groupSizeMin = 2,
				m_groupSizeMax = 6,
				m_minAltitude = 0,
				m_forcePlacement = true,
				m_max = 2,
				m_prefab = gemSpawner,
			});
		}
	}
}
