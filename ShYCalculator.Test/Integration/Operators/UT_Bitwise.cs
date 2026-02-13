using ShYCalculator.Classes;
using ShYCalculator.Calculator.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShYCalculator.Test.Integration.Operators;

[TestClass]
public class UT_Bitwise {
    private readonly ShYCalculator m_calculator = new();

    private void CheckDoubleResult(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue!.Value, 1e-10, $"Expression '{expression}' returned wrong value");
    }

    [TestMethod]
    public void Bitwise_And() {
        CheckDoubleResult("7 & 1", 1);
        CheckDoubleResult("15 & 10", 10);
        CheckDoubleResult("31 & 20", 20);
        CheckDoubleResult("63 & 36", 36);

        // Mixed precedence
        CheckDoubleResult("(1 + 2) & (7 - 4)", 3);
    }

    [TestMethod]
    public void Bitwise_Or() {
        CheckDoubleResult("7 | 2", 7);
        CheckDoubleResult("15 | 15", 15);
        CheckDoubleResult("15 | 16", 31);
    }

    [TestMethod]
    public void Bitwise_Xor() {
        // Unicode symbol ⊕
        CheckDoubleResult("7 ⊕ 2", 5);
        CheckDoubleResult("63 ⊕ 63", 0);
        CheckDoubleResult("63 ⊕ 6", 57);
        CheckDoubleResult("(10 + 5) ⊕ (8 + 7)", 0);

        // ASCII alias ^^
        CheckDoubleResult("7 ^^ 2", 5);
        CheckDoubleResult("63 ^^ 63", 0);
        CheckDoubleResult("63 ^^ 6", 57);
        CheckDoubleResult("(10 + 5) ^^ (8 + 7)", 0);
    }

    [TestMethod]
    public void Bitwise_Not() {
        // ~ operator
        CheckDoubleResult("~7", -8);
        CheckDoubleResult("~0", -1);
        CheckDoubleResult("~(-1)", 0);
        CheckDoubleResult("~~7", 7);

        // With negative inputs
        CheckDoubleResult("~(-2)", 1);

        // In expressions
        CheckDoubleResult("~0 + 1", 0);
        CheckDoubleResult("~1 + 3", 1);
    }

    [TestMethod]
    public void Bitwise_Not_ErrorHandling() {
        // Non-integer input
        var result = m_calculator.Calculate("~1.5");
        Assert.IsFalse(result.Success);
        Assert.Contains("integer", result.Message, $"Expected 'integer' error, got: {result.Message}");

        // Boolean input (should fail type check)
        result = m_calculator.Calculate("~true");
        Assert.IsFalse(result.Success);
        // Checking via manual invocation for precise message if needed, but integration test confirms failure.
    }
}
