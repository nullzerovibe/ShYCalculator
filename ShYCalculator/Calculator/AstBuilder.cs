using System.Text.Json.Serialization;
using ShYCalculator.Classes;
using ShYCalculator.Calculator.Operations;
using System.Buffers;
using System.Collections.Frozen;
using System.Globalization;

namespace ShYCalculator.Calculator;

/// <summary>
/// Responsible for constructing an Abstract Syntax Tree (AST) from RPN tokens.
/// </summary>
internal class AstBuilder(IGlobalScope globalScope) {
    private readonly IGlobalScope m_globalScope = globalScope;

    /// <summary>
    /// Builds the AST from the provided RPN tokens.
    /// </summary>
    /// <param name="rpnTokens">The sequence of RPN tokens.</param>
    /// <param name="contextVariables">Optional dictionary of variables for evaluation context.</param>
    /// <returns>The root node of the AST.</returns>
    public AstNode Build(IEnumerable<Token> rpnTokens, IDictionary<string, Value>? contextVariables) {
        var stack = new Stack<AstNode>();

        foreach (var token in rpnTokens) {
            ProcessToken(token, stack, contextVariables);
        }

        if (stack.Count != 1) {
            // Should not happen if RPN is valid, but return a dummy error node if it does
            return new AstNode { Type = AstNodeType.Error, EvaluatedValue = "Invalid AST: Stack count " + stack.Count };
        }

        return stack.Pop();
    }

    /// <summary>
    /// Processes a single RPN token and updates the AST stack.
    /// </summary>
    /// <param name="token">The token to process.</param>
    /// <param name="stack">The current AST node stack.</param>
    /// <param name="contextVariables">Variables for evaluation context.</param>
    private void ProcessToken(Token token, Stack<AstNode> stack, IDictionary<string, Value>? contextVariables) {
        switch (token.Type) {
            case TokenType.Number:
                ProcessNumber(token, stack);
                break;
            case TokenType.String:
                ProcessString(token, stack);
                break;
            case TokenType.Constant:
                ProcessConstant(token, stack);
                break;
            case TokenType.Variable:
                ProcessVariable(token, stack, contextVariables);
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
            case TokenType.Ternary:
                ProcessTernary(token, stack, contextVariables);
                break;
        }
    }

    private void ProcessNumber(Token token, Stack<AstNode> stack) {
        double val = 0;
        double.TryParse(token.KeySpan, NumberFormatInfo.InvariantInfo, out val);
        stack.Push(new AstNode {
            Type = AstNodeType.Number,
            Value = val,
            EvaluatedValue = val,
            Range = new RangeInfo { Start = (int)token.Index, End = (int)token.Index + token.Length }
        });
    }

    private void ProcessString(Token token, Stack<AstNode> stack) {
        var val = token.KeyString;
        stack.Push(new AstNode {
            Type = AstNodeType.String,
            Value = val,
            EvaluatedValue = val,
            Range = new RangeInfo { Start = (int)token.Index, End = (int)token.Index + token.Length }
        });
    }

    private void ProcessConstant(Token token, Stack<AstNode> stack) {
        object? evalVal = null;
        if (m_globalScope.Constants.TryGetValue(token.KeyString, out var val)) {
            evalVal = GetObjectValue(val);
        }

        stack.Push(new AstNode {
            Type = AstNodeType.Constant,
            Name = token.KeyString,
            EvaluatedValue = evalVal,
            Range = new RangeInfo { Start = (int)token.Index, End = (int)token.Index + token.Length }
        });
    }

    private void ProcessVariable(Token token, Stack<AstNode> stack, IDictionary<string, Value>? contextVariables) {
        object? evalVal = null;
        if (contextVariables != null && contextVariables.TryGetValue(token.KeyString, out var val)) {
            evalVal = GetObjectValue(val);
        }

        stack.Push(new AstNode {
            Type = AstNodeType.Variable,
            Name = token.KeyString,
            EvaluatedValue = evalVal,
            Range = new RangeInfo { Start = (int)token.Index, End = (int)token.Index + token.Length }
        });
    }

    private void ProcessOperator(Token token, Stack<AstNode> stack) {
        if (stack.Count < 2) return; 

        var right = stack.Pop();
        var left = stack.Pop();

        // Calculate evaluated value
        object? evalVal = null;
        if (left.EvaluatedValue != null && right.EvaluatedValue != null) {
            var vLeft = CreateValueFromObject(left.EvaluatedValue);
            var vRight = CreateValueFromObject(right.EvaluatedValue);
            
            // Need to lookup operator to get details
            if (m_globalScope.Operators.TryGetValue(token.KeyString, out var op)) {
                 var res = PerformOperation(vLeft, vRight, token, op);
                 if (res.Success) evalVal = GetObjectValue(res.Value);
            }
        }

        stack.Push(new AstNode {
            Type = AstNodeType.Binary,
            Operator = token.KeyString,
            Left = left,
            Right = right,
            EvaluatedValue = evalVal,
            Range = new RangeInfo { Start = left.Range.Start, End = right.Range.End } 
        });
    }

    private void ProcessUnaryOperator(Token token, Stack<AstNode> stack) {
         if (stack.Count < 1) return;

         var operand = stack.Pop();
         
         object? evalVal = null;
         if (operand.EvaluatedValue != null) {
             var vOp = CreateValueFromObject(operand.EvaluatedValue);
             var res = UnaryOperations.PerformUnaryOperation(vOp, token);
             if (res.Success) evalVal = GetObjectValue(res.Value);
         }

         // Unary usually wraps the operand. Range might be tricky (prefix vs postfix).
         // Simplified range calculation.
         int start = Math.Min((int)token.Index, operand.Range.Start);
         int end = Math.Max((int)token.Index + token.Length, operand.Range.End);

         stack.Push(new AstNode {
             Type = AstNodeType.Unary,
             Operator = token.KeyString,
             Left = operand, // Using Left as the operand
             EvaluatedValue = evalVal,
             Range = new RangeInfo { Start = start, End = end }
         });
    }

