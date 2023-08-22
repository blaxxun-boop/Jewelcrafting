using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ItemManager;
using Jewelcrafting.WorldBosses;
using UnityEngine;

namespace Jewelcrafting;

public static class BossSetup
{
	public static BalanceConfig plainsConfigs = new();
	public static BalanceConfig mistlandsConfigs = new();
	private static Aoe fireBossAoe = null!;
	private static Aoe poisonBossAoe = null!;
	private static Aoe frostBossAoe = null!;
	private static readonly List<BossCharacter> bosses = new();
	private static Aoe bossSmashAttack = null!;

	public struct BalanceConfig
	{
		public float smashBlunt;
		public float punchBlunt;
		public float aoePoison;
		public float aoeFire;
		public float aoeFrost;
		public float health;
	}

	public static void initializeBosses(AssetBundle assets)
	{
		bosses.Add(Utils.ConvertComponent<BossCharacter, Humanoid>(PrefabManager.RegisterPrefab(assets, "Crystal_Frost_Reaper")));
		bosses.Add(Utils.ConvertComponent<BossCharacter, Humanoid>(PrefabManager.RegisterPrefab(assets, "Crystal_Flame_Reaper")));
		bosses.Add(Utils.ConvertComponent<BossCharacter, Humanoid>(PrefabManager.RegisterPrefab(assets, "Crystal_Soul_Reaper")));

		bossSmashAttack = PrefabManager.RegisterPrefab(assets, "JC_Boss_AOE_Hit_2").GetComponent<Aoe>();
		fireBossAoe = PrefabManager.RegisterPrefab(assets, "JC_Boss_Explosion_Flames").GetComponent<Aoe>();
		frostBossAoe = PrefabManager.RegisterPrefab(assets, "JC_Boss_Explosion_Frost").GetComponent<Aoe>();
		poisonBossAoe = PrefabManager.RegisterPrefab(assets, "JC_Boss_Explosion_Poison").GetComponent<Aoe>();

		plainsConfigs = new BalanceConfig
		{
			health = bosses[0].m_health,
			aoeFire = fireBossAoe.m_damage.m_fire,
			aoeFrost = frostBossAoe.m_damage.m_frost,
			aoePoison = poisonBossAoe.m_damage.m_poison,
			smashBlunt = bossSmashAttack.m_damage.m_blunt,
		};
		foreach (GameObject attackItem in bosses[0].m_randomSets[0].m_items)
		{
			if (attackItem.name == "JC_Reaper_Punch")
			{
				plainsConfigs.punchBlunt = attackItem.GetComponent<ItemDrop>().m_itemData.m_shared.m_damages.m_blunt;
			}
		}
		mistlandsConfigs = new BalanceConfig
		{
			health = 10000f,
			punchBlunt = 140f,
			smashBlunt = 200f,
			aoeFire = 190f,
			aoeFrost = 140f,
			aoePoison = 400f,
		};

		PrefabManager.RegisterPrefab(assets, "Crystal_Frost_Reaper_Cage").AddComponent<RemoveBossDestructible>();
		PrefabManager.RegisterPrefab(assets, "Crystal_Flame_Reaper_Cage").AddComponent<RemoveBossDestructible>();
		PrefabManager.RegisterPrefab(assets, "Crystal_Soul_Reaper_Cage").AddComponent<RemoveBossDestructible>();

		BossSpawn.bossIcons["Crystal_Frost_Reaper_Cage"] = assets.LoadAsset<Sprite>("JCBossIconBlue");
		BossSpawn.bossIcons["Crystal_Flame_Reaper_Cage"] = assets.LoadAsset<Sprite>("JCBossIconRed");
		BossSpawn.bossIcons["Crystal_Soul_Reaper_Cage"] = assets.LoadAsset<Sprite>("JCBossIconGreen");
	}

	public static void ApplyBalanceConfig(BalanceConfig config)
	{
		foreach (BossCharacter boss in bosses)
		{
			boss.m_health = config.health;
			foreach (GameObject attackItem in boss.m_randomSets[0].m_items)
			{
				if (attackItem.name == "JC_Reaper_Punch")
				{
					attackItem.GetComponent<ItemDrop>().m_itemData.m_shared.m_damages.m_blunt = config.punchBlunt;
				}
			}
		}
		bossSmashAttack.m_damage.m_blunt = config.smashBlunt;
		fireBossAoe.m_damage.m_fire = config.aoeFire;
		frostBossAoe.m_damage.m_frost = config.aoeFrost;
		poisonBossAoe.m_damage.m_poison = config.aoePoison;
	}

