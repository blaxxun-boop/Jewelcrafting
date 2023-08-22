using System.Globalization;
using HarmonyLib;
using ItemDataManager;
using UnityEngine;

namespace Jewelcrafting;

public class PositionStorage : ItemData
{
	public Vector3 Position;

	public override void Save() => Value = Position.x.ToString(CultureInfo.InvariantCulture) + "|" + Position.y.ToString(CultureInfo.InvariantCulture) + "|" + Position.z.ToString(CultureInfo.InvariantCulture);

	public override void Load()
	{
		string[] numbers = Value.Split('|');
		Position = Vector3.zero;
		if (numbers.Length == 3)
		{
			float.TryParse(numbers[0], out Position.x);
			float.TryParse(numbers[1], out Position.y);
			float.TryParse(numbers[2], out Position.z);
		}
	}
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(bool))]
public static class TagItemsWithPosition
{
	private static void Postfix(ItemDrop.ItemData? __result)
	{
		if (__result is not null && Utils.IsSocketableItem(__result))
		{
			__result.Data().GetOrCreate<PositionStorage>().Position = Player.m_localPlayer.transform.position;
		}
	}
}
