using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Calculator.Operations;
using ShYCalculator.Classes;

namespace ShYCalculator.Test.Calculator;

[TestClass]
public class UT_BitwiseOperations {
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

    #region PerformBitwiseOperationOnNumbers Tests

    [TestMethod]
    public void Test_BitwiseNumbers_And() {
        var left = new Value { DataType = DataType.Number, Nvalue = 12 }; // 1100 in binary
        var right = new Value { DataType = DataType.Number, Nvalue = 10 }; // 1010 in binary
        var token = new Token { Key = "&", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.BitwiseAnd };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(8.0, result.Value.Nvalue); // 1000 in binary = 8
        Assert.AreEqual(DataType.Number, result.Value.DataType);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_Or() {
        var left = new Value { DataType = DataType.Number, Nvalue = 12 }; // 1100 in binary
        var right = new Value { DataType = DataType.Number, Nvalue = 10 }; // 1010 in binary
        var token = new Token { Key = "|", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.BitwiseOr };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(14.0, result.Value.Nvalue); // 1110 in binary = 14
    }

    [TestMethod]
    public void Test_BitwiseNumbers_Xor() {
        var left = new Value { DataType = DataType.Number, Nvalue = 12 }; // 1100 in binary
        var right = new Value { DataType = DataType.Number, Nvalue = 10 }; // 1010 in binary
        var token = new Token { Key = "⊕", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.BitwiseXor };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(6.0, result.Value.Nvalue); // 0110 in binary = 6
    }

    [TestMethod]
    public void Test_BitwiseNumbers_Xor_AsciiAlias() {
        // Tests the ^^ ASCII alias for XOR (same as ⊕)
        var left = new Value { DataType = DataType.Number, Nvalue = 12 }; // 1100 in binary
        var right = new Value { DataType = DataType.Number, Nvalue = 10 }; // 1010 in binary
        var token = new Token { Key = "^^", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.BitwiseXor };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(6.0, result.Value.Nvalue); // 0110 in binary = 6
    }

    [TestMethod]
    public void Test_BitwiseNumbers_WithZero() {
        var left = new Value { DataType = DataType.Number, Nvalue = 0 };
        var right = new Value { DataType = DataType.Number, Nvalue = 15 };
        var token = new Token { Key = "&", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.BitwiseAnd };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_NegativeNumbers() {
        var left = new Value { DataType = DataType.Number, Nvalue = -5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 3 };
        var token = new Token { Key = "&", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.BitwiseAnd };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        // -5 & 3 in two's complement
        Assert.AreEqual(3.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_LeftDataTypeMismatch() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "&", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.BitwiseAnd };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Number data type", result.Message);
        Assert.Contains("Left operand data type String mismatch", result.Message);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_RightDataTypeMismatch() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "|", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.BitwiseOr };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Number data type", result.Message);
        Assert.Contains("Right operand data type Boolean mismatch", result.Message);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_LeftNullValue() {
        var left = new Value { DataType = DataType.Number, Nvalue = null };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "&", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.BitwiseAnd };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_RightNullValue() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = null };
        var token = new Token { Key = "|", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.BitwiseOr };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_BothNullValues() {
        var left = new Value { DataType = DataType.Number, Nvalue = null };
        var right = new Value { DataType = DataType.Number, Nvalue = null };
        var token = new Token { Key = "&", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.BitwiseAnd };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_LeftNotInteger() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5.5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 3 };
        var token = new Token { Key = "&", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.BitwiseAnd };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("BitwiseOperation require integer numbers", result.Message);
        Assert.Contains("position 2", result.Message);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_RightNotInteger() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 3.7 };
        var token = new Token { Key = "|", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.BitwiseOr };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("BitwiseOperation require integer numbers", result.Message);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_BothNotInteger() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5.2 };
        var right = new Value { DataType = DataType.Number, Nvalue = 3.8 };
        var token = new Token { Key = "⊕", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.BitwiseXor };

        var result = BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("BitwiseOperation require integer numbers", result.Message);
    }

    [TestMethod]
    public void Test_BitwiseNumbers_UnexpectedOperator() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 3 };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Add };

        AssertThrows<Exception>(() => {
            BitwiseOperations.PerformBitwiseOperationOnNumbers(left, right, token);
        });
    }

    #endregion
}



