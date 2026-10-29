// -----------------------------------------------------------------------------
// <summary>
//     Central registry of supported operators, their precedence, and associativity.
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;
using System.Collections.Frozen;

namespace ShY.Calculator;

/// <summary>
/// Single source of truth for all operator definitions.
/// Provides derived FrozenDictionary lookups for O(1) runtime performance.
/// </summary>
internal static class OperatorRegistry {
    // ═══════════════════════════════════════════════════════════════════════════
    // SINGLE SOURCE OF TRUTH - Add new operators here
    // ═══════════════════════════════════════════════════════════════════════════
    private static readonly OperatorDef[] AllOperators = [
        // WHY: precedence mirrors C#: || < && < | < xor < & < equality < relational < shift < additive < multiplicative < power.
        // Logical
        new("||", "Logical OR",       OperatorKind.Or,             1, Associativity.Left,  Category.Logical,    DataType.Boolean),
        new("&&", "Logical AND",      OperatorKind.And,            2, Associativity.Left,  Category.Logical,    DataType.Boolean),

        // Bitwise
        new("|",  "Bitwise OR",       OperatorKind.BitwiseOr,      3, Associativity.Left,  Category.Bitwise,    DataType.Number),
        new("⊕",  "Bitwise XOR",      OperatorKind.BitwiseXor,     4, Associativity.Left,  Category.Bitwise,    DataType.Number),
        new("^^", "Bitwise XOR",      OperatorKind.BitwiseXor,     4, Associativity.Left,  Category.Bitwise,    DataType.Number),  // ASCII XOR alias
        new("&",  "Bitwise AND",      OperatorKind.BitwiseAnd,     5, Associativity.Left,  Category.Bitwise,    DataType.Number),

        // Equality
        new("==", "Equal To",         OperatorKind.Eq,             6, Associativity.Left,  Category.Comparison, DataType.Boolean | DataType.Number | DataType.String | DataType.Date),
        new("!=", "Not Equal",        OperatorKind.NotEq,          6, Associativity.Left,  Category.Comparison, DataType.Boolean | DataType.Number | DataType.String | DataType.Date),

        // Relational
        new("<",  "Less Than",        OperatorKind.Lt,             7, Associativity.Left,  Category.Comparison, DataType.Number | DataType.Date),
        new(">",  "Greater Than",     OperatorKind.Gt,             7, Associativity.Left,  Category.Comparison, DataType.Number | DataType.Date),
        new("<=", "Less or Equal",    OperatorKind.LtEq,           7, Associativity.Left,  Category.Comparison, DataType.Number | DataType.Date),
        new(">=", "Greater or Equal", OperatorKind.GtEq,           7, Associativity.Left,  Category.Comparison, DataType.Number | DataType.Date),

        // Bitwise Shift
        new("<<", "Bitwise Left Shift", OperatorKind.BitwiseLeftShift, 8, Associativity.Left, Category.Bitwise, DataType.Number),
        new(">>", "Bitwise Right Shift", OperatorKind.BitwiseRightShift, 8, Associativity.Left, Category.Bitwise, DataType.Number),

        // Additive
        new("+",  "Add",              OperatorKind.Add,            9, Associativity.Left,  Category.Arithmetic, DataType.Number | DataType.String),
        new("-",  "Subtract",         OperatorKind.Sub,            9, Associativity.Left,  Category.Arithmetic, DataType.Number),
        new("−",  "Subtract",         OperatorKind.Sub,            9, Associativity.Left,  Category.Arithmetic, DataType.Number),  // U+2212 MINUS SIGN

        // Multiplicative
        new("*",  "Multiply",         OperatorKind.Mul,           10, Associativity.Left,  Category.Arithmetic, DataType.Number),
        new("×",  "Multiply",         OperatorKind.Mul,           10, Associativity.Left,  Category.Arithmetic, DataType.Number),  // U+00D7 MULTIPLICATION SIGN
        new("/",  "Divide",           OperatorKind.Div,           10, Associativity.Left,  Category.Arithmetic, DataType.Number),
        new("÷",  "Divide",           OperatorKind.Div,           10, Associativity.Left,  Category.Arithmetic, DataType.Number),  // U+00F7 DIVISION SIGN
        new("%",  "Modulo",           OperatorKind.Mod,           10, Associativity.Left,  Category.Arithmetic, DataType.Number),

        // Power (right associative)
        new("^",  "Power",            OperatorKind.Pow,           11, Associativity.Right, Category.Arithmetic, DataType.Number),

        // Grouping
        new("(",  "Group Start",      OperatorKind.None,          13, Associativity.Left,  Category.Grouping,   DataType.Boolean | DataType.Number | DataType.Date | DataType.String),
        new(")",  "Group End",        OperatorKind.None,          13, Associativity.Left,  Category.Grouping,   DataType.Boolean | DataType.Number | DataType.Date | DataType.String),

        // Ternary
        new("?",  "Ternary If",       OperatorKind.TernaryCondition, 0, Associativity.Right, Category.Logical, DataType.Boolean | DataType.Number | DataType.Date | DataType.String),
        new(":",  "Ternary Else",     OperatorKind.TernaryBranch,    0, Associativity.Right, Category.Logical, DataType.Boolean | DataType.Number | DataType.Date | DataType.String),
        
        // Unary prefix operators
        new("~",  "Bitwise NOT",      OperatorKind.BitwiseNot,    12, Associativity.Right, Category.Unary,      DataType.Number,  IsUnaryPrefix: true),
        new("√",  "Square Root",      OperatorKind.SquareRoot,    12, Associativity.Right, Category.Unary,      DataType.Number,  IsUnaryPrefix: true),
        new("!",  "NOT / Factorial",  OperatorKind.Not,           12, Associativity.Right, Category.Unary,      DataType.Boolean, IsUnaryPrefix: true),  // Also postfix factorial
        new("∑",  "Summation",        OperatorKind.None,          12, Associativity.Right, Category.Unary,      DataType.Number,  IsUnaryPrefix: true),  // Summation (function-like)
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
                    Name = o.Name,
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
        string Name,
        OperatorKind Kind,
        int Precedence,
        Associativity Associativity,
        Category Category,
        DataType ValidDataTypes,
        bool IsUnaryPrefix = false,
        bool IsUnaryPostfix = false
    );
}
