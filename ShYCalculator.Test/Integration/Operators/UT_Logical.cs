using ShYCalculator.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace ShYCalculator.Test.Integration.Operators;

[TestClass]
public class UT_Logical {
    private readonly ShYCalculator m_calculator = new();
    private Dictionary<int, double[]> m_numbers = [];

    [TestInitialize]
    public void Setup() {
        m_numbers = UT_Comparison.GenerateTestNumbers(6, 20); // Reuse helper if possible, or duplicate. 
        // Generics or shared class would be better, but duplication for valid separation is okay. 
        // Actually, let's duplicate the generator to keep file independent.
        m_numbers = GenerateTestNumbers(6, 20);
    }

    internal static Dictionary<int, double[]> GenerateTestNumbers(int dict_count, int array_count) {
        var random = new Random();
        var test_numbers = new Dictionary<int, double[]>();

        for (int di = 0; di < dict_count; di++) {
            var key = (int)Math.Pow(2, di);
            var numbers = new double[array_count];
            for (int ai = 0; ai < array_count; ai++) {
                var n = (int)Math.Pow(10, di);
                numbers[ai] = random.NextDouble() * (2 * n) - (1 * n);
            }
            test_numbers[key] = numbers;
        }
        return test_numbers;
    }

    internal void CheckResult(bool randomize, Dictionary<string, Func<double, double, bool>> functions) {
        foreach (var function in functions) {
            foreach (var numbers in m_numbers) {
                foreach (var number in numbers.Value) {
                    var number2 = randomize ? RandomizeNumber(number) : number;
                    string formula = string.Format(function.Key, number.ToString(NumberFormatInfo.InvariantInfo), number2.ToString(NumberFormatInfo.InvariantInfo));

                    var result = m_calculator.Calculate(formula);
                    bool expected = function.Value(number, number2);

                    if (result.Success && result.DataType == DataType.Boolean) {
                        Assert.AreEqual(expected, result.Bvalue!.Value, $"Failed for: {formula}");
                    }
                    else {
                        Assert.Fail($"Failed execution for: {formula}. Msg: {result.Message}");
                    }
                }
            }
        }
    }

    static double RandomizeNumber(double number, double percentage = 1) {
        var rnd = new Random();
        double sign = rnd.Next(2) * 2 - 1;
        double change = number * percentage / 100 * sign;
        return number + change;
    }

    [TestMethod]
    public void Logical_AndOr_Numeric() {
        var functions = new Dictionary<string, Func<double, double, bool>> {
            { "{0} < {1} && {1} > {0}", (a, b) => a < b && b > a },
            { "{0} > {1} && {1} < {0}", (a, b) => a > b && b < a },
            { "{0} < {1} || {1} < {0}", (a, b) => a < b || b < a },
            { "{0} > {1} || {1} > {0}", (a, b) => a > b || b > a },
            { "{0} <= {1} && {1} > {0}", (a, b) => a <= b && b > a },
            { "{0} >= {1} && {1} < {0}", (a, b) => a >= b && b < a },
            { "{0} == {1} || {1} < {0}", (a, b) => a == b || b < a },
            { "{0} != {1} && {1} < {0} * 2", (a, b) => a != b && b < a * 2 },
        };

        CheckResult(true, functions);
        CheckResult(false, functions);
    }

    [TestMethod]
    public void Logical_Constants_Recursion() {
        var functions = new Dictionary<string, Func<double, double, bool>> {
            { "true && true", (a, b) => true },
            { "true && false", (a, b) => false },
            { "false && false", (a, b) => false },
            { "true || true", (a, b) => true },
            { "true || false", (a, b) => true },
            { "false || false", (a, b) => false },
            { "{0} < {1} && true", (a, b) => a < b },
            { "{0} > {1} && true", (a, b) => a > b },
            { "{0} < {1} || true", (a, b) => true },
            { "{0} > {1} || false", (a, b) => a > b },
            { "({0} < {1}) == true && ({1} > {0}) == true", (a, b) => (a < b) && (b > a) },
            { "true && (false || true) && true", (a, b) => true },
            { "false || (true && false) || false", (a, b) => false },
        };
        CheckResult(true, functions);
        CheckResult(false, functions);
    }

    [TestMethod]
    public void Logical_Not_Basic() {
        CheckBoolean("!true", false);
        CheckBoolean("!false", true);
        CheckBoolean("!!true", true);
        CheckBoolean("!!false", false);
        CheckBoolean("!!!true", false);
    }

    [TestMethod]
    public void Logical_Not_Expression() {
        CheckBoolean("!true && !false", false);
        CheckBoolean("!true || !false", true);
        CheckBoolean("!(1 > 2)", true);
        CheckBoolean("!(1 < 2)", false);
    }

    private void CheckBoolean(string expression, bool expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Bvalue!.Value);
    }
}
