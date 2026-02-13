using Microsoft.JSInterop;
using ShYCalculator.Classes;

namespace ShYCalculator.Wasm;

/// <summary>
/// Exposes ShYCalculator functionality to WebAssembly via JS Interop.
/// </summary>
public class ShYCalculatorInterop
{
    private readonly ShYCalculator _calculator = new ShYCalculator();

    /// <summary>
    /// Simple ping method to verify interop connection.
    /// </summary>
    /// <returns>A pong message from the instance.</returns>
    [JSInvokable]
    public string Ping() => "Pong from ShYCalculator.Wasm Instance";

    /// <summary>
    /// Calculates the result of a mathematical expression.
    /// This method is callable from JavaScript.
    /// </summary>
    /// <param name="expression">The expression string to evaluate.</param>
    /// <returns>The result of the calculation.</returns>
    [JSInvokable]
    public CalculationResult Calculate(string expression)
    {
        return _calculator.Calculate(expression);
    }

    /// <summary>
    /// Calculates the result of a mathematical expression using provided variables.
    /// </summary>
    /// <param name="expression">The mathematical expression to evaluate.</param>
    /// <param name="variables">A dictionary of variable names and their values.</param>
    /// <returns>The result of the calculation.</returns>
    [JSInvokable]
    public CalculationResult CalculateWithVars(string expression, Dictionary<string, double> variables)
    {
        return _calculator.Calculate(expression, variables);
    }

    /// <summary>
    /// Retrieves the documentation for functions and operators.
    /// </summary>
    /// <returns>A JSON string containing the documentation.</returns>
    [JSInvokable]
    public string GetDocumentation()
    {
        var functions = _calculator.Environment.Functions.Values
            .Distinct()
            .SelectMany(ext => ext.GetFunctions())
            .Select(f => new
            {
                f.Name,
                f.Description,
                f.Examples,
                Arguments = f.Arguments?.Select(a => $"{a.Type}{(a.Optional ? "?" : "")}").ToList()
            })
            .OrderBy(f => f.Name)
            .ToList();

        var operators = _calculator.Environment.Operators
            .Select(o => new
            {
                Symbol = o.Key,
                o.Value.Precedence,
                Associativity = o.Value.Associativity.ToString(),
                Category = o.Value.Category.ToString()
            })
            .OrderByDescending(o => o.Precedence)
            .ToList();

        var doc = new
        {
            functions,
            operators
        };

        return System.Text.Json.JsonSerializer.Serialize(doc);
    }
}
