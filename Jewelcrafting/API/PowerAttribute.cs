using System;
using UnityEngine;

namespace Jewelcrafting;

[AttributeUsage(AttributeTargets.Field)]
public abstract class PowerAttribute : Attribute
{
	public abstract float Add(float a, float b);
	public abstract float Multiply(float a, float b);
}

public class AdditivePowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => a + b;
	public override float Multiply(float a, float b) => a * b;
}

// Use when doing 1 + effect / 100
public class MultiplicativePercentagePowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => ((1 + a / 100) * (1 + b / 100) - 1) * 100;
	public override float Multiply(float a, float b) => a * b;
}

// Use when doing 1 - effect / 100 or when doing Random.Value < effect power
public class InverseMultiplicativePercentagePowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => (1 - (1 - a / 100) * (1 - b / 100)) * 100;
	public override float Multiply(float a, float b) => a != 0 && b != 0 ? (1 - 1 / ((1 / (1 - a / 100) - 1) * b + 1)) * 100 : 0;
}

public class MinPowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => Mathf.Min(a, b);
	public override float Multiply(float a, float b) => a / b;
}

public class MaxPowerAttribute : PowerAttribute
{
	public override float Add(float a, float b) => Mathf.Max(a, b);
	public override float Multiply(float a, float b) => a * b;
}

[AttributeUsage(AttributeTargets.Field)]
public class OptionalPowerAttribute(float defaultValue) : Attribute
{
	public readonly float DefaultValue = defaultValue;
}
