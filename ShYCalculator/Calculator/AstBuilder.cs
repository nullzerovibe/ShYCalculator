using ShY.Classes;
using ShY.Calculator.Operations;
using System.Globalization;

namespace ShY.Calculator;

/// <summary>
/// Responsible for constructing an Abstract Syntax Tree (AST) from RPN tokens.
/// </summary>
/// <remarks>
/// Node values are best-effort annotations for tooling: a node whose evaluation fails is annotated with null.
/// The authoritative result of an expression always comes from <see cref="ShuntingYardParser"/>.
/// </remarks>
internal class AstBuilder(IGlobalScope globalScope, ShYCalculatorOptions? options = null) {
    private readonly IGlobalScope m_globalScope = globalScope;
    private readonly ShYCalculatorOptions? m_options = options;
    private readonly bool m_decimal = options?.NumericMode == NumericMode.Decimal;

    /// <summary>
    /// Builds the AST from the provided RPN tokens.
    /// </summary>
    /// <param name="rpnTokens">The sequence of RPN tokens.</param>
    /// <param name="contextVariables">Optional dictionary of variables for evaluation context.</param>
    /// <returns>The root node of the AST.</returns>
    public AstNode Build(IEnumerable<Token> rpnTokens, IDictionary<string, Value>? contextVariables) {
        System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
        var stack = new Stack<AstNode>();

        foreach (var token in rpnTokens) {
            ProcessToken(token, stack, contextVariables);
        }

        if (stack.Count != 1) {
            return new AstNode { Type = AstNodeType.Error, EvaluatedValue = "Invalid AST: Stack count " + stack.Count };
        }

        return stack.Pop();
    }

    private void ProcessToken(Token token, Stack<AstNode> stack, IDictionary<string, Value>? contextVariables) {
        switch (token.Type) {
            case TokenType.Number:
                object? number = m_decimal
                    ? (decimal.TryParse(token.KeySpan, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var exact) ? exact : null)
                    : (double.TryParse(token.KeySpan, NumberFormatInfo.InvariantInfo, out var approximate) ? approximate : 0d);
                stack.Push(Leaf(AstNodeType.Number, token, number, value: number));
                break;
            case TokenType.String:
                var text = token.KeyString;
                stack.Push(Leaf(AstNodeType.String, token, text, value: text));
                break;
            case TokenType.Constant:
                stack.Push(Leaf(AstNodeType.Constant, token, m_globalScope.Constants.TryGetValue(token.KeyString, out var constant) ? Annotate(constant) : null, name: token.KeyString));
                break;
            case TokenType.Variable:
                stack.Push(Leaf(AstNodeType.Variable, token, contextVariables != null && contextVariables.TryGetValue(token.KeyString, out var variable) ? Annotate(variable) : null, name: token.KeyString));
                break;
            case TokenType.Operator:
                ProcessOperator(token, stack);
                break;
            case TokenType.UnaryPrefixOperator:
            case TokenType.UnaryPostfixOperator:
                ProcessUnaryOperator(token, stack);
                break;
            case TokenType.Function:
                ProcessFunction(token, stack);
                break;
            case TokenType.Ternary when token.OperatorKind is OperatorKind.And or OperatorKind.Or:
                ProcessShortCircuit(token, stack, contextVariables);
                break;
            case TokenType.Ternary:
                ProcessTernary(token, stack, contextVariables);
                break;
        }
    }

    private static AstNode Leaf(string type, Token token, object? evaluated, object? value = null, string? name = null) => new() {
        Type = type,
        Value = value,
        Name = name,
        EvaluatedValue = evaluated,
        Range = new RangeInfo { Start = (int)token.Index, End = (int)token.Index + token.Length }
    };

    private void ProcessOperator(Token token, Stack<AstNode> stack) {
        if (stack.Count < 2) return;

        var right = stack.Pop();
        var left = stack.Pop();

        object? evaluated = null;
        if (left.EvaluatedValue != null && right.EvaluatedValue != null && m_globalScope.Operators.TryGetValue(token.KeyString, out var op)) {
            var result = ShuntingYardParser.PerformOperation(ToValue(left.EvaluatedValue), ToValue(right.EvaluatedValue), token, op);
            if (result.Success) evaluated = Annotate(result.Value);
        }

        stack.Push(Binary(token.KeyString, left, right, evaluated));
    }

    private static AstNode Binary(string op, AstNode left, AstNode right, object? evaluated) => new() {
        Type = AstNodeType.Binary,
        Operator = op,
        Left = left,
        Right = right,
        EvaluatedValue = evaluated,
        Range = new RangeInfo { Start = left.Range.Start, End = right.Range.End }
    };

