// -----------------------------------------------------------------------------
// <summary>
//     Configuration options for the calculator instance.
// </summary>
// -----------------------------------------------------------------------------
namespace ShY.Classes;

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

    /// <summary>
    /// Maximum nesting depth of lazily evaluated constructs (ternaries, <c>if()</c>, <c>&amp;&amp;</c>, <c>||</c>).
    /// Plain parentheses are not limited. 0 disables the check; an execution-stack guard still applies.
    /// </summary>
    /// <remarks>Defaults to 64, matching the nested-function limit of common spreadsheet engines.</remarks>
    public int MaxNestingDepth { get; set; } = DefaultMaxNestingDepth;

    internal const int DefaultMaxNestingDepth = 64;

    /// <summary>
    /// Maximum expression length in characters. 0 (default) means unlimited.
    /// </summary>
    public int MaxExpressionLength { get; set; }

    /// <summary>
    /// Clock and local time zone used by <c>dt_now()</c> and <c>dt_today()</c>. Defaults to <see cref="TimeProvider.System"/>.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Numeric representation used during evaluation. Defaults to <see cref="NumericMode.Double"/>.
    /// </summary>
    public NumericMode NumericMode { get; set; } = NumericMode.Double;
}

/// <summary>
/// Numeric representation used during evaluation.
/// </summary>
public enum NumericMode {
    /// <summary>IEEE 754 double precision: fastest, wide range, binary rounding artifacts (0.1 + 0.2 != 0.3).</summary>
    Double,
    /// <summary>
    /// Exact base-10 <see cref="decimal"/> for literals, variables, constants, operators and rounding/aggregate functions;
    /// transcendental functions compute in double and convert back. Results outside the decimal range fail with
    /// <see cref="ErrorCode.ValueOutOfRange"/>.
    /// </summary>
    Decimal,
}
