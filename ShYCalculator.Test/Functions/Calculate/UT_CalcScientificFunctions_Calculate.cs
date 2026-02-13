using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Calculate;

[TestClass]
public class UT_CalcScientificFunctions_Calculate {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        m_environment.RegisterFunctions(new CalcScientificFunctions());
        m_environment.RegisterFunctions(new CalcArithmeticalFunctions());
    }

    private void CheckNumber(string expression, double expected, double delta = 1e-9) {
        var result = m_shyCalculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Calculation failed for '{expression}': {result.Message}");
        Assert.AreEqual(DataType.Number, result.DataType, $"Expected Number for '{expression}'");
        Assert.AreEqual(expected, result.Nvalue!.Value, delta, $"Wrong result for '{expression}'");
    }

    [TestMethod]
    public void Sci_Trig_Sin_Extensive() {
        CheckNumber("sin(0)", 0);
        CheckNumber("sin(1.570796327)", 1, 1e-7); // pi/2
        CheckNumber("sin(3.141592654)", 0, 1e-7); // pi
        CheckNumber("sin(-1.570796327)", -1, 1e-7); // -pi/2
        CheckNumber("sin(30 * 3.141592654 / 180)", 0.5, 1e-5); // 30 deg
    }

    [TestMethod]
    public void Sci_Trig_Cos_Extensive() {
        CheckNumber("cos(0)", 1);
        CheckNumber("cos(3.141592654)", -1, 1e-7); // pi
        CheckNumber("cos(1.570796327)", 0, 1e-7); // pi/2
        CheckNumber("cos(6.283185307)", 1, 1e-7); // 2pi
        CheckNumber("cos(60 * 3.141592654 / 180)", 0.5, 1e-5); // 60 deg
    }

    [TestMethod]
    public void Sci_Trig_Tan_Extensive() {
        CheckNumber("tan(0)", 0);
        CheckNumber("tan(0.785398163)", 1, 1e-7); // pi/4
        CheckNumber("tan(-0.785398163)", -1, 1e-7);
        CheckNumber("tan(3.141592654)", 0, 1e-5);
        CheckNumber("tan(45 * 3.141592654 / 180)", 1, 1e-5);
    }

    [TestMethod]
    public void Sci_Trig_Cot_Extensive() {
        CheckNumber("cot(0.785398163)", 1, 1e-7);
        CheckNumber("cot(1.570796327)", 0, 1e-7);
        CheckNumber("cot(-0.785398163)", -1, 1e-7);
    }

    [TestMethod]
    public void Sci_InverseTrig_Asin_Extensive() {
        CheckNumber("asin(0)", 0);
        CheckNumber("asin(1)", 1.570796327, 1e-7); // pi/2
        CheckNumber("asin(-1)", -1.570796327, 1e-7);
        CheckNumber("asin(0.5)", 0.523598776, 1e-7); // pi/6
    }

    [TestMethod]
    public void Sci_InverseTrig_Acos_Extensive() {
        CheckNumber("acos(1)", 0);
        CheckNumber("acos(0)", 1.570796327, 1e-7); // pi/2
        CheckNumber("acos(-1)", 3.141592654, 1e-7); // pi
        CheckNumber("acos(0.5)", 1.04719755, 1e-7); // pi/3
    }

    [TestMethod]
    public void Sci_InverseTrig_Atan_Extensive() {
        CheckNumber("atan(0)", 0);
        CheckNumber("atan(1)", 0.785398163, 1e-7); // pi/4
        CheckNumber("atan(-1)", -0.785398163, 1e-7);
        CheckNumber("atan(1000)", 1.57, 1e-2); // approaches pi/2
    }

    [TestMethod]
    public void Sci_InverseTrig_Acot_Extensive() {
        CheckNumber("acot(1)", 0.785398163, 1e-7);
        CheckNumber("acot(0)", 1.570796327, 1e-7);
        CheckNumber("acot(1000)", 0.001, 1e-3);
    }

    [TestMethod]
    public void Sci_Hyperbolic_Extended() {
        CheckNumber("sinh(0)", 0);
        CheckNumber("cosh(0)", 1);
        CheckNumber("tanh(0)", 0);
        CheckNumber("coth(1)", 1.31303528, 1e-7);

        CheckNumber("sinh(1)", 1.17520119, 1e-7);
        CheckNumber("cosh(1)", 1.54308063, 1e-7);
        CheckNumber("tanh(1)", 0.761594156, 1e-7);
    }

    [TestMethod]
    public void Sci_Log_Extended() {
        CheckNumber("ln(2.718281828)", 1, 1e-4); // e
        CheckNumber("ln(1)", 0);

        // Log requires 2 args!
        CheckNumber("log(100, 10)", 2);
        CheckNumber("log(1000, 10)", 3);
        CheckNumber("log(1, 10)", 0);

        CheckNumber("log2(8)", 3);
        CheckNumber("log2(1024)", 10);
        CheckNumber("log2(1)", 0);

        CheckNumber("log10(100)", 2);
        CheckNumber("log10(0.1)", -1);
    }

    [TestMethod]
    public void Sci_Nested_Coverage() {
        CheckNumber("sin(asin(0.5))", 0.5);
        CheckNumber("tan(atan(1))", 1);
        CheckNumber("pow(sin(1), 2) + pow(cos(1), 2)", 1);

        // log(10*100) base 10 = 3
        CheckNumber("log(10, 10) + log(100, 10)", 3);

        CheckNumber("sqrt(pow(3, 2) + pow(4, 2))", 5);
    }
}
