// -----------------------------------------------------------------------------
// <summary>
//     Helper methods for performing numeric checks and type validations during operations.
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;

namespace ShYCalculator.Calculator.Operations;

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
}
