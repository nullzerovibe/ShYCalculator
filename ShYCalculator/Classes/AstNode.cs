using System.Text.Json.Serialization;

namespace ShYCalculator.Classes;

/// <summary>
/// Represents a range in the source string.
/// </summary>
public struct RangeInfo {
    /// <summary>Gets or sets the start index.</summary>
    [JsonPropertyName("start")]
    public int Start { get; set; }
    /// <summary>Gets or sets the end index.</summary>
    [JsonPropertyName("end")]
    public int End { get; set; }
}

/// <summary>
/// Represents a node in the Abstract Syntax Tree (AST).
/// </summary>
public class AstNode {
    /// <summary>Gets or sets the type of the node (e.g., "binary", "number").</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    /// <summary>Gets or sets the range of this node in the original expression.</summary>
    [JsonPropertyName("range")]
    public RangeInfo Range { get; set; }

    /// <summary>Gets or sets the evaluated value of this node.</summary>
    [JsonPropertyName("evaluated_value")]
    public object? EvaluatedValue { get; set; }

    // Number / String
    /// <summary>Gets or sets the raw value for number/string nodes.</summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; set; }

    // Operator / Unary
    /// <summary>Gets or sets the operator symbol for operator nodes.</summary>
    [JsonPropertyName("operator")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Operator { get; set; }

    // Function / Variable
    /// <summary>Gets or sets the name for function/variable/constant nodes.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    // Binary / Unary
    /// <summary>Gets or sets the left child node.</summary>
    [JsonPropertyName("left")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AstNode? Left { get; set; }

    /// <summary>Gets or sets the right child node.</summary>
    [JsonPropertyName("right")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AstNode? Right { get; set; }

    // Function
    /// <summary>Gets or sets the list of arguments for function nodes.</summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AstNode>? Arguments { get; set; }

    // Ternary
    /// <summary>Gets or sets the condition node for ternary operators.</summary>
    [JsonPropertyName("condition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AstNode? Condition { get; set; }

    /// <summary>Gets or sets the true branch node for ternary operators.</summary>
    [JsonPropertyName("true_branch")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AstNode? TrueBranch { get; set; }

    /// <summary>Gets or sets the false branch node for ternary operators.</summary>
    [JsonPropertyName("false_branch")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AstNode? FalseBranch { get; set; }
}
