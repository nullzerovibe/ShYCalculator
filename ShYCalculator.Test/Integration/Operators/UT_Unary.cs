using ShYCalculator.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace ShYCalculator.Test.Integration.Operators;

[TestClass]
public class UT_Unary {
    private readonly ShYCalculator m_calculator = new();

    private void CheckDoubleResult(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue!.Value, 1e-10);
    }

    [TestMethod]
    public void Unary_Factorial() {
        CheckDoubleResult("0!", 1);
        CheckDoubleResult("1!", 1);
        CheckDoubleResult("2!", 2);
        CheckDoubleResult("3!", 6);
        CheckDoubleResult("4!", 24);
        CheckDoubleResult("5!", 120);
        CheckDoubleResult("6!", 720);
    }

    [TestMethod]
    public void Unary_Factorial_Expression() {
        CheckDoubleResult("(3+2)!", 120);
        CheckDoubleResult("5! + 1", 121);
        CheckDoubleResult("5! * 2", 240);
    }

    [TestMethod]
    public void Unary_Factorial_Errors() {
        Assert.IsFalse(m_calculator.Calculate("(-1)!").Success);
        Assert.IsFalse(m_calculator.Calculate("3.5!").Success);
        Assert.IsFalse(m_calculator.Calculate("171!").Success); // Overflow
    }

    [TestMethod]
    public void Unary_SquareRoot() {
        CheckDoubleResult("√4", 2);
        CheckDoubleResult("√9", 3);
        CheckDoubleResult("√16", 4);
        CheckDoubleResult("√0", 0);

        CheckDoubleResult("√2", Math.Sqrt(2));
    }

    [TestMethod]
    public void Unary_SquareRoot_Expression() {
        CheckDoubleResult("√4 + √9", 5);
        CheckDoubleResult("√(16 + 9)", 5);
        CheckDoubleResult("2 * √9", 6);
    }

    [TestMethod]
    public void Unary_SquareRoot_Errors() {
        Assert.IsFalse(m_calculator.Calculate("√-4").Success);
    }
}
