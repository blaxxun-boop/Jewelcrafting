using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExtendedItemDataFramework;
using UnityEngine;

namespace Jewelcrafting;

public abstract class Socketable : BaseExtendedItemComponent
{
	public List<string> socketedGems = new() { "" };
	public bool boxSealed = false;

	protected Socketable(string id, ExtendedItemData parent) : base(id, parent)
	{
	}

	public override BaseExtendedItemComponent Clone()
	{
		Socketable copy = (Socketable)MemberwiseClone();
		copy.socketedGems = new List<string>(copy.socketedGems);
		return copy;
	}
}

public class Sockets : Socketable
{
	public Sockets(ExtendedItemData parent) : base(typeof(Sockets).AssemblyQualifiedName, parent)
	{
	}

	public override string Serialize()
	{
		return string.Join(",", socketedGems.ToArray());
	}

	public override void Deserialize(string data)
	{
		socketedGems = data.Split(',').ToList();
	}
}

public class Box : Socketable
{
	public float progress;

	public int Tier => ObjectDB.instance.GetItemPrefab(socketedGems[0]) is { } gem && GemStoneSetup.GemInfos.TryGetValue(gem.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info) ? info.Tier - 1 : 0;

	public Box(ExtendedItemData parent) : base(typeof(Box).AssemblyQualifiedName, parent)
	{
		socketedGems.Add("");
	}

	public override string Serialize()
	{
		return string.Join(",", socketedGems.ToArray()) + (boxSealed ? $"|{progress.ToString(CultureInfo.InvariantCulture)}" : "");
	}

	public override void Deserialize(string data)
	{
		string[] boxData = data.Split('|');
		if (boxData.Length > 1)
		{
			float.TryParse(boxData[1], NumberStyles.Float, CultureInfo.InvariantCulture, out progress);
			boxSealed = progress != -1;
		}
		socketedGems = boxData[0].Split(',').ToList();
	}

	public void AddProgress(float amount)
	{
		progress += amount;
		if (progress >= Jewelcrafting.crystalFusionBoxMergeDuration[FusionBoxSetup.boxTier[ItemData.m_dropPrefab]].Value)
		{
			if (Random.value < Jewelcrafting.boxMergeChances[ItemData.m_shared.m_name][Tier].Value / 100f)
			{
				if (GemStoneSetup.GemInfos.TryGetValue(ObjectDB.instance.GetItemPrefab(socketedGems[0]).GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info1))
				{
					if (GemStoneSetup.GemInfos.TryGetValue(ObjectDB.instance.GetItemPrefab(socketedGems[1]).GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info2))
					{
						socketedGems[0] = MergedGemStoneSetup.mergedGems[info1.Type][info2.Type][Tier].name;
						socketedGems.RemoveAt(1);
					}
				}
			}
			else
			{
				socketedGems[0] = GemStones.gemToShard[socketedGems[0]].name;
				socketedGems[1] = GemStones.gemToShard[socketedGems[1]].name;
			}

			boxSealed = false;
			progress = -1;
		}
		Save();
	}
}
