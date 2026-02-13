using ShYCalculator.Classes;
using ShYCalculator.Functions;
using ShYCalculator.Functions.Logical;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Linq;

namespace ShYCalculator.Test.Functions.Internal;

[TestClass]
public class UT_CalcLogicalFunctions_Internal {
    private CalcLogicalFunctions logicalFuncs = null!;

    [TestInitialize]
    public void Setup() {
        logicalFuncs = new CalcLogicalFunctions();
    }

    // Helper to create Value span from booleans
    private static ReadOnlySpan<Value> Vars(params bool[] bools) {
#pragma warning disable IDE0305 // Simplify collection initialization
        return bools.Select(b => new Value(DataType.Boolean, bValue: b)).ToArray().AsSpan();
#pragma warning restore IDE0305 // Simplify collection initialization
    }

    [TestMethod]
    public void Internal_Any_Coverage() {
        // any(true, false) -> true
        Assert.IsTrue(CalcLogicalFunctions.Any(Vars(true, false)).Bvalue!.Value);
        // any(false, false) -> false
        Assert.IsFalse(CalcLogicalFunctions.Any(Vars(false, false)).Bvalue!.Value);
        // any(true) -> true
        Assert.IsTrue(CalcLogicalFunctions.Any(Vars(true)).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_All_Coverage() {
        // all(true, true) -> true
        Assert.IsTrue(CalcLogicalFunctions.All(Vars(true, true)).Bvalue!.Value);
        // all(true, false) -> false
        Assert.IsFalse(CalcLogicalFunctions.All(Vars(true, false)).Bvalue!.Value);
        // all(false) -> false
        Assert.IsFalse(CalcLogicalFunctions.All(Vars(false)).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_Not_Coverage() {
        // not(true) -> false
        Assert.IsFalse(CalcLogicalFunctions.Not(Vars(true)).Bvalue!.Value);
        // not(false) -> true
        Assert.IsTrue(CalcLogicalFunctions.Not(Vars(false)).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_Logical_EdgeCases() {
        // any() -> false (safe default)
        Assert.IsFalse(CalcLogicalFunctions.Any(Vars()).Bvalue!.Value);

        // all() -> true (vacuous truth)
        Assert.IsTrue(CalcLogicalFunctions.All(Vars()).Bvalue!.Value);
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
    public void Internal_ExecuteFunction_Unknown_Throws() {
        AssertThrows<FunctionException>(() => logicalFuncs.ExecuteFunction("UnknownFunc", []));
    }

    [TestMethod]
    public void Internal_ExecuteFunction_Fallback_Coverage() {
        // Use reflection to add a dummy entry to m_funcDef to test fallback logic if any (though currently it throws)
        // Even if we fake the definition, if dispatch misses it, it should throw or handle it.
        // The original test added "DummyFunc" to m_funcDef but not m_dispatch

        var field = typeof(CalcLogicalFunctions).GetField("m_funcDef", BindingFlags.NonPublic | BindingFlags.Instance);
        var existing = (System.Collections.Frozen.FrozenDictionary<string, CalcFunction>)field!.GetValue(logicalFuncs)!;

        var updated = existing.ToDictionary(StringComparer.OrdinalIgnoreCase);
        updated["DummyFunc"] = new CalcFunction { Name = "DummyFunc" };

        field.SetValue(logicalFuncs, updated.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));

        // This should hit the code path where funcDef exists but dispatch does not
        AssertThrows<FunctionException>(() => logicalFuncs.ExecuteFunction("DummyFunc", []));
    }
}
