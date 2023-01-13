using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ItemDataManager;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting;

public struct SocketItem
{
	public readonly string Name;
	public int Count;

	public SocketItem(string name, int count = 1)
	{
		Name = name;
		Count = name == "" ? 0 : count;
	}
}

public abstract class ItemContainer : ItemData
{
	public bool boxSealed = false;

	public abstract Inventory ReadInventory();

	public abstract void SaveSocketsInventory(Inventory inv);
}

public abstract class Socketable : ItemContainer
{
	public List<SocketItem> socketedGems = new() { new SocketItem("") };
	
	public override Inventory ReadInventory()
	{
		int size = socketedGems.Count;
		int width = size % Jewelcrafting.gemBagSlotsColumns.Value == 0 && size / Jewelcrafting.gemBagSlotsColumns.Value <= 4 ? Jewelcrafting.gemBagSlotsColumns.Value : Enumerable.Range(1, 8).Reverse().First(n => size % n == 0);
		Inventory inv = new("Sockets", Player.m_localPlayer.GetInventory().m_bkg, width, socketedGems.Count / width);
		int slot = 0;
		foreach (SocketItem gem in socketedGems)
		{
			if (gem.Name != "" && ObjectDB.instance.GetItemPrefab(gem.Name) is { } prefab)
			{
				ItemDrop.ItemData itemData = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
				itemData.m_dropPrefab = prefab;
				itemData.m_stack = gem.Count;
				itemData.m_gridPos = new Vector2i(slot % inv.m_width, slot / inv.m_width);
				inv.m_inventory.Add(itemData);
			}
			++slot;
		}
		return inv;
	}

	public override void SaveSocketsInventory(Inventory inv)
	{
		for (int i = 0; i < socketedGems.Count; ++i)
		{
			socketedGems[i] = new SocketItem("");
		}
		foreach (ItemDrop.ItemData item in inv.m_inventory)
		{
			socketedGems[item.m_gridPos.x + item.m_gridPos.y * inv.m_width] = new SocketItem(item.m_dropPrefab.name, item.m_stack);
		}
	}
}

public class Sockets : Socketable
{
	public int Worth = 0;
	
	public override void Save()
	{
		Worth = CalculateItemWorth();
		Value = string.Join(",", socketedGems.Select(i => i.Name).ToArray());
	}

	public override void Load()
	{
		socketedGems = Value.Split(',').Select(s => new SocketItem(s)).ToList();
		Worth = CalculateItemWorth();
	}
	
	private int CalculateItemWorth()
	{
		int sum = socketedGems.Count * (socketedGems.Count + 1) / 2;

		foreach (string socket in socketedGems.Select(i => i.Name))
		{
			if (ObjectDB.instance?.GetItemPrefab(socket) is { } gem)
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

public interface ItemBag
{
}

public class SocketBag : Socketable, ItemBag
{
	public override void Save()
	{
		Value = string.Join(",", socketedGems.Select(i => $"{i.Name}:{i.Count}").ToArray());
	}

	public override void Load()
	{
		socketedGems = Value.Split(',').Select(s => { string[] split = s.Split(':'); return new SocketItem(split[0], split.Length > 1 && int.TryParse(split[1], out int value) ? value : 0); }).ToList();
	}
}

public class InventoryBag : ItemContainer, ItemBag
{
	private Inventory inventory = new("Items", Player.m_localPlayer?.GetInventory().m_bkg, Jewelcrafting.gemBoxSlotsColumns.Value, Jewelcrafting.gemBoxSlotsRows.Value);
	
	public override void Save()
	{
		ZPackage pkg = new();
		inventory.Save(pkg);
		Value = $"{inventory.m_width};{inventory.m_height};{Convert.ToBase64String(pkg.GetArray())}";
	}

	public override void Load()
	{
		string[] info = Value.Split(';');
		if (info.Length > 2 && int.TryParse(info[0], out int width) && int.TryParse(info[1], out int height))
		{
			inventory = new Inventory("Items", Player.m_localPlayer?.GetInventory().m_bkg, width, height);
			inventory.Load(new ZPackage(Convert.FromBase64String(info[2])));
		}
	}
	
	public override Inventory ReadInventory() => inventory;

	public override void SaveSocketsInventory(Inventory inv) => inventory = inv;
}

public class Frame : Socketable
{

}

public class Box : Socketable
{
	private bool soundPlayed = false;
	public float progress;

	public int Tier => ObjectDB.instance.GetItemPrefab(socketedGems[0].Name) is { } gem && GemStoneSetup.GemInfos.TryGetValue(gem.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info) ? info.Tier - 1 : 0;

	public Box()
	{
		socketedGems.Add(new SocketItem(""));
	}

	public override Inventory ReadInventory()
	{
		if (!soundPlayed && progress == 100)
		{
			Debug.Log("Playing Sound");
			GameObject sound = Object.Instantiate(socketedGems.Count > 1 ? GemEffectSetup.fusingFailSound : GemEffectSetup.fusingSuccessSound, Player.m_localPlayer.transform.position, Quaternion.identity);
			sound.GetComponent<AudioSource>().Play();
			soundPlayed = true;
		}
		return base.ReadInventory();
	}

	public override void Save()
	{
		Value = string.Join(",", socketedGems.Select(i => i.Name).ToArray()) + (boxSealed || progress > 0 ? $"|{progress.ToString(CultureInfo.InvariantCulture)}" : "");
	}

	public override void Load()
	{
		string[] boxData = Value.Split('|');
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
			else if (Random.value < Jewelcrafting.boxMergeChances[Item.m_shared.m_name][Tier].Value / 100f)
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
