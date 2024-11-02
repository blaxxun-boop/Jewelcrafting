using HarmonyLib;
using JetBrains.Annotations;
using Jewelcrafting.GemEffects;

namespace Jewelcrafting.Effects.GemEffects.Groups;

public static class ExtensiveEmbrace
{
	static ExtensiveEmbrace()
	{
		EffectDef.ConfigTypes.Add(Effect.Extensiveembrace, typeof(Config));
	}
	
	[PublicAPI]
	public struct Config
	{
		[AdditivePower] public float Power;
	}

	[HarmonyPatch(typeof(Aoe), nameof(Aoe.Setup))]
	private static class IncreaseRadius
	{
		private static void Postfix(Aoe __instance, Character owner)
		{
			if (owner is Player player && player.GetEffect(Effect.Extensiveembrace) > 0 && global::Utils.GetPrefabName(__instance.gameObject) == "staff_shield_aoe")
			{
				__instance.m_radius += player.GetEffect(Effect.Extensiveembrace);
			}
		}
	}
}
