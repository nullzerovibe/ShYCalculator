using ShYCalculator.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ShYCalculator.Test.Integration.Operators;

[TestClass]
public class UT_Arithmetic {
    private readonly ShYCalculator m_calculator = new();

    private void CheckDoubleResult(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue!.Value, 1e-10, $"Expression '{expression}' returned wrong value");
    }

    [TestMethod]
    public void Arithmetic_BasicOperations() {
        CheckDoubleResult("1 + 2", 3);
        CheckDoubleResult("1 + 2 + 3", 6);
        CheckDoubleResult("1 + 2 + 3 + 4", 10);

        CheckDoubleResult("3 - 2", 1);
        CheckDoubleResult("10 - 5 - 4", 1);
        CheckDoubleResult("10 - 5 - 4 - 0 - 1", 0);

        // Unicode minus
        CheckDoubleResult("3 − 2", 1);
        CheckDoubleResult("10 − 5 - 4", 1);
        CheckDoubleResult("10 − 5 - 4 − 0 - 1", 0);

        CheckDoubleResult("1 * 2", 2);
        CheckDoubleResult("1 * -2 * 3", -6);
        CheckDoubleResult("-1 * 2 * -3 * 4 * -1", -24);

        // Unicode multiplication
        CheckDoubleResult("1 × 2", 2);
        CheckDoubleResult("1 * -2 × 3", -6);
        CheckDoubleResult("-1 × 2 * -3 × 4 * -1", -24);

        CheckDoubleResult("81 / 3 / 3", 9);
        CheckDoubleResult("-81 / 3 / 9", -3);
        CheckDoubleResult("81 / 3 / 3 / 3", 3);

        // Unicode division
        CheckDoubleResult("81 ÷ 3 ÷ 3", 9);
        CheckDoubleResult("-81 ÷ 3 / 9", -3);
        CheckDoubleResult("81 ÷ 3 / 3 ÷ 3", 3);

        CheckDoubleResult("2 ^ 2", 4);
        CheckDoubleResult("2 ^ 3", 8);
        CheckDoubleResult("2 ^ 3 ^ 1", 8);

        CheckDoubleResult("9 % 2", 1);
        CheckDoubleResult("7 % 5", 2);
        CheckDoubleResult("20 % 5", 0);
    }

    [TestMethod]
    public void Arithmetic_Precedence_Basic() {
        CheckDoubleResult("1 + 2 * 3", 7);
        CheckDoubleResult("3 + 4 * 2", 11);
        CheckDoubleResult("9 / 3 * 9 + 3", 30);
        CheckDoubleResult("9 / 3 * -9 + 3", -24);
        CheckDoubleResult("9 * 3 / 9 - 3", 0);
        CheckDoubleResult("9 * 3^2 / 9 - 3", 6);
        CheckDoubleResult("8 + 6 * 3 % 4 - 2", 8);
    }

    [TestMethod]
    public void Arithmetic_UnaryNegation_Chained() {
        CheckDoubleResult("-1 -1", -2);
        CheckDoubleResult("-1 + -1", -2);
        CheckDoubleResult("-1 --1", 0);
        CheckDoubleResult("-1 +--1", 0);
        CheckDoubleResult("-1 +---1", -2);
    }

    [TestMethod]
    public void Arithmetic_SimpleEdgeCases() {
        CheckDoubleResult("-1*4", -4);
        CheckDoubleResult("1+4", 5);
    }
}
