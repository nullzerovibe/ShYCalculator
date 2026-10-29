using ShY.Classes;

namespace ShY.Test;

[TestClass]
public class UT_Hardening {
    private static string NestedIf(int depth) => string.Concat(Enumerable.Repeat("if(true, ", depth)) + "1" + string.Concat(Enumerable.Repeat(", 0)", depth));
    private static string NestedTernary(int depth) => string.Concat(Enumerable.Repeat("true ? (", depth)) + "1" + string.Concat(Enumerable.Repeat(") : 0", depth));
    private static string NestedAnd(int depth) => string.Concat(Enumerable.Repeat("true && (", depth)) + "true" + new string(')', depth);

    private static void AssertLimitExceeded(CalculationResult result) {
        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.LimitExceeded, result.Errors[0].Code, result.Message);
    }

    #region 1. Nesting and size limits
    [TestMethod]
    [DataRow(64)]
    public void Nesting_AtDefaultLimit_Evaluates(int depth) {
        var calc = new ShYCalculator();

        Assert.AreEqual(1, calc.Calculate(NestedIf(depth)).Nvalue);
        Assert.AreEqual(1, calc.Calculate(NestedTernary(depth)).Nvalue);
        Assert.IsTrue(calc.Calculate(NestedAnd(depth)).Bvalue);
    }

    [TestMethod]
    [DataRow(65)]
    [DataRow(1_000)]
    [DataRow(50_000)]
    public void Nesting_BeyondDefaultLimit_IsLimitExceeded(int depth) {
        var calc = new ShYCalculator();

        AssertLimitExceeded(calc.Calculate(NestedIf(depth)));
        AssertLimitExceeded(calc.Calculate(NestedTernary(depth)));
        AssertLimitExceeded(calc.Calculate(NestedAnd(depth)));
    }

    [TestMethod]
    public void Nesting_BeyondLimit_IsLimitExceeded_ForCompileAndAst() {
        var compiled = ShYCalculator.Compile(NestedAnd(1_000));
        Assert.IsFalse(compiled.Success);
        Assert.AreEqual(ErrorCode.LimitExceeded, compiled.Errors[0].Code);

        var ast = new ShYCalculator().GetAst(NestedIf(1_000));
        Assert.IsFalse(ast.Success);
        Assert.AreEqual(ErrorCode.LimitExceeded, ast.Errors[0].Code);
    }

    [TestMethod]
    public void Nesting_CustomLimit_IsHonored() {
        var calc = new ShYCalculator(options: new ShYCalculatorOptions { MaxNestingDepth = 3 });

        Assert.AreEqual(1, calc.Calculate(NestedIf(3)).Nvalue);
        AssertLimitExceeded(calc.Calculate(NestedIf(4)));
        AssertLimitExceeded(calc.Calculate(NestedAnd(4)));
    }

    [TestMethod]
    public void Nesting_LimitDisabled_StillNeverCrashesTheProcess() {
        var calc = new ShYCalculator(options: new ShYCalculatorOptions { MaxNestingDepth = 0 });

        // WHY: without a configured limit the execution-stack guard must turn a would-be StackOverflow into an error.
        AssertLimitExceeded(calc.Calculate(NestedIf(50_000)));
        AssertLimitExceeded(calc.Calculate(NestedAnd(50_000)));
        AssertLimitExceeded(calc.Calculate(NestedTernary(50_000), (IDictionary<string, Value>?)null, includeAst: true));
    }

    [TestMethod]
    public void Nesting_LimitDisabled_GuardHoldsOnSmallStackThread() {
        CalculationResult? result = null;
        var thread = new Thread(() => {
            var calc = new ShYCalculator(options: new ShYCalculatorOptions { MaxNestingDepth = 0 });
            result = calc.Calculate(NestedAnd(20_000));
        }, maxStackSize: 256 * 1024);

        thread.Start();
        thread.Join();

        AssertLimitExceeded(result!);
    }

    [TestMethod]
    public void Nesting_Parentheses_AreNotLimited() {
        var expression = new string('(', 10_000) + "1" + new string(')', 10_000);

        Assert.AreEqual(1, new ShYCalculator().Calculate(expression).Nvalue);
    }

    [TestMethod]
    public void Length_CustomLimit_IsHonored() {
        var calc = new ShYCalculator(options: new ShYCalculatorOptions { MaxExpressionLength = 10 });

        Assert.AreEqual(3, calc.Calculate("1 + 2").Nvalue);
        AssertLimitExceeded(calc.Calculate("1 + 2 + 3 + 4"));
    }

    [TestMethod]
    public void Length_DefaultIsUnlimited() {
        var expression = string.Join("+", Enumerable.Repeat("1", 50_000));

        Assert.AreEqual(50_000, new ShYCalculator().Calculate(expression).Nvalue);
    }
    #endregion

    #region 3. Builder isolation
    [TestMethod]
    public void Builder_LaterConfiguration_DoesNotLeakIntoBuiltCalculator() {
        var builder = new ShYCalculatorBuilder().WithMathematics();
        var first = builder.Build();

        builder.WithConstant("pi", 3).WithText();

        Assert.AreEqual(Math.PI, first.Calculate("pi").Nvalue);
        Assert.IsFalse(first.Environment.Functions.ContainsKey("str_contains"));
        Assert.AreEqual(3, builder.Build().Calculate("pi").Nvalue);
    }

    [TestMethod]
    public void Builder_BuildTwice_ReturnsIndependentEnvironments() {
        var builder = new ShYCalculatorBuilder().WithMathematics();

        var first = builder.Build();
        var second = builder.Build();
        second.Environment.AddConstant("pi", 4);

        Assert.AreNotSame(first.Environment, second.Environment);
        Assert.AreEqual(Math.PI, first.Calculate("pi").Nvalue);
    }

    [TestMethod]
    public void Builder_LastRegistrationWins_AsBefore() {
        var calc = new ShYCalculatorBuilder().WithConstant("k", 1).WithConstant("k", 2).Build();

        Assert.AreEqual(2, calc.Calculate("k").Nvalue);
    }
    #endregion

    #region 4. Options through builder and DI
    [TestMethod]
    public void Builder_WithOptions_AppliesDateCulture_RegardlessOfOrder() {
        var calc = new ShYCalculatorBuilder()
            .WithDate()
            .WithOptions(o => { o.CultureName = "fr-FR"; o.DateFormat = "dd MMMM yyyy"; })
            .Build();

        var result = calc.Calculate("dt_month('15 janvier 2026')");

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(1, result.Nvalue);
    }

    [TestMethod]
    public void Builder_WithOptions_AppliesLimits() {
        var calc = new ShYCalculatorBuilder().WithLogic().WithOptions(o => o.MaxNestingDepth = 2).Build();

        Assert.IsTrue(calc.Calculate("true && (true && true)").Bvalue);
        AssertLimitExceeded(calc.Calculate("true && (true && (true && true))"));
        Assert.AreEqual(2, calc.Options.MaxNestingDepth);
    }

    [TestMethod]
    public void DependencyInjection_WithOptions_AppliesLimitsToResolvedCalculator() {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        DependencyInjection.ServiceCollectionExtensions.AddShYCalculator(services, b => b.WithLogic().WithOptions(o => o.MaxExpressionLength = 5));
        using var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);

        var calc = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ShYCalculator>(provider);

        Assert.IsTrue(calc.Calculate("true").Bvalue);
        AssertLimitExceeded(calc.Calculate("true && true"));
    }
    #endregion

    #region 5. Function registration errors
    private sealed class PartiallyConflictingFunctions : global::ShY.Functions.ICalcFunctionsExtension {
        public string Name => "PartiallyConflicting";
        public IEnumerable<global::ShY.Functions.CalcFunction> GetFunctions() => [new() { Name = "brand_new_fn" }, new() { Name = "max" }];
        public Value ExecuteFunction(string functionName, ReadOnlySpan<Value> parameters) => Value.Number(0);
    }

    [TestMethod]
    public void RegisterFunctions_Conflict_ReportsReasonAndLeavesEnvironmentUnchanged() {
        var env = new global::ShY.Calculator.CalculatorEnvironment();

        var ex = Assert.Throws<BaseCalcException>(() => env.RegisterFunctions(new PartiallyConflictingFunctions(), overrideExistingFunctions: false), "should throw");

        Assert.Contains("already registered", ex.Message);
        Assert.Contains("max", ex.Message);
        Assert.IsFalse(env.Functions.ContainsKey("brand_new_fn"));
    }
    #endregion

    #region 6. Construction cost
    [TestMethod]
    public void FunctionDefinitions_AreParsedOncePerProcess() {
        var first = new global::ShY.Functions.Mathematics.CalcNumericFunctions().GetFunctions().First();
        var second = new global::ShY.Functions.Mathematics.CalcNumericFunctions().GetFunctions().First();

        Assert.AreSame(first, second);
    }
    #endregion

    #region 7. Variable and constant lookups
    private sealed class TrimmingComparer : IEqualityComparer<string> {
        public bool Equals(string? x, string? y) => string.Equals(x?.Trim(), y?.Trim(), StringComparison.Ordinal);
        public int GetHashCode(string obj) => obj.Trim().GetHashCode();
    }

    [TestMethod]
    public void Lookup_CustomComparerVariables_Work() {
        var vars = new Dictionary<string, Value>(new TrimmingComparer()) { ["$x"] = Value.Number(2) };

        var result = new ShYCalculator().Calculate("$x * 3", vars);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(6, result.Nvalue);
    }

    [TestMethod]
    public void Lookup_CustomComparerConstants_Work() {
        var env = new global::ShY.Calculator.CalculatorEnvironment();
        env.SetConstants(new Dictionary<string, Value>(new TrimmingComparer()) { ["k"] = Value.Number(7) });

        var result = new ShYCalculator(env).Calculate("k + 1");

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(8, result.Nvalue);
    }

    [TestMethod]
    public void Lookup_DoubleDictionary_WithAnyComparer_Works() {
        var calc = new ShYCalculator();
        var ordinal = new Dictionary<string, double> { ["$a"] = 2, ["$b"] = 5 };
        var ignoreCase = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["$A"] = 2, ["$B"] = 5 };
        var custom = new Dictionary<string, double>(new TrimmingComparer()) { ["$a"] = 2, ["$b"] = 5 };
        IEnumerable<KeyValuePair<string, double>> sequence = [new("$a", 2), new("$b", 5)];

        Assert.AreEqual(10, calc.Calculate("$a * $b", ordinal).Nvalue);
        Assert.AreEqual(10, calc.Calculate("$a * $b", ignoreCase).Nvalue);
        Assert.AreEqual(10, calc.Calculate("$a * $b", custom).Nvalue);
        Assert.AreEqual(10, calc.Calculate("$a * $b", sequence).Nvalue);
    }

    [TestMethod]
    public void Lookup_ContextOverridesEnvironmentVariables() {
        var env = new global::ShY.Calculator.CalculatorEnvironment();
        env.AddVariable("$a", 1);
        env.AddVariable("$b", 10);
        var calc = new ShYCalculator(env);

        Assert.AreEqual(12, calc.Calculate("$a + $b", new Dictionary<string, double> { ["$a"] = 2 }).Nvalue);
    }
    #endregion

    #region 9. Injectable time
    private sealed class FixedTime(DateTimeOffset utcNow, TimeZoneInfo zone) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => utcNow;
        public override TimeZoneInfo LocalTimeZone => zone;
    }

    // 2026-03-04 23:30 UTC is already 2026-03-05 01:30 in a UTC+2 zone: the local date must win.
    private static readonly FixedTime LateEveningUtc = new(
        new DateTimeOffset(2026, 3, 4, 23, 30, 0, TimeSpan.Zero),
        TimeZoneInfo.CreateCustomTimeZone("Test+2", TimeSpan.FromHours(2), "Test+2", "Test+2"));

    [TestMethod]
    [DataRow("dt_day(dt_now())", 5d)]
    [DataRow("dt_hour(dt_now())", 1d)]
    [DataRow("dt_minute(dt_now())", 30d)]
    [DataRow("dt_day(dt_today())", 5d)]
    [DataRow("dt_hour(dt_today())", 0d)]
    public void Time_ProviderFromOptions_DrivesNowAndToday(string expression, double expected) {
        var calc = new ShYCalculator(options: new ShYCalculatorOptions { TimeProvider = LateEveningUtc });

        var result = calc.Calculate(expression);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Nvalue);
    }

    [TestMethod]
    public void Time_ProviderFromBuilder_DrivesNow() {
        var calc = new ShYCalculatorBuilder().WithDate().WithOptions(o => o.TimeProvider = LateEveningUtc).Build();

        Assert.AreEqual(2026, calc.Calculate("dt_year(dt_now())").Nvalue);
        Assert.AreEqual(TimeSpan.FromHours(2), calc.Calculate("dt_now()").Dvalue!.Value.Offset);
    }
    #endregion
}
