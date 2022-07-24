using System.Linq;
using UnityEngine;

namespace Jewelcrafting;

public static class ItemSnapshots
{
	public static void SnapshotItems(ItemDrop item)
	{
		const int layer = 30;

		Camera camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
		camera.backgroundColor = Color.clear;
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.fieldOfView = 0.5f;
		camera.farClipPlane = 10000000;
		camera.cullingMask = 1 << layer;

		Light topLight = new GameObject("Light", typeof(Light)).GetComponent<Light>();
		topLight.transform.rotation = Quaternion.Euler(60, -5f, 0);
		topLight.type = LightType.Directional;
		topLight.cullingMask = 1 << layer;
		topLight.intensity = 0.7f;
		
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
		topLight.transform.position = transform.position + new Vector3(-2, 0.2f) / 3 * zDist;

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

		Object.Destroy(camera);
		Object.Destroy(topLight);
	}
}
