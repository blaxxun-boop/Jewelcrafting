using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class MasterArbalist
{
	static MasterArbalist()
	{
		ApplySkillIncreases.Effects.Add(Skills.SkillType.Crossbows, Effect.Masterarbalist);
	}
}
