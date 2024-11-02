using UnityEngine;

namespace Jewelcrafting.Setup;

public static class VisualSetup
{
	public class RuntimeTextureReducer : MonoBehaviour
	{
		public int reductionFactor = 8;

		private void Start()
		{
			if (Jewelcrafting.pixelateTextures.Value == Jewelcrafting.Toggle.On)
			{
				ReduceTexturesOnAllRenderers();
			}
		}

		private void ReduceTexturesOnAllRenderers()
		{
			Renderer[] renderers = GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in renderers)
			{
				foreach (Material material in renderer.materials)
				{
					foreach (string propertyName in material.GetTexturePropertyNames())
					{
						Texture originalTexture = material.GetTexture(propertyName);
						if (originalTexture == null) continue;
						RenderTexture reducedTexture = ReduceTextureResolution(originalTexture);
						if (reducedTexture != null)
						{
							material.SetTexture(propertyName, reducedTexture);
						}
					}
				}
			}
		}

		private RenderTexture ReduceTextureResolution(Texture originalTexture)
		{
			int width = originalTexture.width / reductionFactor;
			int height = originalTexture.height / reductionFactor;

			RenderTexture reducedTexture = new(width, height, 24)
			{
				filterMode = FilterMode.Point,
			};

			RenderTexture.active = reducedTexture;
			Graphics.Blit(originalTexture, reducedTexture);
			RenderTexture.active = null;

			return reducedTexture;
		}
	}
}
