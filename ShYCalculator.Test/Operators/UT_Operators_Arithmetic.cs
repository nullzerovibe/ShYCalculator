using ShYCalculator.Classes;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace ShYCalculator.Test.Operators;

[TestClass]
public class UT_Operators_Arithmetic {
    private readonly ShYCalculator m_shyCalculator = new();

    [TestInitialize]
    public void TestInitialize() {
        m_shyCalculator.Environment.SetVariables(new Dictionary<string, Value> {
            { "$five", new Value() { Nvalue = 5, DataType = DataType.Number } },
            { "$four", new Value() { Nvalue = 4, DataType = DataType.Number } },
            { "$three", new Value() { Nvalue = 3, DataType = DataType.Number } },
            { "$two", new Value() { Nvalue = 2, DataType = DataType.Number } },
            { "$one", new Value() { Nvalue = 1, DataType = DataType.Number } },
        });
    }

    [TestMethod]
    public void UT01_TS01_SimpleFormulas() {
        CheckDoubleResult("1 + 2", 3);
        CheckDoubleResult("1 + 2 + 3", 6);
        CheckDoubleResult("1 + 2 + 3 + 4", 10);

        CheckDoubleResult("3 - 2", 1);
        CheckDoubleResult("10 - 5 - 4", 1);
        CheckDoubleResult("10 - 5 - 4 - 0 - 1", 0);

        CheckDoubleResult("3 − 2", 1);
        CheckDoubleResult("10 − 5 - 4", 1);
        CheckDoubleResult("10 − 5 - 4 − 0 - 1", 0);

        CheckDoubleResult("1 * 2", 2);
        CheckDoubleResult("1 * -2 * 3", -6);
        CheckDoubleResult("-1 * 2 * -3 * 4 * -1", -24);

        CheckDoubleResult("1 × 2", 2);
        CheckDoubleResult("1 * -2 × 3", -6);
        CheckDoubleResult("-1 × 2 * -3 × 4 * -1", -24);

        CheckDoubleResult("81 / 3 / 3", 9);
        CheckDoubleResult("-81 / 3 / 9", -3);
        CheckDoubleResult("81 / 3 / 3 / 3", 3);

        CheckDoubleResult("81 ÷ 3 ÷ 3", 9);
        CheckDoubleResult("-81 ÷ 3 / 9", -3);
        CheckDoubleResult("81 ÷ 3 / 3 ÷ 3", 3);

        CheckDoubleResult("2 ^ 2", 4);
        CheckDoubleResult("2 ^ 3", 8);
        CheckDoubleResult("2 ^ 3 ^ 1", 8);

        CheckDoubleResult("9 % 2", 1);
        CheckDoubleResult("7 % 5", 2);
        CheckDoubleResult("20 % 5", 0);

        CheckDoubleResult("1 + 2 * 3", 7);
        CheckDoubleResult("3 + 4 * 2", 11);
        CheckDoubleResult("9 / 3 * 9 + 3", 30);
        CheckDoubleResult("9 / 3 * -9 + 3", -24);
        CheckDoubleResult("9 * 3 / 9 - 3", 0);
        CheckDoubleResult("9 * 3^2 / 9 - 3", 6);
        CheckDoubleResult("8 + 6 * 3 % 4 - 2", 8);

        CheckDoubleResult("-1 -1", -2);
        CheckDoubleResult("-1 + -1", -2);
        CheckDoubleResult("-1 --1", 0);
        CheckDoubleResult("-1 +--1", 0);
        CheckDoubleResult("-1 +---1", -2);
    }

