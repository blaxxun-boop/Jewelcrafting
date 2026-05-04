using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using ItemDataManager;
using Jewelcrafting.GemEffects;
using UnityEngine;

namespace Jewelcrafting.CrystalCase;

public class CrystalCaseInteract : MonoBehaviour, Interactable, Hoverable
{
	public static GameObject crystalCaseUIPrefab = null!;
	private static GameObject? crystalCaseWindow;

	private void Awake()
	{
		GetComponent<WearNTear>().m_onDestroyed += OnDestroyed;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (user is not Player)
		{
			return true;
		}

		if (alt)
		{
			StoreGemsInCabinet();
			return false;
		}

		crystalCaseWindow = Instantiate(crystalCaseUIPrefab, Hud.instance.m_rootObject.transform);

		CrystalCaseUI crystalCaseUI = crystalCaseWindow.GetComponent<CrystalCaseUI>();
		crystalCaseUI.Interact = this;
		UpdateGemIconElements(crystalCaseUI);

		return false;
	}

	public void UpdateGemIconElements(CrystalCaseUI ui)
	{
		Inventory inventory = LoadInventory();

		List<GemData> gems = new();
		foreach (ItemDrop.ItemData item in inventory.m_inventory)
		{
			if (Jewelcrafting.EffectPowers.TryGetValue(item.m_dropPrefab.name.GetStableHashCode(), out Dictionary<GemLocation, List<EffectPower>> gem))
			{
				GemData gemData = new() { name = item.m_shared.m_name, rawValue = GemIdentifier(item), worth = Sockets.GemWorth(item.m_dropPrefab), icon = item.GetIcon(), };
				StringBuilder sb = new("");

				foreach (KeyValuePair<GemLocation, List<EffectPower>> kv in GemStones.GroupEffectsByGemLocation(gem))
				{
					string gemName = $"$jc_socket_slot_{kv.Key.ToString().ToLower()}";
					if ((ulong)kv.Key >> 32 != 0)
					{
						if (Utils.GetGemLocationItem(kv.Key) is { } targetItem)
						{
							gemName = targetItem.m_itemData.m_shared.m_name;
						}
						else
						{
							continue;
						}
					}

					sb.Append($"<color=orange>{gemName}:</color> {string.Join(", ", kv.Value.Select(effectPower => $"$jc_effect_{EffectDef.EffectNames[effectPower.Effect].ToLower()} {Utils.DisplayGemEffectPower(effectPower, null, 0, item.Data().GetAll<SocketSeed>() is { Count: > 0 } seeds ? seeds.ToDictionary(kv => kv.Key, kv => kv.Value.Seed) : null, true)}"))}");
					sb.Append("\n");

					if (item.Data().GetAll<SocketSeed>() is { Count: > 0 } seeds)
					{
						Dictionary<string, uint> seed = seeds.ToDictionary(kv => kv.Key, kv => kv.Value.Seed);
						gemData.strength[kv.Key] = kv.Value.Where(power => power.MinPower != power.MaxPower).Sum(power => Utils.CalcRealEffectPower(0, 1, 0, power.Type, power.Effect, seed));
					}
				}
				gemData.effects = sb.ToString();

				FilterCriteria filters = item.Data()["Corrupted Item"] is null ? FilterCriteria.Uncorrupted : FilterCriteria.Corrupted;

				int tier = 0;
				if (GemStoneSetup.GemInfos.TryGetValue(item.m_shared.m_name, out GemInfo info) && GemStoneSetup.Gems[info.Type].Count > 1)
				{
					tier = info.Tier;
					filters |= FilterCriteria.Unmerged;
				}
				else if (MergedGemStoneSetup.mergedGemContents.TryGetValue(item.m_dropPrefab.name, out List<GemInfo> infosList))
				{
					filters |= FilterCriteria.Merged;
					tier = infosList[0].Tier;
				}

				filters |= tier switch { 1 => FilterCriteria.Simple, 2 => FilterCriteria.Advanced, 3 => FilterCriteria.Perfect, _ => FilterCriteria.Boss };
				gemData.filters = filters;

				gems.Add(gemData);
			}
		}

		ui.UpdateGemIconElements(gems);
	}

	private static string GemIdentifier(ItemDrop.ItemData item) => item.Data().GetAll<SocketSeed>().Select(s => s.Key + s.Value.Seed).Join();

