using System.Globalization;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace Jewelcrafting;

public class PositionStorage : BaseExtendedItemComponent
{
	public Vector3 Position;

	public PositionStorage(ExtendedItemData parent) : base(typeof(PositionStorage).AssemblyQualifiedName, parent)
	{
	}

	public override string Serialize() => Position.x.ToString(CultureInfo.InvariantCulture) + "|" + Position.y.ToString(CultureInfo.InvariantCulture) + "|" + Position.z.ToString(CultureInfo.InvariantCulture);

	public override void Deserialize(string data)
	{
		string[] numbers = data.Split('|');
		Position = Vector3.zero;
		if (numbers.Length == 3)
		{
			float.TryParse(numbers[0], out Position.x);
			float.TryParse(numbers[1], out Position.y);
			float.TryParse(numbers[2], out Position.z);
		}
	}

	public override BaseExtendedItemComponent Clone() => (PositionStorage)MemberwiseClone();
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string))]
public static class TagItemsWithPosition
{
	private static void Postfix(Inventory __instance, ItemDrop.ItemData? __result)
	{
		if (__result is not null && Utils.IsSocketableItem(__result.m_shared))
		{
			__result.Extended().AddComponent<PositionStorage>().Position = Player.m_localPlayer.transform.position;
			__result.Extended().Save();
		}
	}
}
