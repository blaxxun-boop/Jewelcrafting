using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class StealthArcher
{
	[HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
	private static class ReduceProjectileHitNoise
	{
		private static void Prefix(Character owner, ref float hitNoise)
		{
			if (owner is Player player)
			{
				hitNoise *= 1 - player.GetEffect(Effect.Stealtharcher) / 100f;
			}
		}
	}
}
