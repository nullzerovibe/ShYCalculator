// -----------------------------------------------------------------------------
// <summary>
//     Defines the core Token structure used by the tokenizer and parser.
//     Includes TokenKey (Span wrapper), TokenType, and Operator definitions.
// </summary>
// -----------------------------------------------------------------------------
namespace ShYCalculator.Classes;

/// <summary>
/// A lightweight wrapper around ReadOnlyMemory&lt;char&gt; to represent a token's text content.
/// </summary>
public readonly struct TokenKey {
    private readonly ReadOnlyMemory<char> _memory;
    /// <summary>Initializes a new instance of the <see cref="TokenKey"/> struct from memory.</summary>
    public TokenKey(ReadOnlyMemory<char> memory) => _memory = memory;
    /// <summary>Initializes a new instance of the <see cref="TokenKey"/> struct from string.</summary>
    public TokenKey(string? s) => _memory = (s ?? "").AsMemory();

    /// <summary>Returns the underlying memory.</summary>
    public ReadOnlyMemory<char> AsMemory() => _memory;
    /// <summary>Returns the underlying span.</summary>
    public ReadOnlySpan<char> AsSpan() => _memory.Span;
    /// <summary>Returns the string representation.</summary>
    public override string ToString() => _memory.ToString();

    /// <summary>Gets the length of the token key.</summary>
    public int Length => _memory.Length;

    /// <summary>Implicit conversion from string.</summary>
    public static implicit operator TokenKey(string? s) => new(s);
    /// <summary>Implicit conversion from char memory.</summary>
    public static implicit operator TokenKey(ReadOnlyMemory<char> m) => new(m);
    /// <summary>Implicit conversion to char memory.</summary>
    public static implicit operator ReadOnlyMemory<char>(TokenKey tk) => tk._memory;
}

/// <summary>
/// Represents a unit of text in the expression, such as a number, operator, or function.
/// </summary>
/// <param name="index">The index in the original expression string.</param>
/// <param name="key">The string content of the token.</param>
/// <param name="type">The type of the token (Number, String, etc.).</param>
/// <param name="negation">Indicates if this number token is negated (unary minus).</param>
/// <param name="functionInfo">Additional info if this is a function token.</param>
/// <param name="ternaryBranches">Branches if this is a ternary operator.</param>
/// <param name="operatorKind">The specific kind of operator.</param>
public readonly struct Token(long index, TokenKey key, TokenType type, bool negation = false, FunctionInfo? functionInfo = null, TernaryBranches? ternaryBranches = null, OperatorKind operatorKind = OperatorKind.None) {
    /// <summary>Gets the index of the token.</summary>
    public long Index { get; init; } = index;
    /// <summary>Gets the length of the token.</summary>
    public int Length { get; init; } = key.Length;
    /// <summary>Gets the token key (content).</summary>
    public TokenKey Key { get; init; } = key;
    /// <summary>Gets the token type.</summary>
    public TokenType Type { get; init; } = type;
    /// <summary>Gets the operator kind if applicable.</summary>
    public OperatorKind OperatorKind { get; init; } = operatorKind;
    /// <summary>Gets a value indicating whether the numeric token is negated.</summary>
    public bool Negation { get; init; } = negation;

    /// <summary>Gets function info if this is a function token.</summary>
    public FunctionInfo? FunctionInfo { get; init; } = functionInfo;
    /// <summary>Gets ternary branch info if this is a ternary operator.</summary>
    public TernaryBranches? TernaryBranches { get; init; } = ternaryBranches;

    /// <summary>Gets the key as a span.</summary>
    public ReadOnlySpan<char> KeySpan => Key.AsSpan();
    /// <summary>Gets the key as a string.</summary>
    public string KeyString => Key.ToString();
}


/// <summary>
/// Contains state information for parsing function arguments.
/// </summary>
public class FunctionInfo {
    /// <summary>Gets or sets the nesting depth.</summary>
    public int Depth { get; set; }
    /// <summary>Gets or sets the argument count parsed so far.</summary>
    public int ArgumentsCount { get; set; }
    /// <summary>Gets or sets a value indicating whether the parser should expect another argument.</summary>
    public bool AwaitNextArgument { get; set; }
}

