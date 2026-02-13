// -----------------------------------------------------------------------------
// <summary>
//     Central registry of supported operators, their precedence, and associativity.
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;
using System.Collections.Frozen;

namespace ShYCalculator.Calculator;

/// <summary>
/// Single source of truth for all operator definitions.
/// Provides derived FrozenDictionary lookups for O(1) runtime performance.
/// </summary>
internal static class OperatorRegistry {
    // ═══════════════════════════════════════════════════════════════════════════
    // SINGLE SOURCE OF TRUTH - Add new operators here
    // ═══════════════════════════════════════════════════════════════════════════
    private static readonly OperatorDef[] AllOperators = [
        // Logical (precedence 1)
        new("&&", OperatorKind.And,            1, Associativity.Left,  Category.Logical,    DataType.Boolean),
        new("||", OperatorKind.Or,             1, Associativity.Left,  Category.Logical,    DataType.Boolean),
        
        // Bitwise (precedence 2-4)
        new("|",  OperatorKind.BitwiseOr,      2, Associativity.Left,  Category.Bitwise,    DataType.Number),
        new("⊕",  OperatorKind.BitwiseXor,     3, Associativity.Left,  Category.Bitwise,    DataType.Number),
        new("^^", OperatorKind.BitwiseXor,     3, Associativity.Left,  Category.Bitwise,    DataType.Number),  // ASCII XOR alias
        new("&",  OperatorKind.BitwiseAnd,     4, Associativity.Left,  Category.Bitwise,    DataType.Number),
        
        // Comparison (precedence 5)
        new("<",  OperatorKind.Lt,             5, Associativity.Left,  Category.Comparison, DataType.Number | DataType.Date),
        new(">",  OperatorKind.Gt,             5, Associativity.Left,  Category.Comparison, DataType.Number | DataType.Date),
        new("==", OperatorKind.Eq,             5, Associativity.Left,  Category.Comparison, DataType.Boolean | DataType.Number | DataType.String | DataType.Date),
        new("!=", OperatorKind.NotEq,          5, Associativity.Left,  Category.Comparison, DataType.Boolean | DataType.Number | DataType.String | DataType.Date),
        new("<=", OperatorKind.LtEq,           5, Associativity.Left,  Category.Comparison, DataType.Boolean | DataType.Number | DataType.Date),
        new(">=", OperatorKind.GtEq,           5, Associativity.Left,  Category.Comparison, DataType.Boolean | DataType.Number | DataType.Date),
        
        // Arithmetic - Addition/Subtraction (precedence 6)
        new("+",  OperatorKind.Add,            6, Associativity.Left,  Category.Arithmetic, DataType.Number | DataType.String),
        new("-",  OperatorKind.Sub,            6, Associativity.Left,  Category.Arithmetic, DataType.Number),
        new("−",  OperatorKind.Sub,            6, Associativity.Left,  Category.Arithmetic, DataType.Number),  // U+2212 MINUS SIGN
        
        // Arithmetic - Modulo (precedence 7)
        new("%",  OperatorKind.Mod,            7, Associativity.Left,  Category.Arithmetic, DataType.Number),
        
        // Arithmetic - Multiplication/Division (precedence 8)
        new("*",  OperatorKind.Mul,            8, Associativity.Left,  Category.Arithmetic, DataType.Number),
        new("×",  OperatorKind.Mul,            8, Associativity.Left,  Category.Arithmetic, DataType.Number),  // U+00D7 MULTIPLICATION SIGN
        new("/",  OperatorKind.Div,            8, Associativity.Left,  Category.Arithmetic, DataType.Number),
        new("÷",  OperatorKind.Div,            8, Associativity.Left,  Category.Arithmetic, DataType.Number),  // U+00F7 DIVISION SIGN
        
        // Arithmetic - Power (precedence 9, right associative)
        new("^",  OperatorKind.Pow,            9, Associativity.Right, Category.Arithmetic, DataType.Number),
        
        // Grouping (precedence 10)
        new("(",  OperatorKind.None,          10, Associativity.Left,  Category.Grouping,   DataType.Boolean | DataType.Number | DataType.Date | DataType.String),
        new(")",  OperatorKind.None,          10, Associativity.Left,  Category.Grouping,   DataType.Boolean | DataType.Number | DataType.Date | DataType.String),
        
        // Ternary
        new("?",  OperatorKind.TernaryCondition, 0, Associativity.Right, Category.Logical, DataType.Boolean | DataType.Number | DataType.Date | DataType.String),
        new(":",  OperatorKind.TernaryBranch,    0, Associativity.Right, Category.Logical, DataType.Boolean | DataType.Number | DataType.Date | DataType.String),
        
        // Unary prefix operators
        new("~",  OperatorKind.BitwiseNot,     9, Associativity.Right, Category.Unary,      DataType.Number,  IsUnaryPrefix: true),
        new("√",  OperatorKind.SquareRoot,     9, Associativity.Right, Category.Unary,      DataType.Number,  IsUnaryPrefix: true),
        new("!",  OperatorKind.Not,            9, Associativity.Right, Category.Unary,      DataType.Boolean, IsUnaryPrefix: true),  // Also postfix factorial
        new("∑",  OperatorKind.None,           9, Associativity.Right, Category.Unary,      DataType.Number,  IsUnaryPrefix: true),  // Summation (function-like)
    ];

