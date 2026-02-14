using ShYCalculator.Classes;

namespace ShYCalculator.Test;

/// <summary>
/// Executable Documentation for the "Stateless" (Modern) Mode.
/// Use this pattern for Web APIs, Parallel Processing, and Thread-Safe applications.
/// Each calculation gets its own isolated context.
/// </summary>
[TestClass]
public class UT_Documentation_Stateless {

    [TestMethod]
    public void Usage_WebAPI_IsolatedContext() {
        // 1. Create a Singleton Calculator
        // In a real app, this would be injected via DI (see UT_07_DependencyInjection)
        var calculator = new ShYCalculatorBuilder()
            .WithAllExtensions()
            .Build();

        // 2. Request 1 comes in (User A)
        var contextA = new Dictionary<string, double> {
            { "input", 100 },
            { "tax", 0.1 }
        };

        // 3. Request 2 comes in (User B) simultaneously
        var contextB = new Dictionary<string, double> {
            { "input", 500 },
            { "tax", 0.2 }
        };

        // 4. Calculate concurrently
        // Neither calculation affects the other or the calculator instance
        var resultA = calculator.Calculate("input * (1 + tax)", contextA);
        var resultB = calculator.Calculate("input * (1 + tax)", contextB);

        Assert.AreEqual(110.0, resultA.Value.Nvalue!.Value, 0.000001);
        Assert.AreEqual(600.0, resultB.Value.Nvalue!.Value, 0.000001);
    }

    [TestMethod]
    public void Usage_Stateless_MixedTypes() {
        var calculator = new ShYCalculatorBuilder()
            .WithLogic()
            .WithText()
            .Build();

        // Use the generic Value struct for mixed types (Context Dictionary)
        // This avoids boxing (allocating objects on heap) for numerical values
        var complexContext = new Dictionary<string, Value> {
            { "Age", Value.Number(30) },
            { "Name", Value.String("nullzerovibe") },
            { "IsAdmin", Value.Boolean(true) },
            { "Joined", Value.Date(DateTimeOffset.Parse("2023-01-01")) }
        };

        var expression = "IsAdmin ? 'Welcome ' + Name : 'Access Denied'";
        var result = calculator.Calculate(expression, complexContext);

        Assert.IsTrue(result.Success, "Calculation failed: " + result.Message);
        Assert.AreEqual("Welcome nullzerovibe", result.Value.Svalue);
    }

    [TestMethod]
    public void Usage_Compiled_Stateless() {
        // Compile once (expensive), Execute many times (cheap) with different contexts

        // 1. Create a "Pure Definition" scope
        // This scope contains functions and constants, but NO variables.
        // It is effectively immutable.
        var globalScope = new ShYCalculatorBuilder()
            .WithMathematics()
            .WithConstant("pi", Math.PI)
            .Build()
            .Environment; // Get the scope

        // 2. Compile
        var compiled = ShYCalculator.Compile("radius * radius * pi", globalScope);
        Assert.IsTrue(compiled.Success);

        var runner = compiled.Value!;

        // 3. Execute for Circle A
        var ctxA = new Dictionary<string, double> { { "radius", 2.0 } };
        var areaA = runner.Calculate(ctxA);
        Assert.AreEqual(4 * Math.PI, areaA.Value.Nvalue);

        // 4. Execute for Circle B
        var ctxB = new Dictionary<string, double> { { "radius", 5.0 } };
        var areaB = runner.Calculate(ctxB);
        Assert.AreEqual(25 * Math.PI, areaB.Value.Nvalue);
    }
}
