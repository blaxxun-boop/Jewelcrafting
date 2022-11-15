using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class Frenzy
{
	static Frenzy()
	{
		EffectDef.ConfigTypes.Add(Effect.Frenzy, typeof(Config));
		ApplyAttackSpeed.Modifiers.Add(player => player.GetEffect(Effect.Frenzy) / 100f);
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}
}
