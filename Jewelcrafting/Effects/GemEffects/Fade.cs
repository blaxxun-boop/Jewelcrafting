using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Fade
{
	static Fade()
	{
		EffectDef.ConfigTypes.Add(Effect.Fade, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[AdditivePower] public float Power;
		[MinPower] [OptionalPower(30f)] public float DamageThreshold;
	}
	
	[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
	private static class TogglePlayerShaderRPC
	{
		private static void Postfix(Player __instance)
		{
			SkinnedMeshRenderer bodyRenderer = __instance.gameObject.transform.Find("Visual/body").GetComponent<SkinnedMeshRenderer>();
			Material defaultPlayerMaterial = bodyRenderer.material;
			__instance.m_nview.Register<bool>("Jewelcrafting Fade Shader", (_, hide) =>
			{
				bodyRenderer.material = hide ? GemEffectSetup.fadingMaterial : defaultPlayerMaterial;
				ToggleEquipment(__instance, !hide);
			});
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.OnDamaged))]
	public static class ReduceDamageTaken
	{
		[UsedImplicitly]
		private static void Postfix(Player __instance, HitData hit)
		{
			if (hit.GetAttacker() is { } attacker && attacker != __instance && __instance.GetEffect(Effect.Fade) > 0)
			{
				if (hit.GetTotalDamage() > __instance.GetMaxHealth() * (__instance.GetEffect<Config>(Effect.Fade).DamageThreshold / 100f) && !__instance.IsDead())
				{
					GemEffectSetup.fading.m_ttl = __instance.GetEffect(Effect.Fade);
					__instance.m_seman.AddStatusEffect(GemEffectSetup.fading);
				}
			}
		}
	}

	public class FadeSE : SE_Stats
	{
		public override void Setup(Character character)
		{
			base.Setup(character);
			character.m_nview.InvokeRPC(ZRoutedRpc.Everybody, "Jewelcrafting Fade Shader", true);
		}

		public override void Stop()
		{
			base.Stop();
			m_character.m_nview.InvokeRPC(ZRoutedRpc.Everybody, "Jewelcrafting Fade Shader", false);
		}

		public override void OnDamaged(HitData hit, Character attacker)
		{
			hit.ApplyModifier(0);
		}
	}

	private static void ToggleEquipment(Player player, bool active)
	{
		player.m_visEquipment.m_helmetItemInstance?.SetActive(active);

		void Toggle(List<GameObject>? list)
		{
			if (list is not null)
			{
				foreach (GameObject equipment in list)
				{
					equipment.SetActive(active);
				}
			}
		}
		Toggle(player.m_visEquipment.m_chestItemInstances);
		Toggle(player.m_visEquipment.m_legItemInstances);
		Toggle(player.m_visEquipment.m_shoulderItemInstances);
		Toggle(player.m_visEquipment.m_utilityItemInstances);
	}
}
