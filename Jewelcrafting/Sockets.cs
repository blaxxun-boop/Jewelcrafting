using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ItemDataManager;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Jewelcrafting;

public struct SocketItem(string name, Dictionary<string, uint>? seed = null, int count = 1)
{
	public readonly string Name = name;
	public int Count = name == "" ? 0 : count;
	public readonly Dictionary<string, uint>? Seed = seed?.Count > 0 ? seed : null;

	public override int GetHashCode()
	{
		return Name.GetHashCode();
	}

	public override bool Equals(object? obj)
	{
		return obj is SocketItem item && item.Name == Name;
	}

	public string SerializeSeed()
	{
		if (Seed is null)
		{
			return "";
		}

		if (MergedGemStoneSetup.mergedGemContents.TryGetValue(Name, out List<GemInfo> infos))
		{
			ulong value = 0;
			foreach (GemInfo info in ((IEnumerable<GemInfo>)infos).Reverse())
			{
				if (!Seed.TryGetValue(info.Type.ToString(), out uint seed))
				{
					seed = Utils.GenerateSocketSeed();
				}
				value = (value << 32) + seed;
			}
		}
		return Seed.First().ToString();
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
				if (gem.Seed is { } seed)
				{
					foreach (KeyValuePair<string, uint> kv in seed)
					{
						itemData.Data().Add<SocketSeed>(kv.Key)!.Seed = kv.Value;
					}
				}
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
			socketedGems[item.m_gridPos.x + item.m_gridPos.y * inv.m_width] = new SocketItem(item.m_dropPrefab.name, count: item.m_stack, seed: item.Data().GetAll<SocketSeed>().ToDictionary(kv => kv.Key, kv => kv.Value.Seed));
		}
	}

	protected Dictionary<string, uint>? loadSeed(string[] socketInfo, int index)
	{
		Dictionary<string, uint>? seeds = null;
		if (socketInfo.Length > index && ulong.TryParse(socketInfo[index], out ulong seed))
		{
			if (MergedGemStoneSetup.mergedGemContents.TryGetValue(socketInfo[0], out List<GemInfo> infos))
			{
				seeds = new Dictionary<string, uint>
				{
					[infos[0].Type.ToString()] = (uint)(seed & 0xFFFFFFFF),
					[infos[1].Type.ToString()] = (uint)(seed >> 32),
				};
			}
			else if (ObjectDB.instance.GetItemPrefab(socketInfo[0]) is { } prefab && GemStoneSetup.GemInfos.TryGetValue(prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info))
			{
				seeds = new Dictionary<string, uint> { [info.Type.ToString()] = (uint)seed };
			}
		}
		else
		{
			foreach (GemInfo info in Utils.GetAllGemInfos(socketInfo[0]))
			{
				if (Jewelcrafting.GemsUsingPowerRanges.Contains(info.Type))
				{
					seeds ??= new Dictionary<string, uint>();
					seeds[info.Type.ToString()] = Utils.GenerateSocketSeed();
				}
			}
		}

		return seeds;
	}
}

public class Sockets : Socketable
{
	public int Worth = 0;

	public override void SaveSocketsInventory(Inventory inv)
	{
		foreach (ItemDrop.ItemData item in inv.m_inventory)
		{
			foreach (GemInfo info in Utils.GetAllGemInfos(item))
			{
				if (Jewelcrafting.GemsUsingPowerRanges.Contains(info.Type) && item.Data().Add<SocketSeed>(info.Type.ToString()) is { } newSeed)
				{
					newSeed.Seed = Utils.GenerateSocketSeed();
				}
			}
		}
		base.SaveSocketsInventory(inv);
	}

	public override void Save()
	{
		Worth = CalculateItemWorth();
		Value = string.Join(",", socketedGems.Select(i => i.Name + (i.Seed is not null ? $":{i.SerializeSeed()}" : "")).ToArray());
	}

