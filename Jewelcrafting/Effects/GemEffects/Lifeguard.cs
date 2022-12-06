using JetBrains.Annotations;
using Jewelcrafting.GemEffects;

namespace Jewelcrafting.Effects.GemEffects;

[UsedImplicitly]
public static class Lifeguard
{
	static Lifeguard()
	{
		ApplySkillIncreases.Effects.Add(Skills.SkillType.Swim, Effect.Lifeguard);
	}
}
