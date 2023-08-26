using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting;

public class Visual
{
	public static readonly Dictionary<VisEquipment, Visual> visuals = new();

	private static bool IsFingerItem(ItemDrop.ItemData item)
	{
		if (Jewelcrafting.ringSlot.Value == Jewelcrafting.Toggle.Off)
		{
			return false;
		}
		
		string name = item.m_shared.m_name;
		return name.CustomStartsWith("$jc_ring_");
	}

	private static bool IsNeckItem(ItemDrop.ItemData item)
	{
		if (Jewelcrafting.necklaceSlot.Value == Jewelcrafting.Toggle.Off)
		{
			return false;
		}
		
		string name = item.m_shared.m_name;
		return name.CustomStartsWith("$jc_necklace_");
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
	private static class ApplyStatusEffects
	{
		private static void CollectEffects(Humanoid humanoid, HashSet<StatusEffect?> statusEffects)
		{
			if (humanoid is Player player && visuals.TryGetValue(player.m_visEquipment, out Visual visual))
			{
				if (visual.equippedFingerItem?.m_shared.m_equipStatusEffect is { } fingerStatusEffect)
				{
					statusEffects.Add(fingerStatusEffect);
				}
				if (humanoid.HaveSetEffect(visual.equippedFingerItem))
				{
					statusEffects.Add(visual.equippedFingerItem!.m_shared.m_equipStatusEffect);
				}
				if (visual.equippedNeckItem?.m_shared.m_equipStatusEffect is { } neckStatusEffect)
				{
					statusEffects.Add(neckStatusEffect);
				}
				if (humanoid.HaveSetEffect(visual.equippedNeckItem))
				{
					statusEffects.Add(visual.equippedNeckItem!.m_shared.m_equipStatusEffect);
				}
			}
		}
		
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
		{
			List<CodeInstruction> instructions = instructionsEnumerable.ToList();
			instructions.InsertRange(2, new []
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldloc_0),
				new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ApplyStatusEffects), nameof(CollectEffects))),
			});
			return instructions;
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetSetCount))]
	private static class IncreaseSetCount
	{
		private static void Postfix(Humanoid __instance, string setName, ref int __result)
		{
			if (__instance is Player player && visuals.TryGetValue(player.m_visEquipment, out Visual visual))
			{
				if (visual.equippedFingerItem?.m_shared.m_name == setName)
				{
					++__result;
				}
				if (visual.equippedNeckItem?.m_shared.m_name == setName)
				{
					++__result;
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
	private static class IsItemEquiped
	{
		private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
		{
			if (__instance is Player player)
			{
				Visual visual = visuals[player.m_visEquipment];
				if (visual.equippedFingerItem == item || visual.equippedNeckItem == item)
				{
					__result = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
	private static class AddVisual
	{
		[HarmonyPriority(Priority.First)]
		private static void Prefix(Player __instance)
		{
			visuals.Add(__instance.GetComponent<VisEquipment>(), new Visual(__instance.GetComponent<VisEquipment>()));
		}
	}

	[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.OnEnable))]
	private static class AddVisualOnEnable
	{
		private static void Postfix(VisEquipment __instance)
		{
			if (!visuals.ContainsKey(__instance) && __instance.m_isPlayer)
			{
				visuals[__instance] = new Visual(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.OnDisable))]
	private static class RemoveVisualOnDisable
	{
		private static void Postfix(VisEquipment __instance)
		{
			visuals.Remove(__instance);
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
	private static class SetupVisEquipment
	{
		private static void Prefix(Humanoid __instance)
		{
			if (__instance is Player player && visuals.TryGetValue(player.m_visEquipment, out Visual visual))
			{
				visual.setFingerItem(visual.equippedFingerItem is null ? "" : visual.equippedFingerItem.m_dropPrefab.name);
				visual.setNeckItem(visual.equippedNeckItem is null ? "" : visual.equippedNeckItem.m_dropPrefab.name);
			}
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
	private static class UnequipAllItems
	{
		private static void Prefix(Humanoid __instance)
		{
			if (__instance is Player player)
			{
				player.UnequipItem(visuals[player.m_visEquipment].equippedFingerItem, false);
				player.UnequipItem(visuals[player.m_visEquipment].equippedNeckItem, false);
			}
		}
	}

	[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
	private static class UpdateEquipmentVisuals
	{
		private static void Postfix(VisEquipment __instance)
		{
			if (__instance.m_isPlayer)
			{
				visuals[__instance].updateEquipmentVisuals();
			}
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
	private static class EquipItem
	{
		private static bool Equip(Humanoid humanoid, ItemDrop.ItemData item, bool triggerEquipmentEffects)
		{
			if (humanoid is Player player)
			{
				if (IsFingerItem(item))
				{
					player.UnequipItem(visuals[player.m_visEquipment].equippedFingerItem, triggerEquipmentEffects);
					visuals[player.m_visEquipment].equippedFingerItem = item;
					return true;
				}
				if (IsNeckItem(item))
				{
					player.UnequipItem(visuals[player.m_visEquipment].equippedNeckItem, triggerEquipmentEffects);
					visuals[player.m_visEquipment].equippedNeckItem = item;
					return true;
				}
			}
			return false;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionEnumerable)
		{
			List<CodeInstruction> instructions = instructionEnumerable.ToList();
			int index = instructions.FindLastIndex(instruction => instruction.LoadsConstant(ItemDrop.ItemData.ItemType.Utility));
			Label? target;
			do
			{
				++index;
			} while (!instructions[index].Branches(out target));
			instructions.InsertRange(index + 1, new[]
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EquipItem), nameof(Equip))),
				new CodeInstruction(OpCodes.Brtrue, target!.Value),
			});

			return instructions;
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
	private static class UnequipItem
	{
		private static void Unequip(Humanoid humanoid, ItemDrop.ItemData item)
		{
			if (humanoid is Player player)
			{
				if (visuals[player.m_visEquipment].equippedFingerItem == item)
				{
					visuals[player.m_visEquipment].equippedFingerItem = null;
				}
				if (visuals[player.m_visEquipment].equippedNeckItem == item)
				{
					visuals[player.m_visEquipment].equippedNeckItem = null;
				}
			}
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionEnumerable)
		{
			MethodInfo setupEquipment = AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.SetupEquipment));
			List<CodeInstruction> instructions = instructionEnumerable.ToList();
			int index = instructions.FindIndex(instruction => instruction.Calls(setupEquipment));
			instructions.InsertRange(index - 1, new[]
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnequipItem), nameof(Unequip))),
			});
			return instructions;
		}
	}

	private readonly VisEquipment visEquipment;
	public ItemDrop.ItemData? equippedFingerItem;
	public ItemDrop.ItemData? equippedNeckItem;
	private string fingerItem = "";
	private string neckItem = "";
	private List<GameObject> fingerItemInstances = new();
	private List<GameObject> neckItemInstances = new();
	public int currentFingerItemHash;
	public int currentNeckItemHash;

	private Visual(VisEquipment visEquipment)
	{
		this.visEquipment = visEquipment;
	}

	private int getHash(string zdoKey, string equippedItem)
	{
		if (visEquipment.m_nview.GetZDO() is { } zdo)
		{
			return zdo.GetInt(zdoKey);
		}
		return string.IsNullOrEmpty(equippedItem) ? 0 : equippedItem.GetStableHashCode();
	}

	private void updateEquipmentVisuals()
	{
		if (setFingerEquipped(getHash("FingerItem", fingerItem)) || setNeckEquipped(getHash("NeckItem", neckItem)))
		{
			visEquipment.UpdateLodgroup();
		}
	}

	private bool setNeckEquipped(int hash)
	{
		if (currentNeckItemHash == hash)
		{
			return false;
		}
		foreach (GameObject neckItemInstance in neckItemInstances)
		{
			Object.Destroy(neckItemInstance);
		}
		neckItemInstances.Clear();
		currentNeckItemHash = hash;
		if (hash != 0)
		{
			neckItemInstances = visEquipment.AttachArmor(hash);
		}
		return true;
	}

	private void setNeckItem(string name)
	{
		if (neckItem == name)
		{
			return;
		}
		neckItem = name;
		if (visEquipment.m_nview.GetZDO() is { } zdo && visEquipment.m_nview.IsOwner())
		{
			zdo.Set("NeckItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}

	private bool setFingerEquipped(int hash)
	{
		if (currentFingerItemHash == hash)
		{
			return false;
		}
		foreach (GameObject fingerItemInstance in fingerItemInstances)
		{
			Object.Destroy(fingerItemInstance);
		}
		fingerItemInstances.Clear();
		currentFingerItemHash = hash;
		if (hash != 0)
		{
			fingerItemInstances = visEquipment.AttachArmor(hash);
		}
		return true;
	}

	private void setFingerItem(string name)
	{
		if (fingerItem == name)
		{
			return;
		}
		fingerItem = name;
		if (visEquipment.m_nview.GetZDO() is { } zdo && visEquipment.m_nview.IsOwner())
		{
			zdo.Set("FingerItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}
}
