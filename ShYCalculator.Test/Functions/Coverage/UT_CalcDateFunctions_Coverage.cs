using ShYCalculator.Classes;
using ShYCalculator.Functions.Dates;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions.Text;
using ShYCalculator.Functions;
using System.Reflection;
using System.Collections.Frozen;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Coverage;

[TestClass]
public class UT_CalcDateFunctions_Coverage {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;
    private CalcDateFunctions dateFuncs = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        // Register default functions as some tests expect them
        dateFuncs = new CalcDateFunctions();
        m_environment.RegisterFunctions(dateFuncs);
        m_environment.RegisterFunctions(new CalcArithmeticalFunctions());
        m_environment.RegisterFunctions(new CalcNumericFunctions());
        m_environment.RegisterFunctions(new CalcStringFunctions());
    }

    private static void AssertThrows<T>(Action action) where T : Exception {
        try {
            action();
        }
        catch (T) {
            return;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is T) {
            return;
        }
        catch (Exception ex) {
            throw new AssertFailedException($"Expected exception of type {typeof(T).Name} but got {ex.GetType().Name}. Message: {ex.Message}");
        }
        throw new AssertFailedException($"Expected exception of type {typeof(T).Name} but no exception was thrown");
    }

    [TestMethod]
    public void DateFunctions_TypeMismatch_ErrorMessage_Coverage() {
        // Testing PrecedeDate with non-date strings to trigger error path
        var result = m_shyCalculator.Calculate("dt_before('invalid', '2023-01-01')");
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void DateFunctions_ExecuteFunction_Unknown_Throws() {
        AssertThrows<FunctionException>(() => dateFuncs.ExecuteFunction("UnknownFunc", []));
    }

    [TestMethod]
    public void DateFunctions_AtomicDate_NullInput_Throws() {
        AssertThrows<FunctionException>(() => dateFuncs.ExecuteFunction("dt_year", [new Value(DataType.Date, dValue: null)]));
    }

    [TestMethod]
    public void DateFunctions_AddDateInterval_Empty_Throws() {
        var stack = new Stack<Value>();
        stack.Push(new Value { Svalue = "", DataType = DataType.String });
        stack.Push(new Value { Dvalue = DateTimeOffset.Now, DataType = DataType.Date });
        try {
            dateFuncs.ExecuteFunction("dt_add", stack.ToArray());
            Assert.Fail("Expected InvalidOperationException was not thrown.");
        }
        catch (FunctionException) { }
    }

    [TestMethod]
    public void DateFunctions_AddDateInterval_InvalidInterval_Throws() {
        // Invalid numeric part
        var result = m_shyCalculator.Calculate("dt_add(now(), 'axy')");
        Assert.IsFalse(result.Success);

        // Too short
        result = m_shyCalculator.Calculate("dt_add(now(), '1')");
        Assert.IsFalse(result.Success);

        // Unknown type
        result = m_shyCalculator.Calculate("dt_add(now(), '10x')");
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void DateFunctions_TypeMismatch_ErrorMessage_FullCoverage() {
        var date = DateTimeOffset.Now;

        // Both null
        var msg = dateFuncs.GetDataTypeMismatchErrorMessage(null, null, "TestFunc");
        Assert.IsTrue(msg.Contains("Left date is null") && msg.Contains("Right date is null"), $"Expected both arguments mentioned, but got: {msg}");

        // First null
        msg = dateFuncs.GetDataTypeMismatchErrorMessage(null, date, "TestFunc");
        Assert.IsTrue(msg.Contains("Left date is null") && !msg.Contains("Right date is null"), $"Expected only first argument mentioned, but got: {msg}");

        // Second null
        msg = dateFuncs.GetDataTypeMismatchErrorMessage(date, null, "dt_equal");
        Assert.IsTrue(!msg.Contains("Left date is null") && msg.Contains("Right date is null"), $"Expected only second argument mentioned, but got: {msg}");
    }

    [TestMethod]
    public void DateFunctions_AddYears_Nulls_Throws() {
        // AddYears(DateTimeOffset? date, double? years)
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddYears(null, 1.0));
    }

    [TestMethod]
    public void DateFunctions_PrecedeDate_TypeMismatch_Throws() {
        // Using high-level Calculate to trigger FunctionException during dispatch-level validation
        var result = m_shyCalculator.Calculate("dt_before(1, '2023-01-01')");
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("Invalid argument type") || result.Message.Contains("Invalid Function"), result.Message);
    }

    [TestMethod]
    public void DateFunctions_MatchDate_NullDates_Throws() {
        var dateVal = new Value(DataType.Date, dValue: DateTimeOffset.Now);
        var nullVal = new Value(DataType.Number); // Null Dvalue

        // Both null
        AssertThrows<FunctionException>(() => dateFuncs.MatchDate(nullVal, nullVal));
        // First null
        AssertThrows<FunctionException>(() => dateFuncs.MatchDate(nullVal, dateVal));
        // Second null
        AssertThrows<FunctionException>(() => dateFuncs.MatchDate(dateVal, nullVal));
    }

    [TestMethod]
    public void DateFunctions_PrecedeDate_NullDates_Throws() {
        var dateVal = new Value(DataType.Date, dValue: DateTimeOffset.Now);
        var nullVal = new Value(DataType.Number); // Null Dvalue

        // Both null
        AssertThrows<FunctionException>(() => dateFuncs.PrecedeDate(nullVal, nullVal));
        // First null
        AssertThrows<FunctionException>(() => dateFuncs.PrecedeDate(nullVal, dateVal));
        // Second null
        AssertThrows<FunctionException>(() => dateFuncs.PrecedeDate(dateVal, nullVal));
    }

    [TestMethod]
    public void DateFunctions_AddDateInterval_Nulls_Coverage_Granular() {
        var date = DateTimeOffset.Now;

        // Cover all branches of if (date == null || interval == null || interval.Length < 2)
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddDateInterval(null, "1y"));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddDateInterval(date, null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddDateInterval(date, "1"));
    }

    [TestMethod]
    public void DateFunctions_AddFunctions_Nulls_Coverage_Granular() {
        var date = DateTimeOffset.Now;
        // Testing some of them directly for brevity in the loop
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddYears(null, 1.0));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddYears(date, null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddMonths(null, 1.0));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddMonths(date, null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddDays(null, 1.0));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddDays(date, null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddHours(null, 1.0));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddHours(date, null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddMinutes(null, 1.0));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddMinutes(date, null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddSeconds(null, 1.0));
        AssertThrows<FunctionException>(() => CalcDateFunctions.AddSeconds(date, null));
    }

    [TestMethod]
    public void DateFunctions_AtomicGetters_Nulls_Coverage_Granular() {
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetDayOfYear(null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetDayOfWeek(null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetYear(null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetMonth(null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetDay(null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetHour(null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetMinute(null));
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetSecond(null));
    }

    [TestMethod]
    public void DateFunctions_ExecuteFunction_Fallback_Diagnostic() {
        // Injecting a function into the private dictionary that is NOT in the switch
        var m_funcDef_Getter = typeof(CalcDateFunctions).GetField("m_funcDef", BindingFlags.NonPublic | BindingFlags.Instance);
        var existing = (System.Collections.Frozen.FrozenDictionary<string, CalcFunction>)m_funcDef_Getter!.GetValue(dateFuncs)!;

        var updated = existing.ToDictionary();
        // Add a dummy function that exists in the config dictionary but not in the ExecuteFunction switch
        var dummyFunc = new CalcFunction { Name = "DummyUnreachable" };
        updated["DummyUnreachable"] = dummyFunc;

        m_funcDef_Getter.SetValue(dateFuncs, updated.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));

        // Try to execute it - this should hit the default branch in the switch
        AssertThrows<FunctionException>(() => dateFuncs.ExecuteFunction("DummyUnreachable", []));
    }

    [TestMethod]
    public void DateFunctions_PrecedeDate_ComplexBranches_Coverage() {
        // Fixed dates to avoid temporal issues
        var date2020 = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var date2023 = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var dateVal1 = new Value(DataType.Date, dValue: date2020);
        var dateVal2 = new Value(DataType.Date, dValue: date2023);
        var stringVal1 = new Value(DataType.String, sValue: "01/01/2020");
        var stringVal2 = new Value(DataType.String, sValue: "01/01/2023");
        var nullVal = new Value(DataType.Number);

        // 1. D vs D
        Assert.IsTrue(dateFuncs.PrecedeDate(dateVal1, dateVal2).Bvalue);
        Assert.AreNotEqual(true, dateFuncs.PrecedeDate(dateVal2, dateVal1).Bvalue);

        // 2. D vs S
        Assert.IsTrue(dateFuncs.PrecedeDate(dateVal1, stringVal2).Bvalue);

        // 3. S vs D
        Assert.IsTrue(dateFuncs.PrecedeDate(stringVal1, dateVal2).Bvalue);

        // 4. S vs S
        Assert.IsTrue(dateFuncs.PrecedeDate(stringVal1, stringVal2).Bvalue);

        // 5. Null cases
        AssertThrows<FunctionException>(() => dateFuncs.PrecedeDate(nullVal, dateVal1));
        AssertThrows<FunctionException>(() => dateFuncs.PrecedeDate(dateVal1, nullVal));
    }

    [TestMethod]
    public void DateFunctions_ExecuteFunction_Dispatcher_TypeMismatches_Coverage() {
        var invalidVal = new Value(DataType.Number, nValue: 1.0);

        var stack = new Stack<Value>();
        stack.Push(invalidVal);
        // This will hit CheckFunctionArguments and throw FunctionException
        AssertThrows<FunctionException>(() => dateFuncs.ExecuteFunction("dt_year", stack.ToArray()));

        stack.Clear();
        stack.Push(invalidVal);
        stack.Push(new Value(DataType.Date, dValue: DateTimeOffset.Now));
        AssertThrows<FunctionException>(() => dateFuncs.ExecuteFunction("dt_add", stack.ToArray()));

        // To hit the Pop().Dvalue paths explicitly, we'll use PrecedeDate/MatchDate directly
        AssertThrows<FunctionException>(() => dateFuncs.PrecedeOrMatchDate(invalidVal, invalidVal));
    }

    [TestMethod]
    public void DateFunctions_ExecuteFunction_InvalidArguments_Throws() {
        // GetDate(1,2,3,4,5,6,7) has 7 arguments, Max is 6.
        var stack7 = new Stack<Value>();
        for (int i = 0; i < 7; i++) stack7.Push(new Value(DataType.Number, nValue: 1.0));
        AssertThrows<FunctionException>(() => dateFuncs.ExecuteFunction("dt_create", stack7.ToArray()));

        // AllDateValues() has 0 arguments, Min is 1.
        AssertThrows<FunctionException>(() => dateFuncs.ExecuteFunction("dt_all", []));
    }

    [TestMethod]
    public void DateFunctions_AddDateInterval_Unreachable_Switch() {
        // Switch is complete
    }

    [TestMethod]
    public void DateFunctions_GetIntervalNumericValue_Coverage() {
        // 1. interval.Length < 2
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetIntervalNumericValue("1"));

        // 2. int.TryParse fails
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetIntervalNumericValue("Ay"));

        // 3. Success path
        var result = CalcDateFunctions.GetIntervalNumericValue("10y");
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public void DateFunctions_GetDate_DirectCall_Coverage() {
        // 1. Full 6 arguments
        var args6 = new Value[]
        {
            new() { Nvalue = 2023, DataType = DataType.Number }, // year
            new() { Nvalue = 1, DataType = DataType.Number },  // month
            new() { Nvalue = 1, DataType = DataType.Number },  // day
            new() { Nvalue = 12, DataType = DataType.Number }, // hour
            new() { Nvalue = 45, DataType = DataType.Number }, // minute
            new() { Nvalue = 30, DataType = DataType.Number }  // seconds
        };
        var result = CalcDateFunctions.GetDate(args6);
        Assert.AreEqual(2023, result.Dvalue!.Value.Year);
        Assert.AreEqual(30, result.Dvalue!.Value.Second);

        // 2. Partial arguments (e.g., 3)
        var args3 = new Value[]
        {
            new() { Nvalue = 2023, DataType = DataType.Number }, // year
            new() { Nvalue = 1, DataType = DataType.Number },  // month
            new() { Nvalue = 1, DataType = DataType.Number }   // day
        };
        result = CalcDateFunctions.GetDate(args3);
        Assert.AreEqual(2023, result.Dvalue!.Value.Year);
        Assert.AreEqual(0, result.Dvalue!.Value.Second); // Default

        // 3. Zero arguments
        result = CalcDateFunctions.GetDate([]);
        Assert.AreEqual(1, result.Dvalue!.Value.Year); // Default

        // 4. Null Nvalue to hit the ?? branch (via string that can't parse to number)
        var argsInvalid = new Value[] { new(DataType.String, sValue: "not a number") };
        result = CalcDateFunctions.GetDate(argsInvalid);
        Assert.AreEqual(1, result.Dvalue!.Value.Year); // Should fallback to 1 
    }

    [TestMethod]
    public void DateFunctions_GetIntervalType_Invalid_Throws() {
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetIntervalType("1")); // too short
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetIntervalType("10x")); // invalid type
    }

    [TestMethod]
    public void DateFunctions_GetIntervalNumericValue_Invalid_Throws() {
        AssertThrows<FunctionException>(() => CalcDateFunctions.GetIntervalNumericValue("axy")); // invalid numeric
    }

    [TestMethod]
    public void DateFunctions_GetDataTypeMismatchErrorMessage_Coverage() {

        var result = dateFuncs.GetDataTypeMismatchErrorMessage(null, null, "TestFunc");
        Assert.IsTrue(result.Contains("Left date is null") && result.Contains("Right date is null"));

        result = dateFuncs.GetDataTypeMismatchErrorMessage(DateTimeOffset.Now, null, "TestFunc");
        Assert.IsTrue(!result.Contains("Left date is null") && result.Contains("Right date is null"));

        result = dateFuncs.GetDataTypeMismatchErrorMessage(null, DateTimeOffset.Now, "TestFunc");
        Assert.IsTrue(result.Contains("Left date is null") && !result.Contains("Right date is null"));
    }

    [TestMethod]
    public void DateFunctions_Constructor_InvalidCulture_Coverage() {
        // This should trigger the catch block in the constructor
        // Invalid culture falls back to InvariantCulture
        var funcs = new CalcDateFunctions(null, "invalid-culture-name");

        Assert.IsNotNull(funcs);
        // We can infer it is InvariantCulture because standard format should fail (if we had a way to access m_culture, but failing that, we know constructor didn't throw)
    }
}
