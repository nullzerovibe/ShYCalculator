// -----------------------------------------------------------------------------
// <summary>
//     Implements comparison operations (==, !=, <, >, <=, >=).
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;

namespace ShY.Calculator.Operations;

internal static class ComparisonOperations {
    internal static OpResult<Value> PerformComparisonOperationOnStrings(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.String || rightOperand.DataType != DataType.String) {
            return OpResult<Value>.Fail(OperationHelper.TypeMismatch(leftOperand, rightOperand, token, DataType.String));
        }

        if (leftOperand.Svalue == null || rightOperand.Svalue == null) {
            return OpResult<Value>.Fail(OperationHelper.NullValue(leftOperand.Svalue == null, rightOperand.Svalue == null, token, DataType.String));
        }

        var operand1 = leftOperand.Svalue!;
        var operand2 = rightOperand.Svalue!;

        bool? result = token.OperatorKind switch {
            OperatorKind.Eq => operand1 == operand2,
            OperatorKind.NotEq => operand1 != operand2,
            _ => null
        };

        if (result is null) return OpResult<Value>.Fail(OperationHelper.NotSupported(token, leftOperand.DataType));

        return OpResult<Value>.Ok(Value.Boolean(result.Value));
    }

    internal static OpResult<Value> PerformComparisonOperationOnNumbers(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Number || rightOperand.DataType != DataType.Number) {
            return OpResult<Value>.Fail(OperationHelper.TypeMismatch(leftOperand, rightOperand, token, DataType.Number));
        }

        if (leftOperand.Nvalue == null || rightOperand.Nvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.NullValue(leftOperand.Nvalue == null, rightOperand.Nvalue == null, token, DataType.Number));
        }

        // WHY: comparing decimals as doubles would reintroduce binary rounding (0.1 + 0.2 == 0.3 must hold).
        var comparison = (leftOperand.IsDecimal || rightOperand.IsDecimal)
            && DecimalMath.TryGetDecimal(leftOperand, out var exact1) && DecimalMath.TryGetDecimal(rightOperand, out var exact2)
            ? exact1.CompareTo(exact2)
            : leftOperand.Nvalue!.Value.CompareTo(rightOperand.Nvalue!.Value);

        bool? result = token.OperatorKind switch {
            OperatorKind.Lt => comparison < 0,
            OperatorKind.Gt => comparison > 0,
            OperatorKind.LtEq => comparison <= 0,
            OperatorKind.GtEq => comparison >= 0,
            OperatorKind.Eq => comparison == 0,
            OperatorKind.NotEq => comparison != 0,
            _ => null
        };

        if (result is null) return OpResult<Value>.Fail(OperationHelper.NotSupported(token, leftOperand.DataType));

        return OpResult<Value>.Ok(Value.Boolean(result.Value));
    }

    internal static OpResult<Value> PerformComparisonOperationOnBooleans(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Boolean || rightOperand.DataType != DataType.Boolean) {
            return OpResult<Value>.Fail(OperationHelper.TypeMismatch(leftOperand, rightOperand, token, DataType.Boolean));
        }

        if (leftOperand.Bvalue == null || rightOperand.Bvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.NullValue(leftOperand.Bvalue == null, rightOperand.Bvalue == null, token, DataType.Boolean));
        }

        var operand1 = leftOperand.Bvalue!.Value;
        var operand2 = rightOperand.Bvalue!.Value;

        bool? result = token.OperatorKind switch {
            OperatorKind.Eq => operand1 == operand2,
            OperatorKind.NotEq => operand1 != operand2,
            _ => null
        };

        if (result is null) return OpResult<Value>.Fail(OperationHelper.NotSupported(token, leftOperand.DataType));

        return OpResult<Value>.Ok(Value.Boolean(result.Value));
    }

    internal static OpResult<Value> PerformComparisonOperationOnDates(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Date || rightOperand.DataType != DataType.Date) {
            return OpResult<Value>.Fail(OperationHelper.TypeMismatch(leftOperand, rightOperand, token, DataType.Date));
        }

        if (leftOperand.Dvalue == null || rightOperand.Dvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.NullValue(leftOperand.Dvalue == null, rightOperand.Dvalue == null, token, DataType.Date));
        }

        var operand1 = leftOperand.Dvalue!.Value;
        var operand2 = rightOperand.Dvalue!.Value;

        bool? result = token.OperatorKind switch {
            OperatorKind.Lt => operand1 < operand2,
            OperatorKind.Gt => operand1 > operand2,
            OperatorKind.LtEq => operand1 <= operand2,
            OperatorKind.GtEq => operand1 >= operand2,
            OperatorKind.Eq => operand1 == operand2,
            OperatorKind.NotEq => operand1 != operand2,
            _ => null
        };

        if (result is null) return OpResult<Value>.Fail(OperationHelper.NotSupported(token, leftOperand.DataType));

        return OpResult<Value>.Ok(Value.Boolean(result.Value));
    }
}
