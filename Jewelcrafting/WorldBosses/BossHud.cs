using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.WorldBosses;

public static class BossHud
{
	[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
	private static class DisplayMultipleBossHuds
	{
		private static void Postfix(EnemyHud __instance)
		{
			List<EnemyHud.HudData> bossHuds = __instance.m_huds.Where(c => c.Key && c.Key.IsBoss() && c.Value.m_gui).Select(kv => kv.Value).ToList();

			int counter = 0;

			foreach (EnemyHud.HudData hud in bossHuds)
			{
				RectTransform hudrect = hud.m_gui.GetComponent<RectTransform>();
				RectTransform rect = hud.m_name.GetComponent<RectTransform>();
				rect.sizeDelta = new Vector2((hudrect.sizeDelta.x - 10 * (bossHuds.Count - 1)) / bossHuds.Count, rect.sizeDelta.y);
				hud.m_gui.transform.Find("Health").localScale = new Vector3(1f / bossHuds.Count, 1) * (bossHuds.Count > 1 ? 1.3f : 1f);
				hudrect.anchorMax = new Vector2((0.5f + counter++) / bossHuds.Count, hudrect.anchorMax.y);
			}
		}
	}
}
