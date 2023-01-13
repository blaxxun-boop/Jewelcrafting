using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Aquatic
{
	static Aquatic()
	{
		API.OnEffectRecalc += () =>
		{
			if (Player.m_localPlayer.m_utilityItem?.m_shared.m_name != "$jc_necklace_blue")
			{
				Player.m_localPlayer.m_seman.RemoveStatusEffect(GemEffectSetup.aquatic);
			}
		};
	}
	
	[HarmonyPatch(typeof(SE_Wet), nameof(SE_Wet.UpdateStatusEffect))]
	private static class ApplyAquatic
	{
		private static void Postfix(SE_Wet __instance)
		{
			if (__instance.m_character is not Humanoid humanoid || humanoid.m_utilityItem?.m_shared.m_name != "$jc_necklace_blue")
			{
				return;
			}
			
			if (humanoid.GetSEMan().GetStatusEffect(GemEffectSetup.aquatic.name) is not { } aquatic)
			{
				aquatic = humanoid.GetSEMan().AddStatusEffect(GemEffectSetup.aquatic);
			}
			aquatic.m_ttl = __instance.m_ttl;
			aquatic.m_time = __instance.m_time;
		}
	}
}
