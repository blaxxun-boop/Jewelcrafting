using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class ElementalChaos
{
	static ElementalChaos()
	{
		EffectDef.ConfigTypes.Add(Effect.Elementalchaos, typeof(Config));
	}
	
	[PublicAPI]
	public struct Config
	{
		[AdditivePower] public float Power;
		[MaxPower] [OptionalPower(20f)] public float Chance;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private class AddBonusElementalDamage
	{
		private static void Prefix(HitData hit)
		{
			if (hit.GetAttacker() is Player attacker)
			{
				Config config = attacker.GetEffect<Config>(Effect.Elementalchaos);
				if (Random.value <= config.Chance / 100f)
				{
					float bonusDamage = hit.GetTotalDamage() * config.Power / 100f;

					switch (Random.Range(0, 4))
					{
						case 0:
						{
							hit.m_damage.m_fire += bonusDamage;
							break;
						}
						case 1:
						{
							hit.m_damage.m_frost += bonusDamage;
							break;
						}
						case 2:
						{
							hit.m_damage.m_lightning += bonusDamage;
							break;
						}
						case 3:
						{
							hit.m_damage.m_poison += bonusDamage;
							break;
						}
					}
				}
			}
		}
	}
}
