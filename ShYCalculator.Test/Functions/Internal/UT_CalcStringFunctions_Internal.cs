using ShYCalculator.Classes;
using ShYCalculator.Functions.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ShYCalculator.Test.Functions.Internal;

[TestClass]
public class UT_CalcStringFunctions_Internal {
    // Helper to create Value span
    private static ReadOnlySpan<Value> Vars(params string[] strings) {
        return strings.Select(s => new Value(DataType.String, sValue: s)).ToArray();
    }

    private static ReadOnlySpan<Value> VarsMixed(params object[] items) {
        var list = new Value[items.Length];
        for (int i = 0; i < items.Length; i++) {
            if (items[i] is string s) list[i] = new Value(DataType.String, sValue: s);
            else if (items[i] is bool b) list[i] = new Value(DataType.Boolean, bValue: b);
            else list[i] = new Value(DataType.Number, nValue: null);
        }
        return list.AsSpan();
    }

    [TestMethod]
    public void Internal_AllStringValues_Coverage() {
        Assert.IsTrue(CalcStringFunctions.AllStringValues(Vars("a", "b")).Bvalue!.Value);
        Assert.IsTrue(CalcStringFunctions.AllStringValues(Vars("")).Bvalue!.Value); // Empty string is valid string

        // Null strings?
        var nullStrVars = new Value[] { new(DataType.String, sValue: null) };
        Assert.IsFalse(CalcStringFunctions.AllStringValues(nullStrVars).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_Contains_Coverage() {
        // Contains
        Assert.IsTrue(CalcStringFunctions.Contains(Vars("hello world", "world"), CalcStringFunctions.StringContainsMode.Contains).Bvalue!.Value);
        Assert.IsFalse(CalcStringFunctions.Contains(Vars("hello world", "bye"), CalcStringFunctions.StringContainsMode.Contains).Bvalue!.Value);

        // StartsWith
        Assert.IsTrue(CalcStringFunctions.Contains(Vars("hello", "he"), CalcStringFunctions.StringContainsMode.StartsWith).Bvalue!.Value);
        Assert.IsFalse(CalcStringFunctions.Contains(Vars("hello", "lo"), CalcStringFunctions.StringContainsMode.StartsWith).Bvalue!.Value);

        // EndsWith
        Assert.IsTrue(CalcStringFunctions.Contains(Vars("hello", "lo"), CalcStringFunctions.StringContainsMode.EndsWith).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_Contains_CaseSensitivity() {
        // Default (Insensitive)
        Assert.IsTrue(CalcStringFunctions.Contains(Vars("ABC", "abc"), CalcStringFunctions.StringContainsMode.Contains).Bvalue!.Value);

        // Sensitive (True)
        Assert.IsFalse(CalcStringFunctions.Contains(VarsMixed("ABC", "abc", true), CalcStringFunctions.StringContainsMode.Contains).Bvalue!.Value);

        // Explicit Insensitive (False)
        Assert.IsTrue(CalcStringFunctions.Contains(VarsMixed("ABC", "abc", false), CalcStringFunctions.StringContainsMode.Contains).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_CompareStrings_Equal() {
        Assert.IsTrue(CalcStringFunctions.CompareStrings(Vars("a", "a"), CalcStringFunctions.StringCompareMode.Equal).Bvalue!.Value);
        Assert.IsTrue(CalcStringFunctions.CompareStrings(Vars("a", "a", "a"), CalcStringFunctions.StringCompareMode.Equal).Bvalue!.Value);

        Assert.IsFalse(CalcStringFunctions.CompareStrings(Vars("a", "b"), CalcStringFunctions.StringCompareMode.Equal).Bvalue!.Value);

        // Case sensitive
        Assert.IsFalse(CalcStringFunctions.CompareStrings(VarsMixed("A", "a", true), CalcStringFunctions.StringCompareMode.Equal).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_CompareStrings_NotEqual_AllDistinct() {
        // A != B -> True
        Assert.IsTrue(CalcStringFunctions.CompareStrings(Vars("a", "b"), CalcStringFunctions.StringCompareMode.NotEqual).Bvalue!.Value);

        // A != A -> False
        Assert.IsFalse(CalcStringFunctions.CompareStrings(Vars("a", "a"), CalcStringFunctions.StringCompareMode.NotEqual).Bvalue!.Value);

        // A, B, C -> True
        Assert.IsTrue(CalcStringFunctions.CompareStrings(Vars("a", "b", "c"), CalcStringFunctions.StringCompareMode.NotEqual).Bvalue!.Value);

        // A, B, A -> False (Logic is "All Distinct")
        Assert.IsFalse(CalcStringFunctions.CompareStrings(Vars("a", "b", "a"), CalcStringFunctions.StringCompareMode.NotEqual).Bvalue!.Value);
    }
}