	public override void Load()
	{
		socketedGems = Value.Split(',').Select(s =>
		{
			string[] socketInfo = s.Split(':');
			Dictionary<string, uint>? seeds = loadSeed(socketInfo, 1);
			foreach (GemInfo info in Utils.GetAllGemInfos(socketInfo[0]))
			{
				if (Jewelcrafting.GemsUsingPowerRanges.Contains(info.Type) && seeds?.ContainsKey(info.Type.ToString()) != true)
				{
					seeds ??= new Dictionary<string, uint>();
					seeds[info.Type.ToString()] = Utils.GenerateSocketSeed();
				}
			}
			return new SocketItem(socketInfo[0], seed: loadSeed(socketInfo, 1));
		}).ToList();
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

public interface ItemBag;

public class SocketBag : Socketable, ItemBag
{
	public override void Save()
	{
		Value = string.Join(",", socketedGems.Select(i => $"{i.Name}:{i.Count}" + (i.Seed is not null ? $":{i.SerializeSeed()}" : "")).ToArray());
	}

	public override void Load()
	{
		socketedGems = Value.Split(',').Select(s =>
		{
			string[] split = s.Split(':');
			return new SocketItem(split[0], count: split.Length > 1 && int.TryParse(split[1], out int value) ? value : 0, seed: loadSeed(split, 2));
		}).ToList();
	}
}

public class InventoryBag : ItemContainer, ItemBag
{
	private Inventory inventory = new("Items", Player.m_localPlayer?.GetInventory().m_bkg, Jewelcrafting.gemBoxSlotsColumns.Value, Jewelcrafting.gemBoxSlotsRows.Value);
	protected int removableItemAmount;

	public override void Save()
	{
		ZPackage pkg = new();
		inventory.Save(pkg);
		Value = $"{inventory.m_width};{inventory.m_height};{Convert.ToBase64String(pkg.GetArray())}" + (GetType() == typeof(DropChest) ? $";{removableItemAmount}" : "");
	}

	public override void Load()
	{
		string[] info = Value.Split(';');
		if (info.Length > 2 && int.TryParse(info[0], out int width) && int.TryParse(info[1], out int height))
		{
			inventory = new Inventory("Items", Player.m_localPlayer?.GetInventory().m_bkg, width, height);
			inventory.Load(new ZPackage(Convert.FromBase64String(info[2])));
			if (GetType() == typeof(DropChest) && info.Length > 3 && int.TryParse(info[3], out int allowed))
			{
				removableItemAmount = allowed;
			}
		}
	}

	public override Inventory ReadInventory() => inventory;

	public override void SaveSocketsInventory(Inventory inv) => inventory = inv;
}

public class DropChest : InventoryBag
{
	public new int removableItemAmount { get => base.removableItemAmount; set => base.removableItemAmount = value; }
}

public class Frame : Socketable
{
	protected override bool AllowStackingIdenticalValues { get; set; } = true;
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
			GameObject sound = Object.Instantiate(socketedGems.Count > 1 ? GemEffectSetup.fusingFailSound : GemEffectSetup.fusingSuccessSound, Player.m_localPlayer.transform.position, Quaternion.identity);
			sound.GetComponent<AudioSource>().Play();
			soundPlayed = true;
		}
		return base.ReadInventory();
	}

	public override void Save()
	{
		Value = string.Join(",", socketedGems.Select(i => i.Name + (i.Seed is not null ? $":{i.SerializeSeed()}" : "")).ToArray()) + (boxSealed || progress > 0 ? $"|{progress.ToString(CultureInfo.InvariantCulture)}" : "");
	}

	public override void Load()
	{
		string[] boxData = Value.Split('|');
		if (boxData.Length > 1)
		{
			float.TryParse(boxData[1], NumberStyles.Float, CultureInfo.InvariantCulture, out progress);
			boxSealed = progress < 100;
		}
		socketedGems = boxData[0].Split(',').Select(s =>
		{
			string[] socketInfo = s.Split(':');
			return new SocketItem(socketInfo[0], seed: loadSeed(socketInfo, 1));
		}).ToList();
	}

	private Dictionary<string, uint>? mergeSeeds(SocketItem a, SocketItem b)
	{
		if (a.Seed is null)
		{
			return b.Seed;
		}
		if (b.Seed is null)
		{
			return a.Seed;
		}
		return a.Seed.Concat(b.Seed).ToDictionary(kv => kv.Key, kv => kv.Value);
	}

