﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using ItemManager;
using Jewelcrafting.Setup;
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
		prefab.AddComponent<CountDestructibleDestruction>();
		prefab.AddComponent<ScaledDestructible>();
		prefab.AddComponent<VisualSetup.RuntimeTextureReducer>();

		if (prefab.transform.Find("Orbs") is { } orbs)
		{
			orbs.gameObject.SetActive(Jewelcrafting.gemstoneFormationParticles.Value == Jewelcrafting.Toggle.On);
		}
	}

	public class ScaledDestructible : MonoBehaviour
	{
		public int destructibleDrops = 1;

		public static readonly HashSet<GameObject> activeDestructibles = new();
		
		public void Awake()
		{
			activeDestructibles.Add(gameObject);
			
			Random.State state = Random.state;
			Random.InitState(transform.position.magnitude.ToString(CultureInfo.InvariantCulture).GetStableHashCode());

			if (Jewelcrafting.bigGemstoneFormationChance.Value / 100f > Random.value)
			{
				destructibleDrops = Math.Min(Random.Range(2, 11), Random.Range(2, 11));
				transform.localScale *= destructibleDrops / 1.8f;
				List<DropTable.DropData> drops = GetComponent<DropOnDestroyed>().m_dropWhenDestroyed.m_drops;
				DropTable.DropData drop = drops[0];
				drop.m_stackMin *= destructibleDrops;
				drop.m_stackMax *= destructibleDrops;
				drops[0] = drop;
				GetComponent<Destructible>().m_health *= destructibleDrops;
			}
			
			Random.state = state;
		}

		public void OnDestroy()
		{
			activeDestructibles.Remove(gameObject);
		}
	}

	private class CountDestructibleDestruction: MonoBehaviour
	{
		public void Awake() => GetComponent<Destructible>().m_onDestroyed += () => Stats.destructiblesDestroyed.Increment();
	}

	[HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList), new Type[0])]
	private static class CountGemsDroppedFromDestructible
	{
		[HarmonyPriority(Priority.Last)]
		private static void Postfix(List<GameObject> __result)
		{
			foreach (GameObject gameObject in __result)
			{
				if (GemStoneSetup.uncutGems.ContainsValue(gameObject))
				{
					Stats.gemsDroppedDestructible.Increment();
				}
			}
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
			gemSpawner.AddComponent<SphereCollider>().radius = 1.2f + 0.7f; // prevent spawning other things on top

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

			if (Physics.SphereCast(transform.position + Vector3.down * 10, 1.2f, Vector3.up, out _, 12f, ZoneSystem.instance.m_blockRayMask) || EffectArea.IsPointInsideArea(transform.position, EffectArea.Type.PlayerBase, 30f))
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
			ZoneSystem.ZoneVegetation template() => new()
			{
				m_groupRadius = 7f,
				m_groupSizeMin = 2,
				m_groupSizeMax = 6,
				m_minAltitude = 0,
				m_forcePlacement = true,
				m_max = 4,
				m_prefab = gemSpawner,
			};

			ZoneSystem.ZoneVegetation generic = template();
			generic.m_biome = ~Heightmap.Biome.AshLands;
			__instance.m_vegetation.Add(generic);

			ZoneSystem.ZoneVegetation ashlandsBorder = template();
			ashlandsBorder.m_biome = Heightmap.Biome.AshLands;
			ashlandsBorder.m_biomeArea = Heightmap.BiomeArea.Edge;
			__instance.m_vegetation.Add(ashlandsBorder);

			ZoneSystem.ZoneVegetation ashlandsNoLava = template();
			ashlandsNoLava.m_biome = Heightmap.Biome.AshLands;
			ashlandsNoLava.m_biomeArea = Heightmap.BiomeArea.Median;
			ashlandsNoLava.m_maxVegetation = 0.5f;
			ashlandsNoLava.m_max += 1;
			__instance.m_vegetation.Add(ashlandsNoLava);
		}
	}
}
