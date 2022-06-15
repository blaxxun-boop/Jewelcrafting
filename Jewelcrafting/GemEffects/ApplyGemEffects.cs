using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupEquipment))]
public class TrackEquipmentChanges
{
	public static event Action? OnEffectRecalc;

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

		float weaponMultiplier = player.m_rightItem?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon && player.m_leftItem?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ? 0.6f : 1;

		Dictionary<Effect, object> effects = new();
		
		Utils.ApplyToAllPlayerItems(player, item =>
		{
			if (item?.Extended()?.GetComponent<Sockets>() is { } itemSockets)
			{
				GemLocation location = Utils.GetGemLocation(item.m_shared);

				foreach (string socket in itemSockets.socketedGems)
				{
					if (Jewelcrafting.EffectPowers.TryGetValue(socket.GetStableHashCode(), out Dictionary<GemLocation, EffectPower> locationPowers) && locationPowers.TryGetValue(location, out EffectPower effectPower))
					{
						if (!effects.TryGetValue(effectPower.Effect, out object effectValue))
						{
							EffectDef.ConfigTypes.TryGetValue(effectPower.Effect, out Type type);
							type ??= typeof(DefaultPower);
							effectValue = effects[effectPower.Effect] = Activator.CreateInstance(type);
						}

						float multiplier = item == player.m_rightItem || item == player.m_leftItem ? weaponMultiplier : 1;

						foreach (FieldInfo field in effectValue.GetType().GetFields())
						{
							field.SetValue(effectValue, ((float)field.GetValue(effectValue) + (float)field.GetValue(effectPower.Config)) * multiplier);
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

		Sockets? leftItemSockets = player.m_leftItem?.Extended()?.GetComponent<Sockets>();
		for (int i = 0; i < 5; ++i)
		{
			if (leftItemSockets?.socketedGems.Count > i && VisualEffects.weaponEffectPrefabs.TryGetValue(leftItemSockets.socketedGems[i], out Dictionary<Skills.SkillType, GameObject> effectsDict) && effectsDict.TryGetValue(player.m_leftItem!.m_shared.m_skillType, out GameObject effectName))
			{
				zdo.m_ints[$"JewelCrafting LeftHand Effect {i}".GetStableHashCode()] = effectName.name.GetStableHashCode();
			}
			else
			{
				zdo.m_ints.Remove($"JewelCrafting LeftHand Effect {i}".GetStableHashCode());
			}
		}
		
		Sockets? rightItemSockets = player.m_rightItem?.Extended()?.GetComponent<Sockets>();
		for (int i = 0; i < 5; ++i)
		{
			if (rightItemSockets?.socketedGems.Count > i && VisualEffects.weaponEffectPrefabs.TryGetValue(rightItemSockets.socketedGems[i], out Dictionary<Skills.SkillType, GameObject> effectsDict) && effectsDict.TryGetValue(player.m_rightItem!.m_shared.m_skillType, out GameObject effectName))
			{
				zdo.m_ints[$"JewelCrafting RightHand Effect {i}".GetStableHashCode()] = effectName.name.GetStableHashCode();
			}
			else
			{
				zdo.m_ints.Remove($"JewelCrafting RightHand Effect {i}".GetStableHashCode());
			}
		}

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
		
		OnEffectRecalc?.Invoke();
	}
}
