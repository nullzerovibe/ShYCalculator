// -----------------------------------------------------------------------------
// <summary>
//     Converts raw string expressions into a stream of Tokens.
//     Handles parsing of numbers, strings, operators, and function calls.
// 
//     Expression Tokenization Phase:
//     This phase involves breaking down the input infix expression into individual tokens.
//     Tokens can represent operators, operands, parentheses, or other elements of the infix expression.
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;
using System.Text.RegularExpressions;

namespace ShYCalculator.Calculator;

internal class ExpressionTokenizer(IGlobalScope globalScope) : IExpressionTokenizer {
    #region Members
    private readonly IGlobalScope m_globalScope = globalScope;
    private static readonly ReadOnlyMemory<char> MinusOneMemory = "-1".AsMemory();
    private static readonly ReadOnlyMemory<char> MultiplyMemory = "*".AsMemory();

    #endregion Members
    #region Constructor
    #endregion Constructor

    #region Methods
    public OpResult<IEnumerable<Token>> Tokenize(string expression) {
        try {
            if (string.IsNullOrWhiteSpace(expression)) {
                return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidExpression, "Expression is empty or null"));
            }

            var tokens = new List<Token>(expression.Length / 2); // Heuristic capacity
            var i = 0;
            var expressionMemory = expression.AsMemory();

