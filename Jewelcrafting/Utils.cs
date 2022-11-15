using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using ExtendedItemDataFramework;
using Groups;
using HarmonyLib;
using Jewelcrafting.GemEffects;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting;

public static class Utils
{
	private static readonly MethodInfo MemberwiseCloneMethod = AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone");
	public static T Clone<T>(T input) where T : notnull => (T)MemberwiseCloneMethod.Invoke(input, Array.Empty<object>());

	public static bool IsSocketableItem(ItemDrop.ItemData.SharedData item)
	{
		return item.m_itemType is
			       ItemDrop.ItemData.ItemType.Bow or
			       ItemDrop.ItemData.ItemType.Chest or
			       ItemDrop.ItemData.ItemType.Hands or
			       ItemDrop.ItemData.ItemType.Helmet or
			       ItemDrop.ItemData.ItemType.Legs or
			       ItemDrop.ItemData.ItemType.Shield or
			       ItemDrop.ItemData.ItemType.Shoulder or
			       ItemDrop.ItemData.ItemType.Utility or
			       ItemDrop.ItemData.ItemType.Tool or
			       ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
		       (item.m_itemType is ItemDrop.ItemData.ItemType.OneHandedWeapon && !item.m_attack.m_consumeItem);
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

	public static GemLocation GetGemLocation(ItemDrop.ItemData.SharedData item) => item.m_itemType switch
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
			Skills.SkillType.Axes => GemLocation.Axe,
			Skills.SkillType.Bows => GemLocation.Bow,
			Skills.SkillType.Pickaxes => GemLocation.Tool,
			Skills.SkillType.Unarmed when item.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon => GemLocation.Knife,
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

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
	private static class TransferEIDFComponentsOnUpgrade
	{
		private static ItemDrop.ItemData BackupOldComponents(ItemDrop.ItemData item, List<BaseExtendedItemComponent> components)
		{
			if (item.Extended() is { } extended)
			{
				components.AddRange(extended.Components);
				extended.Components.Clear();
			}
			return item;
		}

		private static ItemDrop.ItemData? AddPreviousComponents(ItemDrop.ItemData? item, List<BaseExtendedItemComponent> components)
		{
			if (item?.Extended() is { } extended)
			{
				extended.Components.AddRange(components);
				extended.Save();
				foreach (BaseExtendedItemComponent component in components)
				{
					component.ItemData = extended;
				}
			}
			return item;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			LocalBuilder localVar = ilg.DeclareLocal(typeof(List<BaseExtendedItemComponent>));
			yield return new CodeInstruction(OpCodes.Newobj, localVar.LocalType!.GetConstructor(Array.Empty<Type>()));
			yield return new CodeInstruction(OpCodes.Stloc, localVar.LocalIndex);

			MethodInfo itemDeleter = AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(ItemDrop.ItemData) });
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(itemDeleter))
				{
					yield return new CodeInstruction(OpCodes.Ldloc, localVar.LocalIndex);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(TransferEIDFComponentsOnUpgrade), nameof(BackupOldComponents)));
				}
				yield return instruction;
				// handle mods replacing the method, recognize it by retval
				if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && instruction.operand is MethodInfo method && method.ReturnType == typeof(ItemDrop.ItemData))
				{
					yield return new CodeInstruction(OpCodes.Ldloc, localVar.LocalIndex);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(TransferEIDFComponentsOnUpgrade), nameof(AddPreviousComponents)));
				}
			}
		}
	}

	[HarmonyPatch(typeof(ExtendedItemData), nameof(ExtendedItemData.ExtendedClone))]
	private static class ReplaceItemDataInComponentsOnClone
	{
		private static void Postfix(ExtendedItemData __result)
		{
			foreach (BaseExtendedItemComponent component in __result.Components)
			{
				component.ItemData = __result;
			}
		}
	}

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
		if (timeSpan.Hours >= 1 && timeSpan.TotalDays < 30)
		{
			if (timeSpan.TotalDays >= 1)
			{
				timeString += " and ";
			}
			timeString += $"{timeSpan.Hours} hour" + (timeSpan.Hours >= 2 ? "s" : "");
		}
		if (timeSpan.Minutes >= 1 && timeSpan.TotalHours < 24)
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

	public static ItemDrop? getRandomGem(int tier = 0, GemType? type = null)
	{
		if (tier == -1)
		{
			return (type is not null ? GemStoneSetup.shardColors[type.Value] : GemStoneSetup.shardColors.Values.ElementAt(Random.Range(0, GemStoneSetup.shardColors.Count))).GetComponent<ItemDrop>();
		}
		IEnumerable<List<GemDefinition>> defLists = GemStoneSetup.Gems.Where(kv => type is null || kv.Key == type).Select(kv => kv.Value).Where(g => g.Count > 1);
		List<GemDefinition> defs = (tier == 0 ? defLists.SelectMany(g => g) : defLists.Where(g => g.Count > tier - 1).Select(g => g[tier - 1])).ToList();
		return defs.Count > 0 ? defs[Random.Range(0, defs.Count)].Prefab.GetComponent<ItemDrop>() : null;
	}
}
