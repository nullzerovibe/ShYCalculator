using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Classes;

namespace ShYCalculator.Test;

[TestClass]
public class UT_Experimental_If {
    private ShYCalculator calculator = new();

    [TestInitialize]
    public void Init() {
        calculator = new ShYCalculatorBuilder()
            .WithAllExtensions()
            .Build();
    }

    [TestMethod]
    public void Test_If_ShortCircuit_Safety() {
        // This test demonstrates why we need lazy evaluation.
        // In eager evaluation, '1/0' is calculated BEFORE 'if' runs, causing a divide by zero error.
        // In lazy evaluation, the second branch is never touched.

        var expression = "if(true, 1, 1/0)";
        var result = calculator.Calculate(expression);

        // Debug output
        if (!result.Success) {
            Console.WriteLine($"Calculation failed: {result.Message}");
            foreach (var err in result.Errors) {
                Console.WriteLine($"Error: {err.Message}");
            }
        }

        Assert.IsTrue(result.Success, "Expected success, but got failure (likely eager evaluation of 1/0).");
        Assert.AreEqual(1.0, result.Value.Nvalue!.Value);
    }

    [TestMethod]
    public void Test_If_ShortCircuit_Performance() {
        // We can simulate performance check by having a side-effect or just ensuring complex logic isn't run.
        // For now, let's stick to the safety check above as it's definitive.
        // We can also test nested ifs.

        var expression = "if(false, 1/0, if(true, 2, 1/0))";
        var result = calculator.Calculate(expression);

        Assert.IsTrue(result.Success, "Nested if failed: " + result.Message);
        Assert.AreEqual(2.0, result.Value.Nvalue!.Value);

        // Test with variable
        var context = new Dictionary<string, double> { { "x", 10 } };
        var result2 = calculator.Calculate("if(x > 5, x*2, x/0)", context);
        Assert.AreEqual(20.0, result2.Value.Nvalue!.Value);

        // Test with string result
        var result3 = calculator.Calculate("if(true, 'Yes', 'No')");
        Assert.AreEqual("Yes", result3.Value.Svalue);

        // Test with complex condition
        var result4 = calculator.Calculate("if(1+1==2 && 3<4, 100, 200)");
        Assert.AreEqual(100.0, result4.Value.Nvalue!.Value);
    }

    [TestMethod]
    public void Test_If_SyntaxErrors() {
        // Missing comma
        var result = calculator.Calculate("if(true 1, 2)");
        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "expected ','");

        // Missing closing parenthesis
        var result2 = calculator.Calculate("if(true, 1, 2");
        Assert.IsFalse(result2.Success);

