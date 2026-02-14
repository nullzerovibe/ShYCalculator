using ShYCalculator.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShYCalculator.Test.Calculator;

[TestClass]
public class UT_ShYCalculatorOptions {

    [TestMethod]
    public void Options_Default_AllEnabled() {
        var options = new ShYCalculatorOptions();
        Assert.AreEqual(FunctionExtensions.All, options.EnabledExtensions);
        Assert.IsTrue(options.AnyEnabled);

        var calc = new ShYCalculator(options: options);
        // Check function count. Should be > 0.
        // We can check specific known functions.
        Assert.IsTrue(calc.Environment.Functions.ContainsKey("abs")); // Mathematics (Arithmetical/Numeric)
        Assert.IsTrue(calc.Environment.Functions.ContainsKey("dt_now")); // Date
        Assert.IsTrue(calc.Environment.Functions.ContainsKey("str_contains")); // Text
        Assert.IsTrue(calc.Environment.Functions.ContainsKey("any")); // Logical
    }

    [TestMethod]
    public void Options_DisableAll() {
        var options = new ShYCalculatorOptions { EnabledExtensions = FunctionExtensions.None };
        Assert.IsFalse(options.AnyEnabled);

        var calc = new ShYCalculator(options: options);
        Assert.IsEmpty(calc.Environment.Functions);

        Assert.IsFalse(calc.Environment.Functions.ContainsKey("abs"));
        Assert.IsFalse(calc.Environment.Functions.ContainsKey("dt_now"));
    }

    [TestMethod]
    public void Options_EnableOnlyMathematics() {
        var options = new ShYCalculatorOptions { EnabledExtensions = FunctionExtensions.Mathematics };
        var calc = new ShYCalculator(options: options);

        Assert.IsTrue(calc.Environment.Functions.ContainsKey("abs")); // Math
        Assert.IsFalse(calc.Environment.Functions.ContainsKey("dt_now")); // Date
        Assert.IsFalse(calc.Environment.Functions.ContainsKey("str_contains")); // Text
        Assert.IsFalse(calc.Environment.Functions.ContainsKey("any")); // Logical
    }

    [TestMethod]
    public void Options_EnableMathAndText() {
        var options = new ShYCalculatorOptions { EnabledExtensions = FunctionExtensions.Mathematics | FunctionExtensions.Text };
        var calc = new ShYCalculator(options: options);

        Assert.IsTrue(calc.Environment.Functions.ContainsKey("sqrt"));
        Assert.IsTrue(calc.Environment.Functions.ContainsKey("str_contains"));

        Assert.IsFalse(calc.Environment.Functions.ContainsKey("dt_today"));
    }

    [TestMethod]
    public void Options_CompiledCalculator_RespectsOptions() {
        var options = new ShYCalculatorOptions { EnabledExtensions = FunctionExtensions.Mathematics };
        // Compile static method check
        var compiled = ShYCalculator.Compile("abs(-5)", options: options);
        Assert.IsTrue(compiled.Success);
        var result = compiled.Value!.Calculate();
        Assert.IsTrue(result.Success);
        Assert.AreEqual(5, result.Nvalue);

        // Compile logic checking logic function failure
        var compiledFail = ShYCalculator.Compile("str_contains('ABC', 'A')", options: options);

        // If compilation succeeds (it might if function lookup happens at runtime), then calculation fails.
        // But Compile checks for function existence? Yes.
        Assert.IsTrue(compiledFail.Success); // The function 'str_contains' exists in standard lib?

        var resultFail = compiledFail.Value!.Calculate();
        // It might compile (parsing tokens) but evaluate might fail if function maps are checked at runtime?
        // Actually, tokenization probably identifies functions. 
        // If "lower" is not in registry, Tokenizer might treat it as text or fail if it looks like a function call?
        // Let's see. If function missing, Evaluation fails.
        // It fails with "Unknown Variable" because the tokenizer/parser doesn't see it in the function registry.
        Assert.IsFalse(resultFail.Success);
        var msg = resultFail.Message;
        Assert.IsTrue(msg.Contains("Unknown function") || msg.Contains("Unknown Variable"), $"Msg: {msg}");
        Assert.Contains("str_contains", msg, $"Msg: {msg}");
    }
}
