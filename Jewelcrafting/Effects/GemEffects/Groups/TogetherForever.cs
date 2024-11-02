using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jewelcrafting.GemEffects.Groups;

public static class TogetherForever
{
	static TogetherForever()
	{
		EffectDef.ConfigTypes.Add(Effect.Togetherforever, typeof(Config));
		ApplyAttackSpeed.Modifiers.Add(player => player.m_seman.GetStatusEffect(GemEffectSetup.friendship.name.GetStableHashCode()) is SE_Stats se ? se.m_healthOverTimeDuration / 100f : 0);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Config
	{
		[AdditivePower] public readonly float MovementSpeed;
		[MinPower] public readonly float MinCooldown;
		[MinPower] public readonly float MaxCooldown;
		[AdditivePower] public readonly float Duration;
		[MultiplicativePercentagePower] public readonly float AttackSpeed;
		[MultiplicativePercentagePower] public readonly float DamageIncrease;
		[InverseMultiplicativePercentagePower] public readonly float MovementSpeedReduction;
		[MultiplicativePercentagePower] public readonly float DamageTakenIncrease;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
	private class StartCoroutineForEffect
	{
		private static void Postfix(Player __instance)
		{
			__instance.StartCoroutine(BurstOfSpeed(__instance));
		}
	}

	private static IEnumerator BurstOfSpeed(Player player)
	{
		while (true)
		{
			yield return player.WaitEffect<Config>(Effect.Togetherforever, c => c.MinCooldown, c => c.MaxCooldown);
			Config config = player.GetEffect<Config>(Effect.Togetherforever);
			if (config.Duration > 0 && global::Groups.API.GroupPlayers().Count > 1)
			{
				player.m_seman.AddStatusEffect(GemEffectSetup.friendshipStart);

				yield return new WaitForSeconds(4);
				
				GemEffectSetup.loneliness.m_ttl = config.Duration;

				if (FindBuffTarget() is { } target)
				{
					GemEffectSetup.friendship.m_ttl = config.Duration;
					if (player.m_seman.AddStatusEffect(GemEffectSetup.friendship, true) is SE_Stats friendshipEffect)
					{
						friendshipEffect.m_healthOverTimeDuration = config.AttackSpeed;
						friendshipEffect.m_speedModifier = config.MovementSpeed / 100f;
						friendshipEffect.m_damageModifier = 1 + config.DamageIncrease / 100f;
						friendshipEffect.m_modifyAttackSkill = Skills.SkillType.All;
					}
					GameObject tether = Object.Instantiate(GemEffectSetup.friendshipTether, player.transform);
					StatusEffect statusEffect = player.m_seman.GetStatusEffect(GemEffectSetup.friendship.name.GetStableHashCode());
					statusEffect.m_startEffectInstances = statusEffect.m_startEffectInstances.Concat(new[] { tether }).ToArray();
					target.m_nview.InvokeRPC("Jewelcrafting ApplyFriendship", player.GetZDOID());
					tether.GetComponent<ZNetView>().GetZDO().Set("Jewelcrafting Friendship PlayerStart", player.GetZDOID());
					tether.GetComponent<ZNetView>().GetZDO().Set("Jewelcrafting Friendship PlayerEnd", target.GetZDOID());
				}
				else if (HasPlayersInDebuffRange() && player.GetEffect(Effect.Neveralone) is { } neverAlone and < 100 && player.m_seman.AddStatusEffect(GemEffectSetup.loneliness) is SE_Stats statusEffectDebuff)
				{
					statusEffectDebuff.m_speedModifier = -config.MovementSpeedReduction / 100f * (1 - neverAlone / 100f);
				}
			}
		}
		// ReSharper disable once IteratorNeverReturns
	}
	
	[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
	private static class IncreaseLoadSpeed
	{
		private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
		{
			if (Player.m_localPlayer.m_seman.HaveStatusEffect(GemEffectSetup.friendship.name.GetStableHashCode()) && __instance.m_shared.m_attack.m_requiresReload)
			{
				__result /= 1 + Player.m_localPlayer.GetEffect<Config>(Effect.Togetherforever).AttackSpeed / 100f;
			}
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage))]
	private static class IncreaseDrawSpeed
	{
		private static void Postfix(Humanoid __instance, ref float __result)
		{
			if (__instance is Player player && player.m_seman.HaveStatusEffect(GemEffectSetup.friendship.name.GetStableHashCode()))
			{
				__result *= 1 + player.GetEffect<Config>(Effect.Togetherforever).AttackSpeed / 100f;
				__result = Math.Min(__result, 1f);
			}
		}
	}

	public class LonelinessEffect : SE_Stats
	{
		public override void OnDamaged(HitData hit, Character attacker)
		{
			if (m_character is Player player && attacker != player && player.GetEffect(Effect.Neveralone) is { } neverAlone and < 100)
			{
				hit.ApplyModifier(1 + player.GetEffect<Config>(Effect.Togetherforever).DamageTakenIncrease / 100f * (1 - neverAlone / 100f));
			}
		}

		public override string GetTooltipString()
		{
			Config config = Player.m_localPlayer.GetEffect<Config>(Effect.Togetherforever);
			float neverAloneMultiplier = 1 - Player.m_localPlayer.GetEffect(Effect.Neveralone);
			return Localization.instance.Localize(m_tooltip, config.MovementSpeed.ToString("0.#"), config.MinCooldown.ToString("0.#"), config.MaxCooldown.ToString("0.#"), config.Duration.ToString("0.#"), config.AttackSpeed.ToString("0.#"), config.DamageIncrease.ToString("0.#"), (config.MovementSpeedReduction * neverAloneMultiplier).ToString("0.#"), (config.DamageTakenIncrease * neverAloneMultiplier).ToString("0.#"));
		}
	}

	public class TogetherForeverEffect : SE_Stats
	{
		public override string GetTooltipString()
		{
			return Localization.instance.Localize(m_tooltip, (m_speedModifier * 100).ToString("0.#"), m_healthOverTimeDuration.ToString("0.#"), ((m_damageModifier - 1) * 100).ToString("0.#"));
		}
	}

	private static Player? FindBuffTarget()
	{
		return Utils.GetNearbyGroupMembers(Player.m_localPlayer, 30).OrderBy(p => Vector3.Distance(p.transform.position, Player.m_localPlayer.transform.position)).FirstOrDefault();
	}

	private static bool HasPlayersInDebuffRange()
	{
		return Utils.GetNearbyGroupMembers(Player.m_localPlayer, 100).Count > 0;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
	private static class AddRPCs
	{
		private static void Postfix(Player __instance)
		{
			__instance.m_nview.Register<ZDOID>("Jewelcrafting ApplyFriendship", ApplyFriendship);
		}
	}

	private static void ApplyFriendship(long sender, ZDOID playerId)
	{
		if (ZNetScene.instance.FindInstance(playerId)?.GetComponent<Player>() is { } otherPlayer)
		{
			Config config = otherPlayer.GetEffect<Config>(Effect.Togetherforever);
			GemEffectSetup.friendship.m_ttl = config.Duration;
			if (Player.m_localPlayer.m_seman.AddStatusEffect(GemEffectSetup.friendship, true) is SE_Stats statusEffect)
			{
				statusEffect.m_healthOverTimeDuration = config.AttackSpeed;
				statusEffect.m_speedModifier = config.MovementSpeed / 100f;
				statusEffect.m_damageModifier = 1 + config.DamageIncrease / 100f;
				statusEffect.m_modifyAttackSkill = Skills.SkillType.All;
			}
		}
	}
}

public class FriendshipTether : MonoBehaviour
{
	private Player playerStart = null!;
	private Player playerEnd = null!;
	private LineRenderer lineRenderer = null!;
	private bool isOwner;

	private static readonly List<FriendshipTether> active = new();

	private void Start()
	{
		ZDO zdo = GetComponent<ZNetView>().GetZDO();
		if (ZNetScene.instance.FindInstance(zdo.GetZDOID("Jewelcrafting Friendship PlayerStart"))?.GetComponent<Player>() is { } startPlayer && ZNetScene.instance.FindInstance(zdo.GetZDOID("Jewelcrafting Friendship PlayerEnd"))?.GetComponent<Player>() is { } endPlayer)
		{
			playerStart = startPlayer;
			playerEnd = endPlayer;
			lineRenderer = transform.Find("LineRenderer").GetComponent<LineRenderer>();
			lineRenderer.SetPosition(0, Vector3.zero);
			lineRenderer.transform.rotation = Quaternion.identity;
			transform.rotation = Quaternion.identity;
			isOwner = GetComponent<ZNetView>().IsOwner();

			if (Player.m_localPlayer == playerStart || Player.m_localPlayer == playerEnd)
			{
				active.Add(this);
			}
		}
		else
		{
			gameObject.SetActive(false);
		}
	}

	private void Update()
	{
		if (playerStart && playerEnd && Vector3.Distance(playerStart.transform.position, playerEnd.transform.position) < 30)
		{
			Vector3 startPosition = playerStart.transform.position;
			if (isOwner)
			{
				transform.localRotation = Quaternion.identity;
			}
			transform.Find("LineRenderer").localRotation = Quaternion.Euler(0, -playerStart.transform.rotation.eulerAngles.y, 0);
			transform.position = startPosition + new Vector3(0, 1, 0);
			lineRenderer.SetPosition(1, playerEnd.transform.position - startPosition);
		}
		else if (isOwner)
		{
			ZNetScene.instance.Destroy(gameObject);
		}
	}

	private void OnDestroy()
	{
		active.Remove(this);
		if (active.Count == 0 && Player.m_localPlayer)
		{
			Player.m_localPlayer.m_seman.RemoveStatusEffect(GemEffectSetup.friendship);
		}
	}
}
