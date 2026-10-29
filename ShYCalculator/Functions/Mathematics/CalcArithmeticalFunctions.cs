// -----------------------------------------------------------------------------
// <summary>
//     Implements standard arithmetic functions (sum, min, max, average, abs, round, etc.).
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;
using System.Reflection;
using System.Collections.Frozen;
using Math = System.Math;

namespace ShY.Functions.Mathematics;

/// <summary>
/// Implements standard arithmetic functions (sum, min, max, average, abs, round, etc.).
/// </summary>
public class CalcArithmeticalFunctions : ICalcFunctionsExtension {
    #region Members
    private readonly FrozenDictionary<string, CalcFunction> m_funcDef;
    private readonly FrozenDictionary<string, FunctionBody> m_dispatch;
    #endregion Members

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="CalcArithmeticalFunctions"/> class.
    /// </summary>
    public CalcArithmeticalFunctions() {
        m_funcDef = CalcFunctionsHelper.LoadFunctionDefinitions(Name, typeof(CalcArithmeticalFunctions));
        m_dispatch = SharedDispatch;
    }

    private static readonly FrozenDictionary<string, FunctionBody> SharedDispatch = CreateDispatch();

    private static FrozenDictionary<string, FunctionBody> CreateDispatch() {
        return new Dictionary<string, FunctionBody>(StringComparer.OrdinalIgnoreCase) {
            [ArithmeticalFunctionNames.Abs] = p => Abs(p, ArithmeticalFunctionNames.Abs),
            [ArithmeticalFunctionNames.Min] = p => Min(p, ArithmeticalFunctionNames.Min),
            [ArithmeticalFunctionNames.Max] = p => Max(p, ArithmeticalFunctionNames.Max),
            [ArithmeticalFunctionNames.Avg] = p => Avg(p, ArithmeticalFunctionNames.Avg),
            [ArithmeticalFunctionNames.Sum] = p => Sum(p, ArithmeticalFunctionNames.Sum),
            [ArithmeticalFunctionNames.SumAlias] = p => Sum(p, ArithmeticalFunctionNames.SumAlias),
            [ArithmeticalFunctionNames.Pow] = p => Pow(p, ArithmeticalFunctionNames.Pow),
            [ArithmeticalFunctionNames.Sqrt] = p => Sqrt(p, ArithmeticalFunctionNames.Sqrt),
            [ArithmeticalFunctionNames.Round] = p => Round(p, ArithmeticalFunctionNames.Round),
            [ArithmeticalFunctionNames.Floor] = p => Floor(p, ArithmeticalFunctionNames.Floor),
            [ArithmeticalFunctionNames.Ceiling] = p => Ceiling(p, ArithmeticalFunctionNames.Ceiling),
            [ArithmeticalFunctionNames.Trunc] = p => Trunc(p, ArithmeticalFunctionNames.Trunc),
            [ArithmeticalFunctionNames.Sign] = p => Sign(p, ArithmeticalFunctionNames.Sign),
            [ArithmeticalFunctionNames.Random] = p => Random(p, ArithmeticalFunctionNames.Random),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
    #endregion Constructor

    #region Properties
    /// <inheritdoc/>
    public string Name => "CalcArithmeticalFunctions";
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
    // WHY: exact paths engage whenever any argument is decimal (NumericMode.Decimal or decimal variables); double paths are unchanged.
    private static bool AnyDecimal(ReadOnlySpan<Value> args) {
        foreach (var arg in args) {
            if (arg.IsDecimal) return true;
        }
        return false;
    }

    private static decimal Exact(in Value value) => value.Mvalue ?? 0;

    internal static Value Abs(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Abs) {
        if (args[0].IsDecimal) return Value.Number(Math.Abs(Exact(args[0])));
        return new Value { Nvalue = Math.Abs(args[0].Nvalue ?? 0), DataType = DataType.Number };
    }

    internal static Value Min(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Min) {
        if (AnyDecimal(args)) {
            var exactMin = Exact(args[0]);
            for (int i = 1; i < args.Length; i++) exactMin = Math.Min(exactMin, Exact(args[i]));
            return Value.Number(exactMin);
        }

        double min = args[0].Nvalue ?? 0;
        for (int i = 1; i < args.Length; i++) {
            double val = args[i].Nvalue ?? 0;
            if (val < min) min = val;
        }
        return new Value { Nvalue = min, DataType = DataType.Number };
    }

    internal static Value Max(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Max) {
        if (AnyDecimal(args)) {
            var exactMax = Exact(args[0]);
            for (int i = 1; i < args.Length; i++) exactMax = Math.Max(exactMax, Exact(args[i]));
            return Value.Number(exactMax);
        }

        double max = args[0].Nvalue ?? 0;
        for (int i = 1; i < args.Length; i++) {
            double val = args[i].Nvalue ?? 0;
            if (val > max) max = val;
        }
        return new Value { Nvalue = max, DataType = DataType.Number };
    }

    internal static Value Avg(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Avg) {
        if (AnyDecimal(args)) return Value.Number(ExactSum(args) / args.Length);

        double total = 0;
        for (int i = 0; i < args.Length; i++) {
            total += args[i].Nvalue ?? 0;
        }
        return new Value { Nvalue = total / args.Length, DataType = DataType.Number };
    }

    internal static Value Sum(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Sum) {
        if (AnyDecimal(args)) return Value.Number(ExactSum(args));

        double total = 0;
        for (int i = 0; i < args.Length; i++) {
            total += args[i].Nvalue ?? 0;
        }
        return new Value { Nvalue = total, DataType = DataType.Number };
    }

    private static decimal ExactSum(ReadOnlySpan<Value> args) {
        decimal total = 0;
        foreach (var arg in args) total += Exact(arg);
        return total;
    }

    internal static Value Pow(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Pow) {
        if (AnyDecimal(args)) return Value.Number(Calculator.Operations.DecimalMath.Pow(Exact(args[0]), Exact(args[1])));
        return new Value { Nvalue = Math.Pow(args[0].Nvalue ?? 0, args[1].Nvalue ?? 0), DataType = DataType.Number };
    }

    internal static Value Sqrt(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Sqrt) {
        return new Value { Nvalue = Math.Sqrt(args[0].Nvalue ?? 0), DataType = DataType.Number };
    }

    internal static Value Round(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Round) {
        int digits = 0;
        if (args.Length > 1) {
            digits = (int)(args[1].Nvalue ?? 0);
        }

        if (args[0].IsDecimal) return Value.Number(Math.Round(Exact(args[0]), digits));

        double number = args[0].Nvalue ?? 0;
        return new Value(DataType.Number, nValue: Math.Round(number, digits));
    }

    internal static Value Floor(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Floor) {
        if (args[0].IsDecimal) return Value.Number(Math.Floor(Exact(args[0])));
        return new Value(DataType.Number, nValue: Math.Floor(args[0].Nvalue ?? 0));
    }

    internal static Value Ceiling(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Ceiling) {
        if (args[0].IsDecimal) return Value.Number(Math.Ceiling(Exact(args[0])));
        return new Value(DataType.Number, nValue: Math.Ceiling(args[0].Nvalue ?? 0));
    }

    internal static Value Trunc(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Trunc) {
        if (args[0].IsDecimal) return Value.Number(Math.Truncate(Exact(args[0])));
        return new Value(DataType.Number, nValue: Math.Truncate(args[0].Nvalue ?? 0));
    }

    internal static Value Sign(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Sign) {
        if (args[0].IsDecimal) return Value.Number((decimal)Math.Sign(Exact(args[0])));
        return new Value(DataType.Number, nValue: Math.Sign(args[0].Nvalue ?? 0));
    }

    internal static Value Random(ReadOnlySpan<Value> args, string _ = ArithmeticalFunctionNames.Random) {
        if (args.Length == 2) {
            int min = (int)(args[0].Nvalue ?? 0);
            int max = (int)(args[1].Nvalue ?? 0);
            if (min >= max) return new Value(DataType.Number, nValue: min);
            return new Value(DataType.Number, nValue: System.Random.Shared.Next(min, max));
        }
        return new Value(DataType.Number, nValue: System.Random.Shared.NextDouble());
    }
    #endregion Private Methods
}

internal static class ArithmeticalFunctionNames {
    public const string Abs = "abs";
    public const string Min = "min";
    public const string Max = "max";
    public const string Avg = "avg";
    public const string Sum = "sum";
    public const string SumAlias = "∑";
    public const string Pow = "pow";
    public const string Sqrt = "sqrt";
    public const string Round = "round";
    public const string Floor = "floor";
    public const string Ceiling = "ceiling";
    public const string Trunc = "trunc";
    public const string Sign = "sign";
    public const string Random = "random";
}
