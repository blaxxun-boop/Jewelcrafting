using System.Collections.Generic;
using System.Linq;
using ItemManager;
using Jewelcrafting.GemEffects;
using UnityEngine;

namespace Jewelcrafting;

public static class MergedGemStoneSetup
{
	private static readonly Dictionary<GemType, string> colors = new()
	{
		{ GemType.Black, "StoneBlack" },
		{ GemType.Blue, "StoneBlue" },
		{ GemType.Green, "StoneGreen" },
		{ GemType.Purple, "StonePurple" },
		{ GemType.Red, "StoneRed" },
		{ GemType.Yellow, "StoneYellow" }
	};

	public static readonly Dictionary<GemType, Dictionary<GemType, GameObject[]>> mergedGems = colors.Keys.ToDictionary(first => first, first => colors.Keys.Where(second => first != second).ToDictionary(second => second, _ => new GameObject[3]));

	public static void initializeMergedGemStones(AssetBundle assets)
	{
		GameObject gemList = new("Jewelcrafting Merged Gems");
		gemList.SetActive(false);
		Object.DontDestroyOnLoad(gemList);

		const int layer = 30;

		Camera camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
		camera.backgroundColor = Color.clear;
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.fieldOfView = 0.5f;
		camera.farClipPlane = 10000000;
		camera.cullingMask = 1 << layer;

		Light light = new GameObject("Light", typeof(Light)).GetComponent<Light>();
		light.transform.rotation = Quaternion.Euler(60, -5f, 0);
		light.type = LightType.Directional;
		light.cullingMask = 1 << layer;
		light.intensity = 0.5f;

		void SnapshotItem(ItemDrop item)
		{
			Rect rect = new(0, 0, 64, 64);

			GameObject visual = Object.Instantiate(item.transform.Find("attach").gameObject);
			foreach (Transform child in visual.GetComponentsInChildren<Transform>())
			{
				child.gameObject.layer = layer;
			}

			Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
			Vector3 min = renderers.Aggregate(Vector3.positiveInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Min(cur, renderer.bounds.min));
			Vector3 max = renderers.Aggregate(Vector3.negativeInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Max(cur, renderer.bounds.max));
			Vector3 size = max - min;

			camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);
			float zDist = Mathf.Max(size.x, size.y) * 1.05f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad);
			Transform transform = camera.transform;
			transform.position = (min + max) / 2 + new Vector3(0, 0, -zDist);
			light.transform.position = transform.position + new Vector3(-2, 0.2f) / 3 * zDist;

			camera.Render();

			RenderTexture currentRenderTexture = RenderTexture.active;
			RenderTexture.active = camera.targetTexture;

			Texture2D texture = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
			texture.ReadPixels(rect, 0, 0);
			texture.Apply();

			RenderTexture.active = currentRenderTexture;

			item.m_itemData.m_shared.m_icons = new[] { Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f)) };

			Object.DestroyImmediate(visual);
			camera.targetTexture.Release();
		}

		void CollectMergedGems(string assetName, string localizationPrefix, int tier)
		{
			GameObject mergedGem = assets.LoadAsset<GameObject>(assetName);
			foreach (KeyValuePair<GemType, string> first in colors)
			{
				foreach (KeyValuePair<GemType, string> second in colors)
				{
					if (first.Key != second.Key)
					{
						GameObject prefab = Object.Instantiate(mergedGem, gemList.transform);
						prefab.name = $"{assetName}_{first.Key}_{second.Key}";
						prefab.transform.Find("attach/Gem_Mesh_High").GetComponent<MeshRenderer>().material = assets.LoadAsset<Material>(first.Value);
						prefab.transform.Find("attach/Gem_Mesh_Low").GetComponent<MeshRenderer>().material = assets.LoadAsset<Material>(second.Value);
						ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
						string name = $"$jc_{localizationPrefix}_merged_gemstone_{first.Key.ToString().ToLower()}_{second.Key.ToString().ToLower()}";
						if (!Localization.instance.m_translations.ContainsKey(name))
						{
							name = Localization.instance.Localize($"$jc_{localizationPrefix}_merged_gemstone", $"$jc_merged_gemstone_{first.Key.ToString().ToLower()}", $"$jc_merged_gemstone_{second.Key.ToString().ToLower()}");
						}
						itemDrop.m_itemData.m_shared.m_name = name;
						GemStones.socketableGemStones.Add(name);
						SnapshotItem(itemDrop);
						_ = new Item(prefab);

						mergedGems[first.Key][second.Key][tier] = prefab;
					}
				}
			}
		}

		CollectMergedGems("Common_Merged_Gemstone", "common", 0);
		CollectMergedGems("Advanced_Merged_Gemstone", "adv", 1);
		CollectMergedGems("Perfect_Merged_Gemstone", "perfect", 2);

		Object.Destroy(camera);
		Object.Destroy(light);
	}
}
