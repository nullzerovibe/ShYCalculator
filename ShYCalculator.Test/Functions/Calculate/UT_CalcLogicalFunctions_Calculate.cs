using ShYCalculator.Classes;
using ShYCalculator.Functions.Logical;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Calculate;

[TestClass]
public class UT_CalcLogicalFunctions_Calculate {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        m_environment.RegisterFunctions(new CalcLogicalFunctions());
    }

    private void CheckBool(string expression, bool expected) {
        var result = m_shyCalculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Calculation failed for '{expression}': {result.Message}");
        Assert.AreEqual(DataType.Boolean, result.DataType, $"Expected Boolean for '{expression}'");
        Assert.AreEqual(expected, result.Bvalue!.Value, $"Wrong result for '{expression}'");
    }

    [TestMethod]
    public void Logical_And_Extensive() {
        // 'all' is AND
        CheckBool("all(true)", true);
        CheckBool("all(false)", false);
        CheckBool("all(true, true)", true);
        CheckBool("all(true, false)", false);
        CheckBool("all(false, false)", false);
        CheckBool("all(true, true, true)", true);
        CheckBool("all(true, true, false)", false);
    }

    [TestMethod]
    public void Logical_Or_Extensive() {
        // 'any' is OR
        CheckBool("any(true)", true);
        CheckBool("any(false)", false);
        CheckBool("any(true, false)", true);
        CheckBool("any(false, true)", true);
        CheckBool("any(false, false)", false);
        CheckBool("any(true, true)", true);
        CheckBool("any(false, false, true)", true);
    }

    [TestMethod]
    public void Logical_Not_Extensive() {
        CheckBool("not(true)", false);
        CheckBool("not(false)", true);
        CheckBool("not(not(true))", true);
        // Nested
        CheckBool("not(all(true, false))", true);
        CheckBool("not(any(false, false))", true);
        // Comparisons
        CheckBool("not(1 < 2)", false);
        CheckBool("not(1 > 2)", true);
    }

    [TestMethod]
    public void Logical_Nested_Complex() {
        CheckBool("not(any(true, false))", false);
        CheckBool("all(not(true), not(false))", false);

        CheckBool("not(any(false, false))", true);
        CheckBool("all(not(false), not(false))", true);

        CheckBool("any(all(true, true), all(false, false))", true);
        CheckBool("all(any(true, false), any(false, true))", true);
    }
}
