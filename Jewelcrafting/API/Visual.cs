using JetBrains.Annotations;

namespace Jewelcrafting;

[PublicAPI]
public
#if ! API
	partial
#endif
	class Visual
{
	private readonly VisEquipment visEquipment;
	public ItemDrop.ItemData? equippedFingerItem;
	public ItemDrop.ItemData? equippedNeckItem;
	public int currentFingerItemHash;
	public int currentNeckItemHash;

	private Visual(VisEquipment visEquipment)
	{
		this.visEquipment = visEquipment;
	}
}
