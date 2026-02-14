using System.Diagnostics;
using ShYCalculator.Classes;

namespace ShYCalculator.Performance;

public class BenchmarkDeepNesting {
    public static void Run() {
        Console.WriteLine("\n=== Deep Nesting Benchmark ===");

        var options = new ShYCalculatorOptions(); // Default
        var env = new Calculator.Environment(null, options);
        // Register all needed functions
        env.RegisterFunctions(new Functions.Mathematics.CalcNumericFunctions());
        env.RegisterFunctions(new Functions.Logical.CalcLogicalFunctions());

        var calculator = new ShYCalculator(env);

        // The stress test expression from UT_Experimental_If
        // Compacted to avoiding parsing overhead of newlines if any (though parser handles it)
        var deepExpr = "if(if(1 < 2, true, false) && (true ? true : false), if(min(10, 20) == 10, if(max(5, 5) == 5, if(true, 100, 0) + (false ? 0 : 50), 0), 0), -1)";

        // Warmup
        var result = calculator.Calculate(deepExpr);
        if (!result.Success) {
            Console.WriteLine($"[ERROR] Warmup failed: {result.Message}");
            return;
        }
        Console.WriteLine($"Warmup Result: {result.Value.Nvalue}");

        // Run
        int iterations = 100_000;
        Console.WriteLine($"Running {iterations} iterations...");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var sw = Stopwatch.StartNew();
        long startMem = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < iterations; i++) {
            calculator.Calculate(deepExpr);
        }

        long endMem = GC.GetAllocatedBytesForCurrentThread();
        sw.Stop();

        double opsPerSec = iterations / sw.Elapsed.TotalSeconds;
        long allocs = endMem - startMem;

        Console.WriteLine($"Time: {sw.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"Ops/Sec: {opsPerSec:N0}");
        Console.WriteLine($"Allocations: {allocs / 1024.0 / 1024.0:F2} MB");

        // Append to results
        string logFile = "benchmark_results.md";
        File.AppendAllText(logFile, $"\n### Deep Nesting Benchmark\n- **Expression**: Very deep nested IFs/Ternaries\n- **Iterations**: {iterations}\n- **Ops/Sec**: {opsPerSec:N0}\n- **Allocations**: {allocs / 1024.0 / 1024.0:F2} MB\n");
    }
}
