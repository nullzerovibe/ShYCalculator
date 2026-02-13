// -----------------------------------------------------------------------------
// <summary>
//     Configuration options for the calculator instance.
// </summary>
// -----------------------------------------------------------------------------
namespace ShYCalculator.Classes;

/// <summary>
/// Configuration options for the calculator instance.
/// </summary>
public class ShYCalculatorOptions {
    /// <summary>
    /// Gets or sets the function extensions to enable.
    /// </summary>
    public FunctionExtensions EnabledExtensions { get; set; } = FunctionExtensions.All;

    /// <summary>
    /// Checks if any standard functions are enabled.
    /// </summary>
    public bool AnyEnabled => EnabledExtensions != FunctionExtensions.None;
}
