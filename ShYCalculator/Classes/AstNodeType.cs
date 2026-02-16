namespace ShYCalculator.Classes;

/// <summary>
/// Defines standard constants for AST node types to ensure consistency and avoid direct string usage.
/// </summary>
public static class AstNodeType {
    /// <summary>Represents a numeric value node.</summary>
    public const string Number = "number";
    /// <summary>Represents a string value node.</summary>
    public const string String = "string";
    /// <summary>Represents a constant value node (e.g. PI, E).</summary>
    public const string Constant = "constant";
    /// <summary>Represents a variable node.</summary>
    public const string Variable = "variable";
    /// <summary>Represents a binary operation node (e.g. 1 + 2).</summary>
    public const string Binary = "binary";
    /// <summary>Represents a unary operation node (e.g. -5, 5!).</summary>
    public const string Unary = "unary";
    /// <summary>Represents a function call node (e.g. sin(90)).</summary>
    public const string Function = "function";
    /// <summary>Represents a ternary operation node (e.g. condition ? true : false).</summary>
    public const string Ternary = "ternary";
    /// <summary>Represents an error node.</summary>
    public const string Error = "error";
}
