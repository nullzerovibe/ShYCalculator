// -----------------------------------------------------------------------------
// <summary>
//     Defines result types (Result<T>, CalculationResult) and the universal Value struct.
//     Centralizes data passing to ensure zero-allocation where possible.
// </summary>
// -----------------------------------------------------------------------------
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ShY.Classes;

/// <summary>
/// Represents the base result of an operation, indicating success or failure.
/// </summary>
public class Result {
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string Message { get; set; } = "";
    /// <summary>
    /// Gets or sets the collection of detailed errors.
    /// </summary>
    public IReadOnlyList<CalcError> Errors { get; set; } = [];

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="errors">Optional list of detailed errors.</param>
    /// <returns>A failed result.</returns>
    public static Result Fail(string message, IEnumerable<CalcError>? errors = null) => new() { Success = false, Message = message, Errors = errors?.ToArray() ?? [] };
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Ok() => new() { Success = true };
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T> : Result {
    /// <summary>
    /// Gets or sets the value returned by the operation.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="errors">Optional list of detailed errors.</param>
    /// <returns>A failed result.</returns>
    public new static Result<T> Fail(string message, IEnumerable<CalcError>? errors = null) => new() { Success = false, Message = message, Errors = errors?.ToArray() ?? [] };
    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The returned value.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Ok(T value) => new() { Success = true, Value = value };
}

// Specific result for the Calculator public API to maintain backward compatibility in structure (mostly)
/// <summary>
/// Specific result for the Calculator public API to maintain backward compatibility.
/// </summary>
public class CalculationResult : Result<Value> {
    /// <summary>
    /// Gets or sets the original expression string.
    /// </summary>
    public string Expression { get; set; } = "";

    internal IEnumerable<Token>? InternalExpressionTokens { get; set; }
    internal IEnumerable<Token>? InternalRPNTokens { get; set; }

    private IEnumerable<string>? _expressionTokens;
    /// <summary>
    /// Gets or sets the list of token strings parsed from the expression.
    /// </summary>
    public IEnumerable<string> ExpressionTokens {
        get => _expressionTokens ??= InternalExpressionTokens?.Select(x => x.KeyString).ToList() ?? Enumerable.Empty<string>();
        set => _expressionTokens = value;
    }

    private IEnumerable<string>? _rpnTokens;
    /// <summary>
    /// Gets or sets the list of RPN token strings generated from the expression.
    /// </summary>
    public IEnumerable<string> RPNTokens {
        get => _rpnTokens ??= InternalRPNTokens?.Select(x => x.KeyString).ToList() ?? Enumerable.Empty<string>();
        set => _rpnTokens = value;
    }

    /// <summary>
    /// Gets or sets the generated Abstract Syntax Tree (AST), if requested.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AstNode? Ast { get; set; }

    // Backward compatibility properties
    /// <summary>
    /// Gets the data type of the result value.
    /// </summary>
    public DataType DataType => Value.DataType == 0 ? DataType.Number : Value.DataType;

