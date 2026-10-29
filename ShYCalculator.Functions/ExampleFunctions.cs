using ShY.Classes;
using System.Collections.Frozen;

namespace ShY.Functions;

public class ExampleFunctions : ICalcFunctionsExtension {
    // WHY: Func<ReadOnlySpan<Value>, Value> requires .NET 9+; a private delegate keeps the extension portable to net8.0.
    private delegate Value Body(ReadOnlySpan<Value> parameters);

    private readonly FrozenDictionary<string, CalcFunction> m_funcDef;
    private readonly FrozenDictionary<string, Body> m_dispatch;

    public ExampleFunctions() {
        // Read configuration using the helper, passing the Type of this class to locate the resource
        var funcList = CalcFunctionsHelper.ReadFunctionsConfiguration(Name, typeof(ExampleFunctions));
        CalcFunctionsHelper.CheckFunctionsConfiguration(funcList);

        m_funcDef = funcList.ToDictionary(x => x.Name!, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        m_dispatch = new Dictionary<string, Body>(StringComparer.OrdinalIgnoreCase) {
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
