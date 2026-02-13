// -----------------------------------------------------------------------------
// <summary>
//     Defines the properties of a mathematical operator (Precedence, Associativity).
//     Used by the Shunting-Yard algorithm to determine operation order.
// </summary>
// -----------------------------------------------------------------------------
namespace ShYCalculator.Classes;

/// <summary>
/// Defines the properties of a mathematical operator (Precedence, Associativity).
/// Used by the Shunting-Yard algorithm to determine operation order.
/// </summary>
public readonly struct Operator {
    /// <summary>
    /// Gets the precedence level of the operator. Higher values mean higher precedence.
    /// </summary>
    public int Precedence { get; init; }
    
    /// <summary>
    /// Gets the associativity of the operator (Left or Right).
    /// </summary>
    public Associativity Associativity { get; init; }
    
    /// <summary>
    /// Gets the supported data types for this operator.
    /// </summary>
    public DataType ValidDataTypes { get; init; }
    
    /// <summary>
    /// Gets the category of the operator (Arithmetic, Logical, etc.).
    /// </summary>
    public Category Category { get; init; }
}

/// <summary>
/// Defines the associativity of an operator.
/// </summary>
public enum Associativity {
    /// <summary>
    /// Left-associative (e.g., 1 - 2 - 3 -> (1 - 2) - 3).
    /// </summary>
    Left,
    /// <summary>
    /// Right-associative (e.g., 2 ^ 3 ^ 4 -> 2 ^ (3 ^ 4)).
    /// </summary>
    Right,
}

/// <summary>
/// categorization of operators for grouping and validation.
/// </summary>
public enum Category {
    /// <summary>Arithmetic operators (+, -, *, /, %, ^).</summary>
    Arithmetic,
    /// <summary>Comparison operators (==, !=, &lt;, &gt;, &lt;=, &gt;=).</summary>
    Comparison,
    /// <summary>Logical operators (&amp;&amp;, ||, !).</summary>
    Logical,
    /// <summary>Bitwise operators (&amp;, |, ^, ~).</summary>
    Bitwise,
    /// <summary>Grouping operators ((, )).</summary>
    Grouping,
    /// <summary>Unary operators (-, !).</summary>
    Unary,
}