	public void AddProgress(float amount)
	{
		progress += amount;
		if (progress >= 100)
		{
			if (socketedGems[0].Name is "JC_Common_Gembox" or "JC_Epic_Gembox")
			{
				if (Random.value <= Jewelcrafting.boxSelfMergeChances[Item.m_shared.m_name].Value / 100f)
				{
					socketedGems[0] = new SocketItem(FusionBoxSetup.Boxes[FusionBoxSetup.boxTier[Item.m_dropPrefab] + 1].name);
					socketedGems.RemoveAt(1);

					Stats.boxFusionsCompleted.Increment();
					(socketedGems[0].Name == "JC_Common_Gembox" ? Stats.commonFusionCompletedFusion : Stats.epicFusionCompletedFusion).Increment();
				}
				else
				{
					socketedGems[0] = new SocketItem(Utils.getRandomGem(-1)!.name);
					socketedGems[1] = new SocketItem(Utils.getRandomGem(-1)!.name);

					Stats.boxFusionsFailed.Increment();
					(socketedGems[0].Name == "JC_Common_Gembox" ? Stats.commonFusionFailedFusion : Stats.epicFusionFailedFusion).Increment();
				}
			}
			else if (socketedGems[0].Name == "Boss_Crystal_7" || socketedGems[1].Name == "Boss_Crystal_7")
			{
				if (Random.value <= Jewelcrafting.boxBossGemMergeChance.Value / 100f)
				{
					socketedGems[0] = new SocketItem("Friendship_Group_Gem", mergeSeeds(socketedGems[0], socketedGems[1]));
					socketedGems.RemoveAt(1);

					Stats.fusionsCompleted.Increment();
					Stats.legendaryFusionCompleted.Increment();
					Stats.legendaryFusionCompletedBoss.Increment();
				}
				else
				{
					socketedGems[0] = new SocketItem("Shattered_Cyan_Crystal");
					socketedGems[1] = new SocketItem("Shattered_Cyan_Crystal");

					Stats.fusionsFailed.Increment();
					Stats.legendaryFusionFailed.Increment();
					Stats.legendaryFusionFailedBoss.Increment();
				}
			}
			else if (Random.value <= Jewelcrafting.boxMergeChances[Item.m_shared.m_name][Tier].Value / 100f)
			{
				if (GemStoneSetup.GemInfos.TryGetValue(ObjectDB.instance.GetItemPrefab(socketedGems[0].Name).GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info1))
				{
					if (GemStoneSetup.GemInfos.TryGetValue(ObjectDB.instance.GetItemPrefab(socketedGems[1].Name).GetComponent<ItemDrop>().m_itemData.m_shared.m_name, out GemInfo info2))
					{
						socketedGems[0] = new SocketItem(MergedGemStoneSetup.mergedGems[info1.Type][info2.Type][Tier].name, mergeSeeds(socketedGems[0], socketedGems[1]));
						socketedGems.RemoveAt(1);

						Stats.fusionsCompleted.Increment();
						Stats.tieredFusionCompleted[FusionBoxSetup.boxTier[Item.m_dropPrefab]].Increment();
						Stats.tieredFusionTiersCompleted[FusionBoxSetup.boxTier[Item.m_dropPrefab]][Tier].Increment();
					}
				}
			}
			else
			{
				socketedGems[0] = new SocketItem(GemStones.gemToShard[socketedGems[0].Name].name);
				socketedGems[1] = new SocketItem(GemStones.gemToShard[socketedGems[1].Name].name);

				Stats.fusionsFailed.Increment();
				Stats.tieredFusionFailed[FusionBoxSetup.boxTier[Item.m_dropPrefab]].Increment();
				Stats.tieredFusionTiersFailed[FusionBoxSetup.boxTier[Item.m_dropPrefab]][Tier].Increment();
			}

			boxSealed = false;
			progress = 100;
		}
		Save();
	}
}

public class SocketSeed : ItemData
{
	private uint seed;

	public uint Seed
	{
		get => seed;
		set
		{
			seed = value;
			Value = seed.ToString();
		}
	}

	public override void Load()
	{
		if (Utils.ItemUsesGemPowerRange(Item))
		{
			Seed = uint.Parse(Value);
		}
		else
		{
			Info.Remove(this);
		}
	}
}
