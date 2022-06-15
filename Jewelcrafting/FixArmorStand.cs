using System;
using System.Collections.Generic;
using System.Reflection;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting;

public static class FixArmorStand
{
	[HarmonyPatch(typeof(ArmorStand), nameof(ArmorStand.DropItem))]
	private static class ArmorStandFixer
	{
		private static GameObject wrapItemInEIDF(GameObject itemObject)
		{
			ItemDrop.ItemData item = itemObject.GetComponent<ItemDrop>().m_itemData;
			itemObject.GetComponent<ItemDrop>().m_itemData = new ExtendedItemData(item, item.m_stack, item.m_durability, new Vector2i(), false, item.m_quality, item.m_variant, item.m_crafterID, item.m_crafterName);
			return itemObject;
		}
		
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo rigidBodyGetter = AccessTools.DeclaredMethod(typeof(GameObject), nameof(GameObject.GetComponent), Array.Empty<Type>(), new []{ typeof(Rigidbody) });
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.Calls(rigidBodyGetter))
				{
					yield return CodeInstruction.Call(typeof(ArmorStandFixer), nameof(wrapItemInEIDF));
				}
				yield return instruction;
			}
		}
	}
}
