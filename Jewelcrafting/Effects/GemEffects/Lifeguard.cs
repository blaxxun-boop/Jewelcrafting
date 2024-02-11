using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class Lifeguard
{
	static Lifeguard()
	{
		ApplySkillIncreases.Effects.Add(Skills.SkillType.Swim, Effect.Lifeguard);
	}
}