	public Inventory LoadInventory()
	{
		Inventory inventory = new("Crystal Case", Player.m_localPlayer?.GetInventory().m_bkg, 10000, 10000);
		if (GetComponent<ZNetView>().GetZDO().GetByteArray("JC Crystal Case Inventory") is { } content)
		{
			inventory.Load(new ZPackage(content));
		}
		return inventory;
	}

	public void StoreInventory(Inventory cabinet)
	{
		ZPackage pkg = new();
		cabinet.Save(pkg);

		GetComponent<ZNetView>().GetZDO().Set("JC Crystal Case Inventory", pkg.GetArray());
	}

	public void StoreGemsInCabinet()
	{
		Player player = Player.m_localPlayer;
		Inventory cabinet = LoadInventory();

		int counter = 0;
		List<ItemDrop.ItemData> gemsToDelete = new();
		foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
		{
			if (GemStones.socketableGemStones.Contains(item.m_shared.m_name))
			{
				++counter;
				cabinet.AddItem(item);
				gemsToDelete.Add(item);
			}
		}

		foreach (ItemDrop.ItemData item in gemsToDelete)
		{
			player.GetInventory().RemoveItem(item);
		}

		StoreInventory(cabinet);

		player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_crystal_case_gems_stored", counter.ToString()));
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (GemStones.socketableGemStones.Contains(item.m_shared.m_name) && user.GetInventory().RemoveItem(item))
		{
			Inventory cabinet = LoadInventory();
			cabinet.AddItem(item);
			StoreInventory(cabinet);
			return true;
		}
		user.Message(MessageHud.MessageType.Center, "$jc_crystal_case_invalid_item");
		return false;
	}

	public string GetHoverText() => Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $jc_crystal_case_hovertext\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $jc_crystal_case_hovertext_alt\n[<color=yellow><b>1-8</b></color>] $jc_crystal_case_hovertext_item");

	public string GetHoverName() => Localization.instance.Localize("$jc_crystal_case_hovername");

	[HarmonyPatch]
	private class DisablePlayerInputInCaseInterface
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(StoreGui), nameof(StoreGui.IsVisible)),
			AccessTools.DeclaredMethod(typeof(TextInput), nameof(TextInput.IsVisible)),
		};

		private static void Postfix(ref bool __result)
		{
			if (crystalCaseWindow)
			{
				__result = true;
			}
		}
	}

	private const int hideDistance = 5;

	public void Update()
	{
		if (!Player.m_localPlayer || Player.m_localPlayer.IsDead() || Player.m_localPlayer.InCutscene() || Vector3.Distance(transform.position, Player.m_localPlayer.transform.position) > hideDistance || InventoryGui.IsVisible() || Minimap.IsOpen())
		{
			Hide();
		}
		else if (Chat.instance?.HasFocus() != true && !Console.IsVisible() && !Menu.IsVisible() && (!TextViewer.instance || !TextViewer.instance.IsVisible()) && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape)))
		{
			ZInput.ResetButtonStatus("JoyButtonB");
			Hide();
		}
	}

	public static void Hide()
	{
		Destroy(crystalCaseWindow);
		crystalCaseWindow = null;
	}

	public void DropAllItems()
	{
		Inventory inventory = LoadInventory();
		List<ItemDrop.ItemData> allItems = inventory.GetAllItems();
		foreach (ItemDrop.ItemData itemData in allItems)
		{
			Vector3 vector3 = transform.position + Vector3.up * 0.5f + Random.insideUnitSphere * 0.3f;
			Quaternion quaternion = Quaternion.Euler(0.0f, Random.Range(0, 360), 0.0f);
			ItemDrop.DropItem(itemData, 0, vector3, quaternion);
		}
		inventory.RemoveAll();
		StoreInventory(inventory);
	}

	public void OnDestroyed()
	{
		if (GetComponent<ZNetView>().IsOwner())
		{
			DropAllItems();
		}
	}

	public bool MoveToInventory(GemData gemData)
	{
		Inventory inventory = LoadInventory();
		List<ItemDrop.ItemData> allItems = inventory.GetAllItems();
		foreach (ItemDrop.ItemData itemData in allItems)
		{
			if (itemData.m_shared.m_name == gemData.name && gemData.rawValue == GemIdentifier(itemData))
			{
				if (Player.m_localPlayer?.GetInventory().AddItem(itemData) == true)
				{
					inventory.RemoveItem(itemData);
					StoreInventory(inventory);
					return true;
				}
				Player.m_localPlayer?.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$jc_crystal_case_inventory_not_enough_space"));
			}
		}
		return false;
	}
}
