// -----------------------------------------------------------------------------
// <summary>
//     Implements numeric analysis functions (is_number, is_integer, is_even, is_odd).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;
using System.Reflection;
using ShYCalculator.Functions;
using System.Collections.Frozen;

namespace ShYCalculator.Functions.Mathematics;

/// <summary>
/// Implements numeric analysis functions (is_number, is_integer, is_even, is_odd, etc.).
/// </summary>
public class CalcNumericFunctions : ICalcFunctionsExtension {
    #region Members
    private readonly FrozenDictionary<string, CalcFunction> m_funcDef;
    private readonly FrozenDictionary<string, Func<ReadOnlySpan<Value>, Value>> m_dispatch;
    #endregion Members

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="CalcNumericFunctions"/> class.
    /// </summary>
    public CalcNumericFunctions() {
        var funcList = CalcFunctionsHelper.ReadFunctionsConfiguration(Name, typeof(CalcNumericFunctions));
        CalcFunctionsHelper.CheckFunctionsConfiguration(funcList);
        m_funcDef = funcList.ToDictionary(x => x.Name!, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        m_dispatch = CreateDispatch();
    }

    private static FrozenDictionary<string, Func<ReadOnlySpan<Value>, Value>> CreateDispatch() {
        return new Dictionary<string, Func<ReadOnlySpan<Value>, Value>>(StringComparer.OrdinalIgnoreCase) {
            [OtherFunctionNames.IsAscending] = p => CompareNumbersOrder(p, NumberCompareMode.Ascending, true, OtherFunctionNames.IsAscending),
            [OtherFunctionNames.IsDesc] = p => CompareNumbersOrder(p, NumberCompareMode.Descending, true, OtherFunctionNames.IsDesc),
            [OtherFunctionNames.IsAscLoose] = p => CompareNumbersOrder(p, NumberCompareMode.Ascending, false, OtherFunctionNames.IsAscLoose),
            [OtherFunctionNames.IsDescLoose] = p => CompareNumbersOrder(p, NumberCompareMode.Descending, false, OtherFunctionNames.IsDescLoose),
            [OtherFunctionNames.HasAdjacentEquals] = p => AnyEqualAdjacentNumber(p, 0, OtherFunctionNames.HasAdjacentEquals),
            [OtherFunctionNames.AreAllNumbers] = p => AllNumericValues(p, OtherFunctionNames.AreAllNumbers),
            [OtherFunctionNames.Median] = p => Median(p, OtherFunctionNames.Median),
            [OtherFunctionNames.Mode] = p => Mode(p, OtherFunctionNames.Mode),
            [OtherFunctionNames.StDev] = p => StDev(p, OtherFunctionNames.StDev),
            [OtherFunctionNames.Var] = p => Var(p, OtherFunctionNames.Var),
            [OtherFunctionNames.Count] = p => Count(p, OtherFunctionNames.Count),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
    #endregion Constructor

    #region Properties
    /// <inheritdoc/>
    public string Name => "CalcNumericFunctions";
    #endregion Properties

    #region Methods
    /// <inheritdoc/>
    public IEnumerable<CalcFunction> GetFunctions() {
        return m_funcDef.Values;
    }

    /// <inheritdoc/>
    public Value ExecuteFunction(string functionName, ReadOnlySpan<Value> parameters) {
        if (!m_funcDef.TryGetValue(functionName, out var functionDef)) {
            throw new FunctionException($"Invalid Function name in extension {Name} on function {functionName}");
        }

        CalcFunctionsHelper.CheckFunctionArguments(functionDef, parameters, Name);

        if (m_dispatch.TryGetValue(functionName, out var function)) {
            return function(parameters);
        }

        throw new FunctionException($"Invalid Function name in extension {Name} on function {functionName}");
    }
    #endregion Methods

    #region Private Methods

    internal static Value CompareNumbersOrder(ReadOnlySpan<Value> parameters, NumberCompareMode compareMode = NumberCompareMode.Ascending, bool? overrideStrictOrder = null, string functionName = OtherFunctionNames.IsAscending) {
        if (parameters.Length < 2) {
            return new Value(DataType.Boolean, bValue: false);
        }

        var last = parameters[^1];
        var hasStrictOrderArg = last.Nvalue == null && last.Bvalue != null;
        var arguments = hasStrictOrderArg ? parameters[..^1] : parameters;

        var allNumbers = AllNumericValues(arguments, functionName);
        if (allNumbers.Bvalue == false) {
            return allNumbers;
        }

        var isStrictOrder = overrideStrictOrder != null ? (bool)overrideStrictOrder : !hasStrictOrderArg || (last.Bvalue ?? true);
        if (isStrictOrder && AnyEqualAdjacentNumber(arguments, 0, functionName).Bvalue == true) {
            return new Value(DataType.Boolean, bValue: false);
        }

        bool result = true;
        double? previous = null;

        foreach (var val in arguments) {
            double current = val.Nvalue ?? 0;
            if (previous != null) {
                if (compareMode == NumberCompareMode.Ascending) {
                    if (current < previous) {
                        result = false;
                        break;
                    }
                }
                else {
                    if (current > previous) {
                        result = false;
                        break;
                    }
                }
            }
            previous = current;
        }

        return new Value(DataType.Boolean, bValue: result);
    }

    internal static Value AnyEqualAdjacentNumber(ReadOnlySpan<Value> parameters, double tolerance = 0, string functionName = OtherFunctionNames.HasAdjacentEquals) {
        var allNumbers = AllNumericValues(parameters, functionName);
        if (allNumbers.Bvalue == false) {
            return allNumbers;
        }

        bool value = false;
        double? last_number = null;
        int equalAdjacentNumberCount = 0;

        foreach (var val in parameters) {
            double current_number = val.Nvalue ?? 0;
            if (last_number == null) {
                last_number = current_number;
                continue;
            }

            if (IsSameDouble((double)last_number, current_number, 7)) {
                equalAdjacentNumberCount++;

                if (equalAdjacentNumberCount <= tolerance) {
                    continue;
                }

                value = true;
                break;
            }

            equalAdjacentNumberCount = 0;
            last_number = current_number;
        }

        return new Value(DataType.Boolean, bValue: value);
    }

    internal static Value AllNumericValues(ReadOnlySpan<Value> numbers, string _ = OtherFunctionNames.AreAllNumbers) {
        for (int i = 0; i < numbers.Length; i++) {
            if (numbers[i].Nvalue == null) return new Value(DataType.Boolean, bValue: false);
        }
        return new Value(DataType.Boolean, bValue: true);
    }

    internal static bool IsSameDouble(double thisDouble, double other, byte decimals = 7) {
        if (thisDouble == other) {
            return true;
        }

        if (thisDouble == 0.0 || other == 0.0) {
            if (Math.Log10((thisDouble == 0.0) ? other : thisDouble) <= (double)(-decimals)) {
                return true;
            }

            return false;
        }

        if ((thisDouble < 0.0 && other > 0.0) || (thisDouble > 0.0 && other < 0.0)) {
            return false;
        }

        double num = Math.Abs(thisDouble);
        double num2 = Math.Abs(other);
        double num3 = ((num > num2) ? (num2 / num) : (num / num2));
        return (int)Math.Log10(1.0 - num3) < -decimals;
    }

    internal static Value Median(ReadOnlySpan<Value> numbers, string _ = "median") {
        var values = numbers.ToArray().Select(v => v.Nvalue ?? 0).OrderBy(x => x).ToList();
        if (values.Count == 0) return new Value(DataType.Number, nValue: 0);

        if (values.Count % 2 == 1) {
            return new Value(DataType.Number, nValue: values[values.Count / 2]);
        }

        var a = values[(values.Count / 2) - 1];
        var b = values[values.Count / 2];
        return new Value(DataType.Number, nValue: (a + b) / 2.0);
    }

    internal static Value Mode(ReadOnlySpan<Value> numbers, string _ = "mode") {
        var values = numbers.ToArray().Select(v => v.Nvalue ?? 0).ToList();
        if (values.Count == 0) return new Value(DataType.Number, nValue: 0);

        var groups = values.GroupBy(x => x).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).ToList();
        return new Value(DataType.Number, nValue: groups.First().Key);
    }

    internal static Value StDev(ReadOnlySpan<Value> numbers, string _ = "stdev") {
        var values = numbers.ToArray().Select(v => v.Nvalue ?? 0).ToList();
        if (values.Count < 2) return new Value(DataType.Number, nValue: 0);

        double avg = values.Average();
        double sum = values.Sum(d => Math.Pow(d - avg, 2));
        return new Value(DataType.Number, nValue: Math.Sqrt(sum / (values.Count - 1)));
    }

    internal static Value Var(ReadOnlySpan<Value> numbers, string _ = "var") {
        var values = numbers.ToArray().Select(v => v.Nvalue ?? 0).ToList();
        if (values.Count < 2) return new Value(DataType.Number, nValue: 0);

        double avg = values.Average();
        double sum = values.Sum(d => Math.Pow(d - avg, 2));
        return new Value(DataType.Number, nValue: sum / (values.Count - 1));
    }

    internal static Value Count(ReadOnlySpan<Value> args, string _ = "count") {
        return new Value(DataType.Number, nValue: args.Length);
    }
    #endregion Private Methods

    internal enum NumberCompareMode {
        Ascending = 0,
        Descending = 1,
    }
}

internal static class OtherFunctionNames {
    public const string IsAscending = "is_ascending";
    public const string IsDesc = "is_descending";
    public const string IsAscLoose = "is_ascending_loose";
    public const string IsDescLoose = "is_descending_loose";
    public const string HasAdjacentEquals = "has_adjacent_equals";
    public const string AreAllNumbers = "are_all_numbers";
    public const string Median = "median";
    public const string Mode = "mode";
    public const string StDev = "stdev";
    public const string Var = "var";
    public const string Count = "count";
}
