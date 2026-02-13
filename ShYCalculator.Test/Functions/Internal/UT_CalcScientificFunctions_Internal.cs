using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ShYCalculator.Test.Functions.Internal;

[TestClass]
public class UT_CalcScientificFunctions_Internal {
    private CalcScientificFunctions m_funcs = null!;

    [TestInitialize]
    public void Setup() {
        m_funcs = new CalcScientificFunctions();
    }

    [TestMethod]
    public void Internal_Instance_Cot_Coverage() {
        // Cot(x) = 1/Tan(x)
        Assert.AreEqual(1.0, m_funcs.Cot(Math.PI / 4), 1e-9);
        Assert.AreEqual(0.0, m_funcs.Cot(Math.PI / 2), 1e-9); // 1 / Tan(PI/2) -> 1 / Infinity = 0? 
                                                              // Tan(PI/2) is huge number in double usually, not Infinity due to precision.
                                                              // Let's check Math.Tan(Math.PI/2) -> 1.633123935319537E+16
                                                              // 1 / that is close to 0.

        // Cot(0) = 1/0 = Infinity
        Assert.AreEqual(double.PositiveInfinity, m_funcs.Cot(0));

        // Error handling
        try { m_funcs.Cot(double.NaN); Assert.Fail(); } catch (FunctionException) { }
    }

    [TestMethod]
    public void Internal_Instance_Coth_Coverage() {
        // Coth(x) = Cosh(x)/Sinh(x)
        // Coth(1) ~ 1.313
        Assert.AreEqual(1.3130352855, m_funcs.Coth(1), 1e-6);

        // Coth(0) = 1/0 = Infinity
        Assert.AreEqual(double.PositiveInfinity, m_funcs.Coth(0));

        try { m_funcs.Coth(double.NaN); Assert.Fail(); } catch (FunctionException) { }
    }

    [TestMethod]
    public void Internal_Instance_Acot_Coverage() {
        // Acot(x) = Atan(1/x)
        // Acot(1) = PI/4
        Assert.AreEqual(Math.PI / 4, m_funcs.Acot(1), 1e-9);

        // Acot(0) = Atan(Infinity) = PI/2
        Assert.AreEqual(Math.PI / 2, m_funcs.Acot(0), 1e-9);

        try { m_funcs.Acot(double.NaN); Assert.Fail(); } catch (FunctionException) { }
    }

    [TestMethod]
    public void Internal_ExecuteFunction_NaN_Propagation() {
        // Check if other standard maps pass NaN or throw/handle differently
        var stack = new Stack<Value>();
        stack.Push(new Value { Nvalue = double.NaN, DataType = DataType.Number });

        // Sin(NaN) -> NaN
        var res = m_funcs.ExecuteFunction("sin", stack.ToArray());
        Assert.IsTrue(double.IsNaN(res.Nvalue!.Value));

        // Log(NaN) -> NaN
        stack.Clear();
        stack.Push(new Value { Nvalue = double.NaN, DataType = DataType.Number });
        res = m_funcs.ExecuteFunction("ln", stack.ToArray());
        Assert.IsTrue(double.IsNaN(res.Nvalue!.Value));
    }

    [TestMethod]
    public void Internal_ExecuteFunction_Infinity_Coverage() {
        var stack = new Stack<Value>();
        stack.Push(new Value { Nvalue = double.PositiveInfinity, DataType = DataType.Number });

        // Atan(Inf) -> PI/2
        var res = m_funcs.ExecuteFunction("atan", stack.ToArray());
        Assert.AreEqual(Math.PI / 2, res.Nvalue!.Value, 1e-9);

        // Tanh(Inf) -> 1
        stack.Clear();
        stack.Push(new Value { Nvalue = double.PositiveInfinity, DataType = DataType.Number });
        res = m_funcs.ExecuteFunction("tanh", stack.ToArray());
        Assert.AreEqual(1.0, res.Nvalue!.Value, 1e-9);
    }
}
