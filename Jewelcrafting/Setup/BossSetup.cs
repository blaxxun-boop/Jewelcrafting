using System.Collections.Generic;
using System.Linq;
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
			aoePoison = 400f
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
	
	private class RemoveBossDestructible: MonoBehaviour
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

					Character boss = Character.m_characters.Last();
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
		public new void Damage(HitData hit)
		{
			if (hit.GetAttacker() is Humanoid attacker && GachaSetup.worldBossBonusItems.Contains(attacker.GetCurrentWeapon().m_dropPrefab.name))
			{
				hit.m_damage.Modify(1 + Jewelcrafting.worldBossBonusWeaponDamage.Value / 100f);
			}
			base.Damage(hit);
		}
	}
}
