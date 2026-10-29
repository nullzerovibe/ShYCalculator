// -----------------------------------------------------------------------------
// <summary>
//     Implements basic arithmetic operations (+, -, *, /, %, ^, !).
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;

namespace ShY.Calculator.Operations;

internal static class ArithmeticOperations {
    internal static OpResult<Value> PerformArithmeticOperationOnNumbers(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Number || rightOperand.DataType != DataType.Number) {
            return OpResult<Value>.Fail(OperationHelper.TypeMismatch(leftOperand, rightOperand, token, DataType.Number));
        }

        if (leftOperand.Nvalue == null || rightOperand.Nvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.NullValue(leftOperand.Nvalue == null, rightOperand.Nvalue == null, token, DataType.Number));
        }

        if (leftOperand.IsDecimal || rightOperand.IsDecimal) {
            return PerformArithmeticOperationOnDecimals(leftOperand, rightOperand, token);
        }

        var operand1 = leftOperand.Nvalue!.Value;
        var operand2 = rightOperand.Nvalue!.Value;

        if (token.OperatorKind is OperatorKind.Div or OperatorKind.Mod && operand2 == 0) {
            return OpResult<Value>.Fail(OperationHelper.Error(ErrorCode.DivisionByZero, $"Division by zero at position {token.Index}.", token));
        }

        double? result = token.OperatorKind switch {
            OperatorKind.Add => operand1 + operand2,
            OperatorKind.Sub => operand1 - operand2,
            OperatorKind.Mul => operand1 * operand2,
            OperatorKind.Div => operand1 / operand2,
            OperatorKind.Mod => operand1 % operand2,
            OperatorKind.Pow => Math.Pow(operand1, operand2),
            _ => null
        };

        if (result is null) return OpResult<Value>.Fail(OperationHelper.NotSupported(token, leftOperand.DataType));

        return OpResult<Value>.Ok(Value.Number(result.Value));
    }


    private static OpResult<Value> PerformArithmeticOperationOnDecimals(in Value leftOperand, in Value rightOperand, Token token) {
        if (!DecimalMath.TryGetDecimal(leftOperand, out var operand1) || !DecimalMath.TryGetDecimal(rightOperand, out var operand2)) {
            return OpResult<Value>.Fail(OperationHelper.Error(ErrorCode.ValueOutOfRange, $"Operand of '{token.KeyString}' at position {token.Index} cannot be represented as a decimal.", token));
        }

        if (token.OperatorKind is OperatorKind.Div or OperatorKind.Mod && operand2 == 0) {
            return OpResult<Value>.Fail(OperationHelper.Error(ErrorCode.DivisionByZero, $"Division by zero at position {token.Index}.", token));
        }

        try {
            decimal? result = token.OperatorKind switch {
                OperatorKind.Add => operand1 + operand2,
                OperatorKind.Sub => operand1 - operand2,
                OperatorKind.Mul => operand1 * operand2,
                OperatorKind.Div => operand1 / operand2,
                OperatorKind.Mod => operand1 % operand2,
                OperatorKind.Pow => DecimalMath.Pow(operand1, operand2),
                _ => null
            };

            if (result is null) return OpResult<Value>.Fail(OperationHelper.NotSupported(token, DataType.Number));

            return OpResult<Value>.Ok(Value.Number(result.Value));
        }
        catch (Exception ex) when (ex is OverflowException or DivideByZeroException) {
            return OpResult<Value>.Fail(OperationHelper.Error(ErrorCode.ValueOutOfRange, $"Result of '{token.KeyString}' at position {token.Index} is outside the decimal range.", token));
        }
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
            return OpResult<Value>.Fail(OperationHelper.TypeMismatch(leftOperand, rightOperand, token, DataType.String));
        }

        return OpResult<Value>.Fail(OperationHelper.NotSupported(token, DataType.String));
    }

    private static string GetValueString(Value v) {
        if (v.DataType.HasFlag(DataType.String)) return v.Svalue ?? "";
        if (v.DataType.HasFlag(DataType.Number)) return v.IsDecimal
            ? v.Mvalue!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : v.Nvalue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        if (v.DataType.HasFlag(DataType.Boolean)) return v.Bvalue?.ToString() ?? "";
        if (v.DataType.HasFlag(DataType.Date)) return v.Dvalue?.ToString("o", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        return "";
    }
}
