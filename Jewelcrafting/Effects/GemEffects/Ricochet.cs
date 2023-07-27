using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Ricochet
{
	static Ricochet()
	{
		EffectDef.ConfigTypes.Add(Effect.Ricochet, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
	private class SpawnSecondArrow
	{
		private static void Postfix(Projectile __instance, Collider? collider)
		{
			if (__instance is { m_didHit: true, m_owner: Player player } && collider is not null && Projectile.FindHitObject(collider)?.GetComponent<Character>() is {} hitCharacter)
			{
				if (Random.value < player.GetEffect(Effect.Ricochet) / 100f)
				{
					Vector3 hitCharacterCenter = hitCharacter.transform.position + hitCharacter.m_body.centerOfMass;
					
					List<Character> characters = Object.FindObjectsOfType<Character>().Where(c => c != hitCharacter && !c.IsPlayer() && !c.IsTamed() && Vector3.Distance(player.transform.position, c.transform.position) < 15f).ToList();
					if (characters.Count > 0)
					{
						characters.Shuffle();
						foreach (Character character in characters)
						{
							Vector3 characterCenter = character.transform.position + character.m_body.centerOfMass;
							
							if (Physics.Raycast(characterCenter, hitCharacterCenter - characterCenter, out RaycastHit hitInfo, 15f, Projectile.s_rayMaskSolids) && Projectile.FindHitObject(hitInfo.collider) == hitCharacter.gameObject)
							{
								Vector3 direction = characterCenter - hitInfo.point;
								GameObject newArrow = Object.Instantiate(__instance.gameObject, hitInfo.point, Quaternion.LookRotation(direction));
								newArrow.GetComponent<ZNetView>().enabled = true;
								newArrow.GetComponent<ZSyncTransform>().enabled = true;
								Projectile projectile = newArrow.GetComponent<Projectile>();
								projectile.enabled = true;
								projectile.m_didHit = false;
								projectile.m_gravity = 0;
								projectile.m_vel = (__instance.m_vel with { y = 0 }).magnitude * direction.normalized;
								break;
							}
						}
					}
				}
			}
		}
	}
}
