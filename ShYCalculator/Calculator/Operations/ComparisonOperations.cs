// -----------------------------------------------------------------------------
// <summary>
//     Implements comparison operations (==, !=, <, >, <=, >=).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;

namespace ShYCalculator.Calculator.Operations;

internal static class ComparisonOperations {
    internal static OpResult<Value> PerformComparisonOperationOnStrings(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.String || rightOperand.DataType != DataType.String) {
            return OpResult<Value>.Fail(OperationHelper.GetDataTypeMismatchErrorMessage(leftOperand, rightOperand, token, DataType.String));
        }

        if (leftOperand.Svalue == null || rightOperand.Svalue == null) {
            return OpResult<Value>.Fail(OperationHelper.GetNullValueErrorMessage(leftOperand.Svalue == null, rightOperand.Svalue == null, token, DataType.String));
        }

        var operand1 = leftOperand.Svalue!;
        var operand2 = rightOperand.Svalue!;

        bool result = token.OperatorKind switch {
            OperatorKind.Eq => operand1 == operand2,
            OperatorKind.NotEq => operand1 != operand2,
            _ => throw new ShuntingYardParserException($"Unexpected comparison operator token '{token.KeyString}' of type {token.Type} at position {token.Index} on data type {DataType.String}.")
        };

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Boolean,
            Bvalue = result
        });
    }

    internal static OpResult<Value> PerformComparisonOperationOnNumbers(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Number || rightOperand.DataType != DataType.Number) {
            return OpResult<Value>.Fail(OperationHelper.GetDataTypeMismatchErrorMessage(leftOperand, rightOperand, token, DataType.Number));
        }

        if (leftOperand.Nvalue == null || rightOperand.Nvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.GetNullValueErrorMessage(leftOperand.Nvalue == null, rightOperand.Nvalue == null, token, DataType.Number));
        }

        var operand1 = leftOperand.Nvalue!.Value;
        var operand2 = rightOperand.Nvalue!.Value;

        bool result = token.OperatorKind switch {
            OperatorKind.Lt => operand1.CompareTo(operand2) < 0,
            OperatorKind.Gt => operand1.CompareTo(operand2) > 0,
            OperatorKind.LtEq => operand1.CompareTo(operand2) <= 0,
            OperatorKind.GtEq => operand1.CompareTo(operand2) >= 0,
            OperatorKind.Eq => operand1.CompareTo(operand2) == 0,
            OperatorKind.NotEq => operand1.CompareTo(operand2) != 0,
            _ => throw new ShuntingYardParserException($"Unexpected comparison operator token '{token.KeyString}' of type {token.Type} at position {token.Index} on data type {DataType.Number}.")
        };

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Boolean,
            Bvalue = result
        });
    }

    internal static OpResult<Value> PerformComparisonOperationOnBooleans(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Boolean || rightOperand.DataType != DataType.Boolean) {
            return OpResult<Value>.Fail(OperationHelper.GetDataTypeMismatchErrorMessage(leftOperand, rightOperand, token, DataType.Boolean));
        }

        if (leftOperand.Bvalue == null || rightOperand.Bvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.GetNullValueErrorMessage(leftOperand.Bvalue == null, rightOperand.Bvalue == null, token, DataType.Boolean));
        }

        var operand1 = leftOperand.Bvalue!.Value;
        var operand2 = rightOperand.Bvalue!.Value;

        bool result = token.OperatorKind switch {
            OperatorKind.Eq => operand1 == operand2,
            OperatorKind.NotEq => operand1 != operand2,
            _ => throw new ShuntingYardParserException($"Unexpected comparison operator token '{token.KeyString}' of type {token.Type} at position {token.Index} on data type {DataType.Boolean}.")
        };

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Boolean,
            Bvalue = result
        });
    }

    internal static OpResult<Value> PerformComparisonOperationOnDates(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType != DataType.Date || rightOperand.DataType != DataType.Date) {
            return OpResult<Value>.Fail(OperationHelper.GetDataTypeMismatchErrorMessage(leftOperand, rightOperand, token, DataType.Date));
        }

        if (leftOperand.Dvalue == null || rightOperand.Dvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.GetNullValueErrorMessage(leftOperand.Dvalue == null, rightOperand.Dvalue == null, token, DataType.Date));
        }

        var operand1 = leftOperand.Dvalue!.Value;
        var operand2 = rightOperand.Dvalue!.Value;

        bool result = token.OperatorKind switch {
            OperatorKind.Lt => operand1 < operand2,
            OperatorKind.Gt => operand1 > operand2,
            OperatorKind.LtEq => operand1 <= operand2,
            OperatorKind.GtEq => operand1 >= operand2,
            OperatorKind.Eq => operand1 == operand2,
            OperatorKind.NotEq => operand1 != operand2,
            _ => throw new ShuntingYardParserException($"Unexpected comparison operator token '{token.KeyString}' of type {token.Type} at position {token.Index} on data type {DataType.Date}.")
        };

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Boolean,
            Bvalue = result
        });
    }
}
