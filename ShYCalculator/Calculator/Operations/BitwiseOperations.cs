// -----------------------------------------------------------------------------
// <summary>
//     Implements bitwise operations (&, |, ^, ~).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;

namespace ShYCalculator.Calculator.Operations;

internal static class BitwiseOperations {
    internal static OpResult<Value> PerformBitwiseOperationOnNumbers(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Number || rightOperand.DataType != DataType.Number) {
            return OpResult<Value>.Fail(OperationHelper.GetDataTypeMismatchErrorMessage(leftOperand, rightOperand, token, DataType.Number));
        }

        if (leftOperand.Nvalue == null || rightOperand.Nvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.GetNullValueErrorMessage(leftOperand.Nvalue == null, rightOperand.Nvalue == null, token, DataType.Number));
        }

        if (!OperationHelper.IsInteger(leftOperand.Nvalue!.Value) || !OperationHelper.IsInteger(rightOperand.Nvalue!.Value)) {
            return OpResult<Value>.Fail($"BitwiseOperation require integer numbers token '{token.Key}' of type {token.Type} at position {token.Index}.");
        }

        var operand1 = Convert.ToInt32(leftOperand.Nvalue);
        var operand2 = Convert.ToInt32(rightOperand.Nvalue);

        double result = token.OperatorKind switch {
            OperatorKind.BitwiseAnd => operand1 & operand2,
            OperatorKind.BitwiseOr => operand1 | operand2,
            OperatorKind.BitwiseXor => operand1 ^ operand2,
            _ => throw new ShuntingYardParserException($"Unexpected bitwise operator token '{token.KeyString}' of type {token.Type} at position {token.Index}")
        };

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Number,
            Nvalue = result
        });
    }
}
