using ShYCalculator.Classes;

namespace ShYCalculator.Test;

/// <summary>
/// Executable Documentation for the "Stateful" (Legacy) Mode.
/// Use this pattern when you need a REPL, Scripting Engine, or single-threaded interactive session.
/// Variable state is persisted between calculations.
/// </summary>
[TestClass]
public class UT_Documentation_Stateful {

    [TestMethod]
    public void Usage_Scripting_VariablesPersist() {
        // 1. Create the calculator (Builder is recommended, but new ShYCalculator() works too)
        var calculator = new ShYCalculatorBuilder()
            .WithAllExtensions()
            .Build();

        // 2. Define a variable "cost"
        // This stores "cost" in the global environment (Global State)
        calculator.Environment.AddVariable("cost", 50.0);

        // 3. Define a variable "tax"
        calculator.Environment.AddVariable("tax", 0.10);

        // 4. Calculate total
        var result = calculator.Calculate("cost * (1 + tax)");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(55.0, result.Value.Nvalue!.Value, 0.000001);

        // 5. Variables are still there for the next calculation
        // Update "cost"
        calculator.Environment.AddVariable("cost", 100.0);

        var result2 = calculator.Calculate("cost * (1 + tax)");
        Assert.AreEqual(110.0, result2.Value.Nvalue!.Value, 0.000001);
    }

    [TestMethod]
    public void Usage_AccumulatingResult() {
        var calculator = new ShYCalculatorBuilder()
            .WithMathematics()
            .Build();

        // 1. Start with 0
        calculator.Environment.AddVariable("acc", 0);

        // 2. Add 10
        // We can use the result of previous calculation to update 'acc', 
        // OR we can rely on variable assignment if the language supports it (ShYCalculator currently doesn't support 'a = 10' inside expression without custom function).
        // So we update it manually.
        var step1 = calculator.Calculate("acc + 10");
        calculator.Environment.AddVariable("acc", step1.Value.Nvalue ?? 0);

        // 3. Multiply by 2
        var step2 = calculator.Calculate("acc * 2");
        calculator.Environment.AddVariable("acc", step2.Value.Nvalue ?? 0);

        Assert.AreEqual(20.0, calculator.Environment.Variables["acc"].Nvalue!.Value, 0.000001);
    }

    [TestMethod]
    public void Usage_Compiled_Stateful() {
        // Compile once, execute many times with changing Global State
        var calculator = new ShYCalculatorBuilder()
            .WithAllExtensions()
            .Build();

        // 1. Compile the formula
        // Note: We pass the calculator's environment so the compiled logic knows where to look for variables
        var compiled = ShYCalculator.Compile("price * qty", calculator.Environment);
        Assert.IsTrue(compiled.Success);

        var runner = compiled.Value!;

        // 2. Set State
        calculator.Environment.AddVariable("price", 10.0);
        calculator.Environment.AddVariable("qty", 5.0);

        // 3. Execute
        var result1 = runner.Calculate(); // Uses the calculator.Environment
        Assert.AreEqual(50.0, result1.Value.Nvalue);

        // 4. Update State
        calculator.Environment.AddVariable("qty", 10.0);

        // 5. Execute again (no re-compile needed)
        var result2 = runner.Calculate();
        Assert.AreEqual(100.0, result2.Value.Nvalue);
    }

    [TestMethod]
    public void Usage_MixedTypes() {
        var calculator = new ShYCalculatorBuilder()
            .WithText()
            .WithLogic()
            .Build();

        // Store mixed types in the environment
        calculator.Environment.AddVariable("User", "nullzerovibe");
        calculator.Environment.AddVariable("IsAdmin", true);

        // Use logic based on these variables
        var result = calculator.Calculate("IsAdmin ? 'Hello ' + User : 'Access Denied'");

        Assert.IsTrue(result.Success, "Calculation failed: " + result.Message);
        Assert.AreEqual("Hello nullzerovibe", result.Value.Svalue);
    }
}
