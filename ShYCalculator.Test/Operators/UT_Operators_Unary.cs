using ShYCalculator.Classes;
using ShYCalculator.Calculator.Operations;

namespace ShYCalculator.Test.Operators;

[TestClass]
public class UT_Operators_Unary {
    private readonly ShYCalculator m_calculator = new();

    private void CheckDoubleResult(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue!.Value, 1e-10, $"Expression '{expression}' returned wrong value");
    }

    #region Logical NOT Tests

    [TestMethod]
    [DataRow("!true", false)]
    [DataRow("!false", true)]
    [DataRow("!!true", true)]
    [DataRow("!!false", false)]
    [DataRow("!!!true", false)]
    public void NOT_BasicLogicalNegation(string expression, bool expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Bvalue, $"Expression '{expression}' returned wrong value");
    }

    [TestMethod]
    [DataRow("!(1 > 2)", true)]
    [DataRow("!(1 < 2)", false)]
    [DataRow("!(1 == 1)", false)]
    [DataRow("!(1 != 1)", true)]
    public void NOT_WithComparisons(string expression, bool expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Bvalue, $"Expression '{expression}' returned wrong value");
    }

    [TestMethod]
    [DataRow("!true && !false", false)]
    [DataRow("!true || !false", true)]
    [DataRow("!false && !false", true)]
    public void NOT_WithLogicalOperators(string expression, bool expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Bvalue, $"Expression '{expression}' returned wrong value");
    }

    #endregion

    #region Factorial Tests

    [TestMethod]
    [DataRow("0!", 1.0)]
    [DataRow("1!", 1.0)]
    [DataRow("2!", 2.0)]
    [DataRow("3!", 6.0)]
    [DataRow("4!", 24.0)]
    [DataRow("5!", 120.0)]
    [DataRow("6!", 720.0)]
    [DataRow("10!", 3628800.0)]
    public void Factorial_BasicValues(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue, $"Expression '{expression}' returned wrong value");
    }

    [TestMethod]
    [DataRow("(3+2)!", 120.0)]
    [DataRow("(10-5)!", 120.0)]
    [DataRow("(2*3)!", 720.0)]
    public void Factorial_WithExpressions(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue, $"Expression '{expression}' returned wrong value");
    }

    [TestMethod]
    [DataRow("5! + 1", 121.0)]
    [DataRow("5! - 20", 100.0)]
    [DataRow("5! * 2", 240.0)]
    [DataRow("5! / 2", 60.0)]
    public void Factorial_InArithmeticExpressions(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue, $"Expression '{expression}' returned wrong value");
    }

    [TestMethod]
    public void Factorial_NegativeNumber_Fails() {
        var result = m_calculator.Calculate("(-1)!");
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void Factorial_NonInteger_Fails() {
        var result = m_calculator.Calculate("3.5!");
        Assert.IsFalse(result.Success);
    }

    #endregion

    #region Combined NOT and Factorial Tests

    [TestMethod]
    [DataRow("5! == 120", true)]
    [DataRow("!(5! == 120)", false)]
    [DataRow("!(5! != 120)", true)]
    public void Combined_NOTandFactorial(string expression, bool expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Bvalue, $"Expression '{expression}' returned wrong value");
    }

    [TestMethod]
    public void Combined_ComplexExpression() {
        var result = m_calculator.Calculate("!false && 5! == 120");
        Assert.IsTrue(result.Success, $"Expression failed: {result.Message}");
        Assert.IsTrue(result.Value.Bvalue);
    }

    #endregion

    #region Error Path Coverage Tests

    [TestMethod]
    public void Factorial_Overflow_Fails() {
        // n > 170 should fail due to double precision overflow
        var result = m_calculator.Calculate("171!");
        Assert.IsFalse(result.Success);
        Assert.Contains("overflow", result.Message);
    }

    [TestMethod]
    public void NOT_OnNumber_Fails() {
        // Applying ! prefix to a number should fail (expects boolean)
        var result = m_calculator.Calculate("!5");
        Assert.IsFalse(result.Success);
        Assert.Contains("boolean", result.Message);
    }

    [TestMethod]
    public void Factorial_OnBoolean_Fails() {
        // Applying ! postfix to a boolean should be parsed as NOT, not factorial
        // But we can test via reflection for direct type mismatch
        var token = new Token(0, "!", TokenType.UnaryPostfixOperator, operatorKind: OperatorKind.Factorial);
        var operand = new Value { DataType = DataType.Boolean, Bvalue = true };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        Assert.IsFalse(result.Success);
        Assert.Contains("number", result.Message);
    }

    [TestMethod]
    public void NOT_OnNullBoolean_Fails() {
        var token = new Token(0, "!", TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.Not);
        var operand = new Value { DataType = DataType.Boolean, Bvalue = null };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        Assert.IsFalse(result.Success);
        Assert.Contains("null", result.Message);
    }

    [TestMethod]
    public void Factorial_OnNullNumber_Fails() {
        var token = new Token(0, "!", TokenType.UnaryPostfixOperator, operatorKind: OperatorKind.Factorial);
        var operand = new Value { DataType = DataType.Number, Nvalue = null };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        Assert.IsFalse(result.Success);
        Assert.Contains("null", result.Message);
    }

    [TestMethod]
    public void UnknownUnaryOperator_Throws() {
        // Test with an unknown unary operator - should throw ShuntingYardParserException
        var token = new Token(0, "@", TokenType.UnaryPrefixOperator);
        var operand = new Value { DataType = DataType.Number, Nvalue = 5 };

        bool threw = false;
        try {
            UnaryOperations.PerformUnaryOperation(operand, token);
        }
        catch (ShuntingYardParserException) {
            threw = true;
        }
        Assert.IsTrue(threw, "Expected ShuntingYardParserException was not thrown");
    }

    #endregion

    #region Bitwise NOT (~) Tests

    [TestMethod]
    public void BitwiseNOT_ReturnsCorrectValue() {
        // ~0 = -1, ~1 = -2, ~15 = -16, etc.
        CheckDoubleResult("~0", -1);
        CheckDoubleResult("~1", -2);
        CheckDoubleResult("~15", -16);
        CheckDoubleResult("~255", -256);
    }

    [TestMethod]
    public void BitwiseNOT_NegativeNumber() {
        // ~(-1) = 0, ~(-2) = 1
        CheckDoubleResult("~(-1)", 0);
        CheckDoubleResult("~(-2)", 1);
    }

    [TestMethod]
    public void BitwiseNOT_InExpression() {
        CheckDoubleResult("~0 + 1", 0);
        CheckDoubleResult("~1 + 3", 1);
    }

    [TestMethod]
    public void BitwiseNOT_OnNonInteger_Fails() {
        var token = new Token(0, "~", TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.BitwiseNot);
        var operand = new Value { DataType = DataType.Number, Nvalue = 1.5 };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        Assert.IsFalse(result.Success);
        Assert.Contains("integer", result.Message);
    }

    [TestMethod]
    public void BitwiseNOT_OnBoolean_Fails() {
        var token = new Token(0, "~", TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.BitwiseNot);
        var operand = new Value { DataType = DataType.Boolean, Bvalue = true };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        Assert.IsFalse(result.Success);
        Assert.Contains("number", result.Message);
    }

    [TestMethod]
    public void BitwiseNOT_OnNull_Fails() {
        var token = new Token(0, "~", TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.BitwiseNot);
        var operand = new Value { DataType = DataType.Number, Nvalue = null };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        Assert.IsFalse(result.Success);
        Assert.Contains("null", result.Message);
    }

    #endregion

    #region Square Root (√) Tests

    [TestMethod]
    public void SquareRoot_ReturnsCorrectValue() {
        CheckDoubleResult("√4", 2);
        CheckDoubleResult("√9", 3);
        CheckDoubleResult("√16", 4);
        CheckDoubleResult("√100", 10);
        CheckDoubleResult("√0", 0);
    }

    [TestMethod]
    public void SquareRoot_NonPerfectSquares() {
        CheckDoubleResult("√2", Math.Sqrt(2));
        CheckDoubleResult("√3", Math.Sqrt(3));
    }

    [TestMethod]
    public void SquareRoot_InExpression() {
        CheckDoubleResult("√4 + √9", 5);
        CheckDoubleResult("√(16 + 9)", 5);
        CheckDoubleResult("2 * √9", 6);
    }

    [TestMethod]
    public void SquareRoot_OfNegativeNumber_Fails() {
        var token = new Token(0, "√", TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.SquareRoot);
        var operand = new Value { DataType = DataType.Number, Nvalue = -4 };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        Assert.IsFalse(result.Success);
        Assert.Contains("non-negative", result.Message);
    }

    [TestMethod]
    public void SquareRoot_OnBoolean_Fails() {
        var token = new Token(0, "√", TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.SquareRoot);
        var operand = new Value { DataType = DataType.Boolean, Bvalue = true };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        Assert.IsFalse(result.Success);
        Assert.Contains("number", result.Message);
    }

    [TestMethod]
    public void SquareRoot_OnNull_Fails() {
        var token = new Token(0, "√", TokenType.UnaryPrefixOperator, operatorKind: OperatorKind.SquareRoot);
        var operand = new Value { DataType = DataType.Number, Nvalue = null };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);
        Assert.IsFalse(result.Success);
        Assert.Contains("null", result.Message);
    }

    #endregion

    #region Sum Function Tests

    [TestMethod]
    public void Sum_SingleNumber() {
        CheckDoubleResult("sum(5)", 5);
    }

    [TestMethod]
    public void Sum_MultipleNumbers() {
        CheckDoubleResult("sum(1, 2, 3)", 6);
        CheckDoubleResult("sum(1, 2, 3, 4, 5)", 15);
        CheckDoubleResult("sum(10, 20, 30, 40)", 100);
    }

    [TestMethod]
    public void Sum_WithNegatives() {
        CheckDoubleResult("sum(1, -2, 3)", 2);
        CheckDoubleResult("sum(-1, -2, -3)", -6);
    }

    [TestMethod]
    public void Sum_WithDecimals() {
        CheckDoubleResult("sum(1.5, 2.5, 3.0)", 7);
    }

    [TestMethod]
    public void Sum_InExpression() {
        CheckDoubleResult("sum(1, 2, 3) + 4", 10);
        CheckDoubleResult("2 * sum(1, 2, 3)", 12);
    }

    [TestMethod]
    public void SigmaSum_SingleNumber() {
        CheckDoubleResult("∑(5)", 5);
    }

    [TestMethod]
    public void SigmaSum_MultipleNumbers() {
        CheckDoubleResult("∑(1, 2, 3)", 6);
        CheckDoubleResult("∑(1, 2, 3, 4, 5)", 15);
    }

    [TestMethod]
    public void SigmaSum_InExpression() {
        CheckDoubleResult("∑(1, 2, 3) + ∑(4, 5)", 15);
    }

    #endregion
}

