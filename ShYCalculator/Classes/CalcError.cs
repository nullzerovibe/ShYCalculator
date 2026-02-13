// -----------------------------------------------------------------------------
// <summary>
//     Detailed error reporting structure used in CalculationResult.
//     Provides granular information about parsing or evaluation failures.
// </summary>
// -----------------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace ShYCalculator.Classes;

/// <summary>
/// Specific error codes identifying the type of failure.
/// </summary>
public enum ErrorCode {
    // General
    /// <summary>Unknown error.</summary>
    Unknown = 0,
    /// <summary>An unexpected exception occurred.</summary>
    UnexpectedException = 1,

    // Tokenizer (100-199)
    /// <summary>Expression is invalid or empty.</summary>
    InvalidExpression = 100,
    /// <summary>Unknown token encountered.</summary>
    UnknownToken = 101,
    /// <summary>String literal is not terminated.</summary>
    UnterminatedString = 102,
    /// <summary>Number format is invalid.</summary>
    InvalidNumberFormat = 103,

    // Generator (200-299)
    /// <summary>Parentheses are mismatched.</summary>
    MismatchedParentheses = 200,
    /// <summary>Syntax is invalid.</summary>
    InvalidSyntax = 201,
    /// <summary>Missing operand for operator.</summary>
    MissingOperand = 202,

    // Parser/Runtime (300-399)
    /// <summary>Division by zero attempted.</summary>
    DivisionByZero = 300,
    /// <summary>Invalid argument passed to function.</summary>
    InvalidFunctionArgument = 301,
    /// <summary>Variable not found in context.</summary>
    VariableNotFound = 302,
    /// <summary>Function not found.</summary>
    FunctionNotFound = 303,
    /// <summary>Type mismatch for operation.</summary>
    TypeMismatch = 304,
    /// <summary>Operation not supported.</summary>
    OperationNotSupported = 305,
    /// <summary>Error in ternary branch execution.</summary>
    TernaryBranchError = 306,
}

/// <summary>
/// Severity level of the error.
/// </summary>
public enum ErrorSeverity {
    /// <summary>Critical error prevents calculation.</summary>
    Error,
    /// <summary>Warning does not prevent calculation but indicates issue.</summary>
    Warning,
    /// <summary>Informational message.</summary>
    Info
}

/// <summary>
/// Represents a specific error or warning during calculation.
/// </summary>
public class CalcError {
    /// <summary>Gets or sets the error code.</summary>
    public ErrorCode Code { get; set; }
    /// <summary>Gets or sets the descriptive message.</summary>
    public string Message { get; set; } = "";

    // Position info
    /// <summary>Gets or sets the start index in the expression.</summary>
    public int StartIndex { get; set; } = -1;
    /// <summary>Gets or sets the length of the error segment.</summary>
    public int Length { get; set; } = 0;

    /// <summary>Gets or sets the severity.</summary>
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

    /// <summary>Initializes a new instance of the <see cref="CalcError"/> class.</summary>
    public CalcError() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalcError"/> class with details.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <param name="startIndex">Start index.</param>
    /// <param name="length">Length.</param>
    /// <param name="severity">Severity.</param>
    public CalcError(ErrorCode code, string message, int startIndex = -1, int length = 0, ErrorSeverity severity = ErrorSeverity.Error) {
        Code = code;
        Message = message;
        StartIndex = startIndex;
        Length = length;
        Severity = severity;
    }

    /// <summary>Returns a string representation of the error.</summary>
    public override string ToString() {
        return $"[{Code}] {Message} (at {StartIndex}, len {Length})";
    }
}