    private void ProcessFunction(Token token, Stack<AstNode> stack) {
        int argCount = token.FunctionInfo?.ArgumentsCount ?? 0;
        var args = new List<AstNode>(argCount);
        
        for (int i = 0; i < argCount; i++) {
            if (stack.Count > 0) args.Add(stack.Pop());
        }
        args.Reverse();

        object? evalVal = null;
        // Evaluate if all args have values
        if (args.All(a => a.EvaluatedValue != null)) {
            if (m_globalScope.Functions.TryGetValue(token.KeyString, out var funcModule)) {
                 var processedArgs = args.Select(a => CreateValueFromObject(a.EvaluatedValue!)).ToArray();
                 try {
                    var res = funcModule.ExecuteFunction(token.KeyString, processedArgs);
                    evalVal = GetObjectValue(res);
                 } catch { /* ignore eval errors */ }
            }
        }
        
        int end = (int)token.Index + token.Length; 
        if (args.Count > 0) end = Math.Max(end, args.Last().Range.End + 1); // +1 for closing parenthesis roughly

        stack.Push(new AstNode {
             Type = AstNodeType.Function,
             Name = token.KeyString,
             Arguments = args,
             EvaluatedValue = evalVal,
             Range = new RangeInfo { Start = (int)token.Index, End = end }
        });
    }
    
    private void ProcessTernary(Token token, Stack<AstNode> stack, IDictionary<string, Value>? contextVariables) {
        if (stack.Count < 1) return;
        var condition = stack.Pop();

        // Recurse for branches
        // We act recursively here because Ternary branches are sub-expressions in RPN
        var trueBuilder = new AstBuilder(m_globalScope);
        var trueNode = trueBuilder.Build(token.TernaryBranches!.TrueBranch, contextVariables);

        var falseBuilder = new AstBuilder(m_globalScope);
        var falseNode = falseBuilder.Build(token.TernaryBranches!.FalseBranch, contextVariables);

        object? evalVal = null;
        if (condition.EvaluatedValue is bool bCond) {
            evalVal = bCond ? trueNode.EvaluatedValue : falseNode.EvaluatedValue;
        }

        stack.Push(new AstNode {
            Type = AstNodeType.Ternary,
            Condition = condition,
            TrueBranch = trueNode,
            FalseBranch = falseNode,
            EvaluatedValue = evalVal,
            Range = new RangeInfo { Start = condition.Range.Start, End = Math.Max(trueNode.Range.End, falseNode.Range.End) }
        });
    }

    // Helpers 
    
    private object? GetObjectValue(Value v) {
        if (v.DataType.HasFlag(DataType.Number)) return v.Nvalue;
        if (v.DataType.HasFlag(DataType.Boolean)) return v.Bvalue;
        if (v.DataType.HasFlag(DataType.String)) return v.Svalue;
        if (v.DataType.HasFlag(DataType.Date)) return v.Dvalue; // ISO string?
        return null;
    }

    private Value CreateValueFromObject(object obj) {
        if (obj is double d) return Value.Number(d);
        if (obj is bool b) return Value.Boolean(b);
        if (obj is string s) return Value.String(s);
        if (obj is DateTimeOffset dt) return Value.Date(dt);
        if (obj is int i) return Value.Number(i); // fallback
        return Value.Null(DataType.Number);
    }
    
    private static OpResult<Value> PerformOperation(in Value leftOperand, in Value rightOperand, Token token, Operator op) {
        return op.Category switch {
            Category.Arithmetic => PerformArithmetic(leftOperand, rightOperand, token),
            Category.Comparison => PerformComparison(leftOperand, rightOperand, token),
            Category.Logical => LogicalOperations.PerformLogicalOperationOnBooleans(leftOperand, rightOperand, token),
            Category.Bitwise => BitwiseOperations.PerformBitwiseOperationOnNumbers(leftOperand, rightOperand, token),
            _ => OpResult<Value>.Fail("Unknown category")
        };
    }
    
    private static OpResult<Value> PerformArithmetic(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType.HasFlag(DataType.String) || rightOperand.DataType.HasFlag(DataType.String)) {
            return ArithmeticOperations.PerformArithmeticOperationOnStrings(leftOperand, rightOperand, token);
        }
        return ArithmeticOperations.PerformArithmeticOperationOnNumbers(leftOperand, rightOperand, token);
    }
    
    private static OpResult<Value> PerformComparison(in Value leftOperand, in Value rightOperand, Token token) {
        if (leftOperand.DataType.HasFlag(DataType.String) && rightOperand.DataType.HasFlag(DataType.String)) {
            return ComparisonOperations.PerformComparisonOperationOnStrings(leftOperand, rightOperand, token);
        }
        if (leftOperand.DataType.HasFlag(DataType.Boolean) && rightOperand.DataType.HasFlag(DataType.Boolean)) {
             return ComparisonOperations.PerformComparisonOperationOnBooleans(leftOperand, rightOperand, token);
        }
        if (leftOperand.DataType.HasFlag(DataType.Number) && rightOperand.DataType.HasFlag(DataType.Number)) {
             return ComparisonOperations.PerformComparisonOperationOnNumbers(leftOperand, rightOperand, token);
        }
        return OpResult<Value>.Fail("Comparison mismatch");
    }
}
