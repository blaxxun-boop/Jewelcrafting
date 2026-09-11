using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Jewelcrafting.GemEffects;

public static class SnowGoggles
{
	static SnowGoggles()
	{
		EffectDef.ConfigTypes.Add(Effect.Snowgoggles, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}
	
	[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.Update))]
	public static class ReduceParticleEmission
	{
		private static float lastPower = 0;
		private static GameObject[]? activeParticleSystems = null;
		
		[UsedImplicitly]
		private static void Prefix(EnvMan __instance)
		{
			float power = Player.m_localPlayer?.GetEffect<Config>(Effect.Snowgoggles).Power ?? 0;
			if (lastPower != power || activeParticleSystems != __instance.m_currentPSystems)
			{
				Apply(activeParticleSystems, 1 / (1 - lastPower / 100));
				Apply(__instance.m_currentPSystems, 1 - power / 100);
				lastPower = power;
				activeParticleSystems = __instance.m_currentPSystems;
			}
		}

		private static void Apply(GameObject[]? allPsystems, float multiplier)
		{
			if (allPsystems is not null)
			{
				foreach (GameObject psystems in allPsystems)
				{
					if (psystems)
					{
						foreach (ParticleSystem psystem in psystems.GetComponentsInChildren<ParticleSystem>())
						{
							psystem.emissionRate *= multiplier;
						}
					}
				}
			}
		}
	}
}
