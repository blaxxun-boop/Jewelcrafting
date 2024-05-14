using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting.GemEffects;

public static class LizardFriendship
{
	static LizardFriendship()
	{
		EffectDef.ConfigTypes.Add(Effect.Lizardfriendship, typeof(Config));
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Config
	{
		[AdditivePower] public readonly float MovementDamage;
		[MinPower] public readonly float MinCooldown;
		[MinPower] public readonly float MaxCooldown;
		[AdditivePower] public readonly float Duration;
		[MultiplicativePercentagePower] public readonly float SpeedIncrease;
		[MultiplicativePercentagePower] public readonly float HealthIncrease;
		[MinPower] public readonly float SpawnRange;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
	private class StartCoroutineForEffect
	{
		private static void Postfix(Player __instance)
		{
			__instance.StartCoroutine(BurstOfPower(__instance));
		}
	}

	private static IEnumerator BurstOfPower(Player player)
	{
		while (true)
		{
			yield return player.WaitEffect<Config>(Effect.Lizardfriendship, c => c.MinCooldown, c => c.MaxCooldown);
			Config config = player.GetEffect<Config>(Effect.Lizardfriendship);
			if (config.Duration > 0 && !player.IsDead() && !Utils.SkipBossPower() && Utils.FindSpawnPoint(Player.m_localPlayer.transform.position, config.SpawnRange, out Vector3 spawnPoint))
			{
				player.m_seman.AddStatusEffect(GemEffectSetup.lizardFriendshipStart);
				Object.Instantiate(GemEffectSetup.asksvinSpawnVFX, spawnPoint, Player.m_localPlayer.transform.rotation).GetComponent<TimedDestruction>().m_timeout = 4f;
				
				yield return new WaitForSeconds(4);

				GemEffectSetup.lizardFriendship.m_ttl = config.Duration;
				if (player.m_seman.AddStatusEffect(GemEffectSetup.lizardFriendship) is SE_Stats)
				{
					GameObject asksvin = GemEffectSetup.asksvinPrefabs[Random.Range(0, GemEffectSetup.asksvinPrefabs.Count)];
					asksvin.GetComponent<CharacterTimedDestruction>().m_timeoutMin = config.Duration;
					asksvin.GetComponent<CharacterTimedDestruction>().m_timeoutMax = config.Duration;
					Humanoid pet = Object.Instantiate(asksvin, spawnPoint, player.transform.rotation).GetComponent<Humanoid>();
					pet.m_tamed = true;
					pet.m_nview.m_zdo.Set("tamed", true);
					pet.m_nview.m_zdo.Set(ZDOVars.s_haveSaddleHash, true);
					pet.GetComponent<Tameable>().SetSaddle(true);
					pet.SetMaxHealth(pet.GetHealth() * (1 + config.HealthIncrease / 100f));
					pet.GetComponent<MovementDamage>().m_runDamageObject.GetComponent<Aoe>().m_damage.m_blunt = config.MovementDamage;
					pet.GetComponent<MovementDamage>().m_runDamageObject.GetComponent<Aoe>().m_damage.m_chop = config.MovementDamage * 2;
					pet.m_nview.m_zdo.Set("LizardFriendship Speed", config.SpeedIncrease / 100f);
					pet.GetSEMan().AddStatusEffect(movementSpeed);
				}
			}
		}
		// ReSharper disable once IteratorNeverReturns
	}

	public class LizardSadle : Sadle, Hoverable, Interactable
	{
		public new string GetHoverText()
		{
			string[] str = base.GetHoverText().Split('\n');
			string remove = Localization.instance.Localize("$hud_saddle_remove");
			int idx = Array.FindIndex(str, line => line.Contains(remove));
			return string.Join("\n", idx >= 0 ? str.Take(idx).Concat(str.Skip(idx + 1)) : str);
		}

		public new bool Interact(Humanoid user, bool hold, bool alt) => base.Interact(user, hold, false);
	}

	public class LizardFriendshipEffect : SE_Stats
	{
		public override string GetTooltipString()
		{
			Config config = Player.m_localPlayer.GetEffect<Config>(Effect.Lizardfriendship);
			return Localization.instance.Localize(m_tooltip, config.MovementDamage.ToString("0.#"), config.MinCooldown.ToString("0.#"), config.MaxCooldown.ToString("0.#"), config.Duration.ToString("0.#"), config.SpeedIncrease.ToString("0.#"), config.HealthIncrease.ToString("0.#"), config.SpawnRange.ToString("0.#"));
		}
	}

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private class ReplaceAnimationController
	{
		private static void Postfix()
		{
			GameObject template = ZNetScene.instance.GetPrefab("Asksvin");
			foreach (GameObject asksvin in GemEffectSetup.asksvinPrefabs)
			{
				asksvin.GetComponentInChildren<Animator>().runtimeAnimatorController = template.GetComponentInChildren<Animator>().runtimeAnimatorController;
				asksvin.GetComponentInChildren<Animator>().avatar = template.GetComponentInChildren<Animator>().avatar;
			}
		}
	}

	private static StatusEffect? movementSpeed;
	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	public class AddStatusEffect
	{
		private static void Postfix(ObjectDB __instance)
		{
			movementSpeed = ScriptableObject.CreateInstance<IncreaseMovementSpeed>();
			movementSpeed.name = "Movement Speed Increase";
			movementSpeed.m_name = "Movement Speed Increase";
			__instance.m_StatusEffects.Add(movementSpeed);
		}
	}

	public class IncreaseMovementSpeed : StatusEffect
	{
		public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
		{
			speed *= 1 + character.m_nview.m_zdo.GetFloat("LizardFriendship Speed");
		}
	}
}
