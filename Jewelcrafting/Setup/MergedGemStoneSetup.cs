using System.Collections.Generic;
using ItemManager;
using Jewelcrafting.GemEffects;
using UnityEngine;

namespace Jewelcrafting;

public static class MergedGemStoneSetup
{
	public static readonly GameObject gemList;

	static MergedGemStoneSetup()
	{
		gemList = new GameObject("Jewelcrafting Generated Gems");
		gemList.SetActive(false);
		Object.DontDestroyOnLoad(gemList);
	}
	
	private static readonly Dictionary<GemType, string> colors = new()
	{
		{ GemType.Black, "StoneBlack" },
		{ GemType.Blue, "StoneBlue" },
		{ GemType.Green, "StoneGreen" },
		{ GemType.Purple, "StonePurple" },
		{ GemType.Red, "StoneRed" },
		{ GemType.Yellow, "StoneYellow" },
	};

	private static readonly Dictionary<GemType, Material> colorMaterials = new();

	public static readonly Dictionary<GemType, Dictionary<GemType, GameObject[]>> mergedGems = new();
	public static readonly Dictionary<string, List<GemInfo>> mergedGemContents = new();

	private static readonly List<MergedGemAsset> mergedGemAssets = new();
	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private struct MergedGemAsset
	{
		public GameObject prefab;
		public string localizationPrefix;
		public int tier;
	}
	
	public static void initializeMergedGemStones(AssetBundle assets)
	{
		mergedGemAssets.Add(new MergedGemAsset { prefab = assets.LoadAsset<GameObject>("Common_Merged_Gemstone"), localizationPrefix = "common", tier = 0 });
		mergedGemAssets.Add(new MergedGemAsset { prefab = assets.LoadAsset<GameObject>("Advanced_Merged_Gemstone"), localizationPrefix = "adv", tier = 1 });
		mergedGemAssets.Add(new MergedGemAsset { prefab = assets.LoadAsset<GameObject>("Perfect_Merged_Gemstone"), localizationPrefix = "perfect", tier = 2 });

		foreach (KeyValuePair<GemType, string> kv in colors)
		{
			colorMaterials.Add(kv.Key, assets.LoadAsset<Material>(kv.Value));
		}

		foreach (KeyValuePair<GemType, Color> first in GemStoneSetup.Colors)
		{
			mergedGems[first.Key] = new Dictionary<GemType, GameObject[]>();
			foreach (KeyValuePair<GemType, Color> second in GemStoneSetup.Colors)
			{
				if (first.Key != second.Key)
				{
					CreateMergedGemStone(first, second);
				}
			}
		}
	}

	public static void CreateMergedGemStone(KeyValuePair<GemType, Color> first, KeyValuePair<GemType, Color> second)
	{
		mergedGems[first.Key][second.Key] = new GameObject[mergedGemAssets.Count];
		foreach (MergedGemAsset asset in mergedGemAssets)
		{
			GameObject prefab = Object.Instantiate(asset.prefab, gemList.transform);
			prefab.name = $"{asset.prefab.name}_{EffectDef.GemTypeNames[first.Key]}_{EffectDef.GemTypeNames[second.Key]}";
			void SetColor(string location, KeyValuePair<GemType, Color> kv)
			{
				if (colorMaterials.TryGetValue(kv.Key, out Material material))
				{
					prefab.transform.Find($"attach/{location}").GetComponent<MeshRenderer>().material = material;
				}
				else
				{
					prefab.transform.Find($"attach/{location}").GetComponent<MeshRenderer>().material.color = kv.Value;
					prefab.transform.Find($"attach/{location}").GetComponent<MeshRenderer>().material.SetColor(EmissionColor, kv.Value);
				}
			}
			SetColor("Gem_Mesh_High", first);
			SetColor("Gem_Mesh_Low", second);
			ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
			string name = $"$jc_{asset.localizationPrefix}_merged_gemstone_{EffectDef.GemTypeNames[first.Key].ToLower()}_{EffectDef.GemTypeNames[second.Key].ToLower()}";
			if (!Localization.instance.m_translations.ContainsKey(name))
			{
				name = Localization.instance.Localize($"$jc_{asset.localizationPrefix}_merged_gemstone", $"$jc_merged_gemstone_{EffectDef.GemTypeNames[first.Key].ToLower()}", $"$jc_merged_gemstone_{EffectDef.GemTypeNames[second.Key].ToLower()}");
			}
			itemDrop.m_itemData.m_shared.m_name = name;
			GemStones.socketableGemStones.Add(name);
			ItemSnapshots.SnapshotItems(itemDrop);
			_ = new Item(prefab) { Configurable = Configurability.Disabled };

			mergedGems[first.Key][second.Key][asset.tier] = prefab;
			GemInfo gemInfo(GemType type) => GemStoneSetup.GemInfos[GemStoneSetup.Gems[type][asset.tier].Name];
			mergedGemContents[prefab.name] = new List<GemInfo> { gemInfo(first.Key), gemInfo(second.Key) };
		}
	}
}
