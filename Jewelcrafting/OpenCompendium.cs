using Jewelcrafting.GemEffects;

namespace Jewelcrafting;

public class OpenCompendium : StationExtension, Interactable, Hoverable
{
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return true;
		}
		
		InventoryGui.instance.Show(null);
		InventoryGui.instance.OnOpenTexts();
		InventoryGui.instance.m_textsDialog.ShowText(CompendiumDisplay.compendiumPage);
		CompendiumDisplay.DisplayGemEffectOverview.Render(InventoryGui.instance.m_textsDialog);

		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public new string GetHoverText()
	{
		return base.GetHoverText() + "\n" + Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $jc_open_compendium");
	}
}