    [TestMethod]
    public void UT01_TS02_FunctionsAndPharentesis() {
        CheckDoubleResult("log2(e)", Math.Log2(Math.E));
        CheckDoubleResult("abs(-5)", Math.Abs(-5));
        CheckDoubleResult("sqrt(16)", Math.Sqrt(16));
        CheckDoubleResult("ln(e)", Math.Log(Math.E));
        CheckDoubleResult("log(e, 4)", Math.Log(Math.E, 4));
        CheckDoubleResult("log2(e)", Math.Log2(Math.E));
        CheckDoubleResult("log10(e)", Math.Log10(Math.E));
        CheckDoubleResult("pi*2", Math.PI * 2);
        CheckDoubleResult("max(1,3)", 3);
        CheckDoubleResult("min(8, 3)", 3);
        CheckDoubleResult("sin(e)", Math.Sin(Math.E));
        CheckDoubleResult("cos(pi)", Math.Cos(Math.PI));
        CheckDoubleResult("sin(pi)", Math.Sin(Math.PI));

        CheckDoubleResult("3^2", 9);
        CheckDoubleResult("3^3", 27);
        CheckDoubleResult("3^4", 81);
        CheckDoubleResult("9^(1/2)", 3);
        CheckDoubleResult("sqrt(9)", 3);
        CheckDoubleResult("27^(1/3)", 3);
        CheckDoubleResult("81^(1/4)", 3);
        CheckDoubleResult("81^(1/3)", 4.3267487109222245);
        CheckDoubleResult("2 ^ 4 ^ (1/2)", 4);

        CheckDoubleResult("3^2 / 3", 3);
        CheckDoubleResult("3^4 / 3^2", 9);
        CheckDoubleResult("3^4 / 3^2^2", 1);
        CheckDoubleResult("( 1 - 5 ) ^ 2 ^ 3", 65536);
        CheckDoubleResult("3 + 4 * 2 / ( 1 - 5 ) ^ 2 ^ 3", 3.0001220703125);
        CheckDoubleResult("max(9,3) / min(8, 3) * sqrt(81) + abs(-3)", 30);
        CheckDoubleResult("-1 * abs(max(9,3)/3*pi)", -9.42477796076938);
        CheckDoubleResult("-1 * abs(max(9,3)/3*pi*-1/3)", -3.1415926535897931);
        CheckDoubleResult("max(10, min(5, 7)) * abs(-2) / pow(pi, 2)", 2.0264236728467555);
        CheckDoubleResult("max(10, min(5, 7)) * max(8, abs(-2)) / pow(pi, 3)", 2.5801227546559593);
        CheckDoubleResult("tan(max(180,360))", -3.3801404139609579);
        CheckDoubleResult("sin( max ( 2, 3 ) / 3 * pi )", 1.2246467991473532E-16);
        CheckDoubleResult("-1 * abs(max(9,-3)/3*pi*-1/3 - 2)", -5.1415926535897931);
        CheckDoubleResult("abs(max(9,3)/3*pi-1/3)", 9.0914446274360454);
        CheckDoubleResult("-1 * abs(max(9,-3)/3*pi-1/3)", -9.0914446274360454);
        CheckDoubleResult("-1 * abs(max(9,3)/3*pi-1/3) - 3", -12.0914446274360454);
        CheckDoubleResult("max(((1+3),()((2+--(--4))),(((3)))))", 6);
        CheckDoubleResult("((1 + 2) * (3 + 4)) / (2 + 3)", 4.2);
        CheckDoubleResult("((10 / 2) + (5 * 3)) - ((8 / 4) * 2)", 16);
        CheckDoubleResult("(2 + 3) * (4 + 5) * (1 / (9 + 1))", 4.5);
        CheckDoubleResult("((((1 + 1) + 1) + 1) + 1)", 5);
        CheckDoubleResult("abs(((-2) * (-3)) + (-4))", 2);
    }

    [TestMethod]
    public void UT01_TS03_VariablesFormulas() {
        CheckDoubleResult("-1 +--$one", 0);
        CheckDoubleResult("-1 ---$one", -2);
        CheckDoubleResult("max(-$one,-3)", -1);
        CheckDoubleResult("min(-8, -$three)", -8);
        CheckDoubleResult("min(8, $three, 5, 1)", 1);
        CheckDoubleResult("max(8, $three, 5, 1)", 8);
        CheckDoubleResult("avg(8, $three, 5, 1)", 4.25);
        CheckDoubleResult("9 / $three * 9 * 1 + $three", 30);
        CheckDoubleResult("max(max(9,$three),min(8, 3)) / min(min(8, $three), max(9,$three)) * sqrt(abs($three * -9 * $three)) * -1 + -abs(-$three)", -30);
        CheckDoubleResult("abs(-$five * $two)", 10);
        CheckDoubleResult("sqrt(4 * $two * $two)", 4);
        CheckDoubleResult("sqrt($four * 4)", 4);
        CheckDoubleResult("max(--$one,-$four)", 1);
        CheckDoubleResult("min($four*---2, -$four/$one)", -8);
        CheckDoubleResult("max((--$one,-$four))", 1);
        CheckDoubleResult("max((--$one),(-$four))", 1);
        CheckDoubleResult("max(((--$one),-$four))", 1);
        CheckDoubleResult("min(((($four*---2)), (-$four/$one)))", -8);
    }

