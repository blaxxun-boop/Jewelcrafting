using System.Collections.Generic;
using System.Linq;
using ExtendedItemDataFramework;

namespace Jewelcrafting;

public class Sockets : BaseExtendedItemComponent
{
	public List<string> socketedGems = new() { "" };

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
	
	public override BaseExtendedItemComponent Clone()
	{
		Sockets copy = (Sockets)MemberwiseClone();
		copy.socketedGems = new List<string>(copy.socketedGems);
		return copy;
	}
}
