using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

[HarmonyPatch]
public class TrackEquipmentChanges
{
	private static IEnumerable<MethodInfo> TargetMethods() => new[]
	{
		AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.SetupEquipment)),
		AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.HideHandItems)),
		AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.ShowHandItems)),
	};

	[HarmonyPriority(Priority.Low)]
	private static void Postfix(Humanoid __instance)
	{
		if (__instance == Player.m_localPlayer)
		{
			CalculateEffects();
		}
	}

	public static void CalculateEffects()
	{
		Player player = Player.m_localPlayer;

		float weaponMultiplier = (player.m_rightItem ?? player.m_hiddenRightItem)?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon && (player.m_leftItem ?? player.m_hiddenLeftItem)?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ? 0.6f : 1;

		Dictionary<Effect, object> effects = new();

		Utils.ApplyToAllPlayerItems(player, item =>
		{
			if (item?.Extended()?.GetComponent<Sockets>() is { } itemSockets)
			{
				GemLocation location = Utils.GetGemLocation(item.m_shared);

				foreach (string socket in itemSockets.socketedGems)
				{
					if (Jewelcrafting.EffectPowers.TryGetValue(socket.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> locationPowers) && locationPowers.TryGetValue(location, out List<EffectPower> effectPowers))
					{
						float multiplier = item == player.m_rightItem || item == player.m_leftItem || item == player.m_hiddenRightItem || item == player.m_hiddenRightItem ? weaponMultiplier : 1;

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
				}
			}
		});

		if (player.m_nview.m_zdo is not { } zdo)
		{
			return;
		}

		zdo.m_ints ??= new Dictionary<int, int>();

		StoreSocketGems(zdo, VisSlot.HandLeft, player.m_leftItem);
		StoreSocketGems(zdo, VisSlot.BackLeft, player.m_hiddenLeftItem);
		StoreSocketGems(zdo, VisSlot.HandRight, player.m_rightItem);
		StoreSocketGems(zdo, VisSlot.BackRight, player.m_hiddenRightItem);

		zdo.m_byteArrays ??= new Dictionary<int, byte[]>();
		foreach (Effect effect in (Effect[])Enum.GetValues(typeof(Effect)))
		{
			zdo.m_byteArrays.Remove(effect.ZDOName().GetStableHashCode());
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

	private static void StoreSocketGems(ZDO zdo, VisSlot part, ItemDrop.ItemData? item)
	{
		Dictionary<string, GameObject[]>? effectPrefabs = item is null ? null : VisualEffects.prefabDict(item.m_shared);

		int i = 0;
		if (effectPrefabs is not null && item.Extended()?.GetComponent<Sockets>() is {} itemSockets)
		{
			HashSet<string> effectNames = new();
			foreach (string socket in itemSockets.socketedGems)
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
				zdo.m_ints[$"JewelCrafting {part} Effect {i++}".GetStableHashCode()] = effectName.GetStableHashCode();
			}
		}

		while (zdo.m_ints.ContainsKey($"JewelCrafting {part} Effect {i}".GetStableHashCode()))
		{
			zdo.m_ints[$"JewelCrafting {part} Effect {i++}".GetStableHashCode()] = 0;
		}
	}

	[HarmonyPatch(typeof(ArmorStand), nameof(ArmorStand.RPC_SetVisualItem))]
	private static class AttachArmorStandItemSocketZDO
	{
		private static void Prefix(ArmorStand __instance, int index)
		{
			if (__instance.m_nview?.IsOwner() == true)
			{
				StoreSocketGems(__instance.m_nview.GetZDO(), __instance.m_slots[index].m_slot, __instance.m_queuedItem);
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
				StoreSocketGems(__instance.m_nview.GetZDO(), VisSlot.Beard, __instance.m_queuedItem);
			}
		}
	}
}
