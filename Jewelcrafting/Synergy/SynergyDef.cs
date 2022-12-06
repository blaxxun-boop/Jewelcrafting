using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Jewelcrafting.GemEffects;

namespace Jewelcrafting;

[PublicAPI]
public class SynergyDef
{
	public interface Expr
	{
		float Eval(Dictionary<GemType, int> gems);
	}

	private class Gem : Expr
	{
		private readonly GemType type;
		public Gem(GemType type) => this.type = type;
		public float Eval(Dictionary<GemType, int> gems) => gems.TryGetValue(type, out int num) ? num : 0;
		public override string ToString() => $"$jc_merged_gemstone_{EffectDef.GemTypeNames[type].ToLower()}";
	}

	private delegate float GemsOp(IEnumerable<KeyValuePair<GemType, int>> gems);

	private class GemsExpr : Expr
	{
		private readonly HashSet<GemType> exclude;
		private readonly string func;

		public GemsExpr(string func, HashSet<GemType> exclude)
		{
			this.func = func;
			this.exclude = exclude;
		}

		public float Eval(Dictionary<GemType, int> gems) => gemsOps[func](gems.Where(kv => !exclude.Contains(kv.Key)).DefaultIfEmpty(new KeyValuePair<GemType, int>(GemType.Black, 0)));
		public override string ToString() => $"{func} {(exclude.Count == 0 ? "all" : "other")}";
	}

	private static readonly Dictionary<string, GemsOp> gemsOps = new()
	{
		{ "sum", gems => gems.Sum(kv => kv.Value) },
		{ "min", gems => gems.Min(kv => kv.Value) },
		{ "max", gems => gems.Max(kv => kv.Value) }
	};

	private class Value : Expr
	{
		private readonly float value;
		public Value(float value) => this.value = value;
		public virtual float Eval(Dictionary<GemType, int> gems) => value;
		public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
	}

	private class BooleanInverse : Expr
	{
		private readonly Expr expr;
		public BooleanInverse(Expr expr) => this.expr = expr;
		public virtual float Eval(Dictionary<GemType, int> gems) => expr.Eval(gems) != 0 ? 1 : 0;
		public override string ToString() => expr is BinaryExpr ? $"not ({expr})" : $"not {expr}";
	}

	private class AdditiveInverse : Expr
	{
		private readonly Expr expr;
		public AdditiveInverse(Expr expr) => this.expr = expr;
		public virtual float Eval(Dictionary<GemType, int> gems) => -expr.Eval(gems);
		public override string ToString() => expr is BinaryExpr ? $"-({expr})" : $"-{expr}";
	}

	private abstract class BinaryExpr : Expr
	{
		public Expr Left = null!;
		public Expr Right = null!;
		public abstract string Token { get; }
		public abstract Precedence Precedence { get; }

		public abstract float Eval(Dictionary<GemType, int> gems);
		private string wrapParens(Expr expr) => expr is BinaryExpr binary && binary.Precedence > Precedence ? $"({binary})" : expr.ToString();
		public override string ToString() => $"{wrapParens(Left)} {Token} {wrapParens(Right)}";
	}

	private enum Precedence
	{
		Mul,
		Add,
		Compare,
		Combine
	}

