// -----------------------------------------------------------------------------
// <summary>
//     Implements bitwise operations (&, |, ^, ~).
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;

namespace ShY.Calculator.Operations;

internal static class BitwiseOperations {
    internal static OpResult<Value> PerformBitwiseOperationOnNumbers(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Number || rightOperand.DataType != DataType.Number) {
            return OpResult<Value>.Fail(OperationHelper.TypeMismatch(leftOperand, rightOperand, token, DataType.Number));
        }

        if (leftOperand.Nvalue == null || rightOperand.Nvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.NullValue(leftOperand.Nvalue == null, rightOperand.Nvalue == null, token, DataType.Number));
        }

        // WHY: 64-bit operands keep binary operators consistent with unary '~', which has always used long.
        if (!OperationHelper.TryGetInt64(leftOperand, out var operand1) || !OperationHelper.TryGetInt64(rightOperand, out var operand2)) {
            return OpResult<Value>.Fail(OperationHelper.Error(ErrorCode.ValueOutOfRange, $"BitwiseOperation require integer numbers (64-bit range) token '{token.KeyString}' of type {token.Type} at position {token.Index}.", token));
        }

        // WARN: C# masks shift counts to 6 bits, so 1 << 64 would silently yield 1.
        if (token.OperatorKind is OperatorKind.BitwiseLeftShift or OperatorKind.BitwiseRightShift && operand2 is < 0 or > 63) {
            return OpResult<Value>.Fail(OperationHelper.Error(ErrorCode.ValueOutOfRange, $"Shift count must be between 0 and 63, got {operand2} at position {token.Index}.", token));
        }

        long? result = token.OperatorKind switch {
            OperatorKind.BitwiseAnd => operand1 & operand2,
            OperatorKind.BitwiseOr => operand1 | operand2,
            OperatorKind.BitwiseXor => operand1 ^ operand2,
            OperatorKind.BitwiseLeftShift => operand1 << (int)operand2,
            OperatorKind.BitwiseRightShift => operand1 >> (int)operand2,
            _ => null
        };

        if (result is null) return OpResult<Value>.Fail(OperationHelper.NotSupported(token, DataType.Number));

        return OpResult<Value>.Ok(leftOperand.IsDecimal || rightOperand.IsDecimal ? Value.Number((decimal)result.Value) : Value.Number(result.Value));
    }
}