	private class RemoveBossDestructible : MonoBehaviour
	{
		public void Start()
		{
			long destruction = GetComponent<ZNetView>().GetZDO().GetLong("Jewelcrafting World Boss", long.MaxValue / 2);
			if (destruction < ZNet.instance.GetTimeSeconds())
			{
				ZNetScene.instance.Destroy(gameObject);
			}
			else
			{
				GetComponent<Destructible>().m_onDestroyed = () =>
				{
					// Give players a bit of extra time when a boss is spawned
					destruction += 3600;

					Character boss = Character.s_characters.Last();
					boss.m_nview.GetZDO().Set("Jewelcrafting World Boss", destruction);
					Vector2i sector = ZoneSystem.instance.GetZone(boss.m_baseAI.m_spawnPoint);
					if (ZoneSystem.instance.m_locationInstances.TryGetValue(sector, out ZoneSystem.LocationInstance location))
					{
						location.m_position.y = destruction;
						ZoneSystem.instance.m_locationInstances[sector] = location;

						BossSpawn.BroadcastMinimapUpdate();
					}
				};
			}
		}
	}

	public class BossCharacter : Humanoid, IDestructible
	{
		public int tickCounter = 0;

		public override void Start()
		{
			base.Start();
			IEnumerator checkPlayerInRange()
			{
				while (true)
				{
					if (Player.GetPlayersInRangeXZ(transform.position, 3f) == 0)
					{
						++tickCounter;
					}
					else
					{
						tickCounter = 0;
					}

					if (tickCounter >= 10 && Jewelcrafting.worldBossExploitProtectionHeal.Value == Jewelcrafting.Toggle.On && m_nview.IsOwner())
					{
						Heal(GetMaxHealth() / 10f);
					}

					yield return new WaitForSeconds(1);
				}
				// ReSharper disable once IteratorNeverReturns
			}
			StartCoroutine(checkPlayerInRange());
		}

		public new void Damage(HitData hit)
		{
			int stackCounter = m_nview.GetZDO().GetInt("WorldBoss ranged stacks");
			if (Jewelcrafting.worldBossExploitProtectionRangedShield.Value == Jewelcrafting.Toggle.On)
			{
				hit.m_damage.Modify(1 - stackCounter / 10f);
			}
			if (hit.GetAttacker() is Humanoid attacker && attacker.GetCurrentWeapon()?.m_dropPrefab is { } weaponPrefab && GachaSetup.worldBossBonusItems.Contains(weaponPrefab.name))
			{
				hit.m_damage.Modify(1 + Jewelcrafting.worldBossBonusWeaponDamage.Value / 100f);
			}
			base.Damage(hit);
		}

		public override void OnDamaged(HitData hit)
		{
			base.OnDamaged(hit);

			if (hit.GetAttacker() is Player attacker)
			{
				int stackCounter = m_nview.GetZDO().GetInt("WorldBoss ranged stacks");
				stackCounter = Vector3.Distance(attacker.transform.position, transform.position) < 3f ? Math.Max(stackCounter - 1, 0) : Math.Min(stackCounter + 1, 9);
				m_nview.GetZDO().Set("WorldBoss ranged stacks", stackCounter);
			}
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
	private static class IncreaseShieldBlockPower
	{
		private static float original = 0;
		
		private static void Prefix(Humanoid __instance, Character? attacker)
		{
			if (attacker?.GetComponent<BossCharacter>() is not null && __instance.GetCurrentBlocker()?.m_shared.m_name == "$jc_reaper_shield")
			{
				original = __instance.GetCurrentBlocker().m_shared.m_blockPower;
				__instance.GetCurrentBlocker().m_shared.m_blockPower *= 1 + Jewelcrafting.worldBossBonusBlockPower.Value / 100f;
			}
		}

		private static void Finalizer(Humanoid __instance)
		{
			if (original > 0)
			{
				__instance.GetCurrentBlocker().m_shared.m_blockPower = original;
				original = 0;
			}
		}
	}
}
