using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Jewelcrafting.WorldBosses;

public class GemStoneInteract : MonoBehaviour, Interactable, Hoverable
{
	[HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
	private static class InterceptInput
	{
		private static void Postfix(ref bool __result)
		{
			if (window)
			{
				__result = true;
			}
		}
	}

	private static GameObject? window;
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

	private static void Hide()
	{
		Destroy(window);
		window = null;
	} 

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (!hold && GachaDef.ActivePrizes() is {} prizes)
		{
			string gachaCoinName = GachaSetup.gachaCoins.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
			
			List<Prize> sortedPrizes = prizes.prizes.OrderBy(p => p.Chance).Where(p => GachaDef.getItem(p.Item) is not null).Take(2).ToList();
			if (sortedPrizes.Count == 0)
			{
				return false;
			}
			
			window = Instantiate(GachaSetup.skeletonWindow, Hud.instance.m_rootObject.transform);
			InputField input = window.transform.Find("Bkg/Middle_TextInput").GetComponent<InputField>();
			window.transform.Find("Bkg/Middle_Season_Banner/Season_Text").GetComponent<Text>().text = prizes.Name == "default" ? "Standard" : prizes.Name;
			DateTime endDate = prizes.EndDate;
			if (endDate == DateTime.MaxValue && GachaChest.Expiration(prizes) != DateTimeOffset.MinValue)
			{
				endDate = GachaChest.Expiration(prizes).Date;
			}
			window.transform.Find("Bkg/Middle_Season_Banner/Season_Time").GetComponent<Text>().text = endDate != DateTime.MaxValue ? $"Time left: {Utils.GetHumanFriendlyTime((int)(endDate - DateTime.Now).TotalSeconds)}" : "";
			window.transform.Find("Bkg/Coins_Middle/Middle_Coins_Text").GetComponent<Text>().text = "Celestial Coins: " + Player.m_localPlayer.m_inventory.CountItems(gachaCoinName);

			window.transform.Find("Bkg/Middle_Roll_Button").GetComponent<Button>().onClick.AddListener(() =>
			{
				if (int.TryParse(input.text, out int coins) && coins > 0)
				{
					foreach (GachaChest chest in GachaChest.NearbyGachaChests(transform))
					{
						if (chest.GetInventory().m_inventory.Count != 0 || chest.m_nview.GetZDO().GetInt("Jewelcrafting Gacha Chest") > 0)
						{
							Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Please make sure that all chests are empty");
							return;
						}
					}

					if (coins > 15)
					{
						Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You cannot use more than 15 coins at once");
						return;
					}
					
					Inventory inv = Player.m_localPlayer.GetInventory();
					coins = Math.Min(inv.CountItems(gachaCoinName), coins);
					inv.RemoveItem(gachaCoinName, coins);
					
					AddCoins(coins);
					
					Hide();
				}
			});

			void FillItem(string column, Prize? prize)
			{
				Transform box = window!.transform.Find($"Bkg/Box_{column}");
				Text itemNameText = box.transform.Find($"{column}_Text_Top").GetComponent<Text>();
				Text chanceText = itemNameText.transform.Find($"{column}_Text_Middle").GetComponent<Text>();
				Image itemImage = box.transform.Find($"Box_{column}_Replace").GetComponent<Image>();
				UITooltip itemTooltip = box.transform.Find($"Box_{column}_Replace").GetComponent<UITooltip>();
				
				if (prize is null || GachaDef.getItem(prize.Item) is not {} prizeItem)
				{
					box.gameObject.SetActive(false);
					return;
				}

				ItemDrop.ItemData item = prizeItem.m_itemData;
				ExtendedItemData itemExtended = new(item);
				if (prize.Sockets.Count > 0)
				{
					itemExtended.AddComponent<Sockets>();
					itemExtended.GetComponent<Sockets>().socketedGems.Clear();
					foreach (string socket in prize.Sockets)
					{
						itemExtended.GetComponent<Sockets>().socketedGems.Add(new SocketItem(socket.ToLower() == "empty" ? "" : GachaDef.getItem(socket)?.name ?? ""));
					}
					itemTooltip.m_tooltipPrefab = GemStoneSetup.SocketTooltip;
					GemStones.DisplaySocketTooltip.tooltipItem.Add(itemTooltip, itemExtended);
				}
				else
				{
					itemTooltip.m_tooltipPrefab = InventoryGui.instance.m_playerGrid.m_elementPrefab.GetComponent<UITooltip>().m_tooltipPrefab;
				}
				
				itemTooltip.m_topic = item.m_shared.m_name;
				itemTooltip.m_text = item.GetTooltip();

				itemNameText.text = item.m_shared.m_name;
				chanceText.text = Utils.FormatShortNumber(prize.Chance * 100) + "% chance to get!";
				itemImage.sprite = item.GetIcon();
			}

			Random.State state = GachaChest.SetRandomState(prizes);
			FillItem("Left", sortedPrizes.ElementAtOrDefault(0));
			FillItem("Right", sortedPrizes.ElementAtOrDefault(1));
			Random.state = state;
			
			IEnumerator selectInput()
			{
				yield return new WaitForEndOfFrame();
				input.ActivateInputField();
			}
			input.StartCoroutine(selectInput());

			foreach (Text text in window.GetComponentsInChildren<Text>())
			{
				text.text = Localization.instance.Localize(text.text);
			}
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

	private void AddCoins(int coins)
	{
		foreach (GachaChest chest in GachaChest.NearbyGachaChests(transform))
		{
			chest.m_nview.GetZDO().Set("Jewelcrafting Gacha Chest", chest.m_nview.GetZDO().GetInt("Jewelcrafting Gacha Chest") + coins);
		}
	}

	public string GetHoverText() => Localization.instance.Localize("$jc_gacha_gemstone" + (GachaDef.ActivePrizes() is null ? "" : "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact"));

	public string GetHoverName() => Localization.instance.Localize("$jc_gacha_gemstone");
}
