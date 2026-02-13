using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions;
using System.Reflection;
using System.Collections.Frozen;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Coverage;

[TestClass]
public class UT_CalcArithmeticalFunctions_Coverage {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;
    private CalcArithmeticalFunctions arithFuncs = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;
        arithFuncs = new CalcArithmeticalFunctions();
        m_environment.RegisterFunctions(arithFuncs);
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
    public void Test_ExecuteFunction_InvalidName_Throws() {
        AssertThrows<FunctionException>(() => arithFuncs.ExecuteFunction("NonExistent", []));
    }

    [TestMethod]
    public void Test_ExecuteFunction_AllNullValues_Coverage() {
        var stack = new Stack<Value>();

        void TestFuncWithNull(string name, int argCount, double expected) {
            stack.Clear();
            for (int i = 0; i < argCount; i++) {
                stack.Push(new Value { Nvalue = null, DataType = DataType.Number });
            }
            var result = arithFuncs.ExecuteFunction(name, stack.ToArray());
            Assert.AreEqual(expected, result.Nvalue!.Value, 1e-9, $"Failed for {name} with null values");
        }

        // Single argument functions with null => 0
        TestFuncWithNull("abs", 1, 0.0);
        TestFuncWithNull("sqrt", 1, 0.0);

        // Two argument functions with null => 0
        TestFuncWithNull("pow", 2, 1.0); // 0^0 = 1

        // Variadic functions with null
        TestFuncWithNull("min", 2, 0.0);
        TestFuncWithNull("max", 2, 0.0);
        TestFuncWithNull("avg", 1, 0.0);
        TestFuncWithNull("sum", 1, 0.0);
    }

    [TestMethod]
    public void Test_ExecuteFunction_MixedNullValues_Coverage() {
        var stack = new Stack<Value>();

        // pow with one null
        stack.Clear();
        stack.Push(new Value { Nvalue = null, DataType = DataType.Number }); // exponent = 0
        stack.Push(new Value { Nvalue = 5.0, DataType = DataType.Number });
        var result = arithFuncs.ExecuteFunction("pow", stack.ToArray());
        Assert.AreEqual(1.0, result.Nvalue!.Value, 1e-9); // 5^0 = 1

        stack.Clear();
        stack.Push(new Value { Nvalue = 3.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = null, DataType = DataType.Number }); // base = 0
        result = arithFuncs.ExecuteFunction("pow", stack.ToArray());
        Assert.AreEqual(0.0, result.Nvalue!.Value, 1e-9); // 0^3 = 0

        // min/max with mixed null
        stack.Clear();
        stack.Push(new Value { Nvalue = null, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 5.0, DataType = DataType.Number });
        result = arithFuncs.ExecuteFunction("min", stack.ToArray());
        Assert.AreEqual(0.0, result.Nvalue!.Value);

        stack.Clear();
        stack.Push(new Value { Nvalue = null, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 5.0, DataType = DataType.Number });
        result = arithFuncs.ExecuteFunction("max", stack.ToArray());
        Assert.AreEqual(5.0, result.Nvalue!.Value);

        // avg with mixed null
        stack.Clear();
        stack.Push(new Value { Nvalue = null, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 10.0, DataType = DataType.Number });
        result = arithFuncs.ExecuteFunction("avg", stack.ToArray());
        Assert.AreEqual(5.0, result.Nvalue!.Value); // (0 + 10) / 2
    }

    [TestMethod]
    public void Test_ExecuteFunction_Fallback_Diagnostic() {
        var funcDefField = typeof(CalcArithmeticalFunctions).GetField("m_funcDef", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(funcDefField, "m_funcDef field should exist");

        var existing = (System.Collections.Frozen.FrozenDictionary<string, CalcFunction>)funcDefField!.GetValue(arithFuncs)!;

        var updated = existing.ToDictionary();
        var dummyFunction = new CalcFunction {
            Name = "FakeFunction",
            Description = "Test function",
            Arguments = []
        };
        updated["FakeFunction"] = dummyFunction;

        funcDefField.SetValue(arithFuncs, updated.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));

        var stack = new Stack<Value>();
        AssertThrows<FunctionException>(() => arithFuncs.ExecuteFunction("FakeFunction", stack.ToArray()));
    }

    [TestMethod]
    public void Test_GetFunctions_Coverage() {
        var functions = arithFuncs.GetFunctions().ToList();
        Assert.IsNotNull(functions);
        Assert.IsTrue(functions.Any(f => f.Name == "abs"));
    }

    [TestMethod]
    public void Test_MinMax_Recursion_DeepStack() {
        var stack = new Stack<Value>();

        // min with 5 elements
        stack.Push(new Value { Nvalue = 10.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 3.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 7.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 1.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 5.0, DataType = DataType.Number });
        var result = arithFuncs.ExecuteFunction("min", stack.ToArray());
        Assert.AreEqual(1.0, result.Nvalue!.Value);

        // max with 5 elements
        stack.Clear();
        stack.Push(new Value { Nvalue = 10.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 3.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 7.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 15.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 5.0, DataType = DataType.Number });
        result = arithFuncs.ExecuteFunction("max", stack.ToArray());
        Assert.AreEqual(15.0, result.Nvalue!.Value);
    }

    [TestMethod]
    public void Test_Avg_MultipleElements() {
        var stack = new Stack<Value>();

        // avg with 5 elements
        stack.Push(new Value { Nvalue = 10.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 20.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 30.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 40.0, DataType = DataType.Number });
        stack.Push(new Value { Nvalue = 50.0, DataType = DataType.Number });
        var result = arithFuncs.ExecuteFunction("avg", stack.ToArray());
        Assert.AreEqual(30.0, result.Nvalue!.Value);
    }
}
