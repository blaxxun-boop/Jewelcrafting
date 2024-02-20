using System.Collections.Generic;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public class Legacy : SE_Stats
{
	private static readonly Dictionary<string, GameObject> locations = new()
	{
		{ "Vendor_BlackForest", GemEffectSetup.legacyRingHaldor },
		{ "Hildir_camp", GemEffectSetup.legacyRingHildir },
		{ "Hildir crypt", GemEffectSetup.legacyRingHildirQuest },
	};

	public override void UpdateStatusEffect(float dt)
	{
		m_tickTimer += dt;
		if (m_tickTimer >= Jewelcrafting.legacyCooldown.Value)
		{
			Transform playerPosition = Player.m_localPlayer.transform;
			if (playerPosition.position.y < 3500)
			{
				Dictionary<Vector3, string> locationIcons = new();
				ZoneSystem.instance.GetLocationIcons(locationIcons);
				foreach (Minimap.PinData pin in Minimap.instance.m_pins)
				{
					if (pin.m_type is Minimap.PinType.Hildir1 or Minimap.PinType.Hildir2 or Minimap.PinType.Hildir3)
					{
						locationIcons[pin.m_pos] = "Hildir crypt";
					}
				}
				Vector3 closestLocation = new(1000000, 1000000, 1000000);
				foreach (KeyValuePair<Vector3, string> location in locationIcons)
				{
					if (locations.ContainsKey(location.Value))
					{
						if (global::Utils.DistanceXZ(closestLocation, playerPosition.position) > global::Utils.DistanceXZ(location.Key, playerPosition.position))
						{
							closestLocation = location.Key;
						}
					}
				}

				if (locationIcons.TryGetValue(closestLocation, out string locationName))
				{
					Instantiate(locations[locationName], playerPosition.position + playerPosition.forward * 2 + playerPosition.up, Quaternion.LookRotation((closestLocation - playerPosition.position) with { y = 0 }));
				}
			}

			m_tickTimer = 0;
		}
		base.UpdateStatusEffect(dt);
	}
}
