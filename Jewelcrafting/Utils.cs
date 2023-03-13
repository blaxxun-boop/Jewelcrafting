using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Groups;
using HarmonyLib;
using ItemDataManager;
using Jewelcrafting.GemEffects;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting;

public static class Utils
{
	private static readonly MethodInfo MemberwiseCloneMethod = AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone");
	public static T Clone<T>(T input) where T : notnull => (T)MemberwiseCloneMethod.Invoke(input, Array.Empty<object>());

	public static bool IsSocketableItem(ItemDrop item)
	{
		item.m_itemData.m_dropPrefab = item.gameObject;
		return IsSocketableItem(item.m_itemData);
	}

	public static bool IsSocketableItem(ItemDrop.ItemData item)
	{
		if (Jewelcrafting.socketBlacklist.Value.Replace(" ", "").Split(',').Contains(item.m_dropPrefab.name))
		{
			return false;
		}
		
		return item.m_shared.m_itemType is
			       ItemDrop.ItemData.ItemType.Bow or
			       ItemDrop.ItemData.ItemType.Chest or
			       ItemDrop.ItemData.ItemType.Hands or
			       ItemDrop.ItemData.ItemType.Helmet or
			       ItemDrop.ItemData.ItemType.Legs or
			       ItemDrop.ItemData.ItemType.Shield or
			       ItemDrop.ItemData.ItemType.Shoulder or
			       ItemDrop.ItemData.ItemType.Utility or
			       ItemDrop.ItemData.ItemType.Tool or
			       ItemDrop.ItemData.ItemType.TwoHandedWeapon or
			       ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft ||
		       (item.m_shared.m_itemType is ItemDrop.ItemData.ItemType.OneHandedWeapon && !item.m_shared.m_attack.m_consumeItem);
	}

	public static void ApplyToAllPlayerItems(Player player, Action<ItemDrop.ItemData?> callback)
	{
		callback(player.m_rightItem);
		callback(player.m_leftItem);
		callback(player.m_chestItem);
		callback(player.m_legItem);
		callback(player.m_ammoItem);
		callback(player.m_helmetItem);
		callback(player.m_shoulderItem);
		callback(player.m_utilityItem);
	}

	public static readonly Dictionary<Effect, string> zdoNames = ((Effect[])Enum.GetValues(typeof(Effect))).ToDictionary(e => e, e => "Jewelcrafting Socket " + e);
	public static string ZDOName(this Effect effect) => zdoNames[effect];
	public static float GetEffect(this Player player, Effect effect) => player.GetEffect<DefaultPower>(effect).Power;

	public static T GetEffect<T>(this Player player, Effect effect) where T : struct
	{
		if (player.m_nview.m_zdo?.GetByteArray(effect.ZDOName()) is not { Length: > 0 } effectBytes)
		{
			return default;
		}

		T output;
		unsafe
		{
			fixed (void* source = &effectBytes[0])
			{
				output = Marshal.PtrToStructure<T>((IntPtr)source);
			}
		}

		return output;
	}

	public static WaitForSeconds WaitEffect<T>(this Player player, Effect effect, Func<T, float> minWait, Func<T, float> maxWait) where T : struct
	{
		T config = player.GetEffect<T>(effect);
		float minSeconds = minWait(config);
		float maxSeconds = maxWait(config);
		if (minSeconds > 0)
		{
			return new WaitForSeconds(Mathf.Max(4, Random.Range(minSeconds, maxSeconds) * (1 - player.GetEffect(Effect.Timewarp) / 100f)));
		}

		float wait = 0;
		if (Jewelcrafting.SocketEffects.TryGetValue(effect, out List<EffectDef> defs))
		{
			wait = defs.Min(def => minWait((T)def.Power[0]));
		}
		return new WaitForSeconds(Mathf.Max(wait, 4));
	}

