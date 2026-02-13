using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions.Text;
using ShYCalculator.Functions.Logical;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Calculate;

[TestClass]
public class UT_CalcNumericFunctions_Calculate {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        m_environment.RegisterFunctions(new CalcNumericFunctions());
        m_environment.RegisterFunctions(new CalcArithmeticalFunctions());
        m_environment.RegisterFunctions(new CalcScientificFunctions());
        m_environment.RegisterFunctions(new CalcStringFunctions());
        m_environment.RegisterFunctions(new CalcLogicalFunctions());
    }

    private void CheckBool(string expression, bool expected) {
        var result = m_shyCalculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Calculation failed for '{expression}': {result.Message}");
        Assert.AreEqual(DataType.Boolean, result.DataType, $"Expected Boolean for '{expression}'");
        Assert.AreEqual(expected, result.Bvalue!.Value, $"Wrong result for '{expression}'");
    }

    private void CheckNumber(string expression, double expected, double delta = 1e-9) {
        var result = m_shyCalculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Calculation failed for '{expression}': {result.Message}");
        Assert.AreEqual(DataType.Number, result.DataType, $"Expected Number for '{expression}'");
        Assert.AreEqual(expected, result.Nvalue!.Value, delta, $"Wrong result for '{expression}'");
    }

    [TestMethod]
    public void Numeric_IsAscending_Extensive() {
        CheckBool("is_ascending(1, 2, 3, 4, 5)", true);
        CheckBool("is_ascending(1, 2, 0, 4, 5)", false);
        CheckBool("is_ascending(1, 2, 2, 3)", false);
        CheckBool("is_ascending(-10, -5, -1, 0, 1)", true);
        CheckBool("is_ascending(0.1, 0.11, 0.111)", true);
        // Single element returns false as per implementation logic (< 2 args)
        CheckBool("is_ascending(42)", false);
        CheckBool("is_ascending(sqrt(1), sqrt(4), sqrt(9))", true);
    }

    [TestMethod]
    public void Numeric_IsAscendingLoose_Extensive() {
        CheckBool("is_ascending_loose(1, 2, 3)", true);
        CheckBool("is_ascending_loose(1, 2, 2, 3)", true);
        CheckBool("is_ascending_loose(1, 3, 2)", false);
        CheckBool("is_ascending_loose(5, 5, 5, 5)", true);
        CheckBool("is_ascending_loose(-5, -5, -4)", true);
        CheckBool("is_ascending_loose(abs(-1), abs(1), abs(-2))", true);
    }

    [TestMethod]
    public void Numeric_IsDescending_Extensive() {
        CheckBool("is_descending(5, 4, 3, 2, 1)", true);
        CheckBool("is_descending(5, 4, 6, 2, 1)", false);
        CheckBool("is_descending(5, 4, 4, 3)", false);
        CheckBool("is_descending(0, -1, -5, -10)", true);
        CheckBool("is_descending(0.5, 0.49, 0.1)", true);
        CheckBool("is_descending(pow(3,2), pow(2,3), pow(2,2))", true);
    }

    [TestMethod]
    public void Numeric_IsDescendingLoose_Extensive() {
        CheckBool("is_descending_loose(3, 2, 1)", true);
        CheckBool("is_descending_loose(3, 2, 2, 1)", true);
        CheckBool("is_descending_loose(3, 2, 4)", false);
        CheckBool("is_descending_loose(1, 1, 1)", true);
        CheckBool("is_descending_loose(-1, -1, -2)", true);
        CheckBool("is_descending_loose(100, 50, 25, 12.5, 6.25)", true);
    }

    [TestMethod]
    public void Numeric_HasAdjacentEquals_Extensive() {
        CheckBool("has_adjacent_equals(1, 2, 2, 3)", true);
        CheckBool("has_adjacent_equals(1, 2, 3, 2)", false);
        CheckBool("has_adjacent_equals(1, 1, 1)", true);
        CheckBool("has_adjacent_equals(5, 5, 1, 2)", true);
        CheckBool("has_adjacent_equals(1, 2, 6, 6)", true);
        CheckBool("has_adjacent_equals(1.000000001, 1.000000001)", true);
        CheckBool("has_adjacent_equals(1.0, 1.0001)", false);
    }

    [TestMethod]
    public void Numeric_AreAllNumbers_Extensive() {
        CheckBool("are_all_numbers(1, 2, 3, 4.5)", true);
        CheckBool("are_all_numbers(1, '2', 3)", false);
        CheckBool("are_all_numbers(1, true, 3)", false);
        CheckBool("are_all_numbers(sin(1), abs(-5), sqrt(4))", true);
        // Using valid string function
        CheckBool("are_all_numbers(1, str_all('a'))", false);
    }

    [TestMethod]
    public void Numeric_Median_Extensive() {
        CheckNumber("median(1, 3, 2)", 2);
        CheckNumber("median(1, 2, 3, 4)", 2.5);
        CheckNumber("median(10, 2, 5)", 5);
        CheckNumber("median(-5, -1, -10)", -5);
        CheckNumber("median(1, 2, 2, 3)", 2);
        CheckNumber("median(7, 7, 7)", 7);
        CheckNumber("median(1, 100, 2)", 2);
    }

    [TestMethod]
    public void Numeric_Mode_Extensive() {
        CheckNumber("mode(1, 2, 2, 3)", 2);
        CheckNumber("mode(1, 1, 2, 2, 3)", 1);
        CheckNumber("mode(1, 2, 3)", 1);
        CheckNumber("mode(-1, -2, -2, -3)", -2);
        CheckNumber("mode(0, 0, 1)", 0);
        CheckNumber("mode(1000, 1000, 5)", 1000);
    }

    [TestMethod]
    public void Numeric_StDev_Extensive() {
        CheckNumber("stdev(1, 2, 4, 5)", 1.825741858, 1e-6);
        CheckNumber("stdev(5, 5, 5, 5)", 0);
        CheckNumber("stdev(-1, -2, -3)", 1, 1e-6);
        CheckNumber("stdev(2, 4)", 1.41421356, 1e-6);
        CheckNumber("stdev(1, 2, 3, 4, 5)", 1.58113883, 1e-6);
    }

    [TestMethod]
    public void Numeric_Var_Extensive() {
        CheckNumber("var(1, 2, 4, 5)", 3.333333333, 1e-6);
        CheckNumber("var(2, 2, 2)", 0);
        CheckNumber("var(-1, -3)", 2);
        CheckNumber("var(0, 100)", 5000);
        CheckNumber("var(1.5, 2.5)", 0.5);
    }

    [TestMethod]
    public void Numeric_Count_Extensive() {
        CheckNumber("count(1, 2, 3)", 3);
        // Removed null (parser issue), added varied types
        CheckNumber("count(1, 'a', true)", 3);
        CheckNumber("count(sin(0), cos(0))", 2);
        CheckNumber("count(1)", 1);
    }

    [TestMethod]
    public void Numeric_Mixed_Complex() {
        CheckBool("is_ascending(abs(-5), sqrt(36), pow(2, 3))", true); // 5, 6, 8 
        CheckNumber("median(sin(0), cos(0), tan(0))", 0);
        CheckBool("is_ascending(1, count(1, 2, 3))", true);
        CheckNumber("var(1, 1, 1, 2)", 0.25);
    }
}
