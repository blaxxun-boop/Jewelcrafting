namespace Jewelcrafting.GemEffects;

public class NightWarmth : SE_Stats
{
	public override void ModifyStaminaRegen(ref float staminaRegen)
	{
		if (EnvMan.instance.IsNight())
		{
			base.ModifyStaminaRegen(ref staminaRegen);
		}
	}
}
