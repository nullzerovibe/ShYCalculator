using ShYCalculator.Classes;

namespace ShYCalculator.Test.Operators;

[TestClass]
public class UT_Operators_Ternary {
    private readonly ShYCalculator m_shyCalculator = new();

    [TestInitialize]
    public void TestInitialize() {
        m_shyCalculator.Environment.SetVariables(new Dictionary<string, Value> {
            { "$five", new Value() { Nvalue = 5, DataType = DataType.Number } },
            { "$four", new Value() { Nvalue = 4, DataType = DataType.Number } },
            { "$three", new Value() { Nvalue = 3, DataType = DataType.Number } },
            { "$two", new Value() { Nvalue = 2, DataType = DataType.Number } },
            { "$one", new Value() { Nvalue = 1, DataType = DataType.Number } },
            { "$string1", new Value() { Svalue = "test\"s", DataType = DataType.String } },
            { "$string2", new Value() { Svalue = "test\"s w`ith\"more t'es`t's", DataType = DataType.String } },
            { "$date1", new Value() { Dvalue = new DateTimeOffset(2023, 8, 16, 12, 0 , 0, TimeSpan.Zero), DataType = DataType.Date } },
            { "$date2", new Value() { Dvalue = new DateTimeOffset(2023, 8, 17, 12, 0 , 0, TimeSpan.Zero), DataType = DataType.Date } },
        });
    }

    [TestMethod]
    public void UT04_TS01_SimpleTernary() {
        CheckDoubleResult("1 < 2 ? 3 : 4", 3);
        CheckDoubleResult("1 > 2 ? 3 : 4", 4);

        CheckDoubleResult("true ? 3 : 4", 3);
        CheckDoubleResult("false ? 3 : 4", 4);
    }

    [TestMethod]
    public void UT04_TS02_TernaryWithAndOr() {
        CheckDoubleResult("1 < 2 && 5 > 3 ? 3 : 4", 3);
        CheckDoubleResult("1 < 2 && 5 < 3 ? 3 : 4", 4);
        CheckDoubleResult("1 > 2 && 5 > 3 ? 3 : 4", 4);
        CheckDoubleResult("1 < 2 || 5 < 3 ? 3 : 4", 3);
        CheckDoubleResult("1 > 2 || 5 > 3 ? 3 : 4", 3);
        CheckDoubleResult("1 > 2 || 5 < 3 ? 3 : 4", 4);

        CheckDoubleResult("3 / 3 < 2 && 5 - 1 > 3 && 1 + 2 == 3 ? 3 * 3 : 4", 9);
        CheckDoubleResult("3 / 3 < 2 && 5 - 1 > 4 && 1 + 2 == 3 ? 3 * 3 : 10 - 4 / 2 ", 8);
        CheckDoubleResult("3 / 3 < 2 && 5 - 1 > 3 && 1 + 2 != 3 ? 3 * 3 : 4 * 2 - 3", 5);
        CheckDoubleResult("3 / 3 < 2 || 5 - 1 > 4 || 1 + 2 == 3 ? 3 * 3 : 10 - 4 / 2 ", 9);
        CheckDoubleResult("2 ^ 4 ^ (1/2) == 4 ? 3 : 4", 3);
    }

