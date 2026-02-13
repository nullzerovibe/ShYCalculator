// -----------------------------------------------------------------------------
// <summary>
//     Implements unary operations (negation, logical not, bitwise not).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;

namespace ShYCalculator.Calculator.Operations;

internal static class UnaryOperations {
    internal static OpResult<Value> PerformUnaryOperation(in Value operand, Token token) {
        return token.OperatorKind switch {
            OperatorKind.Not => PerformLogicalNot(operand, token),
            OperatorKind.Factorial => PerformFactorial(operand, token),
            OperatorKind.BitwiseNot => PerformBitwiseNot(operand, token),
            OperatorKind.SquareRoot => PerformSquareRoot(operand, token),
            _ => throw new ShuntingYardParserException($"Unknown unary operator: '{token.KeyString}' at position {token.Index}")
        };
    }


    private static OpResult<Value> PerformLogicalNot(in Value operand, Token token) {
        if (operand.DataType != DataType.Boolean) {
            return OpResult<Value>.Fail($"Logical NOT requires boolean operand, got {operand.DataType} at position {token.Index}");
        }
        if (operand.Bvalue == null) {
            return OpResult<Value>.Fail($"Logical NOT operand is null at position {token.Index}");
        }
        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Boolean,
            Bvalue = !operand.Bvalue.Value
        });
    }

    private static OpResult<Value> PerformFactorial(in Value operand, Token token) {
        if (operand.DataType != DataType.Number) {
            return OpResult<Value>.Fail($"Factorial requires number operand, got {operand.DataType} at position {token.Index}");
        }
        if (operand.Nvalue == null) {
            return OpResult<Value>.Fail($"Factorial operand is null at position {token.Index}");
        }

        var n = operand.Nvalue.Value;
        if (n < 0 || n != Math.Floor(n)) {
            return OpResult<Value>.Fail($"Factorial requires non-negative integer, got {n} at position {token.Index}");
        }
        if (n > 170) {
            return OpResult<Value>.Fail($"Factorial overflow: {n}! exceeds double precision at position {token.Index}");
        }

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Number,
            Nvalue = Factorial((int)n)
        });
    }

    private static double Factorial(int n) {
        if (n <= 1) return 1;
        double result = 1;
        for (int i = 2; i <= n; i++) {
            result *= i;
        }
        return result;
    }

    private static OpResult<Value> PerformBitwiseNot(in Value operand, Token token) {
        if (operand.DataType != DataType.Number) {
            return OpResult<Value>.Fail($"Bitwise NOT requires number operand, got {operand.DataType} at position {token.Index}");
        }
        if (operand.Nvalue == null) {
            return OpResult<Value>.Fail($"Bitwise NOT operand is null at position {token.Index}");
        }

        var n = operand.Nvalue.Value;
        if (n != Math.Floor(n)) {
            return OpResult<Value>.Fail($"Bitwise NOT requires integer, got {n} at position {token.Index}");
        }

        // Perform bitwise NOT on long to support larger integers
        long intValue = (long)n;
        long result = ~intValue;

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Number,
            Nvalue = result
        });
    }

    private static OpResult<Value> PerformSquareRoot(in Value operand, Token token) {
        if (operand.DataType != DataType.Number) {
            return OpResult<Value>.Fail($"Square root requires number operand, got {operand.DataType} at position {token.Index}");
        }
        if (operand.Nvalue == null) {
            return OpResult<Value>.Fail($"Square root operand is null at position {token.Index}");
        }

        var n = operand.Nvalue.Value;
        if (n < 0) {
            return OpResult<Value>.Fail($"Square root requires non-negative number, got {n} at position {token.Index}");
        }

        return OpResult<Value>.Ok(new Value {
            DataType = DataType.Number,
            Nvalue = Math.Sqrt(n)
        });
    }
}
