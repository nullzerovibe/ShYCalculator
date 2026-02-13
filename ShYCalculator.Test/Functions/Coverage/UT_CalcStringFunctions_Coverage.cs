using ShYCalculator.Classes;
using ShYCalculator.Functions.Text;
using ShYCalculator.Functions.Dates;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions;
using System.Reflection;
using System.Collections.Frozen;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Coverage;

[TestClass]
public class UT_CalcStringFunctions_Coverage {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;
    private CalcStringFunctions stringFuncs = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        // Register default functions 
        stringFuncs = new CalcStringFunctions();
        m_environment.RegisterFunctions(new CalcDateFunctions());
        m_environment.RegisterFunctions(new CalcArithmeticalFunctions());
        m_environment.RegisterFunctions(new CalcNumericFunctions());
        m_environment.RegisterFunctions(stringFuncs);
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
    public void StringFunctions_ExecuteFunction_Unknown_Throws() {
        AssertThrows<FunctionException>(() => stringFuncs.ExecuteFunction("UnknownFunc", []));
    }

    [TestMethod]
    public void StringFunctions_Contains_NullParams_Coverage() {
        var stack = new Stack<Value>();
        stack.Push(new Value { Svalue = "test", DataType = DataType.String });
        stack.Push(new Value { Svalue = null, DataType = DataType.String });

        try {
            var result = stringFuncs.ExecuteFunction("str_contains", stack.ToArray());
            Assert.IsFalse(result.Bvalue ?? true);
        }
        catch (FunctionException) {
            // If CheckFunctionArguments blocks null Svalue, that's also coverage.
        }
    }

    [TestMethod]
    public void StringFunctions_CompareStrings_NullParams_Coverage() {
        var stack = new Stack<Value>();
        stack.Push(new Value { Svalue = "test", DataType = DataType.String });
        stack.Push(new Value { Svalue = null, DataType = DataType.String });
        try {
            var result = stringFuncs.ExecuteFunction("str_equal", stack.ToArray());
            Assert.IsFalse(result.Bvalue ?? true);
        }
        catch (FunctionException) { }
    }

    [TestMethod]
    public void StringFunctions_AllStringValues_Internal_Coverage() {
        var result = CalcStringFunctions.AllStringValues([
            new Value { Svalue = "a", DataType = DataType.String },
            new Value { Svalue = "b", DataType = DataType.String }
        ]);
        Assert.IsTrue(result.Bvalue);

        result = CalcStringFunctions.AllStringValues([
            new Value { Svalue = "a", DataType = DataType.String },
            new Value { Svalue = null, DataType = DataType.String },
            new Value { Svalue = "b", DataType = DataType.String }
        ]);
        Assert.IsFalse(result.Bvalue);
    }

    [TestMethod]
    public void StringFunctions_Internal_Branch_Coverage() {
        // Test parameters.Length < 2
        var result = CalcStringFunctions.Contains([new Value { Svalue = "test", DataType = DataType.String }], CalcStringFunctions.StringContainsMode.Contains);
        Assert.AreEqual(DataType.Boolean, result.DataType & DataType.Boolean);
        Assert.IsFalse(result.Bvalue);

        // Test parameters.Length > 3 (using private enum for mode)
        result = CalcStringFunctions.Contains([new Value(), new Value(), new Value(), new Value()], CalcStringFunctions.StringContainsMode.Contains);
        Assert.AreEqual(DataType.Boolean, result.DataType & DataType.Boolean);
        Assert.IsFalse(result.Bvalue);
    }

    [TestMethod]
    public void StringFunctions_Contains_StringNullOrEmpty_Coverage() {
        // Test s1 null/empty
        var result = CalcStringFunctions.Contains([new Value { Svalue = null, DataType = DataType.String }, new Value { Svalue = "test", DataType = DataType.String }], CalcStringFunctions.StringContainsMode.Contains);
        Assert.AreEqual(DataType.Boolean, result.DataType & DataType.Boolean, $"Expected DataType.Boolean flag in {result.DataType}");
        Assert.IsFalse(result.Bvalue, $"Bvalue should be false for null input. DataType was {result.DataType}");

        result = CalcStringFunctions.Contains([new Value { Svalue = "test", DataType = DataType.String }, new Value { Svalue = "", DataType = DataType.String }], CalcStringFunctions.StringContainsMode.Contains);
        Assert.AreEqual(DataType.Boolean, result.DataType & DataType.Boolean, $"Expected DataType.Boolean flag in {result.DataType}");
        Assert.IsFalse(result.Bvalue, $"Bvalue should be false for empty input. DataType was {result.DataType}");
    }

