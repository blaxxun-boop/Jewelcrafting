using System.Linq;
using ItemManager;
using Jewelcrafting.WorldBosses;
using UnityEngine;

namespace Jewelcrafting;

public static class BossSetup
{
	public static void initializeBosses(AssetBundle assets)
	{
		Utils.ConvertComponent<BossCharacter, Humanoid>(PrefabManager.RegisterPrefab(assets, "Crystal_Frost_Reaper"));
		Utils.ConvertComponent<BossCharacter, Humanoid>(PrefabManager.RegisterPrefab(assets, "Crystal_Flame_Reaper"));
		Utils.ConvertComponent<BossCharacter, Humanoid>(PrefabManager.RegisterPrefab(assets, "Crystal_Soul_Reaper"));

		PrefabManager.RegisterPrefab(assets, "Crystal_Frost_Reaper_Cage").AddComponent<RemoveBossDestructible>();
		PrefabManager.RegisterPrefab(assets, "Crystal_Flame_Reaper_Cage").AddComponent<RemoveBossDestructible>();
		PrefabManager.RegisterPrefab(assets, "Crystal_Soul_Reaper_Cage").AddComponent<RemoveBossDestructible>();
		
		BossSpawn.bossIcons["Crystal_Frost_Reaper_Cage"] = assets.LoadAsset<Sprite>("JCBossIconBlue");
		BossSpawn.bossIcons["Crystal_Flame_Reaper_Cage"] = assets.LoadAsset<Sprite>("JCBossIconRed");
		BossSpawn.bossIcons["Crystal_Soul_Reaper_Cage"] = assets.LoadAsset<Sprite>("JCBossIconGreen");
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

	private class BossCharacter : Humanoid, IDestructible
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
