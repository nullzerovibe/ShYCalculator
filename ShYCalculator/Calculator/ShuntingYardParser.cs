// -----------------------------------------------------------------------------
// <summary>
//     Evaluates RPN token streams to calculate the final result.
//     Executes the logic for operators and functions.
// 
//     Parsing and Evaluation Phase:
//     After the Shunting Yard Generate phase, the rearranged tokens are processed and evaluated to compute the result of the expression.
//     This involves traversing the tokens in the RPN list and performing the appropriate operations based on their types.
// </summary>
// -----------------------------------------------------------------------------
using ShY.Calculator.Operations;
using ShY.Classes;
using System.Globalization;
using System.Collections.Frozen;

namespace ShY.Calculator;

internal class ShuntingYardParser(IGlobalScope globalScope, ShYCalculatorOptions? options = null) : IShuntingYardParser {
    #region Members
    private readonly IGlobalScope m_globalScope = globalScope;
    private readonly bool m_decimal = options?.NumericMode == NumericMode.Decimal;

    #endregion Members

    #region Methods
    public OpResult<Value> Evaluate(IEnumerable<Token> rpnTokens, string expression, IDictionary<string, Value>? contextVariables = null) {
        try {
            return EvaluateInternal(rpnTokens, contextVariables);
        }
        catch (InsufficientExecutionStackException) {
            return OpResult<Value>.Fail(ShuntingYardGenerator.StackLimitError());
        }
        catch (Exception ex) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.UnexpectedException, $"Unexpected exception calculating the expression: '{expression}'. {ex.Message}"));
        }
    }
    #endregion Methods

    #region Private Methods
    internal OpResult<Value> EvaluateInternal(IEnumerable<Token> rpnTokens, IDictionary<string, Value>? contextVariables = null) {
        System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
        const int InitialCapacity = 128;
        Value[] pooledStack = System.Buffers.ArrayPool<Value>.Shared.Rent(InitialCapacity);
        int stackCount = 0;

        try {
            foreach (var token in rpnTokens) {
                // WHY: each RPN token grows the stack by at most one value, so one free slot per step is enough.
                if (stackCount == pooledStack.Length) {
                    var grown = System.Buffers.ArrayPool<Value>.Shared.Rent(pooledStack.Length * 2);
                    pooledStack.AsSpan(0, stackCount).CopyTo(grown);
                    System.Buffers.ArrayPool<Value>.Shared.Return(pooledStack);
                    pooledStack = grown;
                }

                var result = ProcessToken(token, pooledStack, ref stackCount, contextVariables);
                if (!result.Success) return result;

                // WHY: every token leaves exactly its result on top, so this single point makes decimal mode hold for
                // constants, variables, function and operator results alike.
                if (m_decimal && !TryNormalizeToDecimal(ref pooledStack[stackCount - 1], token, out var normalizeError)) {
                    return OpResult<Value>.Fail(normalizeError!);
                }
            }

            if (stackCount != 1) {
                return OpResult<Value>.Fail(new CalcError(ErrorCode.InvalidSyntax, "Invalid expression syntax: stack should have exactly one value."));
            }

            return OpResult<Value>.Ok(pooledStack[0]);
        }
        finally {
            System.Buffers.ArrayPool<Value>.Shared.Return(pooledStack);
        }
    }

    internal OpResult<Value> ProcessToken(Token token, Value[] stack, ref int count, IDictionary<string, Value>? contextVariables) {
        return token.Type switch {
            TokenType.String => ProcessString(token, stack, ref count),
            TokenType.Number => ProcessNumber(token, stack, ref count),
            TokenType.Constant => ProcessConstant(token, stack, ref count),
            TokenType.Variable => ProcessVariable(token, stack, ref count, contextVariables),
            TokenType.Function => ProcessFunction(token, stack, ref count),
            TokenType.Operator => ProcessOperator(token, stack, ref count),
            TokenType.Ternary => ProcessTernary(token, stack, ref count, contextVariables),
            TokenType.UnaryPrefixOperator or TokenType.UnaryPostfixOperator => ProcessUnaryOperator(token, stack, ref count),
            _ => OpResult<Value>.Fail(new CalcError(ErrorCode.UnknownToken, $"Unexpected token '{token.KeyString}' of type {token.Type} at position {token.Index}", (int)token.Index, token.Length))
        };
    }

    private static OpResult<Value> ProcessString(Token token, Value[] stack, ref int count) {
        stack[count++] = new Value { Svalue = token.KeyString, DataType = DataType.String };
        return OpResult<Value>.Ok(default);
    }

    private static bool TryNormalizeToDecimal(ref Value value, Token token, out CalcError? error) {
        error = null;
        if (value.DataType != DataType.Number || value.IsDecimal || value.Nvalue is not { } number) return true;

        if (!Value.TryToDecimal(number, out var exact)) {
            error = OperationHelper.Error(ErrorCode.ValueOutOfRange, $"Value {number} at position {token.Index} cannot be represented as a decimal.", token);
            return false;
        }

        value = Value.Number(exact);
        return true;
    }

    private OpResult<Value> ProcessNumber(Token token, Value[] stack, ref int count) {
        if (m_decimal) {
            // WHY: parse literals straight from text; going through double first would reintroduce binary rounding.
            if (!decimal.TryParse(token.KeySpan, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var exact)) {
                return OpResult<Value>.Fail(OperationHelper.Error(ErrorCode.ValueOutOfRange, $"Literal {token.KeyString} at position {token.Index} is outside the decimal range.", token));
            }
            stack[count++] = Value.Number(exact);
            return OpResult<Value>.Ok(default);
        }

        if (token.HasParsedNumber) {
            stack[count++] = Value.Number(token.ParsedNumber);
            return OpResult<Value>.Ok(default);
        }

        if (!double.TryParse(token.KeySpan, NumberFormatInfo.InvariantInfo, out var number)) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.InvalidNumberFormat, $"Value mismatch for DataType '{token.Type}' at position {token.Index}. Provided value {token.KeyString} can't be parsed to {token.Type}.", (int)token.Index, token.Length));
        }
        stack[count++] = new Value { Nvalue = number, DataType = DataType.Number };
        return OpResult<Value>.Ok(default);
    }

    private OpResult<Value> ProcessConstant(Token token, Value[] stack, ref int count) {
        if (SpanLookup.TryGetValue(m_globalScope.Constants, token.KeySpan, out var constant)) {
            stack[count++] = constant;
            return OpResult<Value>.Ok(default);
        }

        return OpResult<Value>.Fail(new CalcError(ErrorCode.VariableNotFound, $"Unknown Constant '{token.KeyString}' at position {token.Index}.", (int)token.Index, token.Length));
    }

    private static OpResult<Value> ProcessVariable(Token token, Value[] stack, ref int count, IDictionary<string, Value>? contextVariables) {
        if (contextVariables != null && SpanLookup.TryGetValue(contextVariables, token.KeySpan, out var contextVar)) {
            stack[count++] = contextVar;
            return OpResult<Value>.Ok(default);
        }

        return OpResult<Value>.Fail(new CalcError(ErrorCode.VariableNotFound, $"Unknown Variable '{token.KeyString}' at position {token.Index}. Ensure it is passed in the context variables.", (int)token.Index, token.Length));
    }

    internal OpResult<Value> ProcessFunction(Token token, Value[] stack, ref int count) {
        var keyString = token.KeyString;
        if (!m_globalScope.Functions.TryGetValue(keyString, out var extensionModule)) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.FunctionNotFound, $"Unknown Function '{keyString}' at position {token.Index}.", (int)token.Index, token.Length));
        }

        if (token.FunctionInfo == null) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.UnexpectedException, $"FunctionInfo is missing for Function '{keyString}' at position {token.Index}.", (int)token.Index, token.Length));
        }

        if (count < token.FunctionInfo.ArgumentsCount) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.InvalidFunctionArgument, $"Stack size mismatch for Function '{keyString}' at position {token.Index}. Expected {token.FunctionInfo.ArgumentsCount}, available {count}.", (int)token.Index, token.Length));
        }

        int argCount = token.FunctionInfo.ArgumentsCount;
        Value[]? pooledArray = null;
        Span<Value> functionParams = default;

        if (argCount > 0) {
            pooledArray = System.Buffers.ArrayPool<Value>.Shared.Rent(argCount);
            functionParams = pooledArray.AsSpan(0, argCount);

            for (int i = argCount - 1; i >= 0; i--) {
                functionParams[i] = stack[--count];
            }
        }

        try {
            var fresult = extensionModule.ExecuteFunction(keyString, functionParams);
            stack[count++] = fresult;
            return OpResult<Value>.Ok(default);
        }
        catch (Exception ex) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.InvalidFunctionArgument, ex.Message, (int)token.Index, token.Length));
        }
        finally {
            if (pooledArray != null) {
                System.Buffers.ArrayPool<Value>.Shared.Return(pooledArray);
            }
        }
    }

    private static OpResult<Value> ProcessUnaryOperator(Token token, Value[] stack, ref int count) {
        if (count < 1) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.MissingOperand, $"Stack size mismatch for Unary Operator '{token.KeyString}' at position {token.Index}. Expected 1, available {count}.", (int)token.Index, token.Length));
        }

        var operand = stack[--count];
        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        if (!result.Success) return result;

        stack[count++] = result.Value;
        return OpResult<Value>.Ok(default);
    }

    private OpResult<Value> ProcessOperator(Token token, Value[] stack, ref int count) {
        var keySpan = token.KeySpan;
        if (count < 2) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.MissingOperand, $"Stack size mismatch for Operator '{token.KeyString}' at position {token.Index}. Expected {2}, available {count}.", (int)token.Index, token.Length));
        }

        if (!SpanLookup.TryGetValue(m_globalScope.Operators, keySpan, out var op)) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.UnknownToken, $"Unknown Operator '{token.KeyString}' at position {token.Index}.", (int)token.Index, token.Length));
        }

        var operand2 = stack[--count];
        var operand1 = stack[--count];

        if (!op.ValidDataTypes.HasFlag(operand1.DataType)) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.TypeMismatch, $"Unexpected operand of data type {operand1.DataType} on token '{token.KeyString}' of type {token.Type} at position {token.Index}", (int)token.Index, token.Length));
        }

        if (!op.ValidDataTypes.HasFlag(operand2.DataType)) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.TypeMismatch, $"Unexpected operand of data type {operand2.DataType} on token '{token.KeyString}' of type {token.Type} at position {token.Index}", (int)token.Index, token.Length));
        }

        var result = PerformOperation(operand1, operand2, token, op);
        if (!result.Success) return result;

        stack[count++] = result.Value;
        return OpResult<Value>.Ok(default);
    }

    internal OpResult<Value> ProcessTernary(Token token, Value[] stack, ref int count, IDictionary<string, Value>? contextVariables) {
        if (count < 1) {
            return OpResult<Value>.Fail(new CalcError(ErrorCode.MissingOperand, $"Stack size mismatch for Ternary Operator '{token.KeyString}' at position {token.Index}. Expected >= {1}, available {count}.", (int)token.Index, token.Length));
        }

        if (token.OperatorKind is OperatorKind.And or OperatorKind.Or) {
            return ProcessShortCircuit(token, stack, ref count, contextVariables);
        }

        var operand1 = stack[--count];

        if (operand1.DataType != DataType.Boolean || operand1.Bvalue == null) {
            var message = $"Operand mismatch for Ternary Operator '{token.KeyString}' at position {token.Index}.";
            if (operand1.DataType != DataType.Boolean) message += " Operand must be of Boolean DataType.";
            if (operand1.Bvalue == null) message += " Operand must have a value set.";
            return OpResult<Value>.Fail(new CalcError(ErrorCode.TypeMismatch, message, (int)token.Index, token.Length));
        }

        if (token.TernaryBranches == null || token.TernaryBranches.TrueBranch == null || token.TernaryBranches.FalseBranch == null) {
            var message = $"Ternary Branches not set for Ternary Operator '{token.KeyString}' at position {token.Index}.";
            if (token.TernaryBranches != null && token.TernaryBranches.TrueBranch == null) message += " Left Branch not present.";
            if (token.TernaryBranches != null && token.TernaryBranches.FalseBranch == null) message += " Right Branch not present.";
            return OpResult<Value>.Fail(new CalcError(ErrorCode.TernaryBranchError, message, (int)token.Index, token.Length));
        }

        var branchResult = EvaluateInternal(operand1.Bvalue == true ? token.TernaryBranches.TrueBranch : token.TernaryBranches.FalseBranch, contextVariables);

        if (!branchResult.Success) {
            return OpResult<Value>.Fail(branchResult.Message, branchResult.Errors);
        }

        stack[count++] = branchResult.Value;
        return OpResult<Value>.Ok(default);
    }

    private OpResult<Value> ProcessShortCircuit(Token token, Value[] stack, ref int count, IDictionary<string, Value>? contextVariables) {
        var left = stack[--count];
        if (left.DataType != DataType.Boolean || left.Bvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.Error(ErrorCode.TypeMismatch, $"Left operand of '{token.KeyString}' must be Boolean, got {left.DataType} at position {token.Index}.", token));
        }

        var isAnd = token.OperatorKind == OperatorKind.And;
        if (left.Bvalue.Value != isAnd) {
            stack[count++] = left;
            return OpResult<Value>.Ok(default);
        }

        var right = EvaluateInternal(token.TernaryBranches!.TrueBranch, contextVariables);
        if (!right.Success) return right;

        if (right.Value.DataType != DataType.Boolean || right.Value.Bvalue == null) {
            return OpResult<Value>.Fail(OperationHelper.Error(ErrorCode.TypeMismatch, $"Right operand of '{token.KeyString}' must be Boolean, got {right.Value.DataType} at position {token.Index}.", token));
        }

        stack[count++] = right.Value;
        return OpResult<Value>.Ok(default);
    }

    internal static OpResult<Value> PerformOperation(in Value leftOperand, in Value rightOperand, Token token, Operator op) {
        return op.Category switch {
            Category.Arithmetic => PerformArithmetic(leftOperand, rightOperand, token),
            Category.Comparison => PerformComparison(leftOperand, rightOperand, token),
            Category.Logical => LogicalOperations.PerformLogicalOperationOnBooleans(leftOperand, rightOperand, token),
            Category.Bitwise => BitwiseOperations.PerformBitwiseOperationOnNumbers(leftOperand, rightOperand, token),
            _ => OpResult<Value>.Fail(OperationHelper.NotSupported(token, leftOperand.DataType))
        };
    }

    private static OpResult<Value> PerformArithmetic(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType == DataType.String || rightOperand.DataType == DataType.String) {
            return ArithmeticOperations.PerformArithmeticOperationOnStrings(leftOperand, rightOperand, token);
        }
        return ArithmeticOperations.PerformArithmeticOperationOnNumbers(leftOperand, rightOperand, token);
    }

    private static OpResult<Value> PerformComparison(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType == DataType.String && rightOperand.DataType == DataType.String) {
            return ComparisonOperations.PerformComparisonOperationOnStrings(leftOperand, rightOperand, token);
        }
        if (leftOperand.DataType == DataType.Boolean && rightOperand.DataType == DataType.Boolean) {
            return ComparisonOperations.PerformComparisonOperationOnBooleans(leftOperand, rightOperand, token);
        }
        if (leftOperand.DataType == DataType.Number && rightOperand.DataType == DataType.Number) {
            return ComparisonOperations.PerformComparisonOperationOnNumbers(leftOperand, rightOperand, token);
        }
        if (leftOperand.DataType == DataType.Date && rightOperand.DataType == DataType.Date) {
            return ComparisonOperations.PerformComparisonOperationOnDates(leftOperand, rightOperand, token);
        }
        return OpResult<Value>.Fail(OperationHelper.TypeMismatch(leftOperand, rightOperand, token));
    }
    #endregion Private Methods
}
