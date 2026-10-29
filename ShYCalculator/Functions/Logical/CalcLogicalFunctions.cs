// -----------------------------------------------------------------------------
// <summary>
//     Implements advanced logical functions (if, switch).
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;
using System.Collections.Frozen;

namespace ShY.Functions.Logical;

/// <summary>
/// Implements advanced logical functions (any, all, not).
/// </summary>
public class CalcLogicalFunctions : ICalcFunctionsExtension {
    #region Members
    private readonly FrozenDictionary<string, CalcFunction> m_funcDef;
    private readonly FrozenDictionary<string, FunctionBody> m_dispatch;
    #endregion Members

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="CalcLogicalFunctions"/> class.
    /// </summary>
    public CalcLogicalFunctions() {
        m_funcDef = CalcFunctionsHelper.LoadFunctionDefinitions(Name, typeof(CalcLogicalFunctions));
        m_dispatch = SharedDispatch;
    }

    private static readonly FrozenDictionary<string, FunctionBody> SharedDispatch = CreateDispatch();

    private static FrozenDictionary<string, FunctionBody> CreateDispatch() {
        return new Dictionary<string, FunctionBody>(StringComparer.OrdinalIgnoreCase) {
            [FunctionNames.Any] = p => Any(p, FunctionNames.Any),
            [FunctionNames.All] = p => All(p, FunctionNames.All),
            [FunctionNames.Not] = p => Not(p, FunctionNames.Not),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
    #endregion Constructor

    #region Properties
    /// <inheritdoc/>
    public string Name => "CalcLogicalFunctions";
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
    internal static Value Any(ReadOnlySpan<Value> args, string _ = FunctionNames.Any) {
        for (int i = 0; i < args.Length; i++) {
            if (args[i].Bvalue == true) {
                return new Value(DataType.Boolean, bValue: true);
            }
        }
        return new Value(DataType.Boolean, bValue: false);
    }

    internal static Value All(ReadOnlySpan<Value> args, string _ = FunctionNames.All) {
        for (int i = 0; i < args.Length; i++) {
            if (args[i].Bvalue != true) {
                return new Value(DataType.Boolean, bValue: false);
            }
        }
        return new Value(DataType.Boolean, bValue: true);
    }

    internal static Value Not(ReadOnlySpan<Value> args, string _ = FunctionNames.Not) {
        // Argument check is done by CheckFunctionArguments based on JSON config
        // which enforces 1 boolean argument.
        return new Value(DataType.Boolean, bValue: !(args[0].Bvalue ?? false));
    }
    #endregion Private Methods
}

internal static class FunctionNames {
    public const string Any = "any";
    public const string All = "all";
    public const string Not = "not";
}
