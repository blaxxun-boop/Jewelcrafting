using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Mirror
{
	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	public class ReflectDamageBack
	{
		private static bool isApplyingReflectiveDmg;

		[UsedImplicitly]
		[HarmonyPriority(Priority.Last)]
		private static void Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player player && hit.GetAttacker() is { } attacker && attacker != __instance && !isApplyingReflectiveDmg)
			{
				if (Random.value < player.GetEffect(Effect.Mirror) / 100f)
				{
					HitData hitData = new()
					{
						m_attacker = __instance.GetZDOID(),
						m_dir = hit.m_dir * -1,
						m_point = attacker.transform.localPosition,
						m_damage = hit.m_damage
					};
					try
					{
						isApplyingReflectiveDmg = true;
						attacker.Damage(hitData);
					}
					finally
					{
						isApplyingReflectiveDmg = false;
					}
				}
			}
		}
	}
}