        // Missing branches
        // var result3 = calculator.Calculate("if(true)");
        //Assert.IsFalse(result3.Success);
    }

    [TestMethod]
    public void Test_Complex_Combinations() {
        // 1. if + standard function + arithmetic
        // if(max(10, 20) > 15, sin(0), -1) -> if(20 > 15, 0, -1) -> 0
        var result1 = calculator.Calculate("if(max(10, 20) > 15, sin(0), -1)");
        Assert.IsTrue(result1.Success, "Test 1 failed: " + result1.Message);
        Assert.AreEqual(0.0, result1.Value.Nvalue!.Value);

        // 2. if + max (number) + ternary
        // if(max(4, 4) == 4, 10, 20) + (true ? 5 : 0) -> 10 + 5 -> 15
        var result2 = calculator.Calculate("if(max(4, 4) == 4, 10, 20) + (true ? 5 : 0)");
        Assert.IsTrue(result2.Success, "Test 2 failed: " + result2.Message);
        Assert.AreEqual(15.0, result2.Value.Nvalue!.Value);

        // 3. Nested if + nested ternary + function
        // if(true, if(false, 0, 1), 2) + max(3, 4) -> 1 + 4 -> 5
        var result3 = calculator.Calculate("if(true, if(false, 0, 1), 2) + max(3, 4)");
        Assert.IsTrue(result3.Success, "Test 3 failed: " + result3.Message);
        Assert.AreEqual(5.0, result3.Value.Nvalue!.Value);

        // 4. Ternary inside if condition
        // if((5 > 3 ? true : false), 100, 200) -> 100
        var result4 = calculator.Calculate("if((5 > 3 ? true : false), 100, 200)");
        Assert.IsTrue(result4.Success, "Test 4 failed: " + result4.Message);
        Assert.AreEqual(100.0, result4.Value.Nvalue!.Value);

        // 5. If inside ternary branch
        // true ? if(true, 77, 88) : 99 -> 77
        var result5 = calculator.Calculate("true ? if(true, 77, 88) : 99");
        Assert.IsTrue(result5.Success, "Test 5 failed: " + result5.Message);
        Assert.AreEqual(77.0, result5.Value.Nvalue!.Value);
    }

    [TestMethod]
    public void Test_Extreme_Nesting() {
        // 1. If inside Ternary inside If
        // if(true, (true ? if(false, 0, 100) : 200), 300) 
        // -> if(true, (true ? 100 : 200), 300) -> if(true, 100, 300) -> 100
        var result1 = calculator.Calculate("if(true, (true ? if(false, 0, 100) : 200), 300)");
        Assert.IsTrue(result1.Success, "Test 1 failed: " + result1.Message);
        Assert.AreEqual(100.0, result1.Value.Nvalue!.Value);

        // 2. Ternary inside If inside Ternary
        // false ? 0 : if(true, (false ? 0 : 50), 0)
        // -> false ? 0 : if(true, 50, 0) -> false ? 0 : 50 -> 50
        var result2 = calculator.Calculate("false ? 0 : if(true, (false ? 0 : 50), 0)");
        Assert.IsTrue(result2.Success, "Test 2 failed: " + result2.Message);
        Assert.AreEqual(50.0, result2.Value.Nvalue!.Value);

        // 3. Deeply nested mixed structure
        // if(
        //    if(true, 1, 0) == 1, 
        //    true ? if(true, 42, 0) : 0, 
        //    0
        // )
        // -> if(1 == 1, 42, 0) -> 42
        var result3 = calculator.Calculate("if(if(true, 1, 0) == 1, true ? if(true, 42, 0) : 0, 0)");
        Assert.IsTrue(result3.Success, "Test 3 failed: " + result3.Message);
        Assert.AreEqual(42.0, result3.Value.Nvalue!.Value);
    }

    [TestMethod]
    public void Test_Maximum_Complexity() {
        // A "stress test" combining multiple features:
        // if(
        //    if(1 < 2, true, false) && (true ? true : false),  <-- Condition: true && true -> true
        //    if(
        //        min(10, 20) == 10,                            <-- True Branch 1 Condition: 10 == 10 -> true
        //        if(
        //            max(5, 5) == 5,                           <-- True Branch 2 Condition: 5 == 5 -> true
        //            if(true, 100, 0) + (false ? 0 : 50),      <-- Target: 100 + 50 -> 150
        //            0
        //        ),
        //        0
        //    ),
        //    -1                                                <-- False Branch (top level)
        // )
        var expression = @"
            if(
                if(1 < 2, true, false) && (true ? true : false),
                if(
                    min(10, 20) == 10,
                    if(
                        max(5, 5) == 5,
                        if(true, 100, 0) + (false ? 0 : 50),
                        0
                    ),
                    0
                ),
                -1
            )";

        // Remove newlines and whitespace for cleaner parsing if needed, 
        // though parser handles whitespace. Let's keep it clean.
        expression = expression.Replace("\r", "").Replace("\n", "").Trim();

        var result = calculator.Calculate(expression);
        Assert.IsTrue(result.Success, "Stress test failed: " + result.Message);
        Assert.AreEqual(150.0, result.Value.Nvalue!.Value);
    }
}
