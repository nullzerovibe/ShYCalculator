using ShYCalculator.Classes;
using System.Collections.Frozen;

namespace ShYCalculator.Functions;

public class ExampleFunctions : ICalcFunctionsExtension {
    private readonly FrozenDictionary<string, CalcFunction> m_funcDef;
    private readonly FrozenDictionary<string, Func<ReadOnlySpan<Value>, Value>> m_dispatch;

    public ExampleFunctions() {
        // Read configuration using the helper, passing the Type of this class to locate the resource
        var funcList = CalcFunctionsHelper.ReadFunctionsConfiguration(Name, typeof(ExampleFunctions));
        CalcFunctionsHelper.CheckFunctionsConfiguration(funcList);

        m_funcDef = funcList.ToDictionary(x => x.Name!, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        m_dispatch = new Dictionary<string, Func<ReadOnlySpan<Value>, Value>>(StringComparer.OrdinalIgnoreCase) {
            ["hello_world"] = _ => new Value(DataType.String, sValue: "Hello World!"),
            ["add_one"] = p => new Value(DataType.Number, nValue: (p[0].Nvalue ?? 0) + 1),
            ["calculateSingleThing"] = p => new Value { Nvalue = 1, DataType = DataType.Number }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public string Name => "ExampleFunctions";

    public IEnumerable<CalcFunction> GetFunctions() => m_funcDef.Values;

    public Value ExecuteFunction(string functionName, ReadOnlySpan<Value> parameters) {
        if (!m_funcDef.TryGetValue(functionName, out var functionDef)) {
            throw new FunctionException($"Function not found: {functionName}");
        }

        CalcFunctionsHelper.CheckFunctionArguments(functionDef, parameters, Name);

        if (m_dispatch.TryGetValue(functionName, out var function)) {
            return function(parameters);
        }

        throw new FunctionException($"Function logic missing: {functionName}");
    }
}