    [TestMethod]
    public void UT04_TS03_NestedTernary() {
        CheckDoubleResult("1 < 2 ? 4 < 5 ? 3 : 4 : 5", 3);
        CheckDoubleResult("1 < 2 ? 4 > 5 ? 3 : 4 : 5", 4);
        CheckDoubleResult("1 > 2 ? 4 > 5 ? 3 : 4 : 5", 5);
        CheckDoubleResult("1 < 2 ? 4 < 5 ? 1 > 2 ? 4 > 5 ? 3 : 4 : 5 : 4 : 5", 5);
        CheckDoubleResult("1 < 2 ? 4 < 5 ? 1 > 2 ? 4 > 5 ? 3 : 4 : 1 < 2 ? 4 > 5 ? 3 : 4 : 5 : 4 : 5", 4);
        CheckDoubleResult("1 > 2 ? 4 > 5 ? 3 : 4 : 1 < 2 ? 4 > 5 ? 3 : 1 < 2 ? 4 < 5 ? 3 : 4 : 5 : 5", 3);
        // Complex nested with arithmetic
        CheckDoubleResult("(1 < 2 ? 10 : 20) + (3 > 4 ? 5 : 6)", 16);
        CheckDoubleResult("2 * (true ? 3 + 1 : 2) - 5", 3);
        CheckDoubleResult("true ? (false ? 1 : 2) : 3", 2);
        CheckDoubleResult("true ? 1 : (true ? 2 : 3)", 1);
        CheckDoubleResult("false ? 1 : (true ? 2 : 3)", 2);
        // Associativity check: a ? b : c ? d : e  -> a ? b : (c ? d : e) (Right-associative usually)
        // In C#, ?: is right-associative.
        // true ? 1 : true ? 2 : 3  => true ? 1 : (true ? 2 : 3) => 1.
        // false ? 1 : true ? 2 : 3 => false ? 1 : (true ? 2 : 3) => 2.
        CheckDoubleResult("true ? 1 : true ? 2 : 3", 1);
        CheckDoubleResult("false ? 1 : true ? 2 : 3", 2);
        CheckDoubleResult("false ? 1 : false ? 2 : 3", 3);
    }

    [TestMethod]
    public void UT04_TS04_Functions() {
        CheckDoubleResult("max(1,3) == 3 ? min(8, 3) : 4", 3);
        CheckDoubleResult("min(8, 3) == 3 ? max(1,3) : 4", 3);
        CheckDoubleResult("27^(1/3) == 3 ? max(9,3) / min(8, 3) * sqrt(81) + abs(-3) : 4", 30);
        CheckDoubleResult("max(--$one,-$four) == 1 ? max(---$one,---$four) : 4", -1);
        CheckDoubleResult("max(--$one,-$four) == 1 ? --max(---$one,---$four) : 4", -1);
        CheckDoubleResult("min(-1 +---1,3) == -2 ? ---max(-----$one,-$four) + --1 : 4", 2);
    }

    [TestMethod]
    public void UT04_TS05_Pharentesis() {
        CheckDoubleResult("((1 + 1 == (2)) && 5 == 2 + min(8,3)) ? (max(1,3) + 4) - 2 : 4", 5);
        CheckDoubleResult("((1 + 1 == (2)) && 5 == 2 + max(1,3)) ? (min(8,3) + 4) - 2 : 4", 5);
        CheckDoubleResult("max(1,3) == 3 ? max(((1+3),()((2+--(--4))),(((3))))) : 4", 6);
        CheckDoubleResult("max(max(((1+3),()((2+--(--4))),(((3))))),3) == max(((1+3),()((2+--(--4))),(((3))))) ? max(((1+3),()((2+--(--4))),(((3))))) : 4", 6);
        CheckDoubleResult("max(max(((1+3),()((2+--(--4))),(((3))))),3) + 2 == 1 + max(((1+3),()((2+--(--4))),(((3))))) + 3 - 2 ? -2 + max(((1+3),()((2+--(--4))),(((3))))) + max(((1+3),()((2+--(--4))),(((3))))) : 4", 10);
    }


