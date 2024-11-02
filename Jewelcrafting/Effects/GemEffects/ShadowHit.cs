using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class ShadowHit
{
	static ShadowHit()
	{
		EffectDef.ConfigTypes.Add(Effect.Shadowhit, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
		[MaxPower] [OptionalPower(5f)] public float Range;
		[MaxPower] [OptionalPower(1)] public float Amount;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private class SpawnShadowSword
	{
		private static void Postfix(Character __instance, HitData hit)
		{
			if (hit.GetAttacker() is not Player player)
			{
				return;
			}

			Config config = player.GetEffect<Config>(Effect.Shadowhit);
			if (Random.value < config.Power / 100f)
			{
				IEnumerator Do()
				{
					for (int i = 0; i < config.Amount; ++i)
					{
						List<Character> characters = Object.FindObjectsOfType<Character>().Where(c => c != __instance && !c.IsPlayer() && !c.IsTamed() && Vector3.Distance(player.transform.position, c.transform.position) < config.Range).ToList();
						if (characters.Count > 0)
						{
							Character character = characters[Random.Range(0, characters.Count)];
							GameObject sword = Object.Instantiate(GemEffectSetup.swordFall, character.gameObject.transform);
							sword.AddComponent<ShadowSword>();
							sword.transform.localPosition = new Vector3(0, 2, 0);
							character.StartCoroutine(DamageEnemy(character, hit));
						}
						yield return new WaitForSeconds(0.1f);
					}
				}
				player.StartCoroutine(Do());
			}
		}

		private static IEnumerator DamageEnemy(Character character, HitData hit)
		{
			yield return new WaitForSeconds(1.3f);
			hit = hit.Clone();
			hit.m_point = character.transform.position;
			character.Damage(hit);
		}
	}

	private class ShadowSword : MonoBehaviour
	{
		private void OnDestroy()
		{
			ZNetScene.instance.Destroy(gameObject);
		}
	}
}