	public static GemLocation GetGemLocation(ItemDrop.ItemData.SharedData item, Player? player = null) => item.m_itemType switch
	{
		ItemDrop.ItemData.ItemType.Helmet => GemLocation.Head,
		ItemDrop.ItemData.ItemType.Chest => GemLocation.Chest,
		ItemDrop.ItemData.ItemType.Legs => GemLocation.Legs,
		ItemDrop.ItemData.ItemType.Utility => GemLocation.Utility,
		ItemDrop.ItemData.ItemType.Shoulder => GemLocation.Cloak,
		ItemDrop.ItemData.ItemType.Tool => GemLocation.Tool,
		_ => item.m_skillType switch
		{
			Skills.SkillType.Swords => GemLocation.Sword,
			Skills.SkillType.Knives => GemLocation.Knife,
			Skills.SkillType.Clubs => GemLocation.Club,
			Skills.SkillType.Polearms => GemLocation.Polearm,
			Skills.SkillType.Spears => GemLocation.Spear,
			Skills.SkillType.Blocking => GemLocation.Shield,
			Skills.SkillType.Axes => (player is not null && player == Player.m_localPlayer ? player.m_utilityItem?.m_shared.m_name == JewelrySetup.yellowNecklaceName : player?.m_visEquipment.m_currentUtilityItemHash == JewelrySetup.yellowNecklaceHash) ? GemLocation.Tool : GemLocation.Axe,
			Skills.SkillType.Bows => GemLocation.Bow,
			Skills.SkillType.Crossbows => GemLocation.Crossbow,
			Skills.SkillType.Pickaxes => GemLocation.Tool,
			Skills.SkillType.Unarmed when item.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon => GemLocation.Knife,
			Skills.SkillType.BloodMagic => GemLocation.BloodMagic,
			Skills.SkillType.ElementalMagic => GemLocation.ElementalMagic,
			_ => GemLocation.Sword
		}
	};

	public static byte[] ReadEmbeddedFileBytes(string name)
	{
		using MemoryStream stream = new();
		Assembly.GetExecutingAssembly().GetManifestResourceStream("Jewelcrafting." + name)?.CopyTo(stream);
		return stream.ToArray();
	}

	public static Texture2D loadTexture(string name)
	{
		Texture2D texture = new(0, 0);
		texture.LoadImage(ReadEmbeddedFileBytes("icons." + name));
		return texture;
	}

	public static Sprite loadSprite(string name, int width, int height) => Sprite.Create(loadTexture(name), new Rect(0, 0, width, height), Vector2.zero);

	public static bool isAdmin(ZRpc? rpc)
	{
		return rpc is null || ZNet.instance.m_adminList.Contains(rpc.GetSocket().GetHostName());
	}

	public static T ConvertStatusEffect<T>(StatusEffect statusEffect) where T : StatusEffect
	{
		T ownSE = ScriptableObject.CreateInstance<T>();

		ownSE.name = statusEffect.name;
		foreach (FieldInfo field in statusEffect.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			field.SetValue(ownSE, field.GetValue(statusEffect));
		}

		return ownSE;
	}

	public static T ConvertComponent<T, U>(GameObject gameObject) where U : MonoBehaviour where T : U
	{
		U component = gameObject.GetComponent<U>();
		T cmp = gameObject.AddComponent<T>();
		foreach (FieldInfo field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			field.SetValue(cmp, field.GetValue(component));
		}
		Object.Destroy(component);
		return cmp;
	}

	public static List<Player> GetNearbyGroupMembers(Player player, float range, bool includeSelf = false)
	{
		List<PlayerReference> groupPlayers = global::Groups.API.GroupPlayers();
		List<Player> nearbyPlayers = new();
		Player.GetPlayersInRange(player.transform.position, range, nearbyPlayers);
		nearbyPlayers.RemoveAll(p => !groupPlayers.Contains(PlayerReference.fromPlayer(p)) || (!includeSelf && p == player));
		return nearbyPlayers;
	}

