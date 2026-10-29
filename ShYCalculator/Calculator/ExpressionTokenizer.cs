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
using ShY.Classes;
using ShY.Functions;

namespace ShY.Calculator;

internal class ExpressionTokenizer(IGlobalScope globalScope, ShYCalculatorOptions? options = null) : IExpressionTokenizer {
    #region Members
    private readonly IGlobalScope m_globalScope = globalScope;
    private readonly int m_maxLength = options?.MaxExpressionLength ?? 0;
    private static readonly ReadOnlyMemory<char> MinusOneMemory = "-1".AsMemory();
    private static readonly ReadOnlyMemory<char> MultiplyMemory = "*".AsMemory();

    #endregion Members

    #region Methods
    public OpResult<IEnumerable<Token>> Tokenize(string expression) {
        try {
            if (string.IsNullOrWhiteSpace(expression)) {
                return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.InvalidExpression, "Expression is empty or null"));
            }

            if (m_maxLength > 0 && expression.Length > m_maxLength) {
                return OpResult<IEnumerable<Token>>.Fail(new CalcError(ErrorCode.LimitExceeded, $"Expression length {expression.Length} exceeds the configured maximum of {m_maxLength} characters", m_maxLength, expression.Length - m_maxLength));
            }

            // WHY: whitespace characters are tokens too, so token count tracks character count closely.
            var tokens = new List<Token>(expression.Length);
            var i = 0;
            var expressionMemory = expression.AsMemory();

            while (i < expression.Length) {
                if (TryReadWhiteSpace(expressionMemory, ref i, tokens)) continue;
                if (TryReadNumber(expressionMemory, ref i, tokens)) continue;
                if (TryReadString(expressionMemory, ref i, tokens, out var stringError)) continue;
                if (stringError != null) return OpResult<IEnumerable<Token>>.Fail(stringError);
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
        var numberMemory = expression.Slice(startIndex, length);
        var parsed = double.TryParse(numberMemory.Span, System.Globalization.NumberFormatInfo.InvariantInfo, out var number);
        tokens.Add(new Token(startIndex, numberMemory, TokenType.Number) { ParsedNumber = number, HasParsedNumber = parsed });
        return true;
    }

    private static bool TryReadString(ReadOnlyMemory<char> expression, ref int i, List<Token> tokens, out CalcError? error) {
        error = null;
        var span = expression.Span;
        if (!IsStringStartToken(span[i])) return false;

        var quote = span[i];
        var startIndex = i;
        var contentStart = startIndex + 1;
        var hasEscapes = false;
        var end = contentStart;

        while (end < span.Length && span[end] != quote) {
            if (span[end] == '\\') {
                hasEscapes = true;
                end++;
            }
            end++;
        }

        if (end >= span.Length) {
            error = new CalcError(ErrorCode.UnterminatedString, $"Unterminated string literal starting at position {startIndex}", startIndex, span.Length - startIndex);
            return false;
        }

        var content = expression[contentStart..end];
        if (hasEscapes) {
            if (!TryUnescape(content.Span, contentStart, out var unescaped, out error)) return false;
            content = unescaped.AsMemory();
        }

        tokens.Add(new Token(startIndex, content, TokenType.String) { Length = end + 1 - startIndex });
        i = end + 1;
        return true;
    }

    private static bool TryUnescape(ReadOnlySpan<char> content, int offset, out string result, out CalcError? error) {
        var builder = new System.Text.StringBuilder(content.Length);
        for (var k = 0; k < content.Length; k++) {
            var ch = content[k];
            if (ch != '\\') {
                builder.Append(ch);
                continue;
            }

            var escapeIndex = k;
            var next = content[++k];
            switch (next) {
                case '\\' or '"' or '\'' or '`': builder.Append(next); break;
                case 'n': builder.Append('\n'); break;
                case 'r': builder.Append('\r'); break;
                case 't': builder.Append('\t'); break;
                case '0': builder.Append('\0'); break;
                case 'u' when k + 4 < content.Length && ushort.TryParse(content.Slice(k + 1, 4), System.Globalization.NumberStyles.AllowHexSpecifier, null, out var code):
                    builder.Append((char)code);
                    k += 4;
                    break;
                default:
                    result = "";
                    var length = next == 'u' ? Math.Min(6, content.Length - escapeIndex) : 2;
                    error = new CalcError(ErrorCode.InvalidSyntax, $"Invalid escape sequence '{content.Slice(escapeIndex, length)}' at position {offset + escapeIndex}", offset + escapeIndex, length);
                    return false;
            }
        }

        result = builder.ToString();
        error = null;
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

        if (SpanLookup.TryGetValue(m_globalScope.Constants, tokenSpan, out _)) {
            tokens.Add(new Token(startIndex, tokenMemory, TokenType.Constant));
            i = tempIndex;
            return true;
        }

        if (TryGetFunctionName(tokenSpan, out var functionName)) {
            // WHY: a string-backed key makes Token.KeyString allocation-free on every later evaluation.
            tokens.Add(new Token(startIndex, functionName.AsMemory(), TokenType.Function, functionInfo: new FunctionInfo()));
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

    private bool TryGetFunctionName(ReadOnlySpan<char> name, out string functionName) {
        if (SpanLookup.TryGetKey(m_globalScope.Functions, name, out var registeredName)) {
            functionName = name.SequenceEqual(registeredName) ? registeredName : name.ToString();
            return true;
        }

        functionName = "";
        return false;
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
            tokens.Add(new Token(startIndex, MinusOneMemory, TokenType.Number, negation: true) { ParsedNumber = -1, HasParsedNumber = true });
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
