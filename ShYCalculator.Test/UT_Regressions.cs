using ShY.Classes;

namespace ShY.Test;

[TestClass]
public class UT_Regressions {
    private ShYCalculator m_calc = null!;

    [TestInitialize]
    public void TestInitialize() => m_calc = new ShYCalculator();

    private static string NestedSum(int depth) => string.Concat(Enumerable.Repeat("1+(", depth)) + "1" + new string(')', depth);

    private static Dictionary<string, Value> Vars(params (string Key, Value Val)[] vars) => vars.ToDictionary(v => v.Key, v => v.Val);

    #region 1. Evaluation stack capacity
    [TestMethod]
    [DataRow(127)]
    [DataRow(128)]
    [DataRow(500)]
    public void Stack_DeepNesting_Evaluates(int depth) {
        var result = m_calc.Calculate(NestedSum(depth));

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(depth + 1, result.Nvalue);
    }

    [TestMethod]
    public void Stack_NestingPlusFunctionArguments_Evaluates() {
        const int depth = 40;
        var countCall = "count(" + string.Join(",", Enumerable.Range(1, 100)) + ")";
        var expression = string.Concat(Enumerable.Repeat("1+(", depth)) + countCall + new string(')', depth);

        var result = m_calc.Calculate(expression);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(depth + 100, result.Nvalue);
    }

    [TestMethod]
    public void Stack_DeepNesting_Compiled_Evaluates() {
        var compiled = ShYCalculator.Compile(NestedSum(300));
        Assert.IsTrue(compiled.Success, compiled.Message);

        var result = compiled.Value!.Calculate();

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(301, result.Nvalue);
    }
    #endregion

