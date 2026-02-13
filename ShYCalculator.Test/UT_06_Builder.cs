using ShYCalculator.Classes;

namespace ShYCalculator.Test;

[TestClass]
public class UT_06_Builder {
    [TestMethod]
    public void TestBuilder_WithAllExtensions() {
        var calculator = new ShYCalculatorBuilder()
            .WithAllExtensions()
            .Build();

        var result = calculator.Calculate("1 + 1");
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2.0, result.Value.Nvalue);

        var dateResult = calculator.Calculate("dt_year(dt_now())");
        Assert.IsTrue(dateResult.Success);
    }

    [TestMethod]
    public void TestBuilder_WithSpecificExtension() {
        var calculator = new ShYCalculatorBuilder()
            .WithMathematics()
            .Build();

        var result = calculator.Calculate("sin(0)");
        Assert.IsTrue(result.Success);
        Assert.AreEqual(0.0, result.Value.Nvalue);

        // Date function should fail
        var dateResult = calculator.Calculate("dt_now()");
        Assert.IsFalse(dateResult.Success);
    }

    [TestMethod]
    public void TestBuilder_WithConstant() {
        var calculator = new ShYCalculatorBuilder()
            .WithMathematics()
            .WithConstant("MyConst", 99.0)
            .Build();

        var result = calculator.Calculate("MyConst + 1");
        Assert.IsTrue(result.Success);
        Assert.AreEqual(100.0, result.Value.Nvalue);
    }
}