    [TestMethod]
    public void UT04_TS06_StringComparason() {
        CheckDoubleResult("$string1 == $string1 ? 3 : 4", 3);
        CheckDoubleResult("$string1 != $string1 ? 3 : 4", 4);

        CheckDoubleResult("$string2 == $string2 ? 3 : 4", 3);
        CheckDoubleResult("$string2 != $string2 ? 3 : 4", 4);

        CheckDoubleResult("$string1 == $string1 && $string2 == $string2 ? 3 : 4", 3);
        CheckDoubleResult("$string1 != $string1 && $string2 != $string2 ? 3 : 4", 4);

        CheckDoubleResult("$string1 == $string1 || $string2 == $string2 ? 3 : 4", 3);
        CheckDoubleResult("$string1 == $string1 || $string2 != $string2 ? 3 : 4", 3);
        CheckDoubleResult("$string1 != $string1 || $string2 == $string2 ? 3 : 4", 3);
        CheckDoubleResult("$string1 != $string1 || $string2 != $string2 ? 3 : 4", 4);

        CheckDoubleResult("$string1 == \"test\\\"s\" ? 3 : 4", 3);
        CheckDoubleResult("$string2 == \"test\\\"s w`ith\\\"more t'es`t's\" ? 3 : 4", 3);

        CheckDoubleResult("$string1 + $string1 == $string1 + $string1 ? 3 : 4", 3);
        CheckDoubleResult("$string1 + $string2 == $string1 + $string2 ? 3 : 4", 3);
        CheckDoubleResult("$string1 + \"test\\\"s\" == \"test\\\"s\" + $string1 ? 3 : 4", 3);
        CheckDoubleResult("`some text` + 'some ' + `text` == \"some text\" + `some` + ' ' + 'text' ? 3 : 4", 3);
        CheckDoubleResult("$string1 + $string2 != $string2 + $string2 ? 3 : 4", 3);
    }

    [TestMethod]
    public void UT04_TS07_DateComparason() {
        CheckDoubleResult("$date1 == $date1 ? 3 : 4", 3);
        CheckDoubleResult("$date2 == $date2 ? 3 : 4", 3);
        CheckDoubleResult("$date2 != $date1 ? 3 : 4", 3);
        CheckDoubleResult("$date1 != $date2 ? 3 : 4", 3);
        CheckDoubleResult("$date1 < $date2 ? 3 : 4", 3);
        CheckDoubleResult("$date1 > $date2 ? 3 : 4", 4);
        CheckDoubleResult("$date2 < $date1 ? 3 : 4", 4);
        CheckDoubleResult("$date2 > $date1 ? 3 : 4", 3);
        CheckDoubleResult("$date1 >= $date1 ? 3 : 4", 3);
        CheckDoubleResult("$date2 >= $date2 ? 3 : 4", 3);
        CheckDoubleResult("$date1 <= $date1 ? 3 : 4", 3);
        CheckDoubleResult("$date2 <= $date2 ? 3 : 4", 3);
        CheckDoubleResult("($date2 <= $date2 ? 3 : 4) + 5", 8);
        CheckDoubleResult("true ? 1 : true ? 2 : 3", 1);
        CheckDoubleResult("false ? 1 : true ? 2 : 3", 2);
        CheckDoubleResult("false ? 1 : false ? 2 : 3", 3);
    }

    [TestMethod]
    public void UT04_TS08_ComplexMixedScenarios() {
        // Function + Ternary + Recursion
        CheckDoubleResult("max(10, 5 > 3 ? 20 : 5)", 20);
        CheckDoubleResult("min(1 < 2 ? 10 : 20, 5 > 3 ? 5 : 100)", 5);
        CheckDoubleResult("abs((1 < 2 ? -10 : 20) * (3 > 4 ? 5 : 2))", 20); // abs(-10 * 2) = 20

        // Variables + Nested Ternary + Arithmetic
        CheckDoubleResult("($five > $three ? $five : $three) * ($two < $four ? $two : $four)", 10); // 5 * 2 = 10
        CheckDoubleResult("$five + ($one < $two ? $three + $two : 0) * 2", 15); // 5 + (5)*2 = 15

        // Nested inside function args
        CheckDoubleResult("max( $one > 0 ? 10 : 0, $two > 5 ? 0 : 20 )", 20);

        // Boolean logic inside ternary condition
        CheckDoubleResult("($one < $two && $three < $four) ? 100 : 200", 100);
        CheckDoubleResult("($one > $two || $three > $four) ? 100 : 200", 200);

        // Complex deeply nested
        // 5 > 3 ? (2 < 4 ? (1 == 1 ? 42 : 0) : 0) : 0
        CheckDoubleResult("$five > $three ? ($two < $four ? ($one == $one ? 42 : 0) : 0) : 0", 42);

        // Ternary result used in function
        CheckDoubleResult("sqrt($four > $two ? 16 : 9)", 4);
        CheckDoubleResult("pow($two, $three > $one ? 3 : 2)", 8); // 2^3

        // String and Date mixed
        CheckDoubleResult("$date1 < $date2 ? ($string1 == \"test\\\"s\" ? 1 : 0) : 0", 1);
    }

