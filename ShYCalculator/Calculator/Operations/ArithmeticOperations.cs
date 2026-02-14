// -----------------------------------------------------------------------------
// <summary>
//     Implements basic arithmetic operations (+, -, *, /, %, ^, !).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;

namespace ShYCalculator.Calculator.Operations;

internal static class ArithmeticOperations {
    internal static OpResult<Value> PerformArithmeticOperationOnNumbers(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Number || rightOperand.DataType != DataType.Number) {
            return OpResult<Value>.Fail(OperationHelper.GetDataTypeMismatchErrorMessage(leftOperand, rightOperand, token, DataType.Number));
        }

        if (leftOperand.Nvalue == null || rightOperand.Nvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.GetNullValueErrorMessage(leftOperand.Nvalue == null, rightOperand.Nvalue == null, token, DataType.Number));
        }

        var operand1 = leftOperand.Nvalue!.Value;
        var operand2 = rightOperand.Nvalue!.Value;

        if (token.OperatorKind == OperatorKind.Div && operand2 == 0) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.DivisionByZero, $"Division by zero at position {token.Index}."));
        }

        double result = token.OperatorKind switch {
            OperatorKind.Add => operand1 + operand2,
            OperatorKind.Sub => operand1 - operand2,
            OperatorKind.Mul => operand1 * operand2,
            OperatorKind.Div => operand1 / operand2,
            OperatorKind.Mod => operand1 % operand2,
            OperatorKind.Pow => Math.Pow(operand1, operand2),
            _ => throw new ShuntingYardParserException($"Unexpected arithmetic operator token '{token.KeyString}' of type {token.Type} at position {token.Index}")
        };

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Number,
            Nvalue = result
        });
    }


    internal static OpResult<Value> PerformArithmeticOperationOnStrings(in Value leftOperand, in Value rightOperand, Token token) {
        if (token.OperatorKind == OperatorKind.Add) {
             string val1 = GetValueString(leftOperand);
             string val2 = GetValueString(rightOperand);
             return OpResult<Value>.Ok(new Value {
                 DataType = DataType.String,
                 Svalue = val1 + val2
             });
        }

        if (leftOperand.DataType != DataType.String || rightOperand.DataType != DataType.String) {
            return OpResult<Value>.Fail(OperationHelper.GetDataTypeMismatchErrorMessage(leftOperand, rightOperand, token, DataType.String));
        }

        throw new ShuntingYardParserException($"Unexpected operator token '{token.KeyString}' of type {token.Type} at position {token.Index} on data type {DataType.String}.");
    }

    private static string GetValueString(Value v) {
        if (v.DataType.HasFlag(DataType.String)) return v.Svalue ?? "";
        if (v.DataType.HasFlag(DataType.Number)) return v.Nvalue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        if (v.DataType.HasFlag(DataType.Boolean)) return v.Bvalue?.ToString() ?? "";
        if (v.DataType.HasFlag(DataType.Date)) return v.Dvalue?.ToString() ?? "";
        return "";
    }
}
