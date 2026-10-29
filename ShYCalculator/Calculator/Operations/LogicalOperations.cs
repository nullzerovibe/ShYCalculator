// -----------------------------------------------------------------------------
// <summary>
//     Implements logical operations (&&, ||, !).
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;

namespace ShY.Calculator.Operations;

internal static class LogicalOperations {
    internal static OpResult<Value> PerformLogicalOperationOnBooleans(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Boolean || rightOperand.DataType != DataType.Boolean) {
            return OpResult<Value>.Fail(OperationHelper.TypeMismatch(leftOperand, rightOperand, token, DataType.Boolean));
        }

        if (leftOperand.Bvalue == null || rightOperand.Bvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.NullValue(leftOperand.Bvalue == null, rightOperand.Bvalue == null, token, DataType.Boolean));
        }

        var operand1 = leftOperand.Bvalue!.Value;
        var operand2 = rightOperand.Bvalue!.Value;

        bool? result = token.OperatorKind switch {
            OperatorKind.And => operand1 && operand2,
            OperatorKind.Or => operand1 || operand2,
            _ => null
        };

        if (result is null) return OpResult<Value>.Fail(OperationHelper.NotSupported(token, leftOperand.DataType));

        return OpResult<Value>.Ok(Value.Boolean(result.Value));
    }
}
