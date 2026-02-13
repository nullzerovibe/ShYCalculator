// -----------------------------------------------------------------------------
// <summary>
//     Implements logical operations (&&, ||, !).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;

namespace ShYCalculator.Calculator.Operations;

internal static class LogicalOperations {
    internal static OpResult<Value> PerformLogicalOperationOnBooleans(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Boolean || rightOperand.DataType != DataType.Boolean) {
            return OpResult<Value>.Fail(OperationHelper.GetDataTypeMismatchErrorMessage(leftOperand, rightOperand, token, DataType.Boolean));
        }

        if (leftOperand.Bvalue == null || rightOperand.Bvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.GetNullValueErrorMessage(leftOperand.Bvalue == null, rightOperand.Bvalue == null, token, DataType.Boolean));
        }

        var operand1 = leftOperand.Bvalue!.Value;
        var operand2 = rightOperand.Bvalue!.Value;

        bool result = token.OperatorKind switch {
            OperatorKind.And => operand1 && operand2,
            OperatorKind.Or => operand1 || operand2,
            _ => throw new ShuntingYardParserException($"Unexpected logical operator token '{token.KeyString}' of type {token.Type} at position {token.Index}")
        };

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Boolean,
            Bvalue = result
        });
    }
}