    private void ProcessUnaryOperator(Token token, Stack<AstNode> stack) {
        if (stack.Count < 1) return;

        var operand = stack.Pop();

        object? evaluated = null;
        if (operand.EvaluatedValue != null) {
            var result = UnaryOperations.PerformUnaryOperation(ToValue(operand.EvaluatedValue), token);
            if (result.Success) evaluated = Annotate(result.Value);
        }

        stack.Push(new AstNode {
            Type = AstNodeType.Unary,
            Operator = token.KeyString,
            Left = operand,
            EvaluatedValue = evaluated,
            Range = new RangeInfo {
                Start = Math.Min((int)token.Index, operand.Range.Start),
                End = Math.Max((int)token.Index + token.Length, operand.Range.End)
            }
        });
    }

    private void ProcessFunction(Token token, Stack<AstNode> stack) {
        var argCount = Math.Min(token.FunctionInfo?.ArgumentsCount ?? 0, stack.Count);
        var args = new AstNode[argCount];
        for (var i = argCount - 1; i >= 0; i--) {
            args[i] = stack.Pop();
        }

        object? evaluated = null;
        if (Array.TrueForAll(args, a => a.EvaluatedValue != null) && m_globalScope.Functions.TryGetValue(token.KeyString, out var module)) {
            try {
                evaluated = Annotate(module.ExecuteFunction(token.KeyString, Array.ConvertAll(args, a => ToValue(a.EvaluatedValue!))));
            }
            catch (Exception) {
                // WHY: annotation is best-effort; the parser reports function errors authoritatively.
            }
        }

        var end = (int)token.Index + token.Length;
        if (argCount > 0) end = Math.Max(end, args[^1].Range.End + 1);

        stack.Push(new AstNode {
            Type = AstNodeType.Function,
            Name = token.KeyString,
            Arguments = [.. args],
            EvaluatedValue = evaluated,
            Range = new RangeInfo { Start = (int)token.Index, End = end }
        });
    }

    private void ProcessShortCircuit(Token token, Stack<AstNode> stack, IDictionary<string, Value>? contextVariables) {
        if (stack.Count < 1) return;

        var left = stack.Pop();
        var right = new AstBuilder(m_globalScope, m_options).Build(token.TernaryBranches!.TrueBranch, contextVariables);

        var isAnd = token.OperatorKind == OperatorKind.And;
        object? evaluated = left.EvaluatedValue switch {
            bool decided when decided != isAnd => decided,
            bool => right.EvaluatedValue as bool?,
            _ => null
        };

        stack.Push(Binary(token.KeyString, left, right, evaluated));
    }

    private void ProcessTernary(Token token, Stack<AstNode> stack, IDictionary<string, Value>? contextVariables) {
        if (stack.Count < 1) return;

        var condition = stack.Pop();
        var trueNode = new AstBuilder(m_globalScope, m_options).Build(token.TernaryBranches!.TrueBranch, contextVariables);
        var falseNode = new AstBuilder(m_globalScope, m_options).Build(token.TernaryBranches!.FalseBranch, contextVariables);

        stack.Push(new AstNode {
            Type = AstNodeType.Ternary,
            Condition = condition,
            TrueBranch = trueNode,
            FalseBranch = falseNode,
            EvaluatedValue = condition.EvaluatedValue is bool c ? (c ? trueNode.EvaluatedValue : falseNode.EvaluatedValue) : null,
            Range = new RangeInfo { Start = condition.Range.Start, End = Math.Max(trueNode.Range.End, falseNode.Range.End) }
        });
    }

    // WHY: mirrors the evaluator's decimal normalization so AST annotations match the computed result.
    private object? Annotate(Value value) {
        if (m_decimal && value.DataType == DataType.Number && !value.IsDecimal) {
            return value.Mvalue;
        }
        return ToObject(value);
    }

    internal static object? ToObject(Value v) => v.DataType switch {
        DataType.Number => v.IsDecimal ? v.Mvalue : v.Nvalue,
        DataType.Boolean => v.Bvalue,
        DataType.String => v.Svalue,
        DataType.Date => v.Dvalue,
        _ => null
    };

    private static Value ToValue(object obj) => obj switch {
        double d => Value.Number(d),
        decimal m => Value.Number(m),
        bool b => Value.Boolean(b),
        string s => Value.String(s),
        DateTimeOffset dt => Value.Date(dt),
        _ => Value.Null(DataType.Number)
    };
}
