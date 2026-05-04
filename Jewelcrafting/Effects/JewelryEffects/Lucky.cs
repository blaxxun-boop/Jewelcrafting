using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using SkillManager;

namespace Jewelcrafting.GemEffects;

public class Lucky : SE_Stats
{
	public static readonly List<string> SeNames = new();
	
	[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
	private static class AddRPCs
	{
		private static void Postfix(Player __instance)
		{
			__instance.m_nview.Register("Jewelcrafting Lucky", _ =>
			{
				ZDO zdo = __instance.m_nview.GetZDO();
				foreach (string seName in SeNames)
				{
					int value = zdo.GetInt("Jewelcrafting Lucky " + seName);
					if (value > 0)
					{
						zdo.Set("Jewelcrafting Lucky " + seName, value + 1);
					}
				}
			});
		}
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
	private static class TrackCreatureKills
	{
		private static void Postfix(Character __instance)
		{
			if (__instance.GetComponent<CharacterDrop>() is not {} drops || __instance.m_lastHit?.GetAttacker() is not Player attacker)
			{
				return;
			}
			
			float chanceFactor = ChanceMultiplier(attacker);
			if (chanceFactor > 1)
			{
				attacker.m_nview.InvokeRPC("Jewelcrafting Lucky");
				foreach (CharacterDrop.Drop drop in drops.m_drops)
				{
					drop.m_chance *= chanceFactor;
					if (drop.m_chance > 1)
					{
						int extra = Mathf.FloorToInt(drop.m_chance);
						if (Random.value < drop.m_chance - extra)
						{
							++extra;
						}
						drop.m_amountMin *= extra;
						drop.m_amountMax *= extra;
						drop.m_chance = 1;
					}
				}
			}
		}
	}

	public static int MaxStacks(Character? character) => 25 + Mathf.RoundToInt(100 * character?.GetSkillFactor("Jewelcrafting") ?? 0);
	public static float Chance(Character? character, string seName) => Mathf.Min(MaxStacks(character ?? Player.m_localPlayer), character?.m_nview.GetZDO().GetInt("Jewelcrafting Lucky " + seName) ?? MaxStacks(Player.m_localPlayer)) * 0.02f;
	public float DamageDoneMultiplier() => Mathf.Max(0.2f, 1 - (m_character?.m_nview.GetZDO().GetInt("Jewelcrafting Lucky " + name) ?? MaxStacks(Player.m_localPlayer)) * 0.01f);
	public float DamageReceivedMultiplier() => 1 + Mathf.Min(3f, (m_character?.m_nview.GetZDO().GetInt("Jewelcrafting Lucky " + name) ?? MaxStacks(Player.m_localPlayer)) * 0.03f);
	
	public static float ChanceMultiplier(Player? player) => 1 + SeNames.Sum(seName => Chance(player, seName));

	public override void Setup(Character character)
	{
		base.Setup(character);
		character.m_nview.GetZDO().Set("Jewelcrafting Lucky " + name, 1);
	}

	public override void Stop()
	{
		base.Stop();
		m_character.m_nview.GetZDO().Set("Jewelcrafting Lucky " + name, 0);
	}

	public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		base.ModifyAttack(skill, ref hitData);
		hitData.ApplyModifier(DamageDoneMultiplier());
	}

	public override void OnDamaged(HitData hit, Character attacker)
	{
		base.OnDamaged(hit, attacker);
		hit.ApplyModifier(DamageReceivedMultiplier());
	}

	public override string GetTooltipString()
	{
		return $"{base.GetTooltipString()}$jc_lucky_luck: <color=orange>+{Chance(m_character, name):P}</color>\n$jc_lucky_damage_done: <color=orange>-{1 - DamageDoneMultiplier():P}</color>\n$jc_lucky_damage_received: <color=orange>+{DamageReceivedMultiplier() - 1:P}</color>\n";
	}

	private float time = 0;
	private bool firstDamage = true;
	public override void UpdateStatusEffect(float dt)
	{
		time += dt;
		if (time > 5)
		{
			time -= 5;
			int value = m_character.m_nview.GetZDO().GetInt("Jewelcrafting Lucky " + name) - MaxStacks(m_character);
			if (value > 0)
			{
				if (firstDamage)
				{
					firstDamage = false;
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$jc_lucky_damage_start");
				}
				m_character.ApplyDamage(new HitData { m_damage = new HitData.DamageTypes { m_damage = value / 2f }, m_point = m_character.GetCenterPoint() }, true, true);
			}
		}
	}
}
