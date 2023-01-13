using System.Linq;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public class MagicRepair : SE_Stats
{
	private int startEffectIndex;
	
	public override void Setup(Character character)
	{
		base.Setup(character);
		startEffectIndex = m_startEffectInstances.Length;
		m_startEffectInstances = m_startEffectInstances.Concat(new GameObject?[] { null }).ToArray();
	}

	public override void UpdateStatusEffect(float dt)
	{
		m_tickTimer += dt;
		if (m_tickTimer >= 60)
		{
			foreach (ItemDrop.ItemData item in Player.m_localPlayer.GetInventory().m_inventory.Where(i => i.m_equiped))
			{
				item.m_durability = Mathf.Min(item.GetMaxDurability(), item.m_durability + Jewelcrafting.magicRepairAmount.Value);
			}
			m_startEffectInstances[startEffectIndex] = Instantiate(GemEffectSetup.magicRepair, Player.m_localPlayer.transform);
			m_tickTimer = 0;
		}
		base.UpdateStatusEffect(dt);
	}
}
