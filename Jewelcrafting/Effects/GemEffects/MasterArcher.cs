using JetBrains.Annotations;

namespace Jewelcrafting.GemEffects;

[UsedImplicitly]
public static class MasterArcher
{
	static MasterArcher()
	{
		ApplySkillIncreases.Effects.Add(Skills.SkillType.Bows, Effect.Masterarcher);
	}
}
