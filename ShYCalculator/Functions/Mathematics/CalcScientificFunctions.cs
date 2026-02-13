// -----------------------------------------------------------------------------
// <summary>
//     Implements scientific functions (sin, cos, tan, log, sqrt, pow, etc.).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;
using Math = System.Math;
using System.Collections.Frozen;

namespace ShYCalculator.Functions.Mathematics;

internal class CalcScientificFunctions : ICalcFunctionsExtension {
    #region Members
    private readonly FrozenDictionary<string, CalcFunction> m_funcDef;
    private readonly FrozenDictionary<string, Func<ReadOnlySpan<Value>, Value>> m_dispatch;
    #endregion Members

    #region Constructor
    public CalcScientificFunctions() {
        var funcList = CalcFunctionsHelper.ReadFunctionsConfiguration(Name, typeof(CalcScientificFunctions));
        CalcFunctionsHelper.CheckFunctionsConfiguration(funcList);
        m_funcDef = funcList.ToDictionary(x => x.Name!, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        m_dispatch = CreateDispatch();
    }

    private FrozenDictionary<string, Func<ReadOnlySpan<Value>, Value>> CreateDispatch() {
        return new Dictionary<string, Func<ReadOnlySpan<Value>, Value>>(StringComparer.OrdinalIgnoreCase) {
            [ScientificFunctionNames.Sin] = p => new Value { Nvalue = Math.Sin(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Cos] = p => new Value { Nvalue = Math.Cos(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Tan] = p => new Value { Nvalue = Math.Tan(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Cot] = p => new Value { Nvalue = Cot(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Ln] = p => new Value { Nvalue = Math.Log(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Log] = p => new Value { Nvalue = Math.Log(p[0].Nvalue ?? 0, p[1].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Log2] = p => new Value { Nvalue = Math.Log2(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Log10] = p => new Value { Nvalue = Math.Log10(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Sinh] = p => new Value { Nvalue = Math.Sinh(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Cosh] = p => new Value { Nvalue = Math.Cosh(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Tanh] = p => new Value { Nvalue = Math.Tanh(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Coth] = p => new Value { Nvalue = Coth(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Asin] = p => new Value { Nvalue = Math.Asin(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Acos] = p => new Value { Nvalue = Math.Acos(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Atan] = p => new Value { Nvalue = Math.Atan(p[0].Nvalue ?? 0), DataType = DataType.Number },
            [ScientificFunctionNames.Acot] = p => new Value { Nvalue = Acot(p[0].Nvalue ?? 0), DataType = DataType.Number },

        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
    #endregion Constructor

    #region Properties
    public string Name => "CalcScientificFunctions";
    #endregion Properties

    #region Methods
    public IEnumerable<CalcFunction> GetFunctions() {
        return m_funcDef.Values;
    }

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
    internal double Cot(double angle) {
        if (double.IsNaN(angle)) {
            throw new FunctionException($"Invalid argument; expected number, provided {angle}; In extension {Name} on function cot");
        }

        var cot = 1 / Math.Tan(angle);
        return cot;
    }

    internal double Coth(double value) {
        if (double.IsNaN(value)) {
            throw new FunctionException($"Invalid argument; expected number, provided {value}; In extension {Name} on function coth");
        }

        double coth = Math.Cosh(value) / Math.Sinh(value);
        return coth;
    }

    internal double Acot(double value) {
        if (double.IsNaN(value)) {
            throw new FunctionException($"Invalid argument; expected number, provided {value}; In extension {Name} on function acot");
        }

        double acot = Math.Atan(1 / value);
        return acot;
    }
    #endregion Private Methods
}

internal static class ScientificFunctionNames {
    public const string Sin = "sin";
    public const string Cos = "cos";
    public const string Tan = "tan";
    public const string Cot = "cot";
    public const string Ln = "ln";
    public const string Log = "log";
    public const string Log2 = "log2";
    public const string Log10 = "log10";
    public const string Sinh = "sinh";
    public const string Cosh = "cosh";
    public const string Tanh = "tanh";
    public const string Coth = "coth";
    public const string Asin = "asin";
    public const string Acos = "acos";
    public const string Atan = "atan";
    public const string Acot = "acot";

}
