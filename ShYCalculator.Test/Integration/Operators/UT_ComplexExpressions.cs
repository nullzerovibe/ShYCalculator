using ShYCalculator.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ShYCalculator.Test.Integration.Operators;

[TestClass]
public class UT_ComplexExpressions {
    private readonly ShYCalculator m_calculator = new();

    [TestInitialize]
    public void Setup() {
        m_calculator.Environment.SetVariables(new Dictionary<string, Value> {
            { "$five", Value.Number(5) },
            { "$four", Value.Number(4) },
            { "$three", Value.Number(3) },
            { "$two", Value.Number(2) },
            { "$one", Value.Number(1) },
            { "$date1", Value.Date(new DateTimeOffset(2023, 8, 16, 12, 0 , 0, TimeSpan.Zero)) },
            { "$date2", Value.Date(new DateTimeOffset(2023, 8, 17, 12, 0 , 0, TimeSpan.Zero)) },
            { "$string1", Value.String("test\"s") },
        });
    }

    private void CheckDoubleResult(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue!.Value, 1e-10);
    }

    [TestMethod]
    public void Complex_FunctionsAndParenthesis() {
        CheckDoubleResult("max(1,3)", 3);
        CheckDoubleResult("min(8, 3)", 3);

        CheckDoubleResult("3^2 / 3", 3);
        CheckDoubleResult("( 1 - 5 ) ^ 2 ^ 3", 65536);

        CheckDoubleResult("max(9,3) / min(8, 3) * sqrt(81) + abs(-3)", 30);

        CheckDoubleResult("((1 + 2) * (3 + 4)) / (2 + 3)", 4.2);
    }

    [TestMethod]
    public void Complex_MathFunctions() {
        CheckDoubleResult("log2(e)", Math.Log2(Math.E));
        CheckDoubleResult("abs(-5)", Math.Abs(-5));
        CheckDoubleResult("sqrt(16)", Math.Sqrt(16));
        CheckDoubleResult("ln(e)", Math.Log(Math.E));
        CheckDoubleResult("log(e, 4)", Math.Log(Math.E, 4));
        CheckDoubleResult("log10(e)", Math.Log10(Math.E));
        CheckDoubleResult("pi*2", Math.PI * 2);

        CheckDoubleResult("sin(e)", Math.Sin(Math.E));
        CheckDoubleResult("cos(pi)", Math.Cos(Math.PI));
        CheckDoubleResult("sin(pi)", Math.Sin(Math.PI));

        CheckDoubleResult("tan(max(180,360))", Math.Tan(360));
    }

    [TestMethod]
    public void Complex_Variables() {
        CheckDoubleResult("max(8, $three, 5, 1)", 8);
        CheckDoubleResult("9 / $three * 9 * 1 + $three", 30);
        CheckDoubleResult("abs(-$five * $two)", 10);
    }

    [TestMethod]
    public void Complex_TernaryMixed() {
        CheckDoubleResult("max(10, 5 > 3 ? 20 : 5)", 20);

        // ($five > $three ? $five : $three) * ($two < $four ? $two : $four) -> 5 * 2 = 10
        CheckDoubleResult("($five > $three ? $five : $three) * ($two < $four ? $two : $four)", 10);

        CheckDoubleResult("sqrt($four > $two ? 16 : 9)", 4);
    }

    [TestMethod]
    public void Complex_SumFunction_LegacyPort() {
        // Ported from UT_Operators_Unary.cs where they were misplaced
        CheckDoubleResult("sum(1, 2, 3)", 6);
        CheckDoubleResult("sum(10, 20, 30, 40)", 100);
        CheckDoubleResult("∑(1, 2, 3)", 6);
        CheckDoubleResult("sum(1, 2, 3) + ∑(4, 5)", 15);
    }
}