	private class Mul : BinaryExpr
	{
		public override string Token => "*";
		public override Precedence Precedence => Precedence.Mul;
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) * Right.Eval(gems);
	}

	private class Div : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Mul;
		public override string Token => "/";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) / Right.Eval(gems);
	}

	private class Add : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Add;
		public override string Token => "+";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) + Right.Eval(gems);
	}

	private class Sub : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Add;
		public override string Token => "-";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) - Right.Eval(gems);
	}

	private class LowerEqual : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Compare;
		public override string Token => "<=";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) <= Right.Eval(gems) ? 1 : 0;
	}

	private class LowerThan : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Compare;
		public override string Token => "<";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) < Right.Eval(gems) ? 1 : 0;
	}

	private class GreaterEqual : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Compare;
		public override string Token => ">=";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) >= Right.Eval(gems) ? 1 : 0;
	}

	private class GreaterThan : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Compare;
		public override string Token => ">";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) > Right.Eval(gems) ? 1 : 0;
	}

	private class Equal : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Compare;
		public override string Token => "=";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) == Right.Eval(gems) ? 1 : 0;
	}

	private class Or : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Combine;
		public override string Token => "or";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) != 0 || Right.Eval(gems) != 0 ? 1 : 0;
	}

	private class And : BinaryExpr
	{
		public override Precedence Precedence => Precedence.Combine;
		public override string Token => "and";
		public override float Eval(Dictionary<GemType, int> gems) => Left.Eval(gems) != 0 && Right.Eval(gems) != 0 ? 1 : 0;
	}

	private class ExprOpPair
	{
		public Expr expr;
		public BinaryOp op;
		public ExprOpPair? next;

		public ExprOpPair(Expr expr) => this.expr = expr;
	}

	private struct BinaryOp
	{
		public Precedence precedence;
		public Func<Expr, Expr, BinaryExpr> createExpr;
	}

	private static readonly Dictionary<string, BinaryOp> binaryOps = typeof(SynergyDef).GetNestedTypes(BindingFlags.NonPublic).Where(t => t.BaseType == typeof(BinaryExpr)).Select(t => (BinaryExpr)Activator.CreateInstance(t)).OrderByDescending(expr => expr.Token.Length).ToDictionary(expr => expr.Token, e => new BinaryOp
	{
		precedence = e.Precedence,
		createExpr = (left, right) =>
		{
			BinaryExpr expr = (BinaryExpr)Activator.CreateInstance(e.GetType());
			expr.Left = left;
			expr.Right = right;
			return expr;
		}
	});

	public string Name = "";
	public Expr Condition = null!;
	public List<EffectPower> EffectPowers = new();

	public bool IsActive(Dictionary<GemType, int> gemTypes) => Condition.Eval(gemTypes) != 0;

	public static SynergyDef? Parse(string name, Dictionary<string, object?> synergyDict, List<string> errorList)
	{
		SynergyDef def = new() { Name = name };

		string errorLocation = $"Found in synergy '{name}'.";

		HashSet<GemType> foundGemTypes = new();
		Expr? parseCondition(string condition)
		{
			Expr? parse(ref int offset)
			{
				int idx = offset;

				bool cmpOp(string value) => condition.Length >= idx + value.Length && string.Compare(condition, idx, value, 0, value.Length, StringComparison.InvariantCultureIgnoreCase) == 0 && (condition.Length == idx + value.Length || char.IsLetter(value[0]) || !char.IsLetter(condition[idx + value.Length]));

				void skipWhitespace()
				{
					while (char.IsWhiteSpace(condition[idx]))
					{
						++idx;
					}
				}

				string conditionErrorLocation() => $"at offset {idx} {(idx >= condition.Length ? "(reached end without further arguments)" : $"(starting at: '{condition.Substring(idx)}')")} for condition '{condition}'";

				Expr? parseOperand()
				{
					skipWhitespace();

					// parse operand
					Expr? operand = null;
					Func<Expr, Expr> wrapUnary = e => e;
					if (cmpOp("not"))
					{
						idx += 3;
						skipWhitespace();
						wrapUnary = e => new BooleanInverse(e);
					}
					if (cmpOp("-"))
					{
						idx += 1;
						skipWhitespace();
						wrapUnary = e => new AdditiveInverse(e);
					}

					if (cmpOp("("))
					{
						idx += 1;
						operand = parse(ref idx);
						if (operand is null)
						{
							return null;
						}
					}
					else
					{
						foreach (string gemsOp in gemsOps.Keys)
						{
							if (cmpOp(gemsOp))
							{
								idx += gemsOp.Length;
								skipWhitespace();
								if (cmpOp("other"))
								{
									idx += 5;
									operand = new GemsExpr(gemsOp, foundGemTypes);
								}
								else if (cmpOp("all"))
								{
									idx += 3;
									operand = new GemsExpr(gemsOp, new HashSet<GemType>());
								}
								else
								{
									errorList.Add($"Found no valid group operator argument {conditionErrorLocation()}. Expecting either 'all' or 'other'. {errorLocation}");
									return null;
								}
							}
						}

						if (operand is null)
						{
							int numberEnd = idx;
							while (numberEnd < condition.Length && (char.IsDigit(condition[numberEnd]) || condition[numberEnd] == '.'))
							{
								++numberEnd;
							}
							if (float.TryParse(condition.Substring(idx, numberEnd - idx), out float number))
							{
								idx = numberEnd;
								operand = new Value(number);
							}
							else
							{
								foreach (KeyValuePair<string, GemType> kv in EffectDef.ValidGemTypes.OrderByDescending(kv => kv.Key.Length))
								{
									if (cmpOp(kv.Key) && (idx + kv.Key.Length >= condition.Length || !char.IsLetter(condition[idx + kv.Key.Length])))
									{
										idx += kv.Key.Length;
										operand = new Gem(kv.Value);
										foundGemTypes.Add(kv.Value);
									}
								}
							}
						}

						if (operand is null)
						{
							errorList.Add($"Found no valid operand {conditionErrorLocation()}. Expecting a parenthesized operation, a group operation, a number or a valid gem type, optionally preceded by unary 'not' or '-'. Allowed group operations are: '{string.Join("', '", gemsOps.Keys)}'. Valid gem types are: '{string.Join("', '", EffectDef.ValidGemTypes.Keys)}'. {errorLocation}");
							return null;
						}
					}

					return wrapUnary(operand);
				}

				BinaryOp? parseOperator()
				{
					foreach (KeyValuePair<string, BinaryOp> binaryOp in binaryOps)
					{
						if (cmpOp(binaryOp.Key))
						{
							idx += binaryOp.Key.Length;
							return binaryOp.Value;
						}
					}

					errorList.Add($"Found no valid operator {conditionErrorLocation()}. Allowed operators are: '{string.Join("', '", binaryOps.Keys)}'. {errorLocation}");
					return null;
				}

				if (parseOperand() is not { } firstOperand)
				{
					return null;
				}

				ExprOpPair start = new(firstOperand);

				ExprOpPair curPair = start;
				for (; idx < condition.Length; ++idx)
				{
					skipWhitespace();
					if (cmpOp(")"))
					{
						++idx;
						break;
					}

					if (parseOperator() is not { } op)
					{
						return null;
					}

					curPair.op = op;

					if (parseOperand() is not { } operand)
					{
						return null;
					}

					curPair = curPair.next = new ExprOpPair(operand);
				}

				// merge operations according to their precedence
				foreach (Precedence precedence in (Precedence[])Enum.GetValues(typeof(Precedence)))
				{
					ExprOpPair? exprOp = start;
					do
					{
						while (exprOp.op.precedence == precedence && exprOp.next is { } nextOp)
						{
							exprOp.expr = exprOp.op.createExpr(exprOp.expr, nextOp.expr);
							exprOp.next = nextOp.next;
							exprOp.op = nextOp.op;
						}
						exprOp = exprOp.next;
					} while (exprOp is not null);
				}

				offset = idx;
				return start.expr;
			}

			int offset = 0;
			Expr? expr = parse(ref offset);
			if (expr is not null && offset < condition.Length)
			{
				errorList.Add($"Found extra characters at offset {offset} (starting at: '{condition.Substring(offset)}') for condition '{condition}'. Expecting either 'all' or 'other'. {errorLocation}");
				return null;
			}

			return expr;
		}

		switch (synergyDict["conditions"])
		{
			case string cond:
			{
				if (parseCondition(cond) is not { } expr)
				{
					return null;
				}

				def.Condition = expr;
				break;
			}
			case List<object?> list:
			{
				Expr? lastExpr = null;
				foreach (object? listVal in list)
				{
					if (listVal is not string cond)
					{
						errorList.Add($"Synergy conditions must be strings expressing conditions. Got unexpected {listVal?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
						return null;
					}

					if (parseCondition(cond) is not { } expr)
					{
						return null;
					}

					if (lastExpr is not null)
					{
						expr = new And { Left = lastExpr, Right = expr };
					}
					lastExpr = expr;
				}

				if (lastExpr is null)
				{
					errorList.Add($"Synergy conditions may not be an empty list. {errorLocation}");
					return null;
				}

				def.Condition = lastExpr;
				break;
			}
			default:
				errorList.Add($"A synergy condition is a string expressing a condition or a list of conditions. Got unexpected {synergyDict["conditions"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
				return null;
		}

		if (!synergyDict.ContainsKey("effect"))
		{
			errorList.Add($"Synergies must contain a mapping of effects to their power. The synergy '{name}' is missing an 'effect' key.");
			return null;
		}

		if (synergyDict["effect"] is Dictionary<object, object?> effectsDict)
		{
			int index = 0;
			foreach (KeyValuePair<string, object?> effectsKv in EffectDef.castDictToStringDict(effectsDict))
			{
				++index;

				string effectName = effectsKv.Key;
				string effectErrorLocation = $"Found in {index}. effect definition for effect '{effectName}' for synergy '{name}'.";

				if (!EffectDef.ValidEffects.TryGetValue(effectName, out Effect effect))
				{
					effectName = effectName.Replace(" ", "");
					if (!EffectDef.ValidEffects.TryGetValue(effectName, out effect))
					{
						errorList.Add($"'{effectName}' is not a valid effect name. Valid effects are: '{string.Join("', '", EffectDef.ValidEffects.Keys)}'. Found in {index}. definition for synergy '{name}'.");
						continue;
					}
				}

				if (!EffectDef.ConfigTypes.TryGetValue(effect, out Type configType))
				{
					configType = typeof(DefaultPower);
				}
				Dictionary<string, FieldInfo> configFields = new(configType.GetFields().ToDictionary(f => Regex.Replace(f.Name, "(?!^)([A-Z])", " $1"), f => f), StringComparer.InvariantCultureIgnoreCase);

				object config = Activator.CreateInstance(configType);

				bool ParsePower(object? powerObj, FieldInfo? field = null)
				{
					bool hasField = field is not null;
					string fieldLocation = field is not null ? $" for the key '{field.Name}'" : "";
					field ??= configFields.First().Value;
					if (powerObj is Dictionary<object, object?> powersList && !hasField)
					{
						if (configFields.Count != powersList.Count)
						{
							errorList.Add($"There are missing configurable values for this effect. Specify values for all of {string.Join(", ", configFields.Keys)}. {effectErrorLocation}");
							return false;
						}

						foreach (KeyValuePair<string, object?> kv in EffectDef.castDictToStringDict(powersList))
						{
							if (!configFields.TryGetValue(kv.Key, out field))
							{
								errorList.Add($"'{kv.Key}' is not a valid configuration key for the powers. Specify values for all of {string.Join(", ", configFields.Keys)}. {effectErrorLocation}");
								return false;
							}

							if (!ParsePower(kv.Value, field))
							{
								return false;
							}
						}
					}
					else if (!hasField && configFields.Count > 1)
					{
						errorList.Add($"There are multiple configurable values for this effect. Specify values for all of {string.Join(", ", configFields.Keys)}. {effectErrorLocation}");
						return false;
					}
					else if (powerObj is string powerNumber)
					{
						if (float.TryParse(powerNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out float power))
						{
							field.SetValue(config, power);
						}
						else
						{
							errorList.Add($"The power{fieldLocation} is not a number. Got unexpected '{powerNumber}'. {effectErrorLocation}");
							return false;
						}
					}
					else
					{
						errorList.Add($"The power{fieldLocation} is not a number. Got unexpected {powerObj?.GetType().ToString() ?? "empty string (null)"}. {effectErrorLocation}");
						return false;
					}

					return true;
				}

				if (!ParsePower(effectsKv.Value))
				{
					continue;
				}

				def.EffectPowers.Add(new EffectPower { Config = config, Effect = effect });
			}
		}
		else
		{
			errorList.Add($"Synergies must contain a mapping of effects to their power. Got unexpected {synergyDict["effect"]?.GetType().ToString() ?? "empty string (null)"}. {errorLocation}");
			return null;
		}

		return def;
	}

	public static void Apply(IEnumerable<SynergyDef> synergies)
	{
		Jewelcrafting.Synergies.Clear();
		Jewelcrafting.Synergies.AddRange(synergies.Where(s => s.Name != ""));
		/*
		foreach (SynergyDef def in Jewelcrafting.Synergies)
		{
			Debug.LogError(Localization.instance.Localize($"{def.Name}: {def.Condition}"));
		}
		*/
	}
}
