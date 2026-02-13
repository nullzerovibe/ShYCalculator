using System.Globalization;

namespace ShYCalculator.Test.Operators;

[TestClass]
public class UT_Operators_Logical {
    private readonly ShYCalculator m_shyCalculator = new();
    private readonly Dictionary<int, double[]> m_numbers = [];

    [TestInitialize]
    public void TestInitialize() {
        foreach (var number in GenerateTestNumbers(6, 20)) {
            m_numbers.Add(number.Key, number.Value);
        }
    }

    [TestMethod]
    public void UT03_TS01_ComparisonOperators() {
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
    public void UT03_TS02_ComparisonOperators2() {
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

    [TestMethod]
    public void UT03_TS03_LogicalOperators() {
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
    public void UT03_TS04_LogicalOperators() {
        var functions = new Dictionary<string, Func<double, double, bool>> {
            { "true && true", (a, b) => true && true },
            { "true && false", (a, b) => true && false },
            { "false && false", (a, b) => false && false },
            { "true || true", (a, b) => true || true },
            { "true || false", (a, b) => true || false },
            { "false || false", (a, b) => false || false },
            { "{0} < {1} && true", (a, b) => a < b && true },
            { "{0} > {1} && true", (a, b) => a > b && true },
            { "{0} < {1} || true", (a, b) => a < b || true },
            { "{0} > {1} || false", (a, b) => a > b || false },
            { "({0} < {1}) == true && ({1} > {0}) == true", (a, b) => (a < b) == true && (b > a) == true },
            { "({0} < {1}) != false || ({1} > {0}) == false", (a, b) => (a < b) != false || (b > a) == false },
            { "true && (false || true) && true", (a, b) => true },
            { "false || (true && false) || false", (a, b) => false },
        };

        CheckResult(true, functions);
        CheckResult(false, functions);
    }

    internal static Dictionary<int, double[]> GenerateTestNumbers(int dict_count, int array_count) {
        var random = new Random();
        var test_numbers = new Dictionary<int, double[]>();

        for (int di = 0; di < dict_count; di++) {
            var key = (int)Math.Pow(2, di); // BitWise Key
            var numbers = new double[array_count];

            for (int ai = 0; ai < array_count; ai++) {
                var n = (int)Math.Pow(10, di); // 1, 10, 100, 1000...
                numbers[ai] = random.NextDouble() * (2 * n) - (1 * n); // -1 -> 1, -10 -> 10, -100 -> 100,...
            }

            test_numbers[key] = numbers;
        }

        return test_numbers;
    }

    internal void CheckResult(bool randomize, Dictionary<string, Func<double, double, bool>> functions, int bitWiseTest = 0) {
        if (m_shyCalculator == null) {
            Assert.Fail($"ShYCalculator not initialized");
            return;
        }

        foreach (var function in functions) {
            foreach (var numbers in m_numbers) {
                if (bitWiseTest == 0 || (bitWiseTest & numbers.Key) == numbers.Key) { // Check BitWiseTest against BitWise Key if needed
                    foreach (var number in numbers.Value) {
                        var number2 = randomize ? RandomizeNumber(number) : number;
                        string formula = string.Format(function.Key, number.ToString(NumberFormatInfo.InvariantInfo), number2.ToString(NumberFormatInfo.InvariantInfo));
                        CheckDoubleResult(formula, function.Value(number, number2));
                    }
                }
            }
        }
    }

    private void CheckDoubleResult(string testString, bool expected) {
        try {
            var result = m_shyCalculator.Calculate(testString);

            if (result.DataType == Classes.DataType.Boolean) {
                Assert.IsTrue(result.Success && result.Bvalue == expected, $"Failed for testString: '{testString}', expected: '{expected}', result: '{result}'");
                return;
            }

            Assert.Fail($"Unexpected Error for testString: '{testString}', expected: '{expected}'");
        }
        catch (Exception ex) {
            Assert.Fail($"Unexpected Error for testString: '{testString}', expected: '{expected}', error: '{ex}'");
        }
    }

    static double RandomizeNumber(double number, double percentage = 1) {
        var rnd = new Random();
        double sign = rnd.Next(2) * 2 - 1; // randomly generates 1 or -1
        double change = number * percentage / 100 * sign;
        return number + change;
    }

}