    [TestMethod]
    public void StringFunctions_CompareStrings_Internal_Branch_Coverage() {
        // Test values.Length < 2
        var result = CalcStringFunctions.CompareStrings([new Value { Svalue = "test", DataType = DataType.String }], CalcStringFunctions.StringCompareMode.Equal);
        Assert.IsFalse(result.Bvalue);
    }

    [TestMethod]
    public void StringFunctions_ExecuteFunction_Fallback_Coverage() {
        // Use reflection to add a dummy entry to m_funcDef
        var field = typeof(CalcStringFunctions).GetField("m_funcDef", BindingFlags.NonPublic | BindingFlags.Instance);
        var existing = (System.Collections.Frozen.FrozenDictionary<string, CalcFunction>)field!.GetValue(stringFuncs)!;

        var updated = existing.ToDictionary();
        // Add a dummy function that isn't in the switch in ExecuteFunction
        updated["DummyFunc"] = new CalcFunction { Name = "DummyFunc" };

        field.SetValue(stringFuncs, updated.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));

        AssertThrows<FunctionException>(() => stringFuncs.ExecuteFunction("DummyFunc", []));
    }

    [TestMethod]
    public void StringFunctions_Contains_FullBranch_Coverage() {
        // Contains - False
        Assert.IsFalse(m_shyCalculator.Calculate("str_contains('Hello', 'world')").Bvalue);

        // StartsWith - True
        Assert.IsTrue(m_shyCalculator.Calculate("str_starts('Hello World', 'Hello')").Bvalue);
        // StartsWith - False
        Assert.IsFalse(m_shyCalculator.Calculate("str_starts('Hello World', 'World')").Bvalue);

        // EndsWith - True
        Assert.IsTrue(m_shyCalculator.Calculate("str_ends('Hello World', 'World')").Bvalue);
        // EndsWith - False
        Assert.IsFalse(m_shyCalculator.Calculate("str_ends('Hello World', 'Hello')").Bvalue);

        // Mode fallback (though enum is exhaustive, good for robustness if we use reflection)
        var result = CalcStringFunctions.Contains([
            new() { Svalue = "a", DataType = DataType.String },
            new() { Svalue = "b", DataType = DataType.String }
        ], (CalcStringFunctions.StringContainsMode)99);
        Assert.IsFalse(result.Bvalue);
    }

    [TestMethod]
    public void StringFunctions_CompareStrings_FullBranch_Coverage() {
        // EqualStr - Case-insensitive
        Assert.IsTrue(m_shyCalculator.Calculate("str_equal('a', 'A')").Bvalue);
        // EqualStr - Case-sensitive true
        Assert.IsTrue(m_shyCalculator.Calculate("str_equal('a', 'a', true)").Bvalue);
        // EqualStr - Case-sensitive false
        Assert.IsFalse(m_shyCalculator.Calculate("str_equal('a', 'A', true)").Bvalue);

        // NotEqualStr - Case-insensitive
        Assert.IsFalse(m_shyCalculator.Calculate("str_notequal('a', 'A')").Bvalue);
        Assert.IsTrue(m_shyCalculator.Calculate("str_notequal('a', 'b')").Bvalue);
        // NotEqualStr - Case-sensitive true
        Assert.IsTrue(m_shyCalculator.Calculate("str_notequal('a', 'A', true)").Bvalue);

        // CompareStrings reflection - trigger arguments.Count() <= 1
        var result = CalcStringFunctions.CompareStrings([new Value { Svalue = "a", DataType = DataType.String }], CalcStringFunctions.StringCompareMode.Equal);
        Assert.IsFalse(result.Bvalue);

        // CompareStrings reflection - trigger allString.Bvalue == false branch
        // Note: Equal mode
        result = CalcStringFunctions.CompareStrings([
            new() { Svalue = "a", DataType = DataType.String },
            new() { Svalue = null, DataType = DataType.String }
        ], CalcStringFunctions.StringCompareMode.Equal);
        Assert.IsFalse(result.Bvalue);

        // CompareStrings reflection - trigger compareMode == NotEqual (1) with Count() <= 1
        result = CalcStringFunctions.CompareStrings([new Value { Svalue = "a", DataType = DataType.String }], CalcStringFunctions.StringCompareMode.NotEqual);
        Assert.IsFalse(result.Bvalue);
    }
}