            while (i < expression.Length) {
                if (TryReadWhiteSpace(expressionMemory, ref i, tokens)) continue;
                if (TryReadNumber(expressionMemory, ref i, tokens)) continue;
                if (TryReadString(expressionMemory, ref i, tokens)) continue;
                if (TryReadFunctionOrConstant(expressionMemory, ref i, tokens)) continue;
                if (TryReadVariable(expressionMemory, ref i, tokens)) continue;
                if (TryReadTernary(expressionMemory, ref i, tokens)) continue;  // Before operator to get correct TokenType
                if (TryReadOperator(expressionMemory, ref i, tokens)) continue;
                if (TryReadComma(expressionMemory, ref i, tokens)) continue;

                // Unknown token
                return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.UnknownToken, $"Unknown Token '{expression[i]}'", i, 1));
            }

            return OpResult<IEnumerable<Token>>.Ok(tokens);
        }
        catch (Exception ex) {
            return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.UnexpectedException, ex.Message));
        }
    }
    #endregion Methods

    #region Private Read Methods
    private static bool TryReadWhiteSpace(ReadOnlyMemory<char> expression, ref int i, List<Token> tokens) {
        var span = expression.Span;
        if (char.IsWhiteSpace(span[i])) {
            tokens.Add(new Token(i, expression.Slice(i, 1), TokenType.WhiteSpace));
            i++;
            return true;
        }
        return false;
    }

    private static bool TryReadNumber(ReadOnlyMemory<char> expression, ref int i, List<Token> tokens) {
        var span = expression.Span;
        if (!IsDigitToken(span[i])) return false;

        var startIndex = i;
        var hasDecimal = false;
        var hasExponent = false;

        while (i < expression.Length) {
            var currentChar = span[i];
            var prevChar = i - 1 >= 0 ? (char?)span[i - 1] : null;
            var nextChar = i + 1 < expression.Length ? (char?)span[i + 1] : null;

            if (IsDigitToken(currentChar)) {
                i++;
                continue;
            }

            if (currentChar == '.') {
                if (!hasDecimal && !hasExponent && IsDigitToken(prevChar) && IsDigitToken(nextChar)) {
                    hasDecimal = true;
                    i++;
                    continue;
                }
            }

            if (currentChar == 'E' || currentChar == 'e') {
                if (!hasExponent && IsDigitToken(prevChar) && (IsDigitToken(nextChar) || nextChar == '+' || nextChar == '-')) {
                    hasExponent = true;
                    i++;
                    continue;
                }
            }

            if (currentChar == '+' || currentChar == '-') {
                if ((prevChar == 'E' || prevChar == 'e') && IsDigitToken(nextChar)) {
                    i++;
                    continue;
                }
            }

            break;
        }

        var length = i - startIndex;
        tokens.Add(new Token(startIndex, expression.Slice(startIndex, length), TokenType.Number));
        return true;
    }

    private static bool TryReadString(ReadOnlyMemory<char> expression, ref int i, List<Token> tokens) {
        var span = expression.Span;
        if (!IsStringStartToken(span[i])) return false;

        var startChar = span[i];
        var startIndex = i;

        i++; // consume start char
        while (i < expression.Length) {
            var ch = span[i];
            var prevChar = i > startIndex ? span[i - 1] : (char?)null;

            if (IsStringEndToken(ch, startChar, prevChar)) {
                break;
            }

            i++;
        }

        if (i < expression.Length) {
            i++; // Consume end char
        }
        else {
            i = startIndex;
            return false;
        }

        var length = i - startIndex;
        var contentStart = startIndex + 1;
        var contentLength = length - 2;
        if (contentLength < 0) contentLength = 0;

        // Regex.Unescape still requires a string. This is a rare allocation for string literals.
        var stringTokenContent = expression.Slice(contentStart, contentLength).ToString();

        tokens.Add(new Token(startIndex, Regex.Unescape(stringTokenContent).AsMemory(), TokenType.String));
        return true;
    }

    private bool TryReadFunctionOrConstant(ReadOnlyMemory<char> expression, ref int i, List<Token> tokens) {
        var span = expression.Span;
        if (!IsConstantOrFunctionToken(span[i])) return false;

        var startIndex = i;
        var tempIndex = i;

        while (tempIndex < expression.Length && IsStillConstantOrFunctionToken(span[tempIndex])) {
            tempIndex++;
        }

        var length = tempIndex - startIndex;
        var tokenMemory = expression.Slice(startIndex, length);
        var tokenSpan = tokenMemory.Span;

        // Use AlternateLookup if available, otherwise fallback to ToString limited check
        if (m_globalScope.Constants is Dictionary<string, Value> constantsDict) {
            var lookup = constantsDict.GetAlternateLookup<ReadOnlySpan<char>>();
            if (lookup.TryGetValue(tokenSpan, out _)) {
                tokens.Add(new Token(startIndex, tokenMemory, TokenType.Constant));
                i = tempIndex;
                return true;
            }
        }
        else if (m_globalScope.Constants.ContainsKey(tokenMemory.ToString())) {
            tokens.Add(new Token(startIndex, tokenMemory, TokenType.Constant));
            i = tempIndex;
            return true;
        }

        if (m_globalScope.Functions.TryGetValue(tokenMemory.ToString(), out _)) {
            tokens.Add(new Token(startIndex, tokenMemory, TokenType.Function, functionInfo: new FunctionInfo()));
            i = tempIndex;
            return true;
        }

        // Explicitly handle 'if' as a function keyword, even if not registered, to support lazy-eval logic in Generator
        if (tokenSpan.Equals("if", StringComparison.OrdinalIgnoreCase)) {
            tokens.Add(new Token(startIndex, tokenMemory, TokenType.Function, functionInfo: new FunctionInfo()));
            i = tempIndex;
            return true;
        }

        // Fallback: Treat unknown identifiers as Variables
        tokens.Add(new Token(startIndex, tokenMemory, TokenType.Variable));
        i = tempIndex;
        return true;
    }

    private static bool TryReadVariable(ReadOnlyMemory<char> expression, ref int i, List<Token> tokens) {
        var span = expression.Span;
        if (!IsVariableToken(span[i])) return false;

        var startIndex = i;
        var tempIndex = i;

        while (tempIndex < expression.Length && IsStillVariableToken(span[tempIndex])) {
            tempIndex++;
        }

        var length = tempIndex - startIndex;
        var variableMemory = expression.Slice(startIndex, length);

        // Always treat as variable, existence check happens at evaluation
        tokens.Add(new Token(startIndex, variableMemory, TokenType.Variable));
        i = tempIndex;
        return true;
    }

    private static bool TryReadOperator(ReadOnlyMemory<char> expression, ref int i, List<Token> tokens) {
        var span = expression.Span;
        var ch = span[i];
        if (!IsOperatorToken(ch)) return false;

        var startIndex = i;
        var length = 1;

        // Check for 2-char operator
        if (IsStillOperatorToken(ch, GetNextChar(i, span))) {
            length = 2;
        }

        var operatorMemory = expression.Slice(startIndex, length);

        // Negative sign check (Unary minus)
        if (length == 1 && IsNegativeSignToken(ch, GetPreviousChar(startIndex, span, true))) {
            tokens.Add(new Token(startIndex, MinusOneMemory, TokenType.Number, negation: true));
            tokens.Add(new Token(startIndex, MultiplyMemory, TokenType.Operator, negation: true, functionInfo: new FunctionInfo(), operatorKind: OperatorKind.Mul));
            i++;
            return true;
        }

        // Unary ! disambiguation: NOT (prefix) vs Factorial (postfix)
        if (length == 1 && ch == '!') {
            var prevChar = GetPreviousChar(startIndex, span, skipWS: true);
            if (IsValuePrecedingChar(prevChar)) {
                // Postfix factorial: follows number, ), variable, constant
                tokens.Add(new Token(startIndex, operatorMemory, TokenType.UnaryPostfixOperator, operatorKind: OperatorKind.Factorial));
            }
            else {
                // Prefix NOT: follows operator, (, start, ?, :, ,
                tokens.Add(new Token(startIndex, operatorMemory, TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.Not));
            }
            i++;
            return true;
        }

        // Bitwise NOT (~) is always prefix
        if (length == 1 && ch == '~') {
            tokens.Add(new Token(startIndex, operatorMemory, TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.BitwiseNot));
            i++;
            return true;
        }

        // Square root (√) is always prefix
        if (length == 1 && ch == '√') {
            tokens.Add(new Token(startIndex, operatorMemory, TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.SquareRoot));
            i++;
            return true;
        }


        // Summation (∑) is a function-like prefix operator
        if (length == 1 && ch == '∑') {
            tokens.Add(new Token(startIndex, operatorMemory, TokenType.Function, functionInfo: new FunctionInfo()));
            i++;
            return true;
        }

        var tokenType = TokenType.Operator;
        var operatorKind = GetOperatorKind(span, startIndex, length);
        if (IsLeftParenthesisToken(ch)) tokenType = TokenType.OpeningParenthesis;
        if (IsRightParenthesisToken(ch)) tokenType = TokenType.ClosingParenthesis;

        tokens.Add(new Token(startIndex, operatorMemory, tokenType, functionInfo: new FunctionInfo(), operatorKind: operatorKind));
        i += length;
        return true;
    }

    private static OperatorKind GetOperatorKind(ReadOnlySpan<char> span, int start, int length) {
        if (length == 1) {
            return OperatorRegistry.SingleCharKinds.GetValueOrDefault(span[start], OperatorKind.None);
        }
        if (length == 2) {
            return OperatorRegistry.TwoCharKinds.GetValueOrDefault((span[start], span[start + 1]), OperatorKind.None);
        }
        return OperatorKind.None;
    }



    private static bool TryReadComma(ReadOnlyMemory<char> expression, ref int i, List<Token> tokens) {
        var span = expression.Span;
        if (IsCommaToken(span[i])) {
            tokens.Add(new Token(i, expression.Slice(i, 1), TokenType.Comma));
            i++;
            return true;
        }
        return false;
    }

    private static bool TryReadTernary(ReadOnlyMemory<char> expression, ref int i, List<Token> tokens) {
        var span = expression.Span;
        var ch = span[i];
        if (IsTernaryOperatorToken(ch)) {
            var operatorKind = ch == '?' ? OperatorKind.TernaryCondition : OperatorKind.TernaryBranch;
            tokens.Add(new Token(i, expression.Slice(i, 1), TokenType.Ternary, ternaryBranches: new TernaryBranches(), functionInfo: new FunctionInfo(), operatorKind: operatorKind));
            i++;
            return true;
        }
        return false;
    }
    #endregion Private Read Methods

    #region Helper Methods (Static where possible)
    private static bool IsDigitToken(char? ch) => ch != null && char.IsDigit(ch.Value);

    private static bool IsStringStartToken(char? ch) => ch == '"' || ch == '\'' || ch == '`';

    private static bool IsStringEndToken(char? ch, char startChar, char? prevChar) => prevChar != '\\' && startChar == ch;

    private static bool IsConstantOrFunctionToken(char? ch) => ch != null && char.IsLetter(ch.Value);

    private static bool IsStillConstantOrFunctionToken(char? ch) => IsConstantOrFunctionToken(ch) || IsDigitToken(ch) || ch == '_';

    private static bool IsVariableToken(char? ch) => ch == '$';

    private static bool IsStillVariableToken(char? ch) => IsConstantOrFunctionToken(ch) || IsDigitToken(ch) || ch == '$' || ch == '_' || ch == '.';

    private static bool IsOperatorToken(char ch) {
        // Use OperatorRegistry as single source of truth
        return OperatorRegistry.ValidFirstChars.Contains(ch);
    }

    private static bool IsStillOperatorToken(char prevChar, char? ch) {
        return OperatorRegistry.CanFormTwoCharOperator(prevChar, ch);
    }

    private static bool IsNegativeSignToken(char ch, char? prevChar) {
        // Note: '!' and '%' are excluded because postfix factorial/percent produce a value
        return ch == '-' && (prevChar == null || IsTernaryOperatorToken(prevChar) || IsCommaToken(prevChar) || (prevChar != ')' && prevChar != '!' && prevChar != '%' && IsOperatorToken(prevChar)));
    }

    private static bool IsTernaryOperatorToken(char? ch) => ch == '?' || ch == ':';
    private static bool IsCommaToken(char? ch) => ch == ',';
    private static bool IsLeftParenthesisToken(char? ch) => ch == '(';
    private static bool IsRightParenthesisToken(char? ch) => ch == ')';

    private static char? GetNextChar(int i, ReadOnlySpan<char> expression) {
        if (i + 1 < expression.Length) return expression[i + 1];
        return null;
    }

    private static char? GetPreviousChar(int i, ReadOnlySpan<char> expression, bool skipWS = false) {
        if (i <= 0) return null;
        while (i > 0) {
            var ch = expression[--i];
            if (!skipWS || !char.IsWhiteSpace(ch)) return ch;
        }
        return null;
    }

    private static bool IsOperatorToken(char? ch) {
        if (ch == null) return false;
        return OperatorRegistry.ValidFirstChars.Contains(ch.Value);
    }

    /// <summary>
    /// Determines if the previous character indicates a "value" token was just parsed.
    /// Used for distinguishing postfix operators (like factorial) from prefix operators (like NOT).
    /// </summary>
    private static bool IsValuePrecedingChar(char? ch) {
        if (ch == null) return false;
        char c = ch.Value;
        // Value-producing tokens end with: digit, letter (variable/constant/function), ), or closing quote
        return char.IsDigit(c) || char.IsLetter(c) || c == ')' || c == '"' || c == '\'' || c == '`';
    }
    #endregion Helper Methods
}
