using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class Berserk
{
	static Berserk()
	{
		ApplyAttackSpeed.Modifiers.Add(player => player.GetEffect(Effect.Berserk) / 100f);
	}
}
