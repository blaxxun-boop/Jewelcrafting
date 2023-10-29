using HarmonyLib;

namespace Jewelcrafting.GemEffects;

public static class Headhunter
{
	static Headhunter()
	{
		API.OnEffectRecalc += () =>
		{
			if (!API.IsJewelryEquipped(Player.m_localPlayer, "JC_Ring_Green"))
			{
				Player.m_localPlayer.m_seman.RemoveStatusEffect(GemEffectSetup.headhunter);
			}
		};
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private static class ApplyHeadHunter
	{
		private static void Postfix(Character __instance, HitData hit)
		{
			if (__instance.IsBoss() && hit.GetAttacker() is Player player && API.IsJewelryEquipped(player, "JC_Ring_Green"))
			{
				if (__instance.m_nview.GetZDO().GetBool($"Jewelcrafting HeadHunter {player.GetPlayerID()}"))
				{
					return;
				}
				
				player.m_seman.AddStatusEffect(GemEffectSetup.headhunter.NameHash(), true);
				__instance.m_nview.GetZDO().Set($"Jewelcrafting HeadHunter {player.GetPlayerID()}", true);
			}
		}
	}
}
