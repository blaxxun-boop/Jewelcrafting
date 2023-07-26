using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelcrafting.GemEffects;

public static class ApplyAttackSpeed
{
	public static readonly List<Func<Player, float>> Modifiers = new();

	static ApplyAttackSpeed()
	{
		AnimationSpeedManager.Add((character, speed) =>
		{
			if (character is not Player player || !player.InAttack() || player.m_currentAttack is null)
			{
				return speed;
			}

			return speed * (1 + Modifiers.Sum(m => m(player)));
		});
	}
}