	public static bool ItemAllowedInGemBag(ItemDrop.ItemData item) => GemStones.socketableGemStones.Contains(item.m_shared.m_name) || GemStoneSetup.uncutGems.ContainsValue(item.m_dropPrefab) || GemStoneSetup.shardColors.ContainsValue(item.m_dropPrefab);
	public static bool ItemAllowedInGemBox(ItemDrop.ItemData item) => JewelrySetup.upgradeableJewelry.Contains(item.m_shared.m_name);

	public static string GetHumanFriendlyTime(int seconds)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

		if (timeSpan.TotalSeconds < 60)
		{
			return "less than 1 minute";
		}

		string timeString = "";
		if (timeSpan.TotalDays >= 1)
		{
			timeString += $"{(int)timeSpan.TotalDays} day" + (timeSpan.TotalDays >= 2 ? "s" : "");
		}
		if (timeSpan is { Hours: >= 1, TotalDays: < 30 })
		{
			if (timeSpan.TotalDays >= 1)
			{
				timeString += " and ";
			}
			timeString += $"{timeSpan.Hours} hour" + (timeSpan.Hours >= 2 ? "s" : "");
		}
		if (timeSpan is { Minutes: >= 1, TotalHours: < 24 })
		{
			if (timeSpan.TotalDays >= 1 || timeSpan.Hours >= 1)
			{
				timeString += " and ";
			}
			timeString += $"{timeSpan.Minutes} minute" + (timeSpan.Minutes >= 2 ? "s" : "");
		}
		return timeString;
	}

	public static string FormatShortNumber(float num) => num.ToString(num < 100 ? "G2" : "0");

	public static string LocalizeDescDetail(Player player, Effect effect, float[] numbers)
	{
		if (EffectDef.DescriptionOverrides.TryGetValue(effect, out EffectDef.OverrideDescription overrideDesc))
		{
			if (overrideDesc(player, ref numbers) is { } desc)
			{
				return desc;
			}
		}
		return Localization.instance.Localize($"$jc_effect_{EffectDef.EffectNames[effect].ToLower()}_desc_detail", numbers.Select(FormatShortNumber).ToArray());
	}

	public static ItemDrop? getRandomGem(int tier = 0, GemType? type = null, HashSet<ItemDrop>? blackList = null)
	{
		List<ItemDrop> gems;
		if (tier == -1)
		{
			gems = type is not null ? new List<ItemDrop> { GemStoneSetup.shardColors[type.Value].GetComponent<ItemDrop>() } : GemStoneSetup.shardColors.Values.Select(i => i.GetComponent<ItemDrop>()).ToList();
		}
		else
		{
			IEnumerable<List<GemDefinition>> defLists = GemStoneSetup.Gems.Where(kv => type is null || kv.Key == type).Select(kv => kv.Value).Where(g => g.Count > 1);
			gems = (tier == 0 ? defLists.SelectMany(g => g) : defLists.Where(g => g.Count > tier - 1).Select(g => g[tier - 1])).Select(g => g.Prefab.GetComponent<ItemDrop>()).ToList();
		}
		if (blackList is not null)
		{
			List<ItemDrop> filteredGems = gems.Where(d => !blackList.Contains(d)).ToList();
			if (filteredGems.Count > 0)
			{
				gems = filteredGems;
			}
		}
		return gems.Count > 0 ? gems[Random.Range(0, gems.Count)] : null;
	}

	public static void DropPlayerItems(ItemDrop.ItemData item, int amount)
	{
		Transform transform = Player.m_localPlayer.transform;
		Vector3 position = transform.position;
		ItemDrop itemDrop = ItemDrop.DropItem(item, amount, position + transform.forward + transform.up, transform.rotation);
		itemDrop.OnPlayerDrop();
		itemDrop.GetComponent<Rigidbody>().velocity = (transform.forward + Vector3.up) * 5f;
		Player.m_localPlayer.m_dropEffects.Create(position, Quaternion.identity);
	}

	public static bool SkipBossPower() => Player.m_localPlayer.m_rightItem?.m_shared.m_buildPieces is not null;
}