    [TestMethod]
    public void UT01_TS04_FormulasWithNegations() {
        CheckDoubleResult("max(--$one,-$four)", 1);
        CheckDoubleResult("--max(---$one,---$four)", -1);
        CheckDoubleResult("-max(----$one,--$four)", -4);
        CheckDoubleResult("---max(-----$one,-$four)", 1);
        CheckDoubleResult("--max(---$one,---$four) -1", -2);
        CheckDoubleResult("-max(----$one,--$four) -1", -5);
        CheckDoubleResult("---max(-----$one,-$four) + --1", 2);
        CheckDoubleResult("max(-$one,-3)", -1);
        CheckDoubleResult("min(-8, -$three)", -8);
        CheckDoubleResult("-1 +--1", 0);
        CheckDoubleResult("-1 +---1", -2);
        CheckDoubleResult("-1 / -1", 1);
        CheckDoubleResult("15 * -3", -45);
        CheckDoubleResult("81 / -3", -27);
        CheckDoubleResult("81 / -3 / 3 / -3", 3);
        CheckDoubleResult("2 ^ 3 ^ -1", 1.2599210498948732);
    }

    [TestMethod]
    public void UT01_TS05_Bitwise() {
        CheckDoubleResult("7 & 1", 1);
        CheckDoubleResult("15 & 10", 10);
        CheckDoubleResult("31 & 20", 20);
        CheckDoubleResult("63 & 36", 36);
        CheckDoubleResult("7 | 2", 7);
        CheckDoubleResult("15 | 15", 15);
        CheckDoubleResult("15 | 16", 31);

        // XOR with Unicode symbol ⊕
        CheckDoubleResult("7 ⊕ 2", 5);
        CheckDoubleResult("63 ⊕ 63", 0);
        CheckDoubleResult("63 ⊕ 6", 57);

        // XOR with ASCII alias ^^
        CheckDoubleResult("7 ^^ 2", 5);
        CheckDoubleResult("63 ^^ 63", 0);
        CheckDoubleResult("63 ^^ 6", 57);
        CheckDoubleResult("(10 + 5) ^^ (8 + 7)", 0);

        // BitwiseNot ~
        CheckDoubleResult("~7", -8);
        CheckDoubleResult("~0", -1);
        CheckDoubleResult("~(-1)", 0);
        CheckDoubleResult("~~7", 7);
    }

    [TestMethod]
    public void UT01_TS06_test123() {
        //CheckDoubleResult("6/2(2+1)", 9);

        CheckDoubleResult("-1*4", -4);
        CheckDoubleResult("1+4", 5);
    }

    [TestMethod]
    public void UT01_TS07_ComplexArithmeticBitwise() {
        // Complex Precedence
        CheckDoubleResult("1 + 2 * 3 - 4 / 2 + (5 * 2)", 15); // 1 + 6 - 2 + 10 = 15
        CheckDoubleResult("((1 + 2) * 3) - (4 / (2 + 2))", 8); // (3 * 3) - (4 / 4) = 9 - 1 = 8

        // Bitwise mixed with arithmetic
        // (1+2) & (7-4) -> 3 & 3 -> 3
        CheckDoubleResult("(1 + 2) & (7 - 4)", 3);

        // Xor
        // (10 + 5) ⊕ (8 + 7) -> 15 ⊕ 15 -> 0
        CheckDoubleResult("(10 + 5) ⊕ (8 + 7)", 0);

        // Nested unary and parens
        // -(-(-1)) -> -1
        CheckDoubleResult("-(-(-1))", -1);
        CheckDoubleResult("-( -( 1 + 1 ) )", 2);

        // Power mixing
        // 2 ^ 3 ^ 2 -> 2 ^ 9 -> 512 (Right associative often, but here checked left usually? let's see standard)
        // Standard Math.Pow(2, Math.Pow(3, 2)) = 512. 
        // If left: (2^3)^2 = 64.
        // My earlier tests: 2^3^1 = 8. (2^3)^1 = 8. 2^(3^1) = 8.
        // Let's rely on defined structure.
        CheckDoubleResult("2 ^ 3 ^ 2", 512);
    }


    internal void CheckDoubleResult(string testString, double expected) {
        if (m_shyCalculator == null) {
            Assert.Fail($"ShYCalculator not initialized");
            return;
        }

        try {
            var result = m_shyCalculator.Calculate(testString);

            if (result.DataType == Classes.DataType.Number) {
                Assert.IsTrue(result.Success && result.Nvalue == expected, $"Failed for testString: '{testString}', expected: '{expected}', result: '{result}'");
                return;
            }

            if (result.DataType == Classes.DataType.Boolean) {
                Assert.IsTrue(result.Success && result.Bvalue == (expected == 1), $"Failed for testString: '{testString}', expected: '{expected}', result: '{result}'");
                return;
            }

            Assert.Fail($"Unexpected Error for testString: '{testString}', expected: '{expected}'");
        }
        catch (Exception ex) {
            Assert.Fail($"Unexpected Error for testString: '{testString}', expected: '{expected}', error: '{ex}'");
        }
    }
}
