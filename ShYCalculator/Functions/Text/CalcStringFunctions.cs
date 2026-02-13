// -----------------------------------------------------------------------------
// <summary>
//     Implements string manipulation functions (len, upper, lower, left, right, mid, etc.).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;
using System.Reflection;
using System.Collections.Frozen;

namespace ShYCalculator.Functions.Text;

/// <summary>
/// Implements string manipulation functions (len, upper, lower, left, right, mid, etc.).
/// </summary>
public class CalcStringFunctions : ICalcFunctionsExtension {
    #region Members
    private readonly FrozenDictionary<string, CalcFunction> m_funcDef;
    private readonly FrozenDictionary<string, Func<ReadOnlySpan<Value>, Value>> m_dispatch;
    #endregion Members

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="CalcStringFunctions"/> class.
    /// </summary>
    public CalcStringFunctions() {
        var funcList = CalcFunctionsHelper.ReadFunctionsConfiguration(Name, typeof(CalcStringFunctions));
        CalcFunctionsHelper.CheckFunctionsConfiguration(funcList);
        m_funcDef = funcList.ToDictionary(x => x.Name!, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        m_dispatch = CreateDispatch();
    }

    private static FrozenDictionary<string, Func<ReadOnlySpan<Value>, Value>> CreateDispatch() {
        return new Dictionary<string, Func<ReadOnlySpan<Value>, Value>>(StringComparer.OrdinalIgnoreCase) {
            [FunctionNames.StrContains] = p => Contains(p, StringContainsMode.Contains, FunctionNames.StrContains),
            [FunctionNames.StrStarts] = p => Contains(p, StringContainsMode.StartsWith, FunctionNames.StrStarts),
            [FunctionNames.StrEnds] = p => Contains(p, StringContainsMode.EndsWith, FunctionNames.StrEnds),
            [FunctionNames.StrEqual] = p => CompareStrings(p, StringCompareMode.Equal, FunctionNames.StrEqual),
            [FunctionNames.StrNotEqual] = p => CompareStrings(p, StringCompareMode.NotEqual, FunctionNames.StrNotEqual),
            [FunctionNames.StrAll] = p => AllStringValues(p, FunctionNames.StrAll)
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
    #endregion Constructor

    #region Properties
    /// <inheritdoc/>
    public string Name => "CalcStringFunctions";
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

        throw new FunctionException("Invalid Function name in extension {Name} on function {functionName}");
    }
    #endregion Methods

    #region Private Methods
    internal static Value Contains(ReadOnlySpan<Value> parameters, StringContainsMode containsMode = StringContainsMode.Contains, string _ = FunctionNames.StrContains) {
        if (parameters.Length < 2 || parameters.Length > 3) {
            return new Value(DataType.Boolean, bValue: false);
        }

        if (parameters[0].Svalue == null || parameters[1].Svalue == null || string.IsNullOrEmpty(parameters[1].Svalue)) {
            return new Value(DataType.Boolean, bValue: false);
        }

        string text = parameters[0].Svalue!;
        string searchTerm = parameters[1].Svalue!;
        bool caseSensitive = parameters.Length == 3 && parameters[2].Bvalue == true;

        var stringComparison = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

        var result = containsMode == StringContainsMode.Contains ? text.Contains(searchTerm, stringComparison) :
            containsMode == StringContainsMode.StartsWith ? text.StartsWith(searchTerm, stringComparison) :
            containsMode == StringContainsMode.EndsWith && text.EndsWith(searchTerm, stringComparison);

        return new Value(DataType.Boolean, bValue: result);
    }

    internal static Value CompareStrings(ReadOnlySpan<Value> parameters, StringCompareMode compareMode = StringCompareMode.Equal, string functionName = FunctionNames.StrEqual) {
        var last = parameters[^1];
        var hasCaseSensitive = last.Svalue == null && last.Bvalue != null;
        var arguments = hasCaseSensitive ? parameters[..^1] : parameters;

        var allString = AllStringValues(arguments, functionName);
        if (allString.Bvalue == false) {
            return allString;
        }

        if (arguments.Length < 2) return new Value(DataType.Boolean, bValue: false);

        var isCaseSensitive = hasCaseSensitive && (last.Bvalue ?? false);
        var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        bool result = true;
        if (compareMode == StringCompareMode.Equal) {
            string first = arguments[0].Svalue!;
            for (int i = 1; i < arguments.Length; i++) {
                if (!string.Equals(first, arguments[i].Svalue, comparison)) {
                    result = false;
                    break;
                }
            }
        }
        else {
            // NotEqual: All must be distinct
            for (int i = 0; i < arguments.Length; i++) {
                for (int j = i + 1; j < arguments.Length; j++) {
                    if (string.Equals(arguments[i].Svalue, arguments[j].Svalue, comparison)) {
                        result = false;
                        goto EndNotEqual;
                    }
                }
            }
        EndNotEqual:;
        }

        return new Value(DataType.Boolean, bValue: result);
    }

    internal static Value AllStringValues(ReadOnlySpan<Value> strings, string _ = FunctionNames.StrAll) {
        for (int i = 0; i < strings.Length; i++) {
            if (strings[i].Svalue == null) return new Value(DataType.Boolean, bValue: false);
        }
        return new Value(DataType.Boolean, bValue: true);
    }
    #endregion Private Methods

    internal enum StringContainsMode {
        Contains = 0,
        StartsWith = 1,
        EndsWith = 2,
    }

    internal enum StringCompareMode {
        Equal = 0,
        NotEqual = 1,
    }
}

internal static class FunctionNames {
    public const string StrContains = "str_contains";
    public const string StrStarts = "str_starts";
    public const string StrEnds = "str_ends";
    public const string StrEqual = "str_equal";
    public const string StrNotEqual = "str_notequal";
    public const string StrAll = "str_all";
}
