using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class Berserk
{
	static Berserk()
	{
		EffectDef.ConfigTypes.Add(Effect.Berserk, typeof(Config));
		ApplyAttackSpeed.Modifiers.Add(player => player.GetEffect(Effect.Berserk) / 100f);
	}
	
	[PublicAPI]
	private struct Config
	{
		[MultiplicativePercentagePower] public float Power;
	}
}
