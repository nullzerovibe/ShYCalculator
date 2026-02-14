using System.Diagnostics;
using ShYCalculator.Classes;

namespace ShYCalculator.Performance;

class Program {
    static void Main(string[] _) {
        Console.WriteLine("Initializing Benchmark...");

        // initialize Calculator and Environment
        var env = new global::ShYCalculator.Calculator.Environment();
        env.SetVariables(new Dictionary<string, Value> {
            { "$five", new Value() { Nvalue = 5, DataType = DataType.Number } },
            { "$four", new Value() { Nvalue = 4, DataType = DataType.Number } },
            { "$three", new Value() { Nvalue = 3, DataType = DataType.Number } },
            { "$two", new Value() { Nvalue = 2, DataType = DataType.Number } },
            { "$one", new Value() { Nvalue = 1, DataType = DataType.Number } },
        });
        var calculator = new global::ShYCalculator.ShYCalculator(env);

        // Test expressions (subset of unit tests covering different complexity)
        var expressions = new[] {
            "1 + 2 * 3.7",
            "log2(e)",
            "max(10, min(5, 7)) * abs(-2) / pow(pi, 2)",
            "max(max(9,$three),min(8, 3)) / min(min(8, $three), max(9,$three)) * sqrt(abs($three * -9 * $three)) * -1 + -abs(-$three)",
            "(1 + 2) & (7 - 4)",
            "3^2^2",
            "1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10",
            "sqrt(16) + abs(-4) * max(2,3)"
        };

        Console.WriteLine($"Running {expressions.Length} expressions x 1000 iterations...");

        // Warmup
        foreach (var expr in expressions) {
            calculator.Calculate(expr);
        }

        string logFile = "benchmark_results.md";
        File.AppendAllText(logFile, $"\n\n# Benchmark Run {DateTime.Now}\n");

        RunBenchmark(calculator, expressions, 1000, 30, "30x 1000", logFile);
        RunBenchmark(calculator, expressions, 100000, 10, "10x 100,000", logFile);
        // RunBenchmark(calculator, expressions, 10000000, 1, "1x 10,000,000", logFile); // Undo comment if needed, takes ~5 mins

        // Run Deep Nesting Benchmark
        BenchmarkDeepNesting.Run();
    }

    static void RunBenchmark(global::ShYCalculator.ShYCalculator calculator, string[] expressions, int iterations, int runs, string label, string logFile) {
        string header = $"\n=== Starting Benchmark: {label} ===";
        Console.WriteLine(header);
        File.AppendAllText(logFile, $"\n## {label}\n| Run | Time (ms) | Ops/Sec | Allocations (KB) |\n|---|---|---|---|\n");

        for (int r = 1; r <= runs; r++) {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var stopwatch = Stopwatch.StartNew();
            long initialMemory = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++) {
                foreach (var expr in expressions) {
                    var result = calculator.Calculate(expr);
                    if (!result.Success) {
                        Console.WriteLine($"Error evaluating '{expr}': {result.Message}");
                    }
                }
            }

            long finalMemory = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Stop();

            long totalAllocated = finalMemory - initialMemory;
            //double msPerRun = stopwatch.Elapsed.TotalMilliseconds / iterations;
            double opsPerSecond = (long)iterations * expressions.Length / stopwatch.Elapsed.TotalSeconds;

            string resultLine = $"Run {r}: {stopwatch.Elapsed.TotalMilliseconds:F2} ms | {opsPerSecond:F0} ops/sec | Allocs: {totalAllocated / 1024.0:F2} KB";
            Console.WriteLine(resultLine);
            File.AppendAllText(logFile, $"| {r} | {stopwatch.Elapsed.TotalMilliseconds:F2} | {opsPerSecond:F0} | {totalAllocated / 1024.0:F2} |\n");
        }
    }
}
