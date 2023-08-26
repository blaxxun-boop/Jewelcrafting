using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Necromancer
{
	static Necromancer()
	{
		EffectDef.ConfigTypes.Add(Effect.Necromancer, typeof(Config));
	}

	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
		[MaxPower] [OptionalPower(10f)] public float Duration;
	}

	public static GameObject skeleton = null!;

	[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
	private class SpawnSkeletonArcher
	{
		private static void Postfix(Projectile __instance, Collider? collider)
		{
			if (__instance is { m_didHit: true, m_owner: Player player } && collider is not null && Projectile.FindHitObject(collider)?.GetComponent<Character>())
			{
				if (Random.value < player.GetEffect(Effect.Necromancer) / 100f)
				{
					Transform transform = player.transform;
					float rand = Random.Range(-0.2f, 0.2f);
					skeleton.GetComponent<CharacterTimedDestruction>().m_timeoutMin = player.GetEffect<Config>(Effect.Necromancer).Duration + 3f;
					skeleton.GetComponent<CharacterTimedDestruction>().m_timeoutMax = player.GetEffect<Config>(Effect.Necromancer).Duration + 3f;
					Humanoid pet = Object.Instantiate(skeleton, transform.position + transform.right * (rand + Mathf.Sign(rand) * 0.9f) + Vector3.up * 0.5f, transform.rotation).GetComponent<Humanoid>();
					pet.m_tamed = true;
					pet.m_nview.m_zdo.Set("tamed", true);
					pet.GetComponent<MonsterAI>().SetAlerted(true);
					pet.GetComponent<MonsterAI>().m_updateTargetTimer = 0.5f;
				}
			}
		}
	}

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private class ReplaceAnimationController
	{
		private static void Postfix()
		{
			GameObject template = ZNetScene.instance.GetPrefab("Skeleton");
			skeleton.GetComponentInChildren<Animator>().runtimeAnimatorController = template.GetComponentInChildren<Animator>().runtimeAnimatorController;
			skeleton.GetComponentInChildren<Animator>().avatar = template.GetComponentInChildren<Animator>().avatar;
			skeleton.GetComponent<Humanoid>().m_randomWeapon = new[] { template.GetComponent<Humanoid>().m_randomWeapon.First(w => w.name == "skeleton_bow") };
		}
	}
}