    [TestMethod]
    public void UT04_TS09_ExtremeCombinations() {
        // "Extreme" nesting and mixing
        // (5 > 3) ? ( (2 < 4) ? ( (1==1) ? 42 : ( (9==9) ? 10 : 20 ) ) : 0 ) : -1
        CheckDoubleResult("$five > $three ? ($two < $four ? ($one == $one ? 42 : ($five == $five ? 10 : 20)) : 0) : -1", 42);

        // Calculation inside ternary branch used in another calculation
        // ( (true ? 2 : 4) * (false ? 10 : 5) ) + ( (1 < 2) ? 100 : 0 )
        CheckDoubleResult("( (true ? 2 : 4) * (false ? 10 : 5) ) + ( (1 < 2) ? 100 : 0 )", 110); // (2 * 5) + 100 = 110

        // Ternary determining function arguments
        // max( true ? 10 : 20, false ? 30 : 5 ) -> max(10, 5) -> 10
        CheckDoubleResult("max( true ? 10 : 20, false ? 30 : 5 )", 10);

        // Date logic with ternary
        // GetDay($date2) > GetDay($date1) ? 1 : 0
        // Assuming date1=16, date2=17 -> 17 > 16 -> 1
        CheckDoubleResult("$date2 > $date1 ? ($date2 >= $date1 ? 1 : 0) : -1", 1);

        // Mixed types in branches (logic check, return numbers)
        // ( 'test' == 'test' ) ? ( 1 + 2 ) : ( 3 * 4 )
        CheckDoubleResult("('test' == 'test') ? (1 + 2) : (3 * 4)", 3);

        // Nested ternary acting as array index simulator (just logic selection)
        // index = 2. if 1->10, 2->20, 3->30.
        CheckDoubleResult("$two == 1 ? 10 : ($two == 2 ? 20 : ($two == 3 ? 30 : 0))", 20);
    }

    internal void CheckDoubleResult(string testString, double expected) {
        if (m_shyCalculator == null) {
            Assert.Fail($"ShYCalculator not initialized");
            return;
        }

        try {
            var result = m_shyCalculator.Calculate(testString);

            if (result.DataType == Classes.DataType.Number) {
                if (!(result.Success && result.Nvalue == expected)) {
                    Console.WriteLine($"FAILURE: Test: {testString}. Expected {expected}. Got Success={result.Success}, Nvalue={result.Nvalue}, Msg={result.Message}");
                }
                Assert.IsTrue(result.Success && result.Nvalue == expected, $"Failed for testString: '{testString}', expected: '{expected}', result: '{result}'");
                return;
            }

            if (result.DataType == Classes.DataType.Boolean) {
                if (!(result.Success && result.Bvalue == (expected == 1))) {
                    Console.WriteLine($"FAILURE: Test: {testString}. Expected {expected}. Got Success={result.Success}, Bvalue={result.Bvalue}, Msg={result.Message}");
                }
                Assert.IsTrue(result.Success && result.Bvalue == (expected == 1), $"Failed for testString: '{testString}', expected: '{expected}', result: '{result}'");
                return;
            }

            Console.WriteLine($"FAILURE: Unexpected DataType {result.DataType}. Msg={result.Message}");
            Assert.Fail($"Unexpected Error for testString: '{testString}', expected: '{expected}'");
        }
        catch (Exception ex) {
            Console.WriteLine($"EXCEPTION: {ex}");
            Assert.Fail($"Unexpected Error for testString: '{testString}', expected: '{expected}', error: '{ex}'");
        }
    }
}
