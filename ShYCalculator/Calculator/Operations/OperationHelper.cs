// -----------------------------------------------------------------------------
// <summary>
//     Helper methods for performing numeric checks and type validations during operations.
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;

namespace ShY.Calculator.Operations;

internal static class OperationHelper {
    internal static string GetDataTypeMismatchErrorMessage(in Value leftOperand, in Value rightOperand, Token token, DataType? expectedDataType = null) {
        var message = $"Unexpected operand data type on token '{token.Key}' of type {token.Type} at position {token.Index}.";

        if (expectedDataType != null) message += $" Expected {expectedDataType} data type.";

        if (expectedDataType == null || leftOperand.DataType != expectedDataType) {
            message += $" Left operand data type {leftOperand.DataType} mismatch.";
        }

        if (expectedDataType == null || rightOperand.DataType != expectedDataType) {
            // Fixed previous bug where it reported leftOperand.DataType instead of rightOperand.DataType
            message += $" Right operand data type {rightOperand.DataType} mismatch.";
        }

        return message;
    }

    internal static string GetNullValueErrorMessage(bool leftOperandNull, bool rightOperandNull, Token token, DataType expectedDataType) {
        var message = $"Unexpected operand value on token '{token.Key}' of type {token.Type} at position {token.Index}. Expected {expectedDataType} value.";

        if (leftOperandNull) {
            message += $" Left operand value is NULL.";
        }

        if (rightOperandNull) {
            message += $" Right operand value is NULL.";
        }

        return message;
    }

    internal static bool IsInteger(double value, double tolerance = 1e-10) {
        return Math.Abs(value - Math.Round(value)) < tolerance;
    }

    // WHY: 2^63 is exactly representable as a double, long.MaxValue is not; comparing against it avoids a saturating cast.
    private const double Int64UpperBoundExclusive = 9223372036854775808d;

    internal static bool TryGetInt64(in Value value, out long result) {
        if (value.IsDecimal) return DecimalMath.TryGetInt64(value.Mvalue!.Value, out result);
        if (value.Nvalue is { } number) return TryGetInt64(number, out result);
        result = 0;
        return false;
    }

    internal static bool TryGetInt64(double value, out long result) {
        if (IsInteger(value) && value >= long.MinValue && value < Int64UpperBoundExclusive) {
            result = (long)Math.Round(value);
            return true;
        }
        result = 0;
        return false;
    }

    internal static CalcError Error(ErrorCode code, string message, Token token) => new(code, message, (int)token.Index, token.Length);

    internal static CalcError TypeMismatch(in Value leftOperand, in Value rightOperand, Token token, DataType? expectedDataType = null) =>
        Error(ErrorCode.TypeMismatch, GetDataTypeMismatchErrorMessage(leftOperand, rightOperand, token, expectedDataType), token);

    internal static CalcError NullValue(bool leftOperandNull, bool rightOperandNull, Token token, DataType expectedDataType) =>
        Error(ErrorCode.NullValue, GetNullValueErrorMessage(leftOperandNull, rightOperandNull, token, expectedDataType), token);

    internal static CalcError NotSupported(Token token, DataType dataType) =>
        Error(ErrorCode.OperationNotSupported, $"Operator '{token.KeyString}' is not supported on data type {dataType} at position {token.Index}.", token);
}
