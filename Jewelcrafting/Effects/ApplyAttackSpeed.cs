using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class ApplyAttackSpeed
{
	public static readonly List<Func<Player, float>> Modifiers = new();

	[HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.CustomFixedUpdate))]
	public static class IncreaseAttackSpeed
	{
		[UsedImplicitly]
		private static void Prefix(Character ___m_character, ref Animator ___m_animator)
		{
			if (___m_character is not Player player || !player.InAttack() || player.m_currentAttack is null)
			{
				return;
			}

			// check if our marker bit is present and not within float epsilon
			double currentSpeedMarker = ___m_animator.speed * 1e7 % 100;
			if (currentSpeedMarker is > 10 and < 30 || ___m_animator.speed <= 0.001f)
			{
				return;
			}

			double speed = ___m_animator.speed * (1 + Modifiers.Sum(m => m(player)));
			___m_animator.speed = (float)(speed - speed % 1e-5 + 19e-7); // number with single bit in mantissa set
		}
	}
}
