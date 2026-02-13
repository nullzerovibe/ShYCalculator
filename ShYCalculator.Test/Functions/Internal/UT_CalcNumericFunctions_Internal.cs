using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ShYCalculator.Test.Functions.Internal;

[TestClass]
public class UT_CalcNumericFunctions_Internal {
    // Helper to create Value span
    private static ReadOnlySpan<Value> Vars(params double[] nums) {
        return nums.Select(n => new Value(DataType.Number, nValue: n)).ToArray();
    }

    private static ReadOnlySpan<Value> VarsMixed(params object?[] items) {
        var list = new Value[items.Length];
        for (int i = 0; i < items.Length; i++) {
            if (items[i] is double d) list[i] = new Value(DataType.Number, nValue: d);
            else if (items[i] is string s) list[i] = new Value(DataType.String, sValue: s);
            else if (items[i] is bool b) list[i] = new Value(DataType.Boolean, bValue: b);
            else list[i] = new Value(DataType.Number, nValue: null); // Null/Empty
        }
        return list.AsSpan();
    }

    [TestMethod]
    public void Internal_AllNumericValues_Coverage() {
        // Valid
        Assert.IsTrue(CalcNumericFunctions.AllNumericValues(Vars(1, 2, 3)).Bvalue!.Value);
        Assert.IsTrue(CalcNumericFunctions.AllNumericValues(Vars()).Bvalue!.Value); // Empty is considered valid (trivial truth) or handled elsewhere? Logic says loop 0 times returns true.

        // Invalid
        Assert.IsFalse(CalcNumericFunctions.AllNumericValues(VarsMixed(1, "a", 2)).Bvalue!.Value);
        Assert.IsFalse(CalcNumericFunctions.AllNumericValues(VarsMixed(null, 1)).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_IsSameDouble_Coverage() {
        // Exact
        Assert.IsTrue(CalcNumericFunctions.IsSameDouble(1.0, 1.0));
        Assert.IsTrue(CalcNumericFunctions.IsSameDouble(0.0, 0.0));

        // Epsilon
        Assert.IsTrue(CalcNumericFunctions.IsSameDouble(1.0, 1.00000001, 7));
        Assert.IsFalse(CalcNumericFunctions.IsSameDouble(1.0, 1.0000002, 7)); // Threshold check

        // Zero handling
        Assert.IsTrue(CalcNumericFunctions.IsSameDouble(0.0, 1e-10, 7));
        Assert.IsFalse(CalcNumericFunctions.IsSameDouble(0.0, 0.1));

        // Signs
        Assert.IsFalse(CalcNumericFunctions.IsSameDouble(1.0, -1.0));

        // Large numbers
        Assert.IsTrue(CalcNumericFunctions.IsSameDouble(1e20, 1e20));
        Assert.IsTrue(CalcNumericFunctions.IsSameDouble(1e20, 1e20 + 1)); // Relatively small diff
    }

    [TestMethod]
    public void Internal_CompareNumbersOrder_Ascending() {
        // Strict Ascending
        Assert.IsTrue(CalcNumericFunctions.CompareNumbersOrder(Vars(1, 2, 3), CalcNumericFunctions.NumberCompareMode.Ascending, true).Bvalue!.Value);
        Assert.IsTrue(CalcNumericFunctions.CompareNumbersOrder(Vars(-10, 0, 10), CalcNumericFunctions.NumberCompareMode.Ascending, true).Bvalue!.Value);

        // Not Ascending
        Assert.IsFalse(CalcNumericFunctions.CompareNumbersOrder(Vars(1, 3, 2), CalcNumericFunctions.NumberCompareMode.Ascending, true).Bvalue!.Value);

        // Duplicates in Strict -> False
        Assert.IsFalse(CalcNumericFunctions.CompareNumbersOrder(Vars(1, 2, 2, 3), CalcNumericFunctions.NumberCompareMode.Ascending, true).Bvalue!.Value);

        // Loose Ascending -> True with duplicates
        Assert.IsTrue(CalcNumericFunctions.CompareNumbersOrder(Vars(1, 2, 2, 3), CalcNumericFunctions.NumberCompareMode.Ascending, false).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_CompareNumbersOrder_Descending() {
        // Strict Descending
        Assert.IsTrue(CalcNumericFunctions.CompareNumbersOrder(Vars(3, 2, 1), CalcNumericFunctions.NumberCompareMode.Descending, true).Bvalue!.Value);

        // Not Descending
        Assert.IsFalse(CalcNumericFunctions.CompareNumbersOrder(Vars(3, 1, 2), CalcNumericFunctions.NumberCompareMode.Descending, true).Bvalue!.Value);

        // Duplicates in Strict -> False
        Assert.IsFalse(CalcNumericFunctions.CompareNumbersOrder(Vars(3, 2, 2, 1), CalcNumericFunctions.NumberCompareMode.Descending, true).Bvalue!.Value);

        // Loose Descending -> True with duplicates
        Assert.IsTrue(CalcNumericFunctions.CompareNumbersOrder(Vars(3, 2, 2, 1), CalcNumericFunctions.NumberCompareMode.Descending, false).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_CompareNumbersOrder_EdgeCases() {
        // Single element -> False (need at least 2 to compare order in this implementation?)
        // Source Check: if (parameters.Length < 2) return false;
        Assert.IsFalse(CalcNumericFunctions.CompareNumbersOrder(Vars(1)).Bvalue!.Value);

        // Empty
        Assert.IsFalse(CalcNumericFunctions.CompareNumbersOrder(Vars()).Bvalue!.Value);

        // Invalid types -> False (AllNumericValues check)
        Assert.IsFalse(CalcNumericFunctions.CompareNumbersOrder(VarsMixed(1, "a")).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_AnyEqualAdjacentNumber_Coverage() {
        Assert.IsTrue(CalcNumericFunctions.AnyEqualAdjacentNumber(Vars(1, 2, 2, 3)).Bvalue!.Value);
        Assert.IsFalse(CalcNumericFunctions.AnyEqualAdjacentNumber(Vars(1, 2, 3, 4)).Bvalue!.Value);

        // Tolerance tests hard to reach properly via public API sometimes, easier here logic-wise
        // But logic hardcodes tolerance=0 for HasAdjacentEquals. The generic method supports it.
        // Let's test basic logic.

        // Triplets
        Assert.IsTrue(CalcNumericFunctions.AnyEqualAdjacentNumber(Vars(1, 1, 1)).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_Stats_Median() {
        // Odd count
        var res = CalcNumericFunctions.Median(Vars(10, 2, 5)); // Sorted: 2, 5, 10
        Assert.AreEqual(5.0, res.Nvalue!.Value);

        // Even count
        res = CalcNumericFunctions.Median(Vars(1, 2, 3, 4)); // 2, 3 -> 2.5
        Assert.AreEqual(2.5, res.Nvalue!.Value);

        // Single
        Assert.AreEqual(10.0, CalcNumericFunctions.Median(Vars(10)).Nvalue!.Value);

        // Empty
        Assert.AreEqual(0.0, CalcNumericFunctions.Median(Vars()).Nvalue!.Value);
    }

    [TestMethod]
    public void Internal_Stats_Mode() {
        Assert.AreEqual(2.0, CalcNumericFunctions.Mode(Vars(1, 2, 2, 3)).Nvalue!.Value);
        Assert.AreEqual(1.0, CalcNumericFunctions.Mode(Vars(1, 1, 2, 2)).Nvalue!.Value); // Smallest key if counts equal? 
                                                                                         // Logic: OrderByDescending(Count).ThenBy(Key). 
                                                                                         // 1 (count 2), 2 (count 2). Key 1 < 2. So 1.

        Assert.AreEqual(5.0, CalcNumericFunctions.Mode(Vars(5)).Nvalue!.Value);
    }

    [TestMethod]
    public void Internal_Stats_StDev_Var() {
        // Population vs Sample? Code: sum / (count - 1). Sample StDev.
        var data = Vars(2, 4, 4, 4, 5, 5, 7, 9);
        // Mean = 5.
        // Diffs: -3, -1, -1, -1, 0, 0, 2, 4
        // Sq: 9, 1, 1, 1, 0, 0, 4, 16 = 32
        // Var = 32 / (8-1) = 32/7 = 4.5714...
        // StDev = Sqrt(Var) = 2.138...

        var varVal = CalcNumericFunctions.Var(data).Nvalue!.Value;
        var stdevVal = CalcNumericFunctions.StDev(data).Nvalue!.Value;

        Assert.AreEqual(32.0 / 7.0, varVal, 1e-6);
        Assert.AreEqual(Math.Sqrt(32.0 / 7.0), stdevVal, 1e-6);

        // < 2 items
        Assert.AreEqual(0.0, CalcNumericFunctions.Var(Vars(1)).Nvalue!.Value);
    }

    [TestMethod]
    public void Internal_Stats_Empty_Coverage() {
        // Mode - Empty
        Assert.AreEqual(0.0, CalcNumericFunctions.Mode(Vars()).Nvalue!.Value);

        // StDev - Empty
        Assert.AreEqual(0.0, CalcNumericFunctions.StDev(Vars()).Nvalue!.Value);

        // Var - Empty
        Assert.AreEqual(0.0, CalcNumericFunctions.Var(Vars()).Nvalue!.Value);

        // Count - Empty
        Assert.AreEqual(0.0, CalcNumericFunctions.Count(Vars()).Nvalue!.Value);
    }

    [TestMethod]
    public void Internal_Dispatch_Coverage() {
        // Triggers constructor and CreateDispatch
        var ext = new CalcNumericFunctions();

        // ExecuteFunction hits m_dispatch lambdas
        // 1. IsAscending
        var res = ext.ExecuteFunction("is_ascending", Vars(1, 2));
        Assert.IsTrue(res.Bvalue!.Value);

        // 2. IsDesc
        res = ext.ExecuteFunction("is_descending", Vars(2, 1));
        Assert.IsTrue(res.Bvalue!.Value);

        // 3. IsAscLoose
        res = ext.ExecuteFunction("is_ascending_loose", Vars(1, 1));
        Assert.IsTrue(res.Bvalue!.Value);

        // 4. IsDescLoose
        res = ext.ExecuteFunction("is_descending_loose", Vars(1, 1));
        Assert.IsTrue(res.Bvalue!.Value);

        // 5. HasAdjacentEquals
        res = ext.ExecuteFunction("has_adjacent_equals", Vars(1, 1));
        Assert.IsTrue(res.Bvalue!.Value);

        // 6. AreAllNumbers
        res = ext.ExecuteFunction("are_all_numbers", Vars(1, 2));
        Assert.IsTrue(res.Bvalue!.Value);

        // 7. Median
        res = ext.ExecuteFunction("median", Vars(1, 2, 3));
        Assert.AreEqual(2.0, res.Nvalue!.Value);

        // 8. Mode
        res = ext.ExecuteFunction("mode", Vars(1, 1, 2));
        Assert.AreEqual(1.0, res.Nvalue!.Value);

        // 9. StDev
        res = ext.ExecuteFunction("stdev", Vars(1, 2, 3));
        Assert.AreEqual(1.0, res.Nvalue!.Value);

        // 10. Var
        res = ext.ExecuteFunction("var", Vars(1, 2, 3));
        Assert.AreEqual(1.0, res.Nvalue!.Value);

        // 11. Count
        res = ext.ExecuteFunction("count", Vars(1, 2));
        Assert.AreEqual(2.0, res.Nvalue!.Value);
    }
}