    #region 2. Leftover tokens
    [TestMethod]
    [DataRow("abs(-3)) + 1", 7)]
    [DataRow("1 + 2)", 5)]
    [DataRow("(1 + 2)) * 3", 7)]
    public void Leftover_StrayClosingParenthesis_Fails(string expression, int index) {
        var result = m_calc.Calculate(expression);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.MismatchedParentheses, result.Errors[0].Code);
        Assert.AreEqual(index, result.Errors[0].StartIndex);
    }

    [TestMethod]
    [DataRow("1,2", 1)]
    [DataRow("1 + 2, 3", 5)]
    public void Leftover_TopLevelComma_Fails(string expression, int index) {
        var result = m_calc.Calculate(expression);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.InvalidSyntax, result.Errors[0].Code);
        Assert.AreEqual(index, result.Errors[0].StartIndex);
    }

    [TestMethod]
    public void Leftover_Compile_Fails() {
        var compiled = ShYCalculator.Compile("abs(-3)) + 1");

        Assert.IsFalse(compiled.Success);
        Assert.AreEqual(ErrorCode.MismatchedParentheses, compiled.Errors[0].Code);
    }
    #endregion

    #region 3. Lazy if() with whitespace
    [TestMethod]
    [DataRow("if($x != 0, 10 / $x, 0)")]
    [DataRow("if ($x != 0, 10 / $x, 0)")]
    [DataRow("if  \t($x != 0, 10 / $x, 0)")]
    [DataRow("if ( $x != 0 , 10 / $x , 0 )")]
    public void If_WithWhitespace_IsLazy(string expression) {
        var result = m_calc.Calculate(expression, Vars(("$x", Value.Number(0))));

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(0, result.Nvalue);
    }
    #endregion

    #region 4. Exceptions surfacing as UnexpectedException
    [TestMethod]
    [DataRow("true <= false")]
    [DataRow("true >= false")]
    [DataRow("true < false")]
    public void Ordering_OnBooleans_IsTypeMismatch(string expression) {
        var result = m_calc.Calculate(expression);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.TypeMismatch, result.Errors[0].Code);
    }

    [TestMethod]
    [DataRow("4294967296 & 1", 0d)]
    [DataRow("4294967297 & 1", 1d)]
    [DataRow("4294967296 | 1", 4294967297d)]
    [DataRow("1 << 40", 1099511627776d)]
    [DataRow("~4294967296", -4294967297d)]
    public void Bitwise_BeyondInt32_Evaluates(string expression, double expected) {
        var result = m_calc.Calculate(expression);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Nvalue);
    }

    [TestMethod]
    [DataRow("1e30 & 1")]
    [DataRow("~1e30")]
    [DataRow("1 << 64")]
    [DataRow("1 >> -1")]
    public void Bitwise_OutOfRange_IsValueOutOfRange(string expression) {
        var result = m_calc.Calculate(expression);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.ValueOutOfRange, result.Errors[0].Code);
    }
    #endregion

    #region 5. Error codes and positions
    [TestMethod]
    [DataRow("1.5 & 1", ErrorCode.ValueOutOfRange, 4)]
    [DataRow("~1.5", ErrorCode.ValueOutOfRange, 0)]
    [DataRow("(-3)!", ErrorCode.ValueOutOfRange, 4)]
    [DataRow("171!", ErrorCode.ValueOutOfRange, 3)]
    [DataRow("√(0-4)", ErrorCode.ValueOutOfRange, 0)]
    [DataRow("!1", ErrorCode.TypeMismatch, 0)]
    [DataRow("true!", ErrorCode.TypeMismatch, 4)]
    [DataRow("~true", ErrorCode.TypeMismatch, 0)]
    [DataRow("1 && true", ErrorCode.TypeMismatch, 2)]
    [DataRow("5 / 0", ErrorCode.DivisionByZero, 2)]
    [DataRow("5 % 0", ErrorCode.DivisionByZero, 2)]
    public void Errors_CarryCodeAndPosition(string expression, ErrorCode code, int index) {
        var result = m_calc.Calculate(expression);

        Assert.IsFalse(result.Success, $"Expected failure, got {result.Nvalue}");
        Assert.HasCount(1, result.Errors);
        Assert.AreEqual(code, result.Errors[0].Code, result.Message);
        Assert.AreEqual(index, result.Errors[0].StartIndex);
        Assert.IsTrue(result.Errors[0].Length > 0);
    }
    #endregion

    #region 6. String literals
    [TestMethod]
    [DataRow(@"""a\\""", @"a\")]
    [DataRow(@"""a\\"" + ""b""", @"a\b")]
    [DataRow(@"""say \""hi\""""", @"say ""hi""")]
    [DataRow(@"'it\'s'", "it's")]
    [DataRow(@"""tab\there""", "tab\there")]
    [DataRow(@"""line\nbreak""", "line\nbreak")]
    [DataRow(@"""A""", "A")]
    [DataRow(@"""a.b*c""", "a.b*c")]
    [DataRow(@"""""", "")]
    public void String_Escapes_AreDecoded(string expression, string expected) {
        var result = m_calc.Calculate(expression);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Svalue);
    }

    [TestMethod]
    [DataRow(@"""abc", 0)]
    [DataRow(@"1 + 'abc", 4)]
    [DataRow(@"""abc\""", 0)]
    public void String_Unterminated_Fails(string expression, int index) {
        var result = m_calc.Calculate(expression);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.UnterminatedString, result.Errors[0].Code);
        Assert.AreEqual(index, result.Errors[0].StartIndex);
    }

    [TestMethod]
    [DataRow(@"""a\qb""", 2)]
    [DataRow(@"""\u12""", 1)]
    public void String_InvalidEscape_Fails(string expression, int index) {
        var result = m_calc.Calculate(expression);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.InvalidSyntax, result.Errors[0].Code);
        Assert.AreEqual(index, result.Errors[0].StartIndex);
    }
    #endregion

    #region 7. Operator precedence (C# conventions)
    [TestMethod]
    [DataRow("10 % 3 * 2", 2d)]
    [DataRow("10 * 3 % 7", 2d)]
    [DataRow("2 + 10 % 4", 4d)]
    [DataRow("1 + 2 << 1", 6d)]
    [DataRow("6 & 3 | 8", 10d)]
    [DataRow("1 | 6 & 3", 3d)]
    [DataRow("1 | 3 ^^ 1", 3d)]
    [DataRow("2 ^ 3 ^ 2", 512d)]
    [DataRow("-2 ^ 2", -4d)]
    public void Precedence_Numeric(string expression, double expected) {
        var result = m_calc.Calculate(expression);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Nvalue);
    }

    [TestMethod]
    [DataRow("true || false && false", true)]
    [DataRow("false && false || true", true)]
    [DataRow("1 < 2 == 3 < 4", true)]
    [DataRow("1 < 2 == false", false)]
    [DataRow("1 << 2 == 4", true)]
    public void Precedence_Boolean(string expression, bool expected) {
        var result = m_calc.Calculate(expression);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Bvalue);
    }
    #endregion

    #region 8. Short-circuit logical operators
    [TestMethod]
    [DataRow("$x != 0 && 10 / $x > 1", false)]
    [DataRow("$x == 0 || 10 / $x > 1", true)]
    [DataRow("false && 1 / 0 > 0", false)]
    [DataRow("true || 1 / 0 > 0", true)]
    [DataRow("false && $missing", false)]
    [DataRow("true || $missing", true)]
    [DataRow("$x != 0 && 10 / $x > 1 || true", true)]
    [DataRow("($x != 0 && 10 / $x > 1) ? 1 == 1 : 2 == 2", true)]
    [DataRow("if($x == 0, true, false && 1 / 0 > 0)", true)]
    [DataRow("$x == 0 || (true && 1 / 0 > 0)", true)]
    [DataRow("$x != 0 && (true || 1 / 0 > 0)", false)]
    [DataRow("!($x != 0 && 1 / $x > 0)", true)]
    public void ShortCircuit_SkipsRightOperand(string expression, bool expected) {
        var result = m_calc.Calculate(expression, Vars(("$x", Value.Number(0))));

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Bvalue);
    }

    [TestMethod]
    [DataRow("true && false", false)]
    [DataRow("true && true", true)]
    [DataRow("false || false", false)]
    [DataRow("false || true", true)]
    [DataRow("true && true && false", false)]
    [DataRow("false || false || true", true)]
    [DataRow("true || false && false", true)]
    [DataRow("1 < 2 && 3 < 4", true)]
    public void ShortCircuit_TruthTable(string expression, bool expected) {
        var result = m_calc.Calculate(expression);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Bvalue);
    }

    [TestMethod]
    [DataRow("$x != 0 && 10 / $x > 1", 0d, false)]
    [DataRow("$x != 0 && 10 / $x > 1", 5d, true)]
    public void ShortCircuit_Compiled(string expression, double x, bool expected) {
        var compiled = ShYCalculator.Compile(expression);
        Assert.IsTrue(compiled.Success, compiled.Message);

        var result = compiled.Value!.Calculate(Vars(("$x", Value.Number(x))));

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Bvalue);
    }

    [TestMethod]
    [DataRow("1 && true", 2)]
    [DataRow("true && 1", 5)]
    [DataRow("false || 1", 6)]
    public void ShortCircuit_NonBooleanOperand_IsTypeMismatch(string expression, int index) {
        var result = m_calc.Calculate(expression);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.TypeMismatch, result.Errors[0].Code);
        Assert.AreEqual(index, result.Errors[0].StartIndex);
    }
    #endregion

    #region 9. includeAst parity
    private static readonly DateTimeOffset Today = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

    private static Dictionary<string, Value> AstVars() => Vars(
        ("$x", Value.Number(0)),
        ("$a", Value.Date(Today)),
        ("$b", Value.Date(Today.AddDays(1))),
        ("$s", Value.String("hi")));

    [TestMethod]
    [DataRow("1 / 0")]
    [DataRow("1 / $missing")]
    [DataRow("$a < $b")]
    [DataRow("$a == $b")]
    [DataRow("1 + 2 * 3")]
    [DataRow("$s + \"!\"")]
    [DataRow("$x != 0 && 10 / $x > 1")]
    [DataRow("$x == 0 || 10 / $x > 1")]
    [DataRow("true && 1")]
    [DataRow("$x > 0 ? 1 / $x : -1")]
    [DataRow("if($x == 0, 1, 1 / $x)")]
    [DataRow("max(1, 5, 3)")]
    [DataRow("sqrt(-1 + 0)")]
    [DataRow("5!")]
    [DataRow("~1.5")]
    public void Ast_MatchesPlainEvaluation(string expression) {
        var plain = m_calc.Calculate(expression, AstVars());
        var withAst = m_calc.Calculate(expression, AstVars(), includeAst: true);

        Assert.AreEqual(plain.Success, withAst.Success, withAst.Message);
        Assert.AreEqual(plain.Message, withAst.Message);
        CollectionAssert.AreEqual(plain.Errors.Select(e => e.Code).ToList(), withAst.Errors.Select(e => e.Code).ToList());
        Assert.AreEqual(plain.Value.DataType, withAst.Value.DataType);
        Assert.AreEqual(plain.Nvalue, withAst.Nvalue);
        Assert.AreEqual(plain.Bvalue, withAst.Bvalue);
        Assert.AreEqual(plain.Svalue, withAst.Svalue);
        Assert.AreEqual(plain.Dvalue, withAst.Dvalue);
        Assert.IsNotNull(withAst.Ast);
    }

    [TestMethod]
    public void Ast_DateComparison_IsAnnotated() {
        var result = m_calc.GetAst("$a < $b", AstVars());

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(true, result.Value!.EvaluatedValue);
    }

    [TestMethod]
    public void Ast_ShortCircuit_IsBinaryNode() {
        var result = m_calc.GetAst("$x != 0 && 10 / $x > 1", AstVars());

        Assert.IsTrue(result.Success, result.Message);
        var node = result.Value!;
        Assert.AreEqual(AstNodeType.Binary, node.Type);
        Assert.AreEqual("&&", node.Operator);
        Assert.AreEqual(false, node.EvaluatedValue);
        Assert.AreEqual(AstNodeType.Binary, node.Left!.Type);
        Assert.AreEqual(">", node.Right!.Operator);
        Assert.AreEqual(0, node.Range.Start);
        Assert.AreEqual(22, node.Range.End);
    }

    [TestMethod]
    public void Ast_ReadmeSample_CompilesAndRuns() {
        var result = m_calc.Calculate("1 + 2 * 3", includeAst: true);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(AstNodeType.Binary, result.Ast!.Type);
        Assert.AreEqual("+", result.Ast.Operator);
        Assert.AreEqual(7, result.Nvalue);
    }
    #endregion

    #region 10. Dates as strings and culture
    [TestMethod]
    [DataRow("dt_year('15/01/2026')", 2026d)]
    [DataRow("dt_month('15/01/2026')", 1d)]
    [DataRow("dt_day('15/01/2026')", 15d)]
    [DataRow("dt_dayofyear('15/01/2026')", 15d)]
    [DataRow("dt_year(dt_addyears('15/01/2026', 1))", 2027d)]
    [DataRow("dt_month(dt_add('15/01/2026', '1M'))", 2d)]
    public void Dates_StringArguments_AreParsed(string expression, double expected) {
        var result = m_calc.Calculate(expression);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Nvalue);
    }

    [TestMethod]
    [DataRow("fr-FR", "dd MMMM yyyy", "15 janvier 2026")]
    [DataRow("es-ES", "dd MMMM yyyy", "15 enero 2026")]
    [DataRow("en-US", "MMMM dd, yyyy", "January 15, 2026")]
    public void Dates_CultureSpecificFormats_AreParsed(string culture, string format, string text) {
        var calc = new ShYCalculator(options: new ShYCalculatorOptions { CultureName = culture, DateFormat = format });

        var result = calc.Calculate($"dt_month('{text}')");

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(1, result.Nvalue);
    }

    [TestMethod]
    public void Concatenation_Date_IsCultureInvariant() {
        var date = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.FromHours(2));
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var token = new Token { Key = "+", Type = TokenType.Operator, OperatorKind = OperatorKind.Add };

            var result = global::ShY.Calculator.Operations.ArithmeticOperations.PerformArithmeticOperationOnStrings(Value.String("at "), Value.Date(date), token);

            Assert.AreEqual("at 2026-01-15T10:30:00.0000000+02:00", result.Value.Svalue);
        }
        finally {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
    #endregion

    #region 11. Compiled calculator error reporting
    private static IGlobalScope ThrowingScope() {
        var env = new global::ShY.Calculator.CalculatorEnvironment();
        var scope = new Moq.Mock<IGlobalScope>();
        scope.SetupGet(s => s.Functions).Returns(env.Functions);
        scope.SetupGet(s => s.Constants).Returns(env.Constants);
        scope.SetupGet(s => s.Operators).Throws(new InvalidOperationException("operators unavailable"));
        return scope.Object;
    }

    [TestMethod]
    public void Compiled_CompileException_KeepsMessageAndError() {
        var result = ShYCalculator.Compile("1 + 2 * 3", ThrowingScope());

        Assert.IsFalse(result.Success);
        Assert.Contains("operators unavailable", result.Message);
        Assert.AreEqual(ErrorCode.UnexpectedException, result.Errors[0].Code);
    }

    [TestMethod]
    public void Compiled_NotCompiled_ReportsError() {
        var compiled = (ShYCompiledCalculator)ShYCalculator.Compile("1 + 2").Value!;
        compiled.Reset();

        var result = compiled.Calculate();

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.InvalidExpression, result.Errors[0].Code);
    }
    #endregion
}