/// <summary>
/// Contains the true/false expression branches for a ternary operator.
/// </summary>
public class TernaryBranches {
    /// <summary>Tokens for the true branch.</summary>
    public IEnumerable<Token> TrueBranch { get; set; } = [];
    /// <summary>Tokens for the false branch.</summary>
    public IEnumerable<Token> FalseBranch { get; set; } = [];
}

// Removing IToken interface as it forces boxing and isn't widely used in the optimized path.
// If needed, we can re-introduce it but Token struct shouldn't implement it implicitly if performance is key.

/// <summary>
/// Enumerates valid token types.
/// </summary>
public enum TokenType {
    /// <summary>Whitespace characters.</summary>
    WhiteSpace,
    /// <summary>Numeric literal.</summary>
    Number,
    /// <summary>Operator symbol.</summary>
    Operator,
    /// <summary>Prefix unary operator (e.g. -x, !x).</summary>
    UnaryPrefixOperator,
    /// <summary>Postfix unary operator (e.g. x!).</summary>
    UnaryPostfixOperator,
    /// <summary>Opening parenthesis '('. </summary>
    OpeningParenthesis,
    /// <summary>Closing parenthesis ')'.</summary>
    ClosingParenthesis,
    /// <summary>Named constant (e.g. pi).</summary>
    Constant,
    /// <summary>Variable name.</summary>
    Variable,
    /// <summary>String literal.</summary>
    String,
    /// <summary>Function call.</summary>
    Function,
    /// <summary>Comma separator.</summary>
    Comma,
    /// <summary>Ternary operator part.</summary>
    Ternary
}

/// <summary>
/// Enumerates specific operator kinds for optimized switching.
/// </summary>
public enum OperatorKind : byte {
    /// <summary>No specific operator.</summary>
    None = 0,
    // Arithmetic
    /// <summary>Addition (+).</summary>
    Add,            // + 
    /// <summary>Subtraction (-).</summary>
    Sub,            // - (also −)
    /// <summary>Multiplication (*).</summary>
    Mul,            // * (also ×)
    /// <summary>Division (/).</summary>
    Div,            // / (also ÷)
    /// <summary>Modulo (%).</summary>
    Mod,            // %
    /// <summary>Power (^).</summary>
    Pow,            // ^
    // Comparison
    /// <summary>Equality (==).</summary>
    Eq,             // ==
    /// <summary>Inequality (!=).</summary>
    NotEq,          // !=
    /// <summary>Less than (&lt;).</summary>
    Lt,             // <
    /// <summary>Less than or equal (&lt;=).</summary>
    LtEq,           // <=
    /// <summary>Greater than (&gt;).</summary>
    Gt,             // >
    /// <summary>Greater than or equal (&gt;=).</summary>
    GtEq,           // >=
    // Logical
    /// <summary>Logical AND (&amp;&amp;).</summary>
    And,            // &&
    /// <summary>Logical OR (||).</summary>
    Or,             // ||
    /// <summary>Logical NOT (!).</summary>
    Not,            // ! (prefix)
    // Bitwise
    /// <summary>Bitwise AND (&amp;).</summary>
    BitwiseAnd,     // &
    /// <summary>Bitwise OR (|).</summary>
    BitwiseOr,      // |
    /// <summary>Bitwise XOR (^).</summary>
    BitwiseXor,     // ⊕
    /// <summary>Bitwise NOT (~).</summary>
    BitwiseNot,     // ~
    // Unary
    /// <summary>Factorial (!).</summary>
    Factorial,      // ! (postfix)
    /// <summary>Square root (√).</summary>
    SquareRoot,     // √
    // Ternary
    /// <summary>Ternary condition (?).</summary>
    TernaryCondition, // ?
    /// <summary>Ternary branch (:).</summary>
    TernaryBranch,    // :
}
