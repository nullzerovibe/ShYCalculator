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
using ShYCalculator.Classes;
using System.Collections.Frozen;

namespace ShYCalculator.Calculator;

internal static class GeneratorExtensions {
    public static int ToInt(this long value) => (int)value;
}

internal class ShuntingYardGenerator(IGlobalScope globalScope) : IShuntingYardGenerator {
    #region Members
    private readonly IGlobalScope m_globalScope = globalScope;

    #endregion Members
    #region Constructor
    #endregion Constructor

    #region Methods
    public OpResult<IEnumerable<Token>> Generate(IEnumerable<Token> expressionTokens) {
        try {
            var expressionTokenPosition = 0;
            var result = GenerateRecursive(expressionTokens, ref expressionTokenPosition);
            
            if (result.Success) {
                // ToList() is needed if GenerateRecursive returns IEnumerable that isn't a list, 
                // but implementation uses List<Token> for rpnOutput so checking cast is better.
                var rpnList = result.Value as IList<Token> ?? result.Value!.ToList();
                
                var error = ValidateRPN(rpnList);
                if (error != null) {
                    return OpResult<IEnumerable<Token>>.Fail(error);
                }
            }

            return result;
        }
        catch (ShuntingYardGeneratorException ex) {
            return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.UnexpectedException, "Unexpected exception while generating RPN. " + ex.Message));
        }
    }

    private OpResult<IEnumerable<Token>> GenerateRecursive(IEnumerable<Token> expressionTokens, ref int expressionTokenPosition, string[]? breakTokens = null, int ternaryDepth = 0) {
        var currentFuncDepth = 0;
        var funcStack = new Dictionary<int, Token>();

        var rpnOutput = new List<Token>(expressionTokens is ICollection<Token> coll ? coll.Count : 32);
        var operatorStack = new Stack<Token>();
        var stop = false;

        var tokensList = expressionTokens as IList<Token> ?? [.. expressionTokens];

        while (expressionTokenPosition < tokensList.Count && !stop) {
            var expressionToken = tokensList[expressionTokenPosition];
            var keySpan = expressionToken.KeySpan;

            // Stop conditions for recursive calls (Ternary Operator)
            if (breakTokens != null && expressionToken.Type != TokenType.Operator) {
                // Ensure we don't break if we are inside parentheses
                bool insideParentheses = operatorStack.Any(t => t.Type == TokenType.OpeningParenthesis);

                if (!insideParentheses) {
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
                    while (operatorStack.Count != 0 && operatorStack.Peek().Type != TokenType.OpeningParenthesis) {
                        rpnOutput.Add(operatorStack.Pop());
                    }

                    expressionTokenPosition++;
                    var trueBranchResult = GenerateRecursive(tokensList, ref expressionTokenPosition, [":"], ternaryDepth + 1);
                    if (!trueBranchResult.Success) return trueBranchResult;

                    if (expressionTokenPosition >= tokensList.Count || tokensList[expressionTokenPosition].OperatorKind != OperatorKind.TernaryBranch) {
                        return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, "Ternary operator missing ':'", expressionToken.Index.ToInt(), expressionToken.Length));
                    }

                    expressionTokenPosition++;
                    var falseBranchResult = GenerateRecursive(tokensList, ref expressionTokenPosition, breakTokens, ternaryDepth + 1);
                    if (!falseBranchResult.Success) return falseBranchResult;

                    expressionToken.FunctionInfo?.Depth = currentFuncDepth;
                    expressionToken.TernaryBranches!.TrueBranch = trueBranchResult.Value!;
                    expressionToken.TernaryBranches!.FalseBranch = falseBranchResult.Value!;

                    rpnOutput.Add(expressionToken);
                    expressionTokenPosition--;
                }
            }
            else if (expressionToken.Type == TokenType.Number || expressionToken.Type == TokenType.String || expressionToken.Type == TokenType.Constant || expressionToken.Type == TokenType.Variable) {
                // Check if it's a variable token followed by an OpeningParenthesis
                if (expressionToken.Type == TokenType.Variable && expressionTokenPosition + 1 < tokensList.Count && tokensList[expressionTokenPosition + 1].Type == TokenType.OpeningParenthesis) {
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
                    funcStack.TryAdd(currentFuncDepth, funcToken);
                }
                else {
                    AddFunctionArgsCount(funcStack, currentFuncDepth);
                    expressionToken.FunctionInfo?.Depth = currentFuncDepth;
                    rpnOutput.Add(expressionToken);
                }
            }
            else if (expressionToken.Type == TokenType.Function) {
                // Special handling for 'if' function to support short-circuiting (lazy evaluation)
                if (expressionToken.KeySpan.Equals("if", StringComparison.OrdinalIgnoreCase)) {
                    // Check if next token is opening parenthesis
                    if (expressionTokenPosition + 1 >= tokensList.Count || tokensList[expressionTokenPosition + 1].Type != TokenType.OpeningParenthesis) {
                        // Let it fail naturally as a variable/function if syntax is wrong
                    }
                    else {
                        // Parse: if ( condition , trueBranch , falseBranch )
                        // Mapping to Ternary: condition ? trueBranch : falseBranch

                        // 1. Consume 'if' and '('
                        expressionTokenPosition += 2;

                        // 2. Parse Condition
                        // We use a break token of ',' to stop when we hit the first comma
                        // Note: We use null for breakTokens because standard Comma/Parenthesis logic handles top-level stops correctly
                        // and we fixed the breakTokens check anyway. But passing explicit [","] is cleaner documentation of intent
                        // provided we trust the nesting check above.
                        var conditionResult = GenerateRecursive(tokensList, ref expressionTokenPosition, [","]);
                        if (!conditionResult.Success) return conditionResult;

                        // 3. Verify we stopped at ','
                        if (expressionTokenPosition >= tokensList.Count || tokensList[expressionTokenPosition].Type != TokenType.Comma) {
                            return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, "Invalid 'if' syntax: expected ',' after condition", expressionToken.Index.ToInt(), expressionToken.Length));
                        }
                        expressionTokenPosition++; // Consume ','

                        // 4. Parse True Branch
                        var trueResult = GenerateRecursive(tokensList, ref expressionTokenPosition, [","]);
                        if (!trueResult.Success) return trueResult;

                        // 5. Verify we stopped at ','
                        if (expressionTokenPosition >= tokensList.Count || tokensList[expressionTokenPosition].Type != TokenType.Comma) {
                            return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, "Invalid 'if' syntax: expected ',' after true branch", expressionToken.Index.ToInt(), expressionToken.Length));
                        }
                        expressionTokenPosition++; // Consume ','

                        // 6. Parse False Branch
                        // Break at ')'
                        var falseResult = GenerateRecursive(tokensList, ref expressionTokenPosition, [")"]);
                        if (!falseResult.Success) return falseResult;

                        // 7. Verify we stopped at ')'
                        if (expressionTokenPosition >= tokensList.Count || tokensList[expressionTokenPosition].Type != TokenType.ClosingParenthesis) {
                            return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, "Invalid 'if' syntax: expected ')' after false branch", expressionToken.Index.ToInt(), expressionToken.Length));
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

                        // Add Condition tokens first
                        rpnOutput.AddRange(conditionResult.Value!);
                        // Then the Ternary operator (which contains the branches)
                        rpnOutput.Add(ternaryToken);

                        expressionTokenPosition++; // Consume closing parenthesis ')'
                        continue; // Continue main loop
                    }
                }

                // Find next non-whitespace token
                var nextTokenIndex = expressionTokenPosition + 1;
                while (nextTokenIndex < tokensList.Count && tokensList[nextTokenIndex].Type == TokenType.WhiteSpace) {
                    nextTokenIndex++;
                }

                if (nextTokenIndex >= tokensList.Count || tokensList[nextTokenIndex].Type != TokenType.OpeningParenthesis) {
                     return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, $"Function '{expressionToken.KeyString}' must be followed by '('", (int)expressionToken.Index, expressionToken.Length));
                }

                AddFunctionArgsCount(funcStack, currentFuncDepth);

                currentFuncDepth++;
                if (expressionToken.FunctionInfo != null) {
                    expressionToken.FunctionInfo.Depth = currentFuncDepth;
                    expressionToken.FunctionInfo.AwaitNextArgument = true;
                }
                operatorStack.Push(expressionToken);
                funcStack.TryAdd(currentFuncDepth, expressionToken);
            }
            else if (expressionToken.Type == TokenType.Operator) {
                if (!expressionToken.Negation) {
                    while (operatorStack.Count != 0) {
                        var topToken = operatorStack.Peek();

                        // Prefix unary operators always have higher precedence and should be popped
                        if (topToken.Type == TokenType.UnaryPrefixOperator) {
                            rpnOutput.Add(operatorStack.Pop());
                            continue;
                        }

                        if (topToken.Type != TokenType.Operator) break;

                        var currentPrecedence = GetOperatorPrecedence(keySpan);
                        var nextPrecedence = GetOperatorPrecedence(topToken.KeySpan);
                        var nextAssociativity = GetOperatorAssociativity(topToken.KeySpan);

                        if (nextPrecedence > currentPrecedence || (nextPrecedence == currentPrecedence && nextAssociativity == Associativity.Left)) {
                            rpnOutput.Add(operatorStack.Pop());
                            continue;
                        }
                        break;
                    }
                }

                expressionToken.FunctionInfo?.Depth = currentFuncDepth;
                operatorStack.Push(expressionToken);
            }
            else if (expressionToken.Type == TokenType.UnaryPostfixOperator) {
                // Postfix unary (e.g., factorial): output immediately after value
                expressionToken.FunctionInfo?.Depth = currentFuncDepth;
                rpnOutput.Add(expressionToken);
            }
            else if (expressionToken.Type == TokenType.UnaryPrefixOperator) {
                // Prefix unary (e.g., NOT): push to operator stack with high precedence
                expressionToken.FunctionInfo?.Depth = currentFuncDepth;
                operatorStack.Push(expressionToken);
            }
            else if (expressionToken.Type == TokenType.OpeningParenthesis) {
                expressionToken.FunctionInfo?.Depth = currentFuncDepth;
                operatorStack.Push(expressionToken);
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

                if (operatorStack.Count != 0 && operatorStack.Peek().Type == TokenType.Function) {
                    rpnOutput.Add(operatorStack.Pop());
                }

                if (operatorStack.Count == 0 || (operatorStack.Peek().FunctionInfo != null && operatorStack.Peek().FunctionInfo!.Depth != currentFuncDepth)) {
                    if (funcStack.TryGetValue(currentFuncDepth, out var stackFunction)) {
                        if (stackFunction.FunctionInfo!.AwaitNextArgument && stackFunction.FunctionInfo.ArgumentsCount > 0) {
                             return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidSyntax, $"Trailing comma or missing argument in function '{stackFunction.KeyString}'", (int)expressionToken.Index, expressionToken.Length));
                        }
                        
                        stackFunction.FunctionInfo!.AwaitNextArgument = false;
                        funcStack.Remove(currentFuncDepth);
                    }
                    currentFuncDepth--;
                }
            }
            else if (expressionToken.Type == TokenType.Comma) {
                if (operatorStack.Count == 0) {
                    stop = true;
                    continue;
                }

                if (funcStack.TryGetValue(currentFuncDepth, out var stackFunction)) {
                    stackFunction.FunctionInfo?.AwaitNextArgument = true;
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

    #region Private utils Methods
    private static void AddFunctionArgsCount(Dictionary<int, Token> functionsStack, int depth) {
        if (functionsStack.TryGetValue(depth, out var fnRpnItem) && fnRpnItem.FunctionInfo!.AwaitNextArgument) {
            fnRpnItem.FunctionInfo.ArgumentsCount += 1;
            fnRpnItem.FunctionInfo.AwaitNextArgument = false;
        }
    }

    private int GetOperatorPrecedence(ReadOnlySpan<char> ch) {
        if (m_globalScope.Operators is FrozenDictionary<string, Operator> frozen) {
            if (frozen.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(ch, out var op)) return op.Precedence;
        }
        else if (m_globalScope.Operators.TryGetValue(ch.ToString(), out var op)) {
            return op.Precedence;
        }

        throw new ShuntingYardGeneratorException($"GetOperatorPrecedence error for token '{ch}'");
    }

    private Associativity GetOperatorAssociativity(ReadOnlySpan<char> ch) {
        if (m_globalScope.Operators is FrozenDictionary<string, Operator> frozen) {
            if (frozen.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(ch, out var op)) return op.Associativity;
        }
        else if (m_globalScope.Operators.TryGetValue(ch.ToString(), out var op)) {
            return op.Associativity;
        }

        throw new ShuntingYardGeneratorException($"GetOperatorAssociativity error for token '{ch}'");
    }

    private CalcError? ValidateRPN(IList<Token> rpnTokens) {
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

