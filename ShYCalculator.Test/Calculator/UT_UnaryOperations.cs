using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Calculator.Operations;
using ShYCalculator.Classes;

namespace ShYCalculator.Test.Calculator;

[TestClass]
public class UT_UnaryOperations {

    [TestMethod]
    public void Test_LogicalNot_True() {
        var operand = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "!", Type = TokenType.UnaryPrefixOperator, OperatorKind = OperatorKind.Not };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_LogicalNot_False() {
        var operand = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token { Key = "!", Type = TokenType.UnaryPrefixOperator, OperatorKind = OperatorKind.Not };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_Factorial() {
        var operand = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "!", Type = TokenType.UnaryPostfixOperator, OperatorKind = OperatorKind.Factorial };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(120.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_Factorial_Zero() {
        var operand = new Value { DataType = DataType.Number, Nvalue = 0 };
        var token = new Token { Key = "!", Type = TokenType.UnaryPostfixOperator, OperatorKind = OperatorKind.Factorial };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_BitwiseNot() {
        var operand = new Value { DataType = DataType.Number, Nvalue = 5 }; // 0...0101
        var token = new Token { Key = "~", Type = TokenType.UnaryPrefixOperator, OperatorKind = OperatorKind.BitwiseNot };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(~5, (int)result.Value.Nvalue!);
    }

    [TestMethod]
    public void Test_SquareRoot() {
        var operand = new Value { DataType = DataType.Number, Nvalue = 16 };
        var token = new Token { Key = "√", Type = TokenType.UnaryPrefixOperator, OperatorKind = OperatorKind.SquareRoot };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(4.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_SquareRoot_Negative_Fail() {
        var operand = new Value { DataType = DataType.Number, Nvalue = -1 };
        var token = new Token { Key = "√", Type = TokenType.UnaryPrefixOperator, OperatorKind = OperatorKind.SquareRoot };

        var result = UnaryOperations.PerformUnaryOperation(operand, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("non-negative", result.Message);
    }
}
