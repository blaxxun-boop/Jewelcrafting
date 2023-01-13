using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using ItemDataManager;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

[HarmonyPatch]
public class TrackEquipmentChanges
{
	public static Dictionary<int, int> VisualEquipmentInts = new();

	private static IEnumerable<MethodInfo> TargetMethods() => new[]
	{
		AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.SetupEquipment)),
		AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.ShowHandItems))
	};

	[HarmonyPriority(Priority.Low)]
	private static void Postfix(Humanoid __instance)
	{
		if (__instance is Player player && (player == Player.m_localPlayer || !ZNetScene.instance))
		{
			CalculateEffects(player);
		}
	}

	// HideHandItems called repeatedly when swimming, prevent recalculating all the time
	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.HideHandItems))]
	private static class TrackHideHandItems
	{
		private static void Prefix(Humanoid __instance, out bool __state) => __state = __instance.m_leftItem is not null || __instance.m_rightItem is not null;

		[HarmonyPriority(Priority.Low)]
		private static void Postfix(Humanoid __instance, bool __state)
		{
			if (__state && __instance == Player.m_localPlayer)
			{
				CalculateEffects((Player)__instance);
			}
		}
	}

	public static void CalculateEffects(Player player)
	{
		float weaponMultiplier = (player.m_rightItem ?? player.m_hiddenRightItem)?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon && (player.m_leftItem ?? player.m_hiddenLeftItem)?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ? 0.6f : 1;

		Dictionary<Effect, object> effects = new();

		void ApplyEffectPowers(List<EffectPower> effectPowers, float multiplier = 1)
		{
			foreach (EffectPower effectPower in effectPowers)
			{
				if (!effects.TryGetValue(effectPower.Effect, out object effectValue))
				{
					effectValue = effects[effectPower.Effect] = Utils.Clone(effectPower.Config);
					foreach (FieldInfo field in effectPower.Config.GetType().GetFields())
					{
						field.SetValue(effectValue, (float)field.GetValue(effectValue) * multiplier);
					}
				}
				else
				{
					foreach (FieldInfo field in effectPower.Config.GetType().GetFields())
					{
						field.SetValue(effectValue, field.GetCustomAttribute<PowerAttribute>().Add((float)field.GetValue(effectValue), (float)field.GetValue(effectPower.Config) * multiplier));
					}
				}
			}
		}

		Utils.ApplyToAllPlayerItems(player, item =>
		{
			if (item?.Data().Get<Sockets>() is { } itemSockets)
			{
				GemLocation location = Utils.GetGemLocation(item.m_shared, player);

				foreach (string socket in itemSockets.socketedGems.Select(i => i.Name))
				{
					if (Jewelcrafting.EffectPowers.TryGetValue(socket.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(location, out List<EffectPower> effectPowers))
					{
						float multiplier = item == player.m_rightItem || item == player.m_leftItem || item == player.m_hiddenRightItem || item == player.m_hiddenRightItem ? weaponMultiplier : 1;
						ApplyEffectPowers(effectPowers, multiplier);
					}
				}
			}
		});

		Dictionary<GemType, int> gemDistribution = Synergy.GetGemDistribution(player);
		List<SynergyDef> synergies = Jewelcrafting.Synergies.Where(synergy => synergy.IsActive(gemDistribution)).ToList();
		foreach (SynergyDef synergy in synergies)
		{
			ApplyEffectPowers(synergy.EffectPowers);
		}

		if (Synergy.activeSynergyDisplay)
		{
			Synergy.activeSynergyDisplay.text.text = synergies.Count.ToString();
		}

		ZDO? zdo = player.m_nview.m_zdo;
		VisualEquipmentInts = zdo is null ? new Dictionary<int, int>() : zdo.m_ints ??= new Dictionary<int, int>();

		StoreSocketGems(VisualEquipmentInts, VisSlot.HandLeft, player.m_leftItem);
		StoreSocketGems(VisualEquipmentInts, VisSlot.BackLeft, player.m_hiddenLeftItem);
		StoreSocketGems(VisualEquipmentInts, VisSlot.HandRight, player.m_rightItem);
		StoreSocketGems(VisualEquipmentInts, VisSlot.BackRight, player.m_hiddenRightItem);

		if (zdo is null)
		{
			return;
		}

		zdo.m_byteArrays ??= new Dictionary<int, byte[]>();
		foreach (string effect in Utils.zdoNames.Values)
		{
			zdo.m_byteArrays[effect.GetStableHashCode()] = Array.Empty<byte>();
		}
		foreach (KeyValuePair<Effect, object> kv in effects)
		{
			int effectHash = kv.Key.ZDOName().GetStableHashCode();

			byte[] buffer = new byte[Marshal.SizeOf(kv.Value.GetType())];
			unsafe
			{
				fixed (void* target = &buffer[0])
				{
					Marshal.StructureToPtr(kv.Value, (IntPtr)target, false);
				}
			}
			zdo.m_byteArrays[effectHash] = buffer;
		}

		zdo.IncreseDataRevision();

		API.InvokeEffectRecalc();
	}

	private static void StoreSocketGems(Dictionary<int, int> zdoInts, VisSlot part, ItemDrop.ItemData? item)
	{
		Dictionary<string, GameObject[]>? effectPrefabs = item is null ? null : VisualEffects.prefabDict(item.m_shared);

		int i = 0;
		if (effectPrefabs is not null && item!.Data().Get<Sockets>() is { } itemSockets)
		{
			HashSet<string> effectNames = new();
			foreach (string socket in itemSockets.socketedGems.Select(i => i.Name))
			{
				if (effectPrefabs.TryGetValue(socket, out GameObject[] effects))
				{
					foreach (GameObject effect in effects)
					{
						effectNames.Add(effect.name);
					}
				}
			}
			foreach (string effectName in effectNames)
			{
				zdoInts[$"JewelCrafting {part} Effect {i++}".GetStableHashCode()] = effectName.GetStableHashCode();
			}
		}

		while (zdoInts.ContainsKey($"JewelCrafting {part} Effect {i}".GetStableHashCode()))
		{
			zdoInts[$"JewelCrafting {part} Effect {i++}".GetStableHashCode()] = 0;
		}
	}

	[HarmonyPatch(typeof(ArmorStand), nameof(ArmorStand.RPC_SetVisualItem))]
	private static class AttachArmorStandItemSocketZDO
	{
		private static void Prefix(ArmorStand __instance, int index)
		{
			if (__instance.m_nview?.IsOwner() == true)
			{
				StoreSocketGems(__instance.m_nview.GetZDO().m_ints, __instance.m_slots[index].m_slot, __instance.m_queuedItem);
			}
		}
	}

	[HarmonyPatch(typeof(ItemStand), nameof(ItemStand.RPC_SetVisualItem))]
	private static class AttachItemStandItemSocketZDO
	{
		private static void Prefix(ItemStand __instance)
		{
			if (__instance.m_nview?.IsOwner() == true)
			{
				StoreSocketGems(__instance.m_nview.GetZDO().m_ints, VisSlot.Beard, __instance.m_queuedItem);
			}
		}
	}
}
