using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Aquatic
{
	static Aquatic()
	{
		API.OnEffectRecalc += () =>
		{
			if (!Utils.IsJewelryEquipped(Player.m_localPlayer, "JC_Necklace_Blue"))
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
			if (__instance.m_character != Player.m_localPlayer || !Utils.IsJewelryEquipped(Player.m_localPlayer, "JC_Necklace_Blue"))
			{
				return;
			}
			
			if (Player.m_localPlayer.GetSEMan().GetStatusEffect(GemEffectSetup.aquatic.name.GetStableHashCode()) is not { } aquatic)
			{
				aquatic = Player.m_localPlayer.GetSEMan().AddStatusEffect(GemEffectSetup.aquatic);
			}
			aquatic.m_ttl = __instance.m_ttl;
			aquatic.m_time = __instance.m_time;
		}
	}
}
