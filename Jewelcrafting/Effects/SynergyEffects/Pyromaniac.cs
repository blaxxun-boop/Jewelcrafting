using JetBrains.Annotations;
using Jewelcrafting.GemEffects;

namespace Jewelcrafting.SynergyEffects;

public static class Pyromaniac
{
	static Pyromaniac()
	{
		EffectDef.ConfigTypes.Add(Effect.Pyromaniac, typeof(Config));
		EffectDef.DescriptionOverrides.Add(Effect.Firestarter, setFirestarterDesc);
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	private static string? setFirestarterDesc(Player player, ref float[] numbers)
	{
		if (player.GetEffect(Effect.Pyromaniac) > 0)
		{
			// chance
			numbers[1] += numbers[1] * player.GetEffect(Effect.Pyromaniac) / 100;
		}
		return null;
	}
}
