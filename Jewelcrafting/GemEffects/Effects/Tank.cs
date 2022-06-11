using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class Tank
{
	static Tank()
	{
		ApplySkillIncreases.Effects.Add(Skills.SkillType.Blocking, Effect.Tank);
	}
}
