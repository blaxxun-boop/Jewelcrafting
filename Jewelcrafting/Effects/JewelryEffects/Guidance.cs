using System.Linq;
using Jewelcrafting.WorldBosses;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public class Guidance : SE_Stats
{
	public override void UpdateStatusEffect(float dt)
	{
		m_tickTimer += dt;
		if (m_tickTimer >= Jewelcrafting.guidanceCooldown.Value)
		{
			Transform playerPosition = Player.m_localPlayer.transform;
			if (playerPosition.position.y < 3500)
			{
				if (BossSpawn.currentBossPositions.Count > 0)
				{
					Vector3 closestBoss = BossSpawn.currentBossPositions.OrderBy(b => global::Utils.DistanceXZ(playerPosition.position, b)).FirstOrDefault();
					Instantiate(GemEffectSetup.guidanceNecklaceWorldBoss, playerPosition.position + playerPosition.forward * 2 + playerPosition.up, Quaternion.LookRotation((closestBoss - playerPosition.position) with { y = 0 }));
				}
				else if (ZoneSystem.instance.GetLocationIcon("JC_Gacha_Location", out Vector3 pos))
				{
					Instantiate(GemEffectSetup.guidanceNecklaceGemstone, playerPosition.position + playerPosition.forward * 2 + playerPosition.up, Quaternion.LookRotation((pos - playerPosition.position) with { y = 0 }));
				}
			}

			m_tickTimer = 0;
		}
		base.UpdateStatusEffect(dt);
	}
}
