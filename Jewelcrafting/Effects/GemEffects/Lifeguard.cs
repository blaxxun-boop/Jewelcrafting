using JetBrains.Annotations;
using Jewelcrafting.GemEffects;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class Lifeguard
{
	static Lifeguard()
	{
		ApplySkillIncreases.Effects.Add(Skills.SkillType.Swim, Effect.Lifeguard);
	}
}
