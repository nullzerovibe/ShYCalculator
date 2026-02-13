using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions.Dates;
using ShYCalculator.Functions.Text;
using System.Reflection;
using ShYCalculator.Functions;
using System.Collections.Frozen;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Coverage;

[TestClass]
public class UT_CalcNumericFunctions_Coverage {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;
    private CalcNumericFunctions otherFuncs = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        // Register default functions as some tests expect them
        otherFuncs = new CalcNumericFunctions();
        m_environment.RegisterFunctions(new CalcDateFunctions());
        m_environment.RegisterFunctions(otherFuncs);
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
    public void OtherFunctions_AnyEqualAdjacentNumber_Tolerance_Internal() {
        var numbers = new[] {
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 2.0, DataType = DataType.Number }
        };
        var result = CalcNumericFunctions.AnyEqualAdjacentNumber(numbers, 1);
        Assert.IsTrue(result.Bvalue);

        numbers = [
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 2.0, DataType = DataType.Number },
            new Value { Nvalue = 3.0, DataType = DataType.Number }
        ];
        result = CalcNumericFunctions.AnyEqualAdjacentNumber(numbers, 1);
        Assert.IsFalse(result.Bvalue);
    }

    [TestMethod]
    public void OtherFunctions_Order_Internal_Branch_Coverage() {
        // Test values.Length < 2
        var result = CalcNumericFunctions.CompareNumbersOrder([new() { Nvalue = 1.0, DataType = DataType.Number }], CalcNumericFunctions.NumberCompareMode.Ascending, null);
        Assert.IsFalse(result.Bvalue);
    }

    [TestMethod]
    public void OtherFunctions_ExecuteFunction_Fallback_Throws() {
        AssertThrows<FunctionException>(() => otherFuncs.ExecuteFunction("NonExistent", []));
    }

    [TestMethod]
    public void OtherFunctions_ExecuteFunction_Fallback_Diagnostic() {
        var m_funcDefField = typeof(CalcNumericFunctions).GetField("m_funcDef", BindingFlags.NonPublic | BindingFlags.Instance);
        var existing = (System.Collections.Frozen.FrozenDictionary<string, CalcFunction>)m_funcDefField!.GetValue(otherFuncs)!;

        var updated = existing.ToDictionary();
        updated["FakeFunction"] = new CalcFunction { Name = "FakeFunction", Arguments = [] };

        m_funcDefField.SetValue(otherFuncs, updated.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));

        AssertThrows<FunctionException>(() => otherFuncs.ExecuteFunction("FakeFunction", []));
    }

    [TestMethod]
    public void OtherFunctions_Order_BooleanArg_Internal() {
        // Test with trailing boolean (hasStrictOrderArg = true)
        // Ascending, strict (last is true)
        var parameters = new Value[] {
            new() { Nvalue = 1.0, DataType = DataType.Number },
            new() { Nvalue = 2.0, DataType = DataType.Number },
            new() { Bvalue = true, DataType = DataType.Boolean }
        };
        var result = CalcNumericFunctions.CompareNumbersOrder(parameters, CalcNumericFunctions.NumberCompareMode.Ascending, null);
        Assert.IsTrue(result.Bvalue);

        // Ascending, non-strict (last is false)
        parameters = [
            new() { Nvalue = 1.0, DataType = DataType.Number },
            new() { Nvalue = 1.0, DataType = DataType.Number },
            new() { Bvalue = false, DataType = DataType.Boolean }
        ];
        var result2 = CalcNumericFunctions.CompareNumbersOrder(parameters, CalcNumericFunctions.NumberCompareMode.Ascending, null);
        Assert.IsTrue(result2.Bvalue);

        // Nulls in arguments
        parameters = [
            new() { Nvalue = 1.0, DataType = DataType.Number },
            new() { Nvalue = null, DataType = DataType.Number },
            new() { Bvalue = false, DataType = DataType.Boolean }
        ];
        var result3 = CalcNumericFunctions.CompareNumbersOrder(parameters, CalcNumericFunctions.NumberCompareMode.Ascending, null);
        Assert.IsFalse(result3.Bvalue);
    }

    [TestMethod]
    public void OtherFunctions_IsSameDouble_Exhaustive_Coverage() {
        // Branch: Math.Log10(other) check when thisDouble is 0
        Assert.IsTrue(CalcNumericFunctions.IsSameDouble(0.0, 1e-8, 7));
        Assert.IsFalse(CalcNumericFunctions.IsSameDouble(0.0, 0.1, 7));

        // Branch: Math.Log10(thisDouble) check when other is 0
        Assert.IsTrue(CalcNumericFunctions.IsSameDouble(1e-8, 0.0, 7));
        Assert.IsFalse(CalcNumericFunctions.IsSameDouble(0.1, 0.0, 7));

        // Opposite signs
        Assert.IsFalse(CalcNumericFunctions.IsSameDouble(-1.0, 1.0, 7));
        Assert.IsFalse(CalcNumericFunctions.IsSameDouble(1.0, -1.0, 7));

        // num > num2 branch in ternary
        Assert.IsTrue(CalcNumericFunctions.IsSameDouble(1.1, 1.099999999, 7));
        Assert.IsFalse(CalcNumericFunctions.IsSameDouble(1.1, 1.0, 7));
    }

    [TestMethod]
    public void OtherFunctions_AnyEqualAdjacentNumber_Shortcuts() {
        // If something is not a number, it returns AllNumericValues result (false)
        var numbers = new[] {
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = null, DataType = DataType.Number }
        };
        var result = CalcNumericFunctions.AnyEqualAdjacentNumber(numbers, 0);
        Assert.IsFalse(result.Bvalue);

        // Tolerance hit: 1, 1, 2 with tolerance 0 -> true
        numbers = [
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 2.0, DataType = DataType.Number }
        ];
        result = CalcNumericFunctions.AnyEqualAdjacentNumber(numbers, 0);
        Assert.IsTrue(result.Bvalue);

        // Tolerance NOT hit: 1, 1, 2 with tolerance 1 -> false
        numbers = [
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 2.0, DataType = DataType.Number }
        ];
        result = CalcNumericFunctions.AnyEqualAdjacentNumber(numbers, 1);
        Assert.IsFalse(result.Bvalue);

        // Reset count on non-match
        numbers = [
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 1.0, DataType = DataType.Number },
            new Value { Nvalue = 2.0, DataType = DataType.Number },
            new Value { Nvalue = 2.0, DataType = DataType.Number },
            new Value { Nvalue = 2.0, DataType = DataType.Number }
        ];
        result = CalcNumericFunctions.AnyEqualAdjacentNumber(numbers, 1);
        Assert.IsTrue(result.Bvalue, "Should find three 2s (two matches) exceeding tolerance 1");
    }

    [TestMethod]
    public void OtherFunctions_Order_Descending_Internal() {
        // Test descending branch
        var parameters = new Value[] {
            new() { Nvalue = 3.0, DataType = DataType.Number },
            new() { Nvalue = 2.0, DataType = DataType.Number },
            new() { Nvalue = 1.0, DataType = DataType.Number }
        };
        // Descending mode
        var result = CalcNumericFunctions.CompareNumbersOrder(parameters, CalcNumericFunctions.NumberCompareMode.Descending, null);
        Assert.IsTrue(result.Bvalue);

        parameters = [
            new() { Nvalue = 1.0, DataType = DataType.Number },
            new() { Nvalue = 2.0, DataType = DataType.Number }
        ];
        var result2 = CalcNumericFunctions.CompareNumbersOrder(parameters, CalcNumericFunctions.NumberCompareMode.Descending, null);
        Assert.IsFalse(result2.Bvalue);
    }

    [TestMethod]
    public void OtherFunctions_Order_InvalidMode_Internal() {
        // Test with an invalid enum value to hit the fallback branch
        var parameters = new Value[] {
            new() { Nvalue = 1.0, DataType = DataType.Number },
            new() { Nvalue = 2.0, DataType = DataType.Number }
        };
        var result = CalcNumericFunctions.CompareNumbersOrder(parameters, (CalcNumericFunctions.NumberCompareMode)99, null);
        Assert.IsFalse(result.Bvalue);
    }
}
