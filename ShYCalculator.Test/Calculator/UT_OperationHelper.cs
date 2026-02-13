using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Calculator.Operations;
using ShYCalculator.Classes;

namespace ShYCalculator.Test.Calculator;

[TestClass]
public class UT_OperationHelper {
    #region GetDataTypeMismatchErrorMessage Tests

    [TestMethod]
    public void Test_GetDataTypeMismatchErrorMessage_BothMismatch_WithExpectedType() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 5 };

        var result = OperationHelper.GetDataTypeMismatchErrorMessage(left, right, token, DataType.Number);

        Assert.Contains("Unexpected operand data type", result);
        Assert.Contains("token '+'", result);
        Assert.Contains("position 5", result);
        Assert.Contains("Expected Number data type", result);
        Assert.Contains("Left operand data type String mismatch", result);
        Assert.Contains("Right operand data type Boolean mismatch", result);
    }

    [TestMethod]
    public void Test_GetDataTypeMismatchErrorMessage_LeftMismatch_WithExpectedType() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Number, Nvalue = 42 };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 3 };

        var result = OperationHelper.GetDataTypeMismatchErrorMessage(left, right, token, DataType.Number);

        Assert.Contains("Expected Number data type", result);
        Assert.Contains("Left operand data type String mismatch", result);
        Assert.DoesNotContain("Right operand data type", result);
    }

    [TestMethod]
    public void Test_GetDataTypeMismatchErrorMessage_RightMismatch_WithExpectedType() {
        var left = new Value { DataType = DataType.Number, Nvalue = 42 };
        var right = new Value { DataType = DataType.String, Svalue = "test" };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 7 };

        var result = OperationHelper.GetDataTypeMismatchErrorMessage(left, right, token, DataType.Number);

        Assert.Contains("Expected Number data type", result);
        Assert.DoesNotContain("Left operand data type", result);
        Assert.Contains("Right operand data type String mismatch", result);
    }

    [TestMethod]
    public void Test_GetDataTypeMismatchErrorMessage_NoExpectedType() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Number, Nvalue = 42 };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 10 };

        var result = OperationHelper.GetDataTypeMismatchErrorMessage(left, right, token, null);

        Assert.Contains("Unexpected operand data type", result);
        Assert.DoesNotContain("Expected", result);
        Assert.Contains("Left operand data type String mismatch", result);
        Assert.Contains("Right operand data type Number mismatch", result);
    }

    #endregion

    #region GetNullValueErrorMessage Tests

    [TestMethod]
    public void Test_GetNullValueErrorMessage_BothNull() {
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 5 };

        var result = OperationHelper.GetNullValueErrorMessage(true, true, token, DataType.Number);

        Assert.Contains("Unexpected operand value", result);
        Assert.Contains("token '+'", result);
        Assert.Contains("position 5", result);
        Assert.Contains("Expected Number value", result);
        Assert.Contains("Left operand value is NULL", result);
        Assert.Contains("Right operand value is NULL", result);
    }

    [TestMethod]
    public void Test_GetNullValueErrorMessage_LeftNull() {
        var token = new Token { Key = "*", Type = TokenType.Operator, Index = 3 };

        var result = OperationHelper.GetNullValueErrorMessage(true, false, token, DataType.String);

        Assert.Contains("Expected String value", result);
        Assert.Contains("Left operand value is NULL", result);
        Assert.DoesNotContain("Right operand value is NULL", result);
    }

    [TestMethod]
    public void Test_GetNullValueErrorMessage_RightNull() {
        var token = new Token { Key = "/", Type = TokenType.Operator, Index = 8 };

        var result = OperationHelper.GetNullValueErrorMessage(false, true, token, DataType.Boolean);

        Assert.Contains("Expected Boolean value", result);
        Assert.DoesNotContain("Left operand value is NULL", result);
        Assert.Contains("Right operand value is NULL", result);
    }

    [TestMethod]
    public void Test_GetNullValueErrorMessage_NeitherNull() {
        var token = new Token { Key = "-", Type = TokenType.Operator, Index = 2 };

        var result = OperationHelper.GetNullValueErrorMessage(false, false, token, DataType.Date);

        Assert.Contains("Expected Date value", result);
        Assert.DoesNotContain("Left operand value is NULL", result);
        Assert.DoesNotContain("Right operand value is NULL", result);
    }

    #endregion

    #region IsInteger Tests

    [TestMethod]
    public void Test_IsInteger_WholeNumber() {
        Assert.IsTrue(OperationHelper.IsInteger(42.0));
        Assert.IsTrue(OperationHelper.IsInteger(0.0));
        Assert.IsTrue(OperationHelper.IsInteger(-15.0));
        Assert.IsTrue(OperationHelper.IsInteger(1000.0));
    }

    [TestMethod]
    public void Test_IsInteger_VeryCloseToInteger() {
        Assert.IsTrue(OperationHelper.IsInteger(42.00000000001)); // 1e-11, well within 1e-10 tolerance
        Assert.IsTrue(OperationHelper.IsInteger(42 + 1e-12));
    }

    [TestMethod]
    public void Test_IsInteger_NotInteger() {
        Assert.IsFalse(OperationHelper.IsInteger(42.5));
        Assert.IsFalse(OperationHelper.IsInteger(3.14159));
        Assert.IsFalse(OperationHelper.IsInteger(0.1));
        Assert.IsFalse(OperationHelper.IsInteger(-7.3));
    }

    [TestMethod]
    public void Test_IsInteger_CustomTolerance() {
        // With larger tolerance, should accept values further from integers
        Assert.IsTrue(OperationHelper.IsInteger(42.0001, 0.001));
        Assert.IsFalse(OperationHelper.IsInteger(42.0001, 1e-10));
    }

    #endregion
}

