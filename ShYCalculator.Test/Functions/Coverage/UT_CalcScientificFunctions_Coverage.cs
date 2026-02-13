using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Frozen;

namespace ShYCalculator.Test.Functions.Coverage;

[TestClass]
public class UT_CalcScientificFunctions_Coverage {
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
    public void Test_ExecuteFunction_InvalidName_Throws() {
        var calcFuncs = new CalcScientificFunctions();
        AssertThrows<FunctionException>(() => calcFuncs.ExecuteFunction("NonExistent", []));
    }

    [TestMethod]
    public void Test_MathInternal_NaN_Handling() {
        var calcFuncs = new CalcScientificFunctions();
        AssertThrows<FunctionException>(() => calcFuncs.Cot(double.NaN));
        AssertThrows<FunctionException>(() => calcFuncs.Coth(double.NaN));
        AssertThrows<FunctionException>(() => calcFuncs.Acot(double.NaN));
    }

    [TestMethod]
    public void Test_GetFunctions_Coverage() {
        var calcFuncs = new CalcScientificFunctions();
        var functions = calcFuncs.GetFunctions().ToList();
        Assert.IsNotNull(functions);
        Assert.IsTrue(functions.Any(f => f.Name == "sin"));
    }

    [TestMethod]
    public void Test_ExecuteFunction_AllNullValues_Coverage() {
        // Test that all functions handle null Nvalue correctly (using ?? 0 fallback)
        var calcFuncs = new CalcScientificFunctions();
        var stack = new Stack<Value>();

        void TestFuncWithNull(string name, int argCount, double expected) {
            stack.Clear();
            for (int i = 0; i < argCount; i++) {
                stack.Push(new Value { Nvalue = null, DataType = DataType.Number });
            }
            var result = calcFuncs.ExecuteFunction(name, stack.ToArray());
            Assert.AreEqual(expected, result.Nvalue!.Value, 1e-9, $"Failed for {name} with null values");
        }

        // Single argument functions with null => 0
        TestFuncWithNull("sin", 1, 0.0);
        TestFuncWithNull("cos", 1, 1.0);
        TestFuncWithNull("tan", 1, 0.0);
        TestFuncWithNull("cot", 1, double.PositiveInfinity); // 1/tan(0) = infinity
        TestFuncWithNull("ln", 1, double.NegativeInfinity); // ln(0) = -infinity
        TestFuncWithNull("log2", 1, double.NegativeInfinity);
        TestFuncWithNull("log10", 1, double.NegativeInfinity);
        TestFuncWithNull("sinh", 1, 0.0);
        TestFuncWithNull("cosh", 1, 1.0);
        TestFuncWithNull("tanh", 1, 0.0);
        TestFuncWithNull("coth", 1, double.PositiveInfinity); // cosh(0)/sinh(0)
        TestFuncWithNull("asin", 1, 0.0);
        TestFuncWithNull("acos", 1, Math.PI / 2);
        TestFuncWithNull("atan", 1, 0.0);
        TestFuncWithNull("acot", 1, Math.PI / 2); // atan(1/0)

        // Two argument functions with null => 0
        TestFuncWithNull("log", 2, double.NaN); // log(0, 0) = NaN
    }

    [TestMethod]
    public void Test_ExecuteFunction_MixedNullValues_Coverage() {
        // Test functions with some null and some non-null values
        var calcFuncs = new CalcScientificFunctions();
        var stack = new Stack<Value>();

        // log with one null
        stack.Clear();
        stack.Push(new Value { Nvalue = null, DataType = DataType.Number }); // base = 0
        stack.Push(new Value { Nvalue = 100.0, DataType = DataType.Number });
        var result = calcFuncs.ExecuteFunction("log", stack.ToArray());
        Assert.IsTrue(double.IsNaN(result.Nvalue!.Value));

        // log with other null
        stack.Clear();
        stack.Push(new Value { Nvalue = 10.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = null, DataType = DataType.Number }); // number = 0
        result = calcFuncs.ExecuteFunction("log", stack.ToArray());
        Assert.IsTrue(double.IsNegativeInfinity(result.Nvalue!.Value));
    }

    [TestMethod]
    public void Test_ExecuteFunction_Fallback_Diagnostic() {
        // This tests the unreachable default case in the switch statement
        var calcFuncs = new CalcScientificFunctions();

        // Get the private m_funcDef field via Reflection
        var funcDefField = typeof(CalcScientificFunctions).GetField("m_funcDef", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(funcDefField, "m_funcDef field should exist");

        var existing = (System.Collections.Frozen.FrozenDictionary<string, CalcFunction>)funcDefField!.GetValue(calcFuncs)!;

        var updated = existing.ToDictionary();
        // Inject a dummy function that exists in the dictionary but not in the switch
        var dummyFunction = new CalcFunction {
            Name = "FakeFunction",
            Description = "Test function",
            Arguments = []
        };
        updated["FakeFunction"] = dummyFunction;

        funcDefField.SetValue(calcFuncs, updated.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));

        // Now call ExecuteFunction - it will pass the dictionary check but fail in the switch
        var stack = new Stack<Value>();
        AssertThrows<FunctionException>(() => calcFuncs.ExecuteFunction("FakeFunction", stack.ToArray()));
    }
}
