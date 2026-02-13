using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Functions;
using System.Collections.Generic;

namespace ShYCalculator.Test.Functions.Internal;

[TestClass]
public class UT_CalcFunction_Coverage {
    [TestMethod]
    public void CalcFunctionArgument_PropertyCoverage() {
        var arg = new CalcFunctionArgument {
            // Type
            Type = "Number"
        };
        Assert.AreEqual("Number", arg.Type);

        // Description
        arg.Description = "A number";
        Assert.AreEqual("A number", arg.Description);

        // Min
        arg.Min = 1;
        Assert.AreEqual(1, arg.Min);

        // Max
        arg.Max = 10;
        Assert.AreEqual(10, arg.Max);

        // Optional
        arg.Optional = true;
        Assert.IsTrue(arg.Optional);

        // Arguments (Nested)
        var nested = new List<CalcFunctionArgument>();
        arg.Arguments = nested;
        Assert.AreSame(nested, arg.Arguments);

        // ToString coverage
        var str = arg.ToString();
        Assert.Contains("Type: Number", str);
        Assert.Contains("Optional: True", str);
    }

    [TestMethod]
    public void CalcFunction_PropertyCoverage() {
        var func = new CalcFunction {
            Name = "TestFunc"
        };

        Assert.AreEqual("TestFunc", func.Name);

        // Description
        func.Description = "Test description";
        Assert.AreEqual("Test description", func.Description);

        // Examples
        var examples = new List<string> { "ex1", "ex2" };
        func.Examples = examples;
        Assert.AreSame(examples, func.Examples);

        // Arguments
        var args = new List<CalcFunctionArgument>();
        func.Arguments = args;
        Assert.AreSame(args, func.Arguments);

        // ToString coverage
        var str = func.ToString();
        Assert.Contains("Function: TestFunc", str);

        // Unnamed ToString
        var func2 = new CalcFunction();
        Assert.Contains("Unnamed", func2.ToString());
    }
}
