using System.Linq;

namespace Jewelcrafting.GemEffects;

public class ModersBlessing : SE_Stats
{
	public override void UpdateStatusEffect(float dt)
	{
		m_tickTimer += dt;
		if (m_tickTimer >= Jewelcrafting.modersBlessingCooldown.Value)
		{
			StatusEffect moderPrefab = ObjectDB.instance.m_StatusEffects.Single(s => s.name == "GP_Moder");
			string originalMessage = moderPrefab.m_startMessage;
			moderPrefab.m_startMessage = "";
			if (Player.m_localPlayer.m_seman.AddStatusEffect(moderPrefab) is { } moder)
			{
				moder.m_time = moder.m_ttl - Jewelcrafting.modersBlessingDuration.Value;
			}
			else
			{
				moder = Player.m_localPlayer.m_seman.GetStatusEffect(moderPrefab.name);
				moder.m_time -= Jewelcrafting.modersBlessingDuration.Value;
			}
			moderPrefab.m_startMessage = originalMessage;
			m_tickTimer = 0;
		}
		base.UpdateStatusEffect(dt);
	}
}