    /// <summary>
    /// Gets the numeric value if available.
    /// </summary>
    public double? Nvalue => Value.Nvalue;
    /// <summary>
    /// Gets the numeric value as an exact decimal if available (see <see cref="NumericMode.Decimal"/>).
    /// </summary>
    public decimal? Mvalue => Value.Mvalue;
    /// <summary>
    /// Gets the boolean value if available.
    /// </summary>
    public bool? Bvalue => Value.Bvalue;
    /// <summary>
    /// Gets the string value if available.
    /// </summary>
    public string? Svalue => Value.Svalue;
    /// <summary>
    /// Gets the date value if available.
    /// </summary>
    public DateTimeOffset? Dvalue => Value.Dvalue;
}

// WHY: one 16-byte slot shared by every primitive; presence, decimal-ness and the date offset live outside it
// so Value stays 32 bytes even though decimal fills the whole slot.
[StructLayout(LayoutKind.Explicit)]
internal struct PrimitiveUnion {
    [FieldOffset(0)] public double DoubleVal;
    [FieldOffset(0)] public long DateTimeTicks;
    [FieldOffset(0)] public bool BoolVal;
    [FieldOffset(0)] public decimal DecimalVal;
}

/// <summary>
/// A universal value struct used throughout the calculator pipeline regardless of type.
/// Uses explicit layout to minimize size and allocations (similar to a union).
/// </summary>
public readonly struct Value {
    private const byte HasValueFlag = 1;
    private const byte DecimalFlag = 2;

    // WARN: decimal.MaxValue as a double rounds up past the real maximum; anything at or above it cannot convert.
    private const double DecimalMaxAsDouble = 7.9228162514264337593543950335E+28;

    /// <summary>
    /// Initializes a new instance of the <see cref="Value"/> struct.
    /// </summary>
    /// <param name="dataType">The data type.</param>
    /// <param name="nValue">Optional numeric value.</param>
    /// <param name="bValue">Optional boolean value.</param>
    /// <param name="sValue">Optional string value.</param>
    /// <param name="dValue">Optional date value.</param>
    public Value(DataType dataType, double? nValue = null, bool? bValue = null, string? sValue = null, DateTimeOffset? dValue = null) {
        DataType = dataType;
        _union = default;
        _stringVal = null;
        _flags = 0;
        _offsetMinutes = 0;

        if (dataType.HasFlag(DataType.Number) && nValue.HasValue) {
            _union = new PrimitiveUnion { DoubleVal = nValue.Value };
            _flags = HasValueFlag;
        }
        else if (dataType.HasFlag(DataType.Boolean) && bValue.HasValue) {
            _union = new PrimitiveUnion { BoolVal = bValue.Value };
            _flags = HasValueFlag;
        }
        else if (dataType.HasFlag(DataType.Date) && dValue.HasValue) {
            _union = new PrimitiveUnion { DateTimeTicks = dValue.Value.Ticks };
            _offsetMinutes = (short)dValue.Value.Offset.TotalMinutes;
            _flags = HasValueFlag;
        }
        else if (dataType.HasFlag(DataType.String)) {
            _stringVal = sValue;
        }
    }

    private Value(decimal value) {
        DataType = DataType.Number;
        _union = new PrimitiveUnion { DecimalVal = value };
        _stringVal = null;
        _flags = HasValueFlag | DecimalFlag;
        _offsetMinutes = 0;
    }

    /// <summary>
    /// Gets the data type of the value.
    /// </summary>
    public DataType DataType { get; init; }

    private readonly PrimitiveUnion _union;
    private readonly string? _stringVal;
    private readonly byte _flags;
    private readonly short _offsetMinutes;

    private bool HasValue => (_flags & HasValueFlag) != 0;

    /// <summary>
    /// Gets a value indicating whether this number is stored as an exact <see cref="decimal"/>.
    /// </summary>
    public bool IsDecimal => (_flags & DecimalFlag) != 0;

    /// <summary>
    /// Gets the numeric value as a double. Returns null if not a number. Decimal numbers are converted.
    /// </summary>
    public double? Nvalue {
        get {
            if (!DataType.HasFlag(DataType.Number) || !HasValue) return null;
            return IsDecimal ? (double)_union.DecimalVal : _union.DoubleVal;
        }
        init {
            if (value.HasValue) {
                ThrowIfConflict(DataType.Number);
                _union = new PrimitiveUnion { DoubleVal = value.Value };
                _flags = HasValueFlag;
                if (DataType == 0) DataType = DataType.Number;
            }
        }
    }

    /// <summary>
    /// Gets the numeric value as an exact decimal (the C# <c>m</c> suffix). Returns null if not a number,
    /// or if a double value is not finite or outside the decimal range. Double numbers are converted.
    /// </summary>
    public decimal? Mvalue {
        get {
            if (!DataType.HasFlag(DataType.Number) || !HasValue) return null;
            if (IsDecimal) return _union.DecimalVal;
            return TryToDecimal(_union.DoubleVal, out var converted) ? converted : null;
        }
        init {
            if (value.HasValue) {
                ThrowIfConflict(DataType.Number);
                _union = new PrimitiveUnion { DecimalVal = value.Value };
                _flags = HasValueFlag | DecimalFlag;
                if (DataType == 0) DataType = DataType.Number;
            }
        }
    }

    /// <summary>
    /// Gets the boolean value. Returns null if not a boolean.
    /// </summary>
    public bool? Bvalue {
        get => (DataType.HasFlag(DataType.Boolean) && HasValue) ? _union.BoolVal : null;
        init {
            if (value.HasValue) {
                ThrowIfConflict(DataType.Boolean);
                _union = new PrimitiveUnion { BoolVal = value.Value };
                _flags = HasValueFlag;
                if (DataType == 0) DataType = DataType.Boolean;
            }
        }
    }

    /// <summary>
    /// Gets the string value. Returns null if not a string.
    /// </summary>
    public string? Svalue {
        get => DataType.HasFlag(DataType.String) ? _stringVal : null;
        init {
            ThrowIfConflict(DataType.String);
            _stringVal = value;
            if (DataType == 0 && value != null) DataType = DataType.String;
        }
    }

    /// <summary>
    /// Gets the date value. Returns null if not a date.
    /// </summary>
    public DateTimeOffset? Dvalue {
        get {
            if (DataType.HasFlag(DataType.Date) && HasValue) {
                return new DateTimeOffset(_union.DateTimeTicks, TimeSpan.FromMinutes(_offsetMinutes));
            }
            return null;
        }
        init {
            if (value.HasValue) {
                ThrowIfConflict(DataType.Date);
                _union = new PrimitiveUnion { DateTimeTicks = value.Value.Ticks };
                _offsetMinutes = (short)value.Value.Offset.TotalMinutes;
                _flags = HasValueFlag;
                if (DataType == 0) DataType = DataType.Date;
            }
        }
    }

    private void ThrowIfConflict(DataType targetType) {
        if (DataType != 0 && !DataType.HasFlag(targetType)) {
            throw new InvalidOperationException($"DataType conflict: Value is already defined as {DataType}, cannot set to {targetType}.");
        }
        if (HasValue && !DataType.HasFlag(targetType)) {
            throw new InvalidOperationException($"Memory overwrite conflict: Value already has primitive data, cannot set to {targetType}.");
        }
    }

    internal static bool TryToDecimal(double number, out decimal result) {
        if (double.IsFinite(number) && Math.Abs(number) < DecimalMaxAsDouble) {
            try {
                result = (decimal)number;
                return true;
            }
            catch (OverflowException) {
            }
        }
        result = 0;
        return false;
    }

    // Static helpers for cleaner creation
    /// <summary>Creates a numeric value.</summary>
    public static Value Number(double val) => new(DataType.Number, nValue: val);
    /// <summary>Creates an exact decimal numeric value.</summary>
    public static Value Number(decimal val) => new(val);
    // WHY: int/long convert implicitly to both double and decimal; these overloads keep Number(5) unambiguous and double-backed.
    /// <summary>Creates a numeric value from an integer.</summary>
    public static Value Number(int val) => new(DataType.Number, nValue: val);
    /// <summary>Creates a numeric value from a 64-bit integer.</summary>
    public static Value Number(long val) => new(DataType.Number, nValue: val);
    /// <summary>Creates a boolean value.</summary>
    public static Value Boolean(bool val) => new(DataType.Boolean, bValue: val);
    /// <summary>Creates a string value.</summary>
    public static Value String(string val) => new(DataType.String, sValue: val);
    /// <summary>Creates a date value.</summary>
    public static Value Date(DateTimeOffset val) => new(DataType.Date, dValue: val);
    /// <summary>Creates a null value of a specific type.</summary>
    public static Value Null(DataType type) => new(type);
}

/// <summary>
/// Defines the data types supported by the calculator.
/// </summary>
[Flags]
public enum DataType {
    /// <summary>Numeric type (double).</summary>
    Number = 1,
    /// <summary>Boolean type.</summary>
    Boolean = 2,
    /// <summary>Date type (DateTimeOffset).</summary>
    Date = 4,
    /// <summary>String type.</summary>
    String = 8,
}

// Optimized struct result for internal high-performance paths
/// <summary>
/// Optimized struct result for internal high-performance paths.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
internal readonly struct OpResult<T>(bool success, T value, string message, IEnumerable<CalcError>? errors = null) {
    /// <summary>Gets a value indicating success.</summary>
    public bool Success { get; } = success;
    /// <summary>Gets the error message.</summary>
    public string Message { get; } = message ?? "";
    /// <summary>Gets the returned value.</summary>
    public T Value { get; } = value;
    /// <summary>Gets the collection of errors.</summary>
    public IEnumerable<CalcError> Errors { get; } = errors ?? [];

    /// <summary>Creates a successful result.</summary>
    public static OpResult<T> Ok(T value) => new(true, value, "");
    /// <summary>Creates a failed result.</summary>
    public static OpResult<T> Fail(string message, IEnumerable<CalcError>? errors = null) => new(false, default!, message, errors);
    /// <summary>Creates a failed result from a single error.</summary>
    public static OpResult<T> Fail(CalcError error) => new(false, default!, error.Message, [error]);
}
