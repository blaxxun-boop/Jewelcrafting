using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Thunderclap
{
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	private static class ApplyThunderclapMark
	{
		private static void Prefix(Character __instance, HitData hit)
		{
			if (hit.GetAttacker() is Player player && player.GetEffect(Effect.Thunderclap) is { } thunder and > 0)
			{
				float explosiveDmg = 0;
				if (__instance.GetSEMan().HaveStatusEffect(Jewelcrafting.thunderclapMark.name))
				{
					explosiveDmg = __instance.m_nview.GetZDO().GetFloat("Jewelcrafting Thunderclap");
				}
				__instance.GetSEMan().AddStatusEffect(Jewelcrafting.thunderclapMark, true);
				explosiveDmg += hit.GetTotalDamage() * thunder / 100f;
				__instance.m_nview.GetZDO().Set("Jewelcrafting Thunderclap", explosiveDmg);
			}
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
	private static class ExplodeThunderclap
	{
		private static void Postfix(Character __instance, HitData hit)
		{
			if (__instance.GetSEMan().HaveStatusEffect(Jewelcrafting.thunderclapMark.name) && !__instance.IsDead())
			{
				float explosiveDmg = __instance.m_nview.GetZDO().GetFloat("Jewelcrafting Thunderclap");
				float damageScale = Game.instance.GetDifficultyDamageScaleEnemy(__instance.transform.position);
				if (explosiveDmg * damageScale > __instance.GetHealth())
				{
					Object.Instantiate(Jewelcrafting.thunderclapExplosion, __instance.transform.position, Quaternion.identity);
					DamageText.instance.ShowText(HitData.DamageModifier.VeryWeak, hit.m_point, explosiveDmg);
					__instance.SetHealth(0);
					__instance.OnDamaged(hit);
					__instance.m_onDamaged?.Invoke(explosiveDmg * damageScale, hit.GetAttacker());
					if (Character.m_dpsDebugEnabled)
					{
						Character.AddDPS(explosiveDmg * damageScale, __instance);
					}
				}
			}
		}
	}
}
