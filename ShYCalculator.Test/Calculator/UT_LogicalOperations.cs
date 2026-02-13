using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Calculator.Operations;
using ShYCalculator.Classes;

namespace ShYCalculator.Test.Calculator;

[TestClass]
public class UT_LogicalOperations {
    private static void AssertThrows<T>(Action action) where T : Exception {
        try {
            action();
        }
        catch (T) {
            return;
        }
        catch (Exception ex) {
            throw new AssertFailedException($"Expected exception of type {typeof(T).Name} but got {ex.GetType().Name}. Message: {ex.Message}");
        }
        throw new AssertFailedException($"Expected exception of type {typeof(T).Name} but no exception was thrown");
    }

    #region PerformLogicalOperationOnBooleans Tests

    [TestMethod]
    public void Test_LogicalBooleans_And_TrueTrue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "&&", Type = TokenType.Operator, Index = 5, OperatorKind = OperatorKind.And };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
        Assert.AreEqual(DataType.Boolean, result.Value.DataType);
    }

    [TestMethod]
    public void Test_LogicalBooleans_And_TrueFalse() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token { Key = "&&", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.And };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_LogicalBooleans_And_FalseTrue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = false };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "&&", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.And };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_LogicalBooleans_And_FalseFalse() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = false };
        var right = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token { Key = "&&", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.And };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_LogicalBooleans_Or_TrueTrue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "||", Type = TokenType.Operator, Index = 4, OperatorKind = OperatorKind.Or };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_LogicalBooleans_Or_TrueFalse() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token { Key = "||", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Or };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_LogicalBooleans_Or_FalseTrue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = false };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "||", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Or };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_LogicalBooleans_Or_FalseFalse() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = false };
        var right = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token { Key = "||", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Or };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_LogicalBooleans_LeftDataTypeMismatch() {
        var left = new Value { DataType = DataType.Number, Nvalue = 42 };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "&&", Type = TokenType.Operator, Index = 5, OperatorKind = OperatorKind.And };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Boolean data type", result.Message);
        Assert.Contains("Left operand data type Number mismatch", result.Message);
    }

    [TestMethod]
    public void Test_LogicalBooleans_RightDataTypeMismatch() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.String, Svalue = "test" };
        var token = new Token { Key = "||", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Or };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Boolean data type", result.Message);
        Assert.Contains("Right operand data type String mismatch", result.Message);
    }

    [TestMethod]
    public void Test_LogicalBooleans_BothDataTypeMismatch() {
        var left = new Value { DataType = DataType.Number, Nvalue = 1 };
        var right = new Value { DataType = DataType.String, Svalue = "test" };
        var token = new Token { Key = "&&", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.And };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Boolean data type", result.Message);
    }

    [TestMethod]
    public void Test_LogicalBooleans_LeftNullValue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = null };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "&&", Type = TokenType.Operator, Index = 4, OperatorKind = OperatorKind.And };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_LogicalBooleans_RightNullValue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = false };
        var right = new Value { DataType = DataType.Boolean, Bvalue = null };
        var token = new Token { Key = "||", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Or };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_LogicalBooleans_BothNullValues() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = null };
        var right = new Value { DataType = DataType.Boolean, Bvalue = null };
        var token = new Token { Key = "&&", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.And };

        var result = LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_LogicalBooleans_UnexpectedOperator() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Eq };

        AssertThrows<Exception>(() => {
            LogicalOperations.PerformLogicalOperationOnBooleans(left, right, token);
        });
    }

    #endregion
}



