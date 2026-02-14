using ShYCalculator.Classes;
using ShYCalculator.Functions;

namespace ShYCalculator.Test;

[TestClass]
public class UT_ThreadSafety {
    [TestMethod]
    public void TestThreadSafety_SharedCalculator_SeparateContexts() {
        // Arrange
        // Global scope (functions, constants) is shared and immutable (conceptually)
        var globalEnv = new global::ShYCalculator.Calculator.Environment();
        // Add a shared function just to be sure
        globalEnv.RegisterFunctions(new TestFunctionExtension());

        var calculator = new ShYCalculator(globalEnv);

        int threadCount = 20;
        var tasks = new Task[threadCount];
        var results = new double[threadCount];
        var errors = new bool[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++) {
            int uniqueId = i; // capture local
            tasks[i] = Task.Run(() => {
                try {
                    // Create a unique context for this thread
                    var contextVars = new Dictionary<string, double> {
                        { "x", uniqueId }
                    };

                    // Calculate "x + 100"
                    // If threads share state, x might be overwritten by another thread
                    var result = calculator.Calculate("x + 100", contextVars);

                    if (result.Success) {
                        results[uniqueId] = result.Value.Nvalue ?? double.NaN;
                    }
                    else {
                        errors[uniqueId] = true;
                    }
                }
                catch {
                    errors[uniqueId] = true;
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert
        for (int i = 0; i < threadCount; i++) {
            Assert.IsFalse(errors[i], $"Thread {i} encountered an error.");
            double expected = i + 100;
            // If x was 5, expected 105.
            // If thread safety failed (shared variables), result might be different.
            // But ShYCalculator now uses passed context, so it should be fine.
            Assert.AreEqual(expected, results[i], $"Thread {i} result mismatch. Expected {expected}, got {results[i]}.");
        }
    }

    private class TestFunctionExtension : ICalcFunctionsExtension {
        public string Name => "ThreadTest";
        public IEnumerable<CalcFunction> GetFunctions() => [
            new CalcFunction { Name = "noop" }
        ];
        public Value ExecuteFunction(string name, ReadOnlySpan<Value> args) => default;
    }
}
