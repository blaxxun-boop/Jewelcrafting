using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExtendedItemDataFramework;
using UnityEngine;

namespace Jewelcrafting;

public struct SocketItem
{
	public string Name;
	public int Count;

	public SocketItem(string name, int count = 1)
	{
		Name = name;
		Count = name == "" ? 0 : count;
	}
}

public abstract class Socketable : BaseExtendedItemComponent
{
	public List<SocketItem> socketedGems = new() { new SocketItem("") };
	public bool boxSealed = false;

	protected Socketable(string id, ExtendedItemData parent) : base(id, parent)
	{
	}

	public override BaseExtendedItemComponent Clone()
	{
		Socketable copy = (Socketable)MemberwiseClone();
		copy.socketedGems = new List<SocketItem>(copy.socketedGems);
		return copy;
	}
}

public class Sockets : Socketable
{
	public int Worth = 0;
	
	public Sockets(ExtendedItemData parent) : base(typeof(Sockets).AssemblyQualifiedName, parent)
	{
	}

	public override string Serialize()
	{
		Worth = CalculateItemWorth();
		return string.Join(",", socketedGems.Select(i => i.Name).ToArray());
	}

	public override void Deserialize(string data)
	{
		socketedGems = data.Split(',').Select(s => new SocketItem(s)).ToList();
		Worth = CalculateItemWorth();
	}
	
	private int CalculateItemWorth()
	{
		int sum = socketedGems.Count * (socketedGems.Count + 1) / 2;

		foreach (string socket in socketedGems.Select(i => i.Name))
		{
			if (ObjectDB.instance.GetItemPrefab(socket) is { } gem)
			{
				if (GemStones.bossToGem.Values.Contains(gem))
				{
					return int.MaxValue;
				}
				if (GemStoneSetup.GemInfos.TryGetValue(gem.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info))
				{
					sum += info.Tier;
				}
				else if (MergedGemStoneSetup.mergedGemContents.TryGetValue(socket, out List<GemInfo> mergedGems))
				{
					sum += mergedGems.Sum(info => info.Tier);
				}
			}
		}

		return sum;
	}
}

public class SocketBag : Socketable
{
	public SocketBag(ExtendedItemData parent) : base(typeof(SocketBag).AssemblyQualifiedName, parent)
	{
	}

	public override string Serialize()
	{
		return string.Join(",", socketedGems.Select(i => $"{i.Name}:{i.Count}").ToArray());
	}

	public override void Deserialize(string data)
	{
		socketedGems = data.Split(',').Select(s => { string[] split = s.Split(':'); return new SocketItem(split[0], split.Length > 1 && int.TryParse(split[1], out int value) ? value : 0); }).ToList();
	}
}

public class Box : Socketable
{
	public float progress;

	public int Tier => ObjectDB.instance.GetItemPrefab(socketedGems[0].Name) is { } gem && GemStoneSetup.GemInfos.TryGetValue(gem.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info) ? info.Tier - 1 : 0;

	public Box(ExtendedItemData parent) : base(typeof(Box).AssemblyQualifiedName, parent)
	{
		socketedGems.Add(new SocketItem(""));
	}

	public override string Serialize()
	{
		return string.Join(",", socketedGems.Select(i => i.Name).ToArray()) + (boxSealed || progress > 0 ? $"|{progress.ToString(CultureInfo.InvariantCulture)}" : "");
	}

	public override void Deserialize(string data)
	{
		string[] boxData = data.Split('|');
		if (boxData.Length > 1)
		{
			float.TryParse(boxData[1], NumberStyles.Float, CultureInfo.InvariantCulture, out progress);
			boxSealed = progress < 100;
		}
		socketedGems = boxData[0].Split(',').Select(s => new SocketItem(s)).ToList();
	}

	public void AddProgress(float amount)
	{
		progress += amount;
		if (progress >= 100)
		{
			if (socketedGems[0].Name == "Boss_Crystal_7" || socketedGems[1].Name == "Boss_Crystal_7")
			{
				if (Random.value <= Jewelcrafting.boxBossGemMergeChance.Value / 100f)
				{
					socketedGems[0] = new SocketItem("Friendship_Group_Gem");
					socketedGems.RemoveAt(1);
				}
				else
				{
					socketedGems[0] = new SocketItem("Shattered_Cyan_Crystal");
					socketedGems[1] = new SocketItem("Shattered_Cyan_Crystal");
				}
			}
			else if (Random.value < Jewelcrafting.boxMergeChances[ItemData.m_shared.m_name][Tier].Value / 100f)
			{
				if (GemStoneSetup.GemInfos.TryGetValue(ObjectDB.instance.GetItemPrefab(socketedGems[0].Name).GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info1))
				{
					if (GemStoneSetup.GemInfos.TryGetValue(ObjectDB.instance.GetItemPrefab(socketedGems[1].Name).GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info2))
					{
						socketedGems[0] = new SocketItem(MergedGemStoneSetup.mergedGems[info1.Type][info2.Type][Tier].name);
						socketedGems.RemoveAt(1);
					}
				}
			}
			else
			{
				socketedGems[0] = new SocketItem(GemStones.gemToShard[socketedGems[0].Name].name);
				socketedGems[1] = new SocketItem(GemStones.gemToShard[socketedGems[1].Name].name);
			}

			boxSealed = false;
			progress = 100;
		}
		Save();
	}
}
