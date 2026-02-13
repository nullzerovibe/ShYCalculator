namespace ShYCalculator.Test;

[TestClass]
public class UT_Scenarios {

    [TestMethod]
    public void Scenario_OneShot_Calculation_With_Context() {
        var calc = new ShYCalculator();
        var formula = "a + b * 2";
        var variables = new Dictionary<string, double> {
            { "a", 10 },
            { "b", 5 }
        };

        // Expected: 10 + 5 * 2 = 20
        var result = calc.Calculate(formula, variables);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(20.0, result.Nvalue);
    }

    [TestMethod]
    public void Scenario_Compiled_Calculation_With_Context() {
        var formula = "x * y";

        // New Factory Pattern
        var compileResult = ShYCalculator.Compile(formula);
        Assert.IsTrue(compileResult.Success, compileResult.Message);
        var compiledWrapper = compileResult.Value!;

        // Compilation happens inside factory. 
        // We can check validity by calculating or if we exposed IsCompiled property.
        // For now, testing via Calculate.

        // Run 1
        var variables1 = new Dictionary<string, double> { { "x", 2 }, { "y", 3 } };
        var result1 = compiledWrapper.Calculate(variables1);
        Assert.IsTrue(result1.Success, "Calculation 1 failed: " + result1.Message);
        Assert.AreEqual(6.0, result1.Nvalue);

        // Run 2 (different values, same compiled instance)
        var variables2 = new Dictionary<string, double> { { "x", 10 }, { "y", 10 } };
        var result2 = compiledWrapper.Calculate(variables2);
        Assert.IsTrue(result2.Success, "Calculation 2 failed");
        Assert.AreEqual(100.0, result2.Nvalue);
    }

    [TestMethod]
    public void Scenario_Context_Overrides_Environment() {
        var calc = new ShYCalculator();

        // Environment variable
        calc.Environment.AddVariable("a", 100);

        var formula = "a";

        // 1. Calculate without context (uses env)
        var result1 = calc.Calculate(formula);
        Assert.AreEqual(100.0, result1.Nvalue);

        // 2. Calculate WITH context (overrides env)
        var context = new Dictionary<string, double> { { "a", 5 } };
        var result2 = calc.Calculate(formula, context);
        Assert.AreEqual(5.0, result2.Nvalue);
    }

    [TestMethod]
    public void Scenario_Ternary_Context_Recursive() {
        var calc = new ShYCalculator();


        // Case 1: check=true -> val1
        var context1 = new Dictionary<string, double> { { "check", 1 }, { "val1", 10 }, { "val2", 20 } };
        // Note: In ShY, numbers != 0 are true? Or strictly booleans? 
        // Logic ops return booleans. "check" is a variable. 
        // If check is a number, Ternary expects boolean operand.
        // We need to pass a boolean or comparison.

        // Let's use comparison in formula to be safe: "check > 0 ? val1 : val2"
        var formulaSafe = "check > 0 ? val1 : val2";

        var result1 = calc.Calculate(formulaSafe, context1);
        Assert.AreEqual(10.0, result1.Nvalue);

        // Case 2: check=false -> val2
        var context2 = new Dictionary<string, double> { { "check", 0 }, { "val1", 10 }, { "val2", 20 } };
        var result2 = calc.Calculate(formulaSafe, context2);
        Assert.AreEqual(20.0, result2.Nvalue);
    }
}
