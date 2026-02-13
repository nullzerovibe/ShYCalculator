using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShYCalculator.Test.Functions.Internal;

[TestClass]
public class UT_CalcArithmeticalFunctions_Internal {
    private CalcArithmeticalFunctions m_funcs = null!;

    [TestInitialize]
    public void Setup() {
        m_funcs = new CalcArithmeticalFunctions();
    }

    // Helper to create Value span
    private static ReadOnlySpan<Value> Vars(params double[] nums) {
        return nums.Select(n => new Value(DataType.Number, nValue: n)).ToArray();
    }

    [TestMethod]
    public void Internal_Static_BasicMath_Coverage() {
        // Pow
        Assert.AreEqual(1024.0, CalcArithmeticalFunctions.Pow(Vars(2, 10)).Nvalue!.Value);
        Assert.AreEqual(0.25, CalcArithmeticalFunctions.Pow(Vars(2, -2)).Nvalue!.Value);
        Assert.AreEqual(1.0, CalcArithmeticalFunctions.Pow(Vars(100, 0)).Nvalue!.Value);

        // Sqrt
        Assert.AreEqual(3.0, CalcArithmeticalFunctions.Sqrt(Vars(9)).Nvalue!.Value);
        Assert.AreEqual(double.NaN, CalcArithmeticalFunctions.Sqrt(Vars(-1)).Nvalue!.Value); // C# Math.Sqrt returns NaN

        // Abs
        Assert.AreEqual(double.MinValue * -1, CalcArithmeticalFunctions.Abs(Vars(double.MinValue)).Nvalue!.Value); // Note: Abs(MinValue) might be problem? min value is -1.79e308. abs is valid. 
        // Wait, double.MinValue is correct. int.MinValue throws. double is fine.
        Assert.AreEqual(double.PositiveInfinity, CalcArithmeticalFunctions.Abs(Vars(double.NegativeInfinity)).Nvalue!.Value);
    }

    [TestMethod]
    public void Internal_Static_Rounding_Coverage() {
        // Round
        Assert.AreEqual(1.0, CalcArithmeticalFunctions.Round(Vars(1.2)).Nvalue!.Value);
        Assert.AreEqual(2.0, CalcArithmeticalFunctions.Round(Vars(1.5)).Nvalue!.Value); // Banker's rounding default in Math.Round? Yes. 1.5 -> 2. 0.5 -> 0.
        Assert.AreEqual(0.0, CalcArithmeticalFunctions.Round(Vars(0.5)).Nvalue!.Value);

        Assert.AreEqual(1.23, CalcArithmeticalFunctions.Round(Vars(1.23456, 2)).Nvalue!.Value);

        // Floor/Ceiling/Trunc
        Assert.AreEqual(1.0, CalcArithmeticalFunctions.Floor(Vars(1.9)).Nvalue!.Value);
        Assert.AreEqual(-2.0, CalcArithmeticalFunctions.Floor(Vars(-1.1)).Nvalue!.Value);

        Assert.AreEqual(2.0, CalcArithmeticalFunctions.Ceiling(Vars(1.1)).Nvalue!.Value);
        Assert.AreEqual(-1.0, CalcArithmeticalFunctions.Ceiling(Vars(-1.9)).Nvalue!.Value);

        Assert.AreEqual(1.0, CalcArithmeticalFunctions.Trunc(Vars(1.9)).Nvalue!.Value);
        Assert.AreEqual(-1.0, CalcArithmeticalFunctions.Trunc(Vars(-1.9)).Nvalue!.Value);
    }

    [TestMethod]
    public void Internal_Static_Random_Coverage() {
        // 0 args -> Double 0..1
        var r1 = CalcArithmeticalFunctions.Random(Vars()).Nvalue!.Value;
        Assert.IsTrue(r1 >= 0.0 && r1 < 1.0);

        // 2 args -> Int range
        var r2 = CalcArithmeticalFunctions.Random(Vars(10, 20)).Nvalue!.Value;
        Assert.IsTrue(r2 >= 10 && r2 < 20);
        Assert.AreEqual(Math.Floor(r2), r2); // Should be integer? Code casts to (int).

        // Min >= Max
        Assert.AreEqual(10.0, CalcArithmeticalFunctions.Random(Vars(10, 10)).Nvalue!.Value);
    }

    [TestMethod]
    public void Internal_Instance_Aggregate_Coverage() {
        // Min
        Assert.AreEqual(-100.0, CalcArithmeticalFunctions.Min(Vars(10, -5, -100, 20)).Nvalue!.Value);
        Assert.AreEqual(double.NegativeInfinity, CalcArithmeticalFunctions.Min(Vars(0, double.NegativeInfinity)).Nvalue!.Value);

        // Max
        Assert.AreEqual(20.0, CalcArithmeticalFunctions.Max(Vars(10, -5, -100, 20)).Nvalue!.Value);

        // Sum
        Assert.AreEqual(double.PositiveInfinity, CalcArithmeticalFunctions.Sum(Vars(double.MaxValue, double.MaxValue)).Nvalue!.Value);
        Assert.AreEqual(0.0, CalcArithmeticalFunctions.Sum(Vars()).Nvalue!.Value);

        // Avg
        Assert.AreEqual(5.0, CalcArithmeticalFunctions.Avg(Vars(0, 10)).Nvalue!.Value);
        Assert.AreEqual(double.NaN, CalcArithmeticalFunctions.Avg(Vars()).Nvalue!.Value); // 0 / 0 -> NaN? Code: total / length. 0/0.
    }

    [TestMethod]
    public void Internal_Sign_Coverage() {
        Assert.AreEqual(1.0, CalcArithmeticalFunctions.Sign(Vars(100)).Nvalue!.Value);
        Assert.AreEqual(-1.0, CalcArithmeticalFunctions.Sign(Vars(-0.5)).Nvalue!.Value);
        Assert.AreEqual(0.0, CalcArithmeticalFunctions.Sign(Vars(0)).Nvalue!.Value);

        Assert.AreEqual(1.0, CalcArithmeticalFunctions.Sign(Vars(double.PositiveInfinity)).Nvalue!.Value);
        Assert.AreEqual(-1.0, CalcArithmeticalFunctions.Sign(Vars(double.NegativeInfinity)).Nvalue!.Value);
        // NaN sign?
        try {
            // Math.Sign(double.NaN) throws ArithmeticException!
            CalcArithmeticalFunctions.Sign(Vars(double.NaN));
            Assert.Fail("Should have thrown");
        }
        catch (ArithmeticException) { }
    }
    [TestMethod]
    public void Internal_MinMax_EdgeCases() {
        // Min - Single arg (hits if (args.Length < 2))
        Assert.AreEqual(42.0, CalcArithmeticalFunctions.Min(Vars(42)).Nvalue!.Value);

        // Max - Single arg
        Assert.AreEqual(42.0, CalcArithmeticalFunctions.Max(Vars(42)).Nvalue!.Value);
    }
}
