﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public class Attunement : SE_Stats
{
	private float skillLevel;
	private static readonly HashSet<Minimap.PinData> activePins = new();

	public override void SetLevel(int itemLevel, float skillLevel) => this.skillLevel = itemLevel;

	public override string GetTooltipString()
	{
		return Localization.instance.Localize("$jc_se_necklace_orange_description", (skillLevel * 1.5f).ToString(CultureInfo.CurrentCulture));
	}

	public override void UpdateStatusEffect(float dt)
	{
		m_tickTimer += dt;
		if (m_tickTimer >= 1)
		{
			AddPortalPins();
			m_tickTimer = 0;
		}
		base.UpdateStatusEffect(dt);
	}

	public override void Stop()
	{
		RemovePortalPins();
		base.Stop();
	}

	private void AddPortalPins()
	{
		HashSet<Vector3> existingPins = new(activePins.Select(p => p.m_pos));

		foreach (GameObject destructible in DestructibleSetup.ScaledDestructible.activeDestructibles)
		{
			if (global::Utils.DistanceXZ(destructible.transform.position, m_character.transform.position) <= skillLevel * 1.5f)
			{
				if (existingPins.Contains(destructible.transform.position))
				{
					existingPins.Remove(destructible.transform.position);
				}
				else
				{
					activePins.Add(Minimap.instance.AddPin(destructible.transform.position, (Minimap.PinType)AddMinimapGemstoneIcon.pinType, "", false, false));
				}
			}
		}

		List<Minimap.PinData> remove = activePins.Where(p => existingPins.Contains(p.m_pos)).ToList();
		foreach (Minimap.PinData pin in remove)
		{
			Minimap.instance.RemovePin(pin);
			activePins.Remove(pin);
		}
	}

	private void RemovePortalPins()
	{
		foreach (Minimap.PinData pinData in activePins)
		{
			Minimap.instance.RemovePin(pinData);
		}
		activePins.Clear();
	}

	[HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
	public class AddMinimapGemstoneIcon
	{
		public static int pinType;

		private static void Postfix(Minimap __instance)
		{
			pinType = __instance.m_visibleIconTypes.Length;
			bool[] visibleIcons = new bool[pinType + 1];
			Array.Copy(__instance.m_visibleIconTypes, visibleIcons, pinType);
			__instance.m_visibleIconTypes = visibleIcons;

			__instance.m_icons.Add(new Minimap.SpriteData
			{
				m_name = (Minimap.PinType)pinType,
				m_icon = JewelrySetup.gemstoneFormationIcon,
			});
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
	private static class FixupItemLevelForEquipmentStatusEffects
	{
		private static readonly MethodInfo AddStatusEffect = AccessTools.DeclaredMethod(typeof(SEMan), nameof(SEMan.AddStatusEffect), new []{ typeof(StatusEffect), typeof(bool), typeof(int), typeof(float) });

		private static void BackupItem(ItemDrop.ItemData item)
		{
			if (item.m_shared.m_equipStatusEffect is { } equip)
			{
				items[equip] = item;
			}
			if (item.m_shared.m_setStatusEffect is { } set)
			{
				items[set] = item;
			}
		}

		private static StatusEffect FetchItem(StatusEffect se)
		{
			items.TryGetValue(se, out nextItem);
			return se;
		}

		private static int GetQuality() => nextItem?.m_quality ?? 0;

		private static ItemDrop.ItemData? nextItem;
		private static readonly Dictionary<StatusEffect, ItemDrop.ItemData> items = new();

		private static void Finalizer() => items.Clear();

		[HarmonyPriority(Priority.VeryLow)]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
		{
			List<CodeInstruction> instructions = instructionsEnumerable.ToList();
			for (int i = 1; i < instructions.Count; ++i)
			{
				if (instructions[i - 1].opcode == OpCodes.Callvirt && ((MethodBase)instructions[i - 1].operand).Name == "Add" && instructions[i].opcode == OpCodes.Pop)
				{
					for (int j = i; j > 0; --j)
					{
						if (instructions[j].LoadsField(AccessTools.DeclaredField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_shared))))
						{
							List<Label> labels = instructions[j].labels;
							instructions[j].labels = new List<Label>();
							instructions.Insert(j, new CodeInstruction(OpCodes.Dup) { labels = labels });
							instructions.Insert(j + 1, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(FixupItemLevelForEquipmentStatusEffects), nameof(BackupItem))));
							break;
						}
					}
					i += 3;
				}
				if (instructions[i].Calls(AddStatusEffect) && instructions[i - 2].opcode == OpCodes.Ldc_I4_0)
				{
					instructions[i - 2] = new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(FixupItemLevelForEquipmentStatusEffects), nameof(GetQuality)));
					FieldInfo seman = AccessTools.DeclaredField(typeof(Character), nameof(Character.m_seman));
					while (!instructions[--i].LoadsField(seman)) ;
					instructions.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(FixupItemLevelForEquipmentStatusEffects), nameof(FetchItem))));
					break;
				}
			}

			return instructions;
		}
	}
}