    // ═══════════════════════════════════════════════════════════════════════════
    // DERIVED LOOKUPS - Built once at startup from AllOperators
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Operator key → Operator struct (precedence, associativity, etc.)</summary>
    public static readonly FrozenDictionary<string, Operator> Operators;

    /// <summary>Single character → OperatorKind for tokenization</summary>
    public static readonly FrozenDictionary<char, OperatorKind> SingleCharKinds;

    /// <summary>Two character tuple → OperatorKind for tokenization</summary>
    public static readonly FrozenDictionary<(char, char), OperatorKind> TwoCharKinds;

    /// <summary>Set of valid first characters for operators</summary>
    public static readonly FrozenSet<char> ValidFirstChars;

    /// <summary>Set of valid two-char operator first characters (for lookahead)</summary>
    public static readonly FrozenSet<char> TwoCharFirstChars;

    static OperatorRegistry() {
        // Build Operators dictionary
        Operators = AllOperators
            .DistinctBy(o => o.Key)  // Take first definition for duplicates (like !)
            .ToFrozenDictionary(
                o => o.Key,
                o => new Operator {
                    Precedence = o.Precedence,
                    Associativity = o.Associativity,
                    Category = o.Category,
                    ValidDataTypes = o.ValidDataTypes
                },
                StringComparer.Ordinal
            );

        // Build SingleCharKinds
        SingleCharKinds = AllOperators
            .Where(o => o.Key.Length == 1)
            .ToFrozenDictionary(o => o.Key[0], o => o.Kind);

        // Build TwoCharKinds
        TwoCharKinds = AllOperators
            .Where(o => o.Key.Length == 2)
            .ToFrozenDictionary(o => (o.Key[0], o.Key[1]), o => o.Kind);

        // Build ValidFirstChars (any char that can start an operator)
        ValidFirstChars = AllOperators
            .Select(o => o.Key[0])
            .ToFrozenSet();

        // Build TwoCharFirstChars (first char of two-char operators)
        TwoCharFirstChars = AllOperators
            .Where(o => o.Key.Length == 2)
            .Select(o => o.Key[0])
            .ToFrozenSet();
    }

    /// <summary>Check if a character can form a two-char operator with the next char</summary>
    public static bool CanFormTwoCharOperator(char first, char? second) {
        if (second == null) return false;
        return TwoCharKinds.ContainsKey((first, second.Value));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // INTERNAL DEFINITION RECORD
    // ═══════════════════════════════════════════════════════════════════════════
    internal readonly record struct OperatorDef(
        string Key,
        OperatorKind Kind,
        int Precedence,
        Associativity Associativity,
        Category Category,
        DataType ValidDataTypes,
        bool IsUnaryPrefix = false,
        bool IsUnaryPostfix = false
    );
}
