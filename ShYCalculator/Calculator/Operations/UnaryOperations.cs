// -----------------------------------------------------------------------------
// <summary>
//     Implements unary operations (negation, logical not, bitwise not).
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;

namespace ShY.Calculator.Operations;

internal static class UnaryOperations {
    internal static OpResult<Value> PerformUnaryOperation(in Value operand, Token token) {
        return token.OperatorKind switch {
            OperatorKind.Not => PerformLogicalNot(operand, token),
            OperatorKind.Factorial => PerformFactorial(operand, token),
            OperatorKind.BitwiseNot => PerformBitwiseNot(operand, token),
            OperatorKind.SquareRoot => PerformSquareRoot(operand, token),
            _ => Fail(ErrorCode.OperationNotSupported, $"Unknown unary operator: '{token.KeyString}' at position {token.Index}", token)
        };
    }

    private static OpResult<Value> Fail(ErrorCode code, string message, Token token) => OpResult<Value>.Fail(OperationHelper.Error(code, message, token));

    private static OpResult<Value> PerformLogicalNot(in Value operand, Token token) {
        if (operand.DataType != DataType.Boolean) {
            return Fail(ErrorCode.TypeMismatch, $"Logical NOT requires boolean operand, got {operand.DataType} at position {token.Index}", token);
        }
        if (operand.Bvalue == null) {
            return Fail(ErrorCode.NullValue, $"Logical NOT operand is null at position {token.Index}", token);
        }
        return OpResult<Value>.Ok(Value.Boolean(!operand.Bvalue.Value));
    }

    private static OpResult<Value> PerformFactorial(in Value operand, Token token) {
        if (operand.DataType != DataType.Number) {
            return Fail(ErrorCode.TypeMismatch, $"Factorial requires number operand, got {operand.DataType} at position {token.Index}", token);
        }
        if (operand.Nvalue == null) {
            return Fail(ErrorCode.NullValue, $"Factorial operand is null at position {token.Index}", token);
        }

        if (operand.IsDecimal) {
            var exact = operand.Mvalue!.Value;
            if (exact < 0 || exact != decimal.Truncate(exact)) {
                return Fail(ErrorCode.ValueOutOfRange, $"Factorial requires non-negative integer, got {exact} at position {token.Index}", token);
            }
            // WHY: 27! is the largest factorial below decimal.MaxValue (~7.9e28).
            if (exact > 27) {
                return Fail(ErrorCode.ValueOutOfRange, $"Factorial overflow: {exact}! exceeds the decimal range at position {token.Index}", token);
            }
            decimal product = 1;
            for (var i = 2; i <= (int)exact; i++) product *= i;
            return OpResult<Value>.Ok(Value.Number(product));
        }

        var n = operand.Nvalue.Value;
        if (n < 0 || n != Math.Floor(n)) {
            return Fail(ErrorCode.ValueOutOfRange, $"Factorial requires non-negative integer, got {n} at position {token.Index}", token);
        }
        if (n > 170) {
            return Fail(ErrorCode.ValueOutOfRange, $"Factorial overflow: {n}! exceeds double precision at position {token.Index}", token);
        }

        return OpResult<Value>.Ok(Value.Number(Factorial((int)n)));
    }

    private static double Factorial(int n) {
        double result = 1;
        for (int i = 2; i <= n; i++) {
            result *= i;
        }
        return result;
    }

    private static OpResult<Value> PerformBitwiseNot(in Value operand, Token token) {
        if (operand.DataType != DataType.Number) {
            return Fail(ErrorCode.TypeMismatch, $"Bitwise NOT requires number operand, got {operand.DataType} at position {token.Index}", token);
        }
        if (operand.Nvalue == null) {
            return Fail(ErrorCode.NullValue, $"Bitwise NOT operand is null at position {token.Index}", token);
        }
        if (!OperationHelper.TryGetInt64(operand, out var n)) {
            return Fail(ErrorCode.ValueOutOfRange, $"Bitwise NOT requires a 64-bit integer, got {operand.Nvalue.Value} at position {token.Index}", token);
        }

        return OpResult<Value>.Ok(operand.IsDecimal ? Value.Number((decimal)~n) : Value.Number(~n));
    }

    private static OpResult<Value> PerformSquareRoot(in Value operand, Token token) {
        if (operand.DataType != DataType.Number) {
            return Fail(ErrorCode.TypeMismatch, $"Square root requires number operand, got {operand.DataType} at position {token.Index}", token);
        }
        if (operand.Nvalue == null) {
            return Fail(ErrorCode.NullValue, $"Square root operand is null at position {token.Index}", token);
        }

        var n = operand.Nvalue.Value;
        if (n < 0) {
            return Fail(ErrorCode.ValueOutOfRange, $"Square root requires non-negative number, got {n} at position {token.Index}", token);
        }

        return OpResult<Value>.Ok(Value.Number(Math.Sqrt(n)));
    }
}
