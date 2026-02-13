using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Calculate;

[TestClass]
public class UT_CalcArithmeticalFunctions_Calculate {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;
    private CalcArithmeticalFunctions arithFuncs = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;
        arithFuncs = new CalcArithmeticalFunctions();

        m_environment.RegisterFunctions(arithFuncs);
        m_environment.RegisterFunctions(new CalcScientificFunctions());
        m_environment.RegisterFunctions(new CalcNumericFunctions());
    }

    private void CheckNumber(string expression, double expected, double delta = 1e-9) {
        var result = m_shyCalculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Calculation failed for '{expression}': {result.Message}");
        Assert.AreEqual(DataType.Number, result.DataType, $"Expected Number for '{expression}'");
        Assert.AreEqual(expected, result.Nvalue!.Value, delta, $"Wrong result for '{expression}'");
    }

    [TestMethod]
    public void Arith_Abs_Extensive() {
        CheckNumber("abs(-5)", 5);
        CheckNumber("abs(5)", 5);
        CheckNumber("abs(0)", 0);
        CheckNumber("abs(-3.14159)", 3.14159);
        CheckNumber("abs(-0.000001)", 0.000001);
    }

    [TestMethod]
    public void Arith_Min_Extensive() {
        CheckNumber("min(1, 2)", 1);
        CheckNumber("min(10, -5, 3)", -5);
        CheckNumber("min(0, 0, 0)", 0);
        CheckNumber("min(1.5, 1.500001)", 1.5);
        CheckNumber("min(-100, -200)", -200);
        CheckNumber("min(5, 5, 1)", 1);
    }

    [TestMethod]
    public void Arith_Max_Extensive() {
        CheckNumber("max(1, 2)", 2);
        CheckNumber("max(-5, -10, -1)", -1);
        CheckNumber("max(0, 0.0001)", 0.0001);
        CheckNumber("max(100, 100, 100)", 100);
        CheckNumber("max(-1, 0, 1)", 1);
        CheckNumber("max(3.14, 3.141)", 3.141);
    }

    [TestMethod]
    public void Arith_Avg_Extensive() {
        CheckNumber("avg(2, 4)", 3);
        CheckNumber("avg(1, 2, 3)", 2);
        CheckNumber("avg(-5, 5)", 0);
        CheckNumber("avg(0, 0, 0)", 0);
        CheckNumber("avg(1.1, 2.2, 3.3)", 2.2, 1e-6);
        CheckNumber("avg(10)", 10);
    }

    [TestMethod]
    public void Arith_Sum_Extensive() {
        CheckNumber("sum(1, 2, 3)", 6);
        CheckNumber("sum(-1, 1)", 0);
        CheckNumber("sum(0, 0)", 0);
        CheckNumber("sum(1.5, 2.5)", 4);
        CheckNumber("sum(10)", 10);
        // Alias
        CheckNumber("∑(1, 2, 3)", 6);
    }

    [TestMethod]
    public void Arith_Pow_Extensive() {
        CheckNumber("pow(2, 3)", 8);
        CheckNumber("pow(5, 0)", 1);
        CheckNumber("pow(10, -1)", 0.1);
        CheckNumber("pow(9, 0.5)", 3);
        CheckNumber("pow(2, 10)", 1024);
        CheckNumber("pow(-2, 2)", 4);
    }

    [TestMethod]
    public void Arith_Sqrt_Extensive() {
        CheckNumber("sqrt(16)", 4);
        CheckNumber("sqrt(2)", 1.41421356, 1e-6);
        CheckNumber("sqrt(1)", 1);
        CheckNumber("sqrt(0)", 0);
        CheckNumber("sqrt(0.25)", 0.5);
    }

    [TestMethod]
    public void Arith_Round_Extensive() {
        // 1. Basic rounding
        CheckNumber("round(1.2)", 1);
        CheckNumber("round(1.8)", 2);
        // 2. Midpoint rounding (Banker's rounding usually in C#, verify behavior)
        // Math.Round(1.5) -> 2, Math.Round(2.5) -> 2 (ToEven is default)
        // Let's verify standard .NET behavior
        CheckNumber("round(1.5)", 2);
        CheckNumber("round(2.5)", 2);
        // 3. With digits
        CheckNumber("round(3.14159, 2)", 3.14);
        CheckNumber("round(3.145, 2)", 3.14); // 3.145 -> 3.14 (ToEven)
        CheckNumber("round(3.146, 2)", 3.15);
        // 4. Zero digits
        CheckNumber("round(123.456, 0)", 123);
        // 5. Negative numbers
        CheckNumber("round(-1.2)", -1);
        CheckNumber("round(-1.8)", -2);
    }

    [TestMethod]
    public void Arith_Floor_Extensive() {
        CheckNumber("floor(1.9)", 1);
        CheckNumber("floor(1.1)", 1);
        CheckNumber("floor(-1.1)", -2); // Important property of floor
        CheckNumber("floor(-1.9)", -2);
        CheckNumber("floor(5)", 5);
        CheckNumber("floor(0)", 0);
    }

    [TestMethod]
    public void Arith_Ceiling_Extensive() {
        CheckNumber("ceiling(1.1)", 2);
        CheckNumber("ceiling(1.9)", 2);
        CheckNumber("ceiling(-1.1)", -1);
        CheckNumber("ceiling(-1.9)", -1);
        CheckNumber("ceiling(5)", 5);
        CheckNumber("ceiling(0)", 0);
    }

    [TestMethod]
    public void Arith_Trunc_Extensive() {
        CheckNumber("trunc(1.9)", 1);
        CheckNumber("trunc(-1.9)", -1);
        CheckNumber("trunc(1.1)", 1);
        CheckNumber("trunc(-1.1)", -1);
        CheckNumber("trunc(0.5)", 0);
        CheckNumber("trunc(0)", 0);
    }

    [TestMethod]
    public void Arith_Sign_Extensive() {
        CheckNumber("sign(10)", 1);
        CheckNumber("sign(-10)", -1);
        CheckNumber("sign(0)", 0);
        CheckNumber("sign(0.0001)", 1);
        CheckNumber("sign(-0.0001)", -1);
    }

    [TestMethod]
    public void Arith_Random_Extensive() {
        // We can't strict equals random, but we can check bounds
        // range [0, 1)
        var res = m_shyCalculator.Calculate("random()");
        Assert.IsTrue(res.Nvalue >= 0 && res.Nvalue < 1);

        // range [min, max)
        res = m_shyCalculator.Calculate("random(10, 20)");
        Assert.IsTrue(res.Nvalue >= 10 && res.Nvalue < 20);

        // Same min/max
        res = m_shyCalculator.Calculate("random(5, 5)");
        Assert.AreEqual(5, res.Nvalue);

        // Check integer nature if possible? The implementation used _random.Next(min, max) which returns int.
        // Let's verify it returns integer values for args.
        res = m_shyCalculator.Calculate("random(1, 10)");
        Assert.AreEqual(Math.Floor(res.Nvalue!.Value), res.Nvalue);

        // Negative range
        res = m_shyCalculator.Calculate("random(-10, -5)");
        Assert.IsTrue(res.Nvalue >= -10 && res.Nvalue < -5);
    }

    [TestMethod]
    public void Arith_Nested_Coverage() {
        // Sqrt inside Pow
        CheckNumber("pow(sqrt(16), 2)", 16); // 4^2 = 16

        // Abs inside Sqrt (handling negative input safely)
        CheckNumber("sqrt(abs(-16))", 4);

        // Sum of varied functions
        CheckNumber("sum(floor(1.5), ceiling(1.5))", 3); // 1 + 2 = 3

        // Min/Max logic
        CheckNumber("max(min(10, 5), 0)", 5);

        // Sign composition
        CheckNumber("sign(abs(-5) * -1)", -1);
    }
}
