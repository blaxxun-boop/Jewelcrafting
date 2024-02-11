using JetBrains.Annotations;
using Jewelcrafting.GemEffects;

namespace Jewelcrafting.SynergyEffects;

public static class ResonatingEchoes
{
	static ResonatingEchoes()
	{
		EffectDef.ConfigTypes.Add(Effect.Resonatingechoes, typeof(Config));
		EffectDef.DescriptionOverrides.Add(Effect.Echo, setEchoDesc);
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	private static string? setEchoDesc(Player player, ref string[] numbers)
	{
		if (player.GetEffect(Effect.Resonatingechoes) > 0)
		{
			// chance
			numbers[0] = Utils.FormatShortNumber(float.Parse(numbers[0]) * (1 + player.GetEffect(Effect.Resonatingechoes) / 100));
		}
		return null;
	}
}
