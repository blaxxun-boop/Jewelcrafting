using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class Ninja
{
	static Ninja()
	{
		ApplySkillIncreases.Effects.Add(Skills.SkillType.Sneak, Effect.Ninja);
	}
}
