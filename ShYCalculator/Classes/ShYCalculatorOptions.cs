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
    /// Gets or sets the date format for parsing strings (e.g. "dd/MM/yyyy").
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Gets or sets the culture info name for parsing (e.g. "en-US", "fr-FR").
    /// </summary>
    public string? CultureName { get; set; }

    /// <summary>
    /// Checks if any standard functions are enabled.
    /// </summary>
    public bool AnyEnabled => EnabledExtensions != FunctionExtensions.None;
}
