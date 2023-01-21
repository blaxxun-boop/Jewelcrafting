namespace Jewelcrafting.GemEffects;

public static class Wisplight
{
	static Wisplight()
	{
		API.OnEffectRecalc += () =>
		{
			if (Jewelcrafting.wisplightGem.Value == Jewelcrafting.Toggle.On)
			{
				if (Player.m_localPlayer.GetEffect(Effect.Wisplight) == 0)
				{
					Player.m_localPlayer.m_seman.RemoveStatusEffect("Demister");
				}
				else
				{
					Player.m_localPlayer.m_seman.AddStatusEffect("Demister");
					if (Player.m_localPlayer.m_seman.GetStatusEffect("Demister") is SE_Demister demister)
					{
						demister.m_maxDistance = Player.m_localPlayer.GetEffect(Effect.Wisplight);
					}
				}
			}
		};
	}
}
