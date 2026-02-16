using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Classes;
using System.Text.Json;

namespace ShYCalculator.Test;

[TestClass]
public class UT_AstExport {
    private ShYCalculator m_calculator = new();

    [TestInitialize]
    public void Init() {
        m_calculator = new ShYCalculator();
    }

    [TestMethod]
    public void Test_SimpleBinaryExpression() {
        var result = m_calculator.GetAst("1 + 2");
        Assert.IsTrue(result.Success);

        var node = result.Value;

        if (node == null) {
            Assert.Fail("Node should not be null");
            return; // To satisfy the compiler
        }

        Assert.AreEqual("binary", node.Type);
        Assert.AreEqual("+", node.Operator);
        Assert.AreEqual(3.0, (double)node.EvaluatedValue!);

        Assert.AreEqual("number", node.Left!.Type);
        Assert.AreEqual(1.0, (double)node.Left.EvaluatedValue!);

        Assert.AreEqual("number", node.Right!.Type);
        Assert.AreEqual(2.0, (double)node.Right.EvaluatedValue!);
    }

    [TestMethod]
    public void Test_FunctionCall() {
        var result = m_calculator.GetAst("max(10, 20)");
        Assert.IsTrue(result.Success);

        var node = result.Value;

        if (node == null) {
            Assert.Fail("Node should not be null");
            return; // To satisfy the compiler
        }

        Assert.AreEqual("function", node.Type);
        Assert.AreEqual("max", node.Name);
        Assert.AreEqual(20.0, (double)node.EvaluatedValue!);

        if (node.Arguments == null) {
            Assert.Fail("Arguments should not be null");
            return; // To satisfy the compiler
        }

        Assert.HasCount(2, node.Arguments);
        Assert.AreEqual(10.0, (double)node.Arguments[0].EvaluatedValue!);
        Assert.AreEqual(20.0, (double)node.Arguments[1].EvaluatedValue!);
    }

    [TestMethod]
    public void Test_Ranges() {
        // "10 + 20"
        //  0123456
        var result = m_calculator.GetAst("10 + 20");
        Assert.IsTrue(result.Success);

        var node = result.Value;

        if (node == null) {
            Assert.Fail("Node should not be null");
            return; // To satisfy the compiler
        }

        // Range for binary op spans from start of left to end of right
        Assert.AreEqual(0, node.Range.Start);
        Assert.AreEqual(7, node.Range.End);

        Assert.AreEqual(0, node.Left!.Range.Start);
        Assert.AreEqual(2, node.Left.Range.End); // "10" length 2

        Assert.AreEqual(5, node.Right!.Range.Start); // " 20" index 5
        Assert.AreEqual(7, node.Right.Range.End);
    }

    [TestMethod]
    public void Test_Ternary() {
        var result = m_calculator.GetAst("10 > 5 ? 100 : 200");
        Assert.IsTrue(result.Success);

        var node = result.Value;

        if (node == null) {
            Assert.Fail("Node should not be null");
            return; // To satisfy the compiler
        }

        Assert.AreEqual("ternary", node.Type);
        Assert.AreEqual(100.0, (double)node.EvaluatedValue!);

        Assert.IsNotNull(node.Condition);
        Assert.IsTrue((bool)node.Condition.EvaluatedValue!);

        Assert.IsNotNull(node.TrueBranch);
        Assert.AreEqual(100.0, (double)node.TrueBranch.EvaluatedValue!);

        Assert.IsNotNull(node.FalseBranch);
        Assert.AreEqual(200.0, (double)node.FalseBranch.EvaluatedValue!);
    }

    [TestMethod]
    public void Test_JsonSerialization() {
        var result = m_calculator.GetAst("1 + 2");
        Assert.IsTrue(result.Success);

        var json = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions { WriteIndented = true });
        // Just checking it doesn't throw and produces something
        Assert.Contains("\"type\": \"binary\"", json);
        Assert.Contains("\"evaluated_value\": 3", json);
    }
}
