using ShYCalculator.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace ShYCalculator.Test.Integration.Operators;

[TestClass]
public class UT_Comparison {
    private readonly ShYCalculator m_calculator = new();
    private Dictionary<int, double[]> m_numbers = [];

    [TestInitialize]
    public void Setup() {
        // Setup variables for string/date tests
        m_calculator.Environment.SetVariables(new Dictionary<string, Value> {
            { "$string1", new Value() { Svalue = "test\"s", DataType = DataType.String } },
            { "$string2", new Value() { Svalue = "test\"s w`ith\"more t'es`t's", DataType = DataType.String } },
            { "$date1", new Value() { Dvalue = new DateTimeOffset(2023, 8, 16, 12, 0 , 0, TimeSpan.Zero), DataType = DataType.Date } },
            { "$date2", new Value() { Dvalue = new DateTimeOffset(2023, 8, 17, 12, 0 , 0, TimeSpan.Zero), DataType = DataType.Date } },
        });

        // Setup numbers for data-driven tests
        m_numbers = GenerateTestNumbers(6, 20);
    }

    #region Data Driven Numeric Tests

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
    public void Comparison_Numeric_Basic() {
        var functions = new Dictionary<string, Func<double, double, bool>> {
            { "{0} < {1}", (a, b) => a < b },
            { "{0} > {1}", (a, b) => a > b },
            { "{0} <= {1}", (a, b) => a <= b },
            { "{0} >= {1}", (a, b) => a >= b },
            { "{0} == {1}", (a, b) => a == b },
            { "{0} != {1}", (a, b) => a != b },
        };

        CheckResult(true, functions);
        CheckResult(false, functions);
    }

    [TestMethod]
    public void Comparison_Numeric_Expression() {
        var functions = new Dictionary<string, Func<double, double, bool>> {
            { "{0} < {1} - {0}", (a, b) => a < b - a },
            { "{0} > {1} + {0}", (a, b) => a > b + a },
            { "{0} <= {1} + {0}", (a, b) => a <= b + a },
            { "{0} >= {1} - {0}", (a, b) => a >= b - a },
            { "{0} * {0} == {1} * {0}", (a, b) => a * a == b * a },
            { "{0} / 2 != {1} / 2", (a, b) => a / 2 != b / 2 },
        };

        CheckResult(true, functions);
        CheckResult(false, functions);
    }

    #endregion

    #region String Comparison

    private void CheckDoubleResult(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue!.Value, 1e-10);
    }

    [TestMethod]
    public void Comparison_String() {
        // Using ternary to verify boolean result as numbers for convenience
        CheckDoubleResult("$string1 == $string1 ? 3 : 4", 3);
        CheckDoubleResult("$string1 != $string1 ? 3 : 4", 4);

        CheckDoubleResult("$string2 == $string2 ? 3 : 4", 3);
        CheckDoubleResult("$string2 != $string2 ? 3 : 4", 4);

        CheckDoubleResult("$string1 == \"test\\\"s\" ? 3 : 4", 3);
        CheckDoubleResult("$string2 == \"test\\\"s w`ith\\\"more t'es`t's\" ? 3 : 4", 3);

        // Concatenation then compare
        CheckDoubleResult("$string1 + $string1 == $string1 + $string1 ? 3 : 4", 3);
        CheckDoubleResult("`some text` + 'some ' + `text` == \"some text\" + `some` + ' ' + 'text' ? 3 : 4", 3);
    }

    #endregion

    #region Date Comparison

    [TestMethod]
    public void Comparison_Date() {
        CheckDoubleResult("$date1 == $date1 ? 3 : 4", 3);
        CheckDoubleResult("$date2 == $date2 ? 3 : 4", 3);
        CheckDoubleResult("$date2 != $date1 ? 3 : 4", 3); // True -> 3

        CheckDoubleResult("$date1 < $date2 ? 3 : 4", 3); // True -> 3
        CheckDoubleResult("$date1 > $date2 ? 3 : 4", 4); // False -> 4

        CheckDoubleResult("$date2 >= $date2 ? 3 : 4", 3); // True -> 3
        CheckDoubleResult("$date1 <= $date1 ? 3 : 4", 3); // True -> 3
    }
    #endregion
}
