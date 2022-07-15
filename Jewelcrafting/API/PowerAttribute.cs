using System;
using UnityEngine;

namespace Jewelcrafting;

[AttributeUsage(AttributeTargets.Field)]
public abstract class PowerAttribute : Attribute
{
	public abstract float Add(float a, float b);
}

public class AdditivePowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => a + b;
}

// Use when doing 1 + effect / 100
public class MultiplicativePercentagePowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => ((1 + a / 100) * (1 + b / 100) - 1) * 100;
}

// Use when doing 1 - effect / 100 or when doing Random.Value < effect power
public class InverseMultiplicativePercentagePowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => (1 - (1 - a / 100) * (1 - b / 100)) * 100;
}

public class MinPowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => Mathf.Min(a, b);
}

public class MaxPowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => Mathf.Max(a, b);
}
