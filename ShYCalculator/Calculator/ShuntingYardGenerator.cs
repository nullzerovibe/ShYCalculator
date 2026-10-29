// -----------------------------------------------------------------------------
// <summary>
//     Implements the Shunting-Yard algorithm to convert infix expressions to Reverse Polish Notation (RPN).
// 
//     Shunting Yard RPN Generator Phase:
//     This is the main phase of the algorithm, where the tokens are processed and rearranged according to their precedence and associativity rules.
//     This phase utilizes a stack data structure and an output queue to determine the correct order of the tokens.
// 
//     Based on Shunting yard algorithm: https://en.wikipedia.org/wiki/Shunting_yard_algorithm
//     Extensions:
//     - Support for negative numbers
//     - Functions with variable number of arguments
//     - Ternary operator
//     - Nested functions/ternary operators
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace ShY.Calculator;

internal class ShuntingYardGenerator(IGlobalScope globalScope, ShYCalculatorOptions? options = null) : IShuntingYardGenerator {
    #region Members
    private readonly IGlobalScope m_globalScope = globalScope;
    private readonly int m_maxDepth = options?.MaxNestingDepth ?? ShYCalculatorOptions.DefaultMaxNestingDepth;

    #endregion Members

    #region Methods
    public OpResult<IEnumerable<Token>> Generate(IEnumerable<Token> expressionTokens) {
        try {
            var expressionTokenPosition = 0;
            var tokensList = expressionTokens as IList<Token> ?? [.. expressionTokens];
            var result = GenerateRecursive(tokensList, ref expressionTokenPosition);

            // WHY: the recursive pass stops at an unmatched ')' or top-level ','; anything left over would otherwise be silently ignored.
            if (result.Success && expressionTokenPosition < tokensList.Count) {
                var leftover = tokensList[expressionTokenPosition];
                return leftover.Type == TokenType.ClosingParenthesis
                    ? OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.MismatchedParentheses, "Unexpected ')' without matching '('", (int)leftover.Index, leftover.Length))
                    : OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, $"Unexpected '{leftover.KeyString}'", (int)leftover.Index, leftover.Length));
            }

            if (result.Success) {
                // ToList() is needed if GenerateRecursive returns IEnumerable that isn't a list, 
                // but implementation uses List<Token> for rpnOutput so checking cast is better.
                var rpnList = result.Value as IList<Token> ?? result.Value!.ToList();
                
                var error = ValidateRPN(rpnList);
                if (error != null) {
                    return OpResult<IEnumerable<Token>>.Fail(error);
                }

                var lowered = LowerShortCircuit(rpnList);
                if (m_maxDepth > 0 && FindTooDeep(lowered, 0, m_maxDepth) is { } tooDeep) {
                    return OpResult<IEnumerable<Token>>.Fail(NestingLimitError(tooDeep));
                }

                return OpResult<IEnumerable<Token>>.Ok(lowered);
            }

            return result;
        }
        catch (InsufficientExecutionStackException) {
            return OpResult<IEnumerable<Token>>.Fail(StackLimitError());
        }
        catch (ShuntingYardGeneratorException ex) {
            return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.UnexpectedException, "Unexpected exception while generating RPN. " + ex.Message));
        }
    }

    private OpResult<IEnumerable<Token>> GenerateRecursive(IEnumerable<Token> expressionTokens, ref int expressionTokenPosition, string[]? breakTokens = null, int depth = 0) {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        var currentFuncDepth = 0;
        Dictionary<int, Token>? funcStack = null;

        var tokensList = expressionTokens as IList<Token> ?? [.. expressionTokens];
        // WHY: branch recursions only consume a slice; sizing them to the whole expression over-allocates per branch.
        var rpnOutput = new List<Token>(breakTokens == null ? tokensList.Count : 8);
        var operatorStack = new Stack<Token>();
        var openParentheses = 0;
        var stop = false;

        while (expressionTokenPosition < tokensList.Count && !stop) {
            var expressionToken = tokensList[expressionTokenPosition];
            var keySpan = expressionToken.KeySpan;

            // Stop conditions for recursive calls (Ternary Operator)
            if (breakTokens != null && expressionToken.Type != TokenType.Operator) {
                if (openParentheses == 0) {
                    foreach (var bt in breakTokens) {
                        if (keySpan.SequenceEqual(bt.AsSpan())) {
                            stop = true;
                            break;
                        }
                    }
                    if (stop) continue;
                }
            }

            if (expressionToken.Type == TokenType.Ternary) {
                if (expressionToken.OperatorKind == OperatorKind.TernaryCondition) {
                    if (m_maxDepth > 0 && depth >= m_maxDepth) return OpResult<IEnumerable<Token>>.Fail(NestingLimitError(expressionToken));
                    while (operatorStack.Count != 0 && operatorStack.Peek().Type != TokenType.OpeningParenthesis) {
                        rpnOutput.Add(operatorStack.Pop());
                    }

                    expressionTokenPosition++;
                    var trueBranchResult = GenerateRecursive(tokensList, ref expressionTokenPosition, [":"], depth + 1);
                    if (!trueBranchResult.Success) return trueBranchResult;

                    if (expressionTokenPosition >= tokensList.Count || tokensList[expressionTokenPosition].OperatorKind != OperatorKind.TernaryBranch) {
                        return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, "Ternary operator missing ':'", (int)expressionToken.Index, expressionToken.Length));
                    }

                    expressionTokenPosition++;
                    var falseBranchResult = GenerateRecursive(tokensList, ref expressionTokenPosition, breakTokens, depth + 1);
                    if (!falseBranchResult.Success) return falseBranchResult;

                    if (expressionToken.FunctionInfo != null) expressionToken.FunctionInfo.Depth = currentFuncDepth;
                    expressionToken.TernaryBranches!.TrueBranch = trueBranchResult.Value!;
                    expressionToken.TernaryBranches!.FalseBranch = falseBranchResult.Value!;

                    rpnOutput.Add(expressionToken);
                    expressionTokenPosition--;
                }
            }
            else if (expressionToken.Type == TokenType.Number || expressionToken.Type == TokenType.String || expressionToken.Type == TokenType.Constant || expressionToken.Type == TokenType.Variable) {
                // Check if it's a variable token followed by an OpeningParenthesis
                if (expressionToken.Type == TokenType.Variable && IsOpeningParenthesisAt(tokensList, NextNonWhiteSpace(tokensList, expressionTokenPosition + 1))) {
                    // Treat as Function
                    // Create new token as properties are init-only
                    var funcToken = new Token {
                        Type = TokenType.Function,
                        Key = expressionToken.Key,
                        FunctionInfo = new FunctionInfo {
                            AwaitNextArgument = true,
                            Depth = currentFuncDepth + 1
                        },
                        Index = expressionToken.Index,
                        Length = expressionToken.Length
                    };

                    AddFunctionArgsCount(funcStack, currentFuncDepth);

                    currentFuncDepth++;
                    
                    operatorStack.Push(funcToken);
                    (funcStack ??= []).TryAdd(currentFuncDepth, funcToken);
                }
                else {
                    AddFunctionArgsCount(funcStack, currentFuncDepth);
                    if (expressionToken.FunctionInfo != null) expressionToken.FunctionInfo.Depth = currentFuncDepth;
                    rpnOutput.Add(expressionToken);
                }
            }
            else if (expressionToken.Type == TokenType.Function) {
                if (expressionToken.KeySpan.Equals("if", StringComparison.OrdinalIgnoreCase)) {
                    var openIndex = NextNonWhiteSpace(tokensList, expressionTokenPosition + 1);
                    if (IsOpeningParenthesisAt(tokensList, openIndex)) {
                        if (m_maxDepth > 0 && depth >= m_maxDepth) return OpResult<IEnumerable<Token>>.Fail(NestingLimitError(expressionToken));
                        // WHY: if(c, t, f) is lowered to the lazy ternary c ? t : f so untaken branches are never evaluated.
                        expressionTokenPosition = openIndex + 1;

                        var conditionResult = GenerateRecursive(tokensList, ref expressionTokenPosition, [","], depth + 1);
                        if (!conditionResult.Success) return conditionResult;

                        if (expressionTokenPosition >= tokensList.Count || tokensList[expressionTokenPosition].Type != TokenType.Comma) {
                            return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, "Invalid 'if' syntax: expected ',' after condition", (int)expressionToken.Index, expressionToken.Length));
                        }
                        expressionTokenPosition++;

                        var trueResult = GenerateRecursive(tokensList, ref expressionTokenPosition, [","], depth + 1);
                        if (!trueResult.Success) return trueResult;

                        if (expressionTokenPosition >= tokensList.Count || tokensList[expressionTokenPosition].Type != TokenType.Comma) {
                            return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, "Invalid 'if' syntax: expected ',' after true branch", (int)expressionToken.Index, expressionToken.Length));
                        }
                        expressionTokenPosition++;

                        var falseResult = GenerateRecursive(tokensList, ref expressionTokenPosition, [")"], depth + 1);
                        if (!falseResult.Success) return falseResult;

                        if (expressionTokenPosition >= tokensList.Count || tokensList[expressionTokenPosition].Type != TokenType.ClosingParenthesis) {
                            return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, "Invalid 'if' syntax: expected ')' after false branch", (int)expressionToken.Index, expressionToken.Length));
                        }

                        var ternaryToken = new Token {
                            Type = TokenType.Ternary,
                            Key = "?",
                            OperatorKind = OperatorKind.TernaryCondition,
                            TernaryBranches = new TernaryBranches {
                                TrueBranch = trueResult.Value!,
                                FalseBranch = falseResult.Value!
                            },
                            Index = expressionToken.Index,
                            Length = expressionToken.Length
                        };

                        rpnOutput.AddRange(conditionResult.Value!);
                        rpnOutput.Add(ternaryToken);

                        expressionTokenPosition++;
                        continue;
                    }
                }

                if (!IsOpeningParenthesisAt(tokensList, NextNonWhiteSpace(tokensList, expressionTokenPosition + 1))) {
                     return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, $"Function '{expressionToken.KeyString}' must be followed by '('", (int)expressionToken.Index, expressionToken.Length));
                }

                AddFunctionArgsCount(funcStack, currentFuncDepth);

                currentFuncDepth++;
                if (expressionToken.FunctionInfo != null) {
                    expressionToken.FunctionInfo.Depth = currentFuncDepth;
                    expressionToken.FunctionInfo.AwaitNextArgument = true;
                }
                operatorStack.Push(expressionToken);
                (funcStack ??= []).TryAdd(currentFuncDepth, expressionToken);
            }
            else if (expressionToken.Type == TokenType.Operator) {
                if (!expressionToken.Negation) {
                    var currentPrecedence = GetOperatorPrecedence(keySpan);
                    while (operatorStack.Count != 0) {
                        var topToken = operatorStack.Peek();

                        // Prefix unary operators always have higher precedence and should be popped
                        if (topToken.Type == TokenType.UnaryPrefixOperator) {
                            rpnOutput.Add(operatorStack.Pop());
                            continue;
                        }

                        if (topToken.Type != TokenType.Operator) break;

                        var nextPrecedence = GetOperatorPrecedence(topToken.KeySpan);
                        var nextAssociativity = GetOperatorAssociativity(topToken.KeySpan);

                        if (nextPrecedence > currentPrecedence || (nextPrecedence == currentPrecedence && nextAssociativity == Associativity.Left)) {
                            rpnOutput.Add(operatorStack.Pop());
                            continue;
                        }
                        break;
                    }
                }

                if (expressionToken.FunctionInfo != null) expressionToken.FunctionInfo.Depth = currentFuncDepth;
                operatorStack.Push(expressionToken);
            }
            else if (expressionToken.Type == TokenType.UnaryPostfixOperator) {
                // Postfix unary (e.g., factorial): output immediately after value
                if (expressionToken.FunctionInfo != null) expressionToken.FunctionInfo.Depth = currentFuncDepth;
                rpnOutput.Add(expressionToken);
            }
            else if (expressionToken.Type == TokenType.UnaryPrefixOperator) {
                // Prefix unary (e.g., NOT): push to operator stack with high precedence
                if (expressionToken.FunctionInfo != null) expressionToken.FunctionInfo.Depth = currentFuncDepth;
                operatorStack.Push(expressionToken);
            }
            else if (expressionToken.Type == TokenType.OpeningParenthesis) {
                if (expressionToken.FunctionInfo != null) expressionToken.FunctionInfo.Depth = currentFuncDepth;
                operatorStack.Push(expressionToken);
                openParentheses++;
            }
            else if (expressionToken.Type == TokenType.ClosingParenthesis) {
                if (operatorStack.Count == 0) {
                    stop = true;
                    continue;
                }

                while (operatorStack.Count != 0 && operatorStack.Peek().Type != TokenType.OpeningParenthesis) {
                    rpnOutput.Add(operatorStack.Pop());
                }

                if (operatorStack.Count == 0) {
                    return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.MismatchedParentheses, "Parenthesis mismatch", (int)expressionToken.Index, expressionToken.Length));
                }

                operatorStack.Pop();
                openParentheses--;

                if (operatorStack.Count != 0 && operatorStack.Peek().Type == TokenType.Function) {
                    rpnOutput.Add(operatorStack.Pop());
                }

                if (operatorStack.Count == 0 || (operatorStack.Peek().FunctionInfo != null && operatorStack.Peek().FunctionInfo!.Depth != currentFuncDepth)) {
                    if (funcStack != null && funcStack.TryGetValue(currentFuncDepth, out var stackFunction)) {
                        if (stackFunction.FunctionInfo!.AwaitNextArgument && stackFunction.FunctionInfo.ArgumentsCount > 0) {
                             return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, $"Trailing comma or missing argument in function '{stackFunction.KeyString}'", (int)expressionToken.Index, expressionToken.Length));
                        }
                        
                        stackFunction.FunctionInfo!.AwaitNextArgument = false;
                        funcStack!.Remove(currentFuncDepth);
                    }
                    currentFuncDepth--;
                }
            }
            else if (expressionToken.Type == TokenType.Comma) {
                if (operatorStack.Count == 0) {
                    stop = true;
                    continue;
                }

                if (funcStack != null && funcStack.TryGetValue(currentFuncDepth, out var stackFunction)) {
                    if (stackFunction.FunctionInfo != null) stackFunction.FunctionInfo.AwaitNextArgument = true;
                }

                while (operatorStack.Count != 0 && operatorStack.Peek().Type != TokenType.OpeningParenthesis) {
                    rpnOutput.Add(operatorStack.Pop());
                }

                if (operatorStack.Count == 0) {
                    stop = true;
                    continue;
                }
            }

            if (!stop) {
                expressionTokenPosition++;
            }
        }

        while (operatorStack.Count != 0) {
            if (operatorStack.Peek().Type == TokenType.OpeningParenthesis) {
                return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.MismatchedParentheses, "Parenthesis mismatch in operatorStack", (int)operatorStack.Peek().Index, operatorStack.Peek().Length));
            }
            rpnOutput.Add(operatorStack.Pop());
        }

        return OpResult<IEnumerable<Token>>.Ok(rpnOutput);
    }
    #endregion Methods

    #region Short-circuit lowering
    // WHY: RPN evaluates both operands eagerly. Rewriting "L R &&" into "L Lazy&&[R]" lets the parser skip R,
    // reusing the lazy-branch mechanism of the ternary operator. Requires a validated RPN.
    private static IList<Token> LowerShortCircuit(IList<Token> rpn) {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        if (!NeedsLowering(rpn)) return rpn;

        var output = new List<Token>(rpn.Count);
        foreach (var token in rpn) {
            if (token.Type == TokenType.Ternary && token.TernaryBranches != null) {
                token.TernaryBranches.TrueBranch = LowerShortCircuit(AsList(token.TernaryBranches.TrueBranch));
                token.TernaryBranches.FalseBranch = LowerShortCircuit(AsList(token.TernaryBranches.FalseBranch));
            }

            if (token.Type != TokenType.Operator || token.OperatorKind is not (OperatorKind.And or OperatorKind.Or)) {
                output.Add(token);
                continue;
            }

            var rightStart = FindOperandStart(output);
            var right = output.GetRange(rightStart, output.Count - rightStart);
            output.RemoveRange(rightStart, right.Count);
            output.Add(token with {
                Type = TokenType.Ternary,
                TernaryBranches = new TernaryBranches { TrueBranch = right }
            });
        }
        return output;
    }

    private static bool NeedsLowering(IList<Token> rpn) {
        foreach (var token in rpn) {
            if (token.Type == TokenType.Ternary || token.OperatorKind is OperatorKind.And or OperatorKind.Or) return true;
        }
        return false;
    }

    // WHY: lazy '&&'/'||' nesting only becomes visible after lowering; bounding it here bounds every later recursion (parser, AST).
    private static Token? FindTooDeep(IList<Token> rpn, int depth, int maxDepth) {
        foreach (var token in rpn) {
            if (token.Type != TokenType.Ternary || token.TernaryBranches == null) continue;
            if (depth >= maxDepth) return token;

            var hit = FindTooDeep(AsList(token.TernaryBranches.TrueBranch), depth + 1, maxDepth)
                ?? FindTooDeep(AsList(token.TernaryBranches.FalseBranch), depth + 1, maxDepth);
            if (hit != null) return hit;
        }
        return null;
    }

    private CalcError NestingLimitError(Token token) =>
        new(ErrorCode.LimitExceeded, $"Nesting depth exceeds the configured maximum of {m_maxDepth} at position {token.Index}", (int)token.Index, token.Length);

    internal static CalcError StackLimitError() =>
        new(ErrorCode.LimitExceeded, "Expression is nested too deeply for the available execution stack");

    private static IList<Token> AsList(IEnumerable<Token> tokens) => tokens as IList<Token> ?? [.. tokens];

    private static int FindOperandStart(List<Token> rpn) {
        var produced = 0;
        for (var i = rpn.Count - 1; i >= 0; i--) {
            produced += StackEffect(rpn[i]);
            if (produced == 1) return i;
        }
        throw new ShuntingYardGeneratorException("Logical operator without right operand");
    }

    private static int StackEffect(Token token) => token.Type switch {
        TokenType.Number or TokenType.String or TokenType.Constant or TokenType.Variable => 1,
        TokenType.Operator => -1,
        TokenType.Function => 1 - (token.FunctionInfo?.ArgumentsCount ?? 0),
        _ => 0
    };
    #endregion

    #region Private utils Methods
    private static int NextNonWhiteSpace(IList<Token> tokens, int index) {
        while (index < tokens.Count && tokens[index].Type == TokenType.WhiteSpace) index++;
        return index;
    }

    private static bool IsOpeningParenthesisAt(IList<Token> tokens, int index) => index < tokens.Count && tokens[index].Type == TokenType.OpeningParenthesis;

    private static void AddFunctionArgsCount(Dictionary<int, Token>? functionsStack, int depth) {
        if (functionsStack != null && functionsStack.TryGetValue(depth, out var fnRpnItem) && fnRpnItem.FunctionInfo!.AwaitNextArgument) {
            fnRpnItem.FunctionInfo.ArgumentsCount += 1;
            fnRpnItem.FunctionInfo.AwaitNextArgument = false;
        }
    }

    private int GetOperatorPrecedence(ReadOnlySpan<char> ch) {
        if (SpanLookup.TryGetValue(m_globalScope.Operators, ch, out var op)) return op.Precedence;

        throw new ShuntingYardGeneratorException($"GetOperatorPrecedence error for token '{ch}'");
    }

    private Associativity GetOperatorAssociativity(ReadOnlySpan<char> ch) {
        if (SpanLookup.TryGetValue(m_globalScope.Operators, ch, out var op)) return op.Associativity;

        throw new ShuntingYardGeneratorException($"GetOperatorAssociativity error for token '{ch}'");
    }

    private CalcError? ValidateRPN(IList<Token> rpnTokens) {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        int stackDepth = 0;
        var count = rpnTokens.Count;
        
        for (int i = 0; i < count; i++) {
            var token = rpnTokens[i];
            switch (token.Type) {
                case TokenType.Number:
                case TokenType.String:
                case TokenType.Constant:
                case TokenType.Variable:
                    stackDepth++;
                    break;
                case TokenType.Operator:
                    // Binary
                    if (stackDepth < 2) return new CalcError(ErrorCode.MissingOperand, $"Missing operand for operator '{token.KeyString}'", (int)token.Index, token.Length);
                    stackDepth--; 
                    break;
                case TokenType.UnaryPrefixOperator:
                case TokenType.UnaryPostfixOperator:
                    if (stackDepth < 1) return new CalcError(ErrorCode.MissingOperand, $"Missing operand for unary operator '{token.KeyString}'", (int)token.Index, token.Length);
                    break;
                case TokenType.Function:
                    int args = token.FunctionInfo?.ArgumentsCount ?? 0;
                    if (stackDepth < args) return new CalcError(ErrorCode.InvalidSyntax, $"Missing arguments for function '{token.KeyString}'", (int)token.Index, token.Length);
                    stackDepth = stackDepth - args + 1;
                    break;
                case TokenType.Ternary:
                    if (stackDepth < 1) return new CalcError(ErrorCode.MissingOperand, "Missing condition for ternary operator", (int)token.Index, token.Length);
                    
                    if (token.TernaryBranches != null) {
                        var trueList = token.TernaryBranches.TrueBranch as IList<Token> ?? token.TernaryBranches.TrueBranch.ToList();
                        var trueRes = ValidateRPN(trueList);
                        if (trueRes != null) return trueRes;
                        
                        var falseList = token.TernaryBranches.FalseBranch as IList<Token> ?? token.TernaryBranches.FalseBranch.ToList();
                        var falseRes = ValidateRPN(falseList);
                        if (falseRes != null) return falseRes;
                    }
                    else {
                         return new CalcError(ErrorCode.TernaryBranchError, "Ternary operator branches missing", (int)token.Index, token.Length);
                    }
                    break;
            }
        }

        if (stackDepth != 1) {
            if (stackDepth == 0 && count == 0) return new CalcError(ErrorCode.InvalidSyntax, "Empty expression or branch", 0, 0);
            return new CalcError(ErrorCode.InvalidSyntax, "Invalid expression structure (incomplete or too many operands)", 0, 0);
        }
        return null; // Success
    }
    #endregion Private utils Methods
}

