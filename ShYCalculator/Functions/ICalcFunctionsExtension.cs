// -----------------------------------------------------------------------------
// <summary>
//     Interface for defining groupings of custom calculator functions (e.g., Math, Date, Text).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;

namespace ShYCalculator.Functions;

/// <summary>
/// Interface for defining groupings of custom calculator functions (e.g., Math, Date, Text).
/// </summary>
public interface ICalcFunctionsExtension {
    /// <summary>Gets the name of the extension.</summary>
    public string Name { get; }
    /// <summary>Gets the list of functions provided by this extension.</summary>
    public IEnumerable<CalcFunction> GetFunctions();
    /// <summary>Executes a function by name.</summary>
    public Value ExecuteFunction(string functionName, ReadOnlySpan<Value> parameters);
}

/// <summary>
/// Represents an argument definition for a calculator function.
/// </summary>
public class CalcFunctionArgument {
    /// <summary>Gets or sets the argument type (Number, String, etc.).</summary>
    public string? Type { get; set; }
    /// <summary>Gets or sets the argument description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the minimum number of arguments (if repetitive).</summary>
    public int? Min { get; set; }
    /// <summary>Gets or sets the maximum number of arguments.</summary>
    public int? Max { get; set; }
    /// <summary>Gets or sets a value indicating whether the argument is optional.</summary>
    public bool Optional { get; set; } = false;
    /// <summary>Gets or sets a list of sub-arguments (for complex types).</summary>
    public List<CalcFunctionArgument>? Arguments { get; set; }

    /// <summary>Returns a string representation of the argument.</summary>
    public override string ToString() => $"Type: {Type}, Optional: {Optional}";
}

/// <summary>
/// Represents a calculator function definition.
/// </summary>
public class CalcFunction {
    /// <summary>Gets or sets the function name.</summary>
    public string? Name { get; set; }
    /// <summary>Gets or sets the function description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets usage examples.</summary>
    public List<string>? Examples { get; set; }
    /// <summary>Gets or sets the list of arguments.</summary>
    public List<CalcFunctionArgument>? Arguments { get; set; }

    /// <summary>Returns a string representation of the function.</summary>
    public override string ToString() => $"Function: {Name ?? "Unnamed"}";
}
