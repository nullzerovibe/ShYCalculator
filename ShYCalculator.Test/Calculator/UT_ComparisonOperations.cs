using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Calculator.Operations;
using ShYCalculator.Classes;

namespace ShYCalculator.Test.Calculator;

[TestClass]
public class UT_ComparisonOperations {
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

    #region PerformComparisonOperationOnStrings Tests

    [TestMethod]
    public void Test_ComparisonStrings_Equals_True() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.String, Svalue = "test" };
        var token = new Token(4, "==", TokenType.Operator, operatorKind: OperatorKind.Eq);

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
        Assert.AreEqual(DataType.Boolean, result.Value.DataType);
    }

    [TestMethod]
    public void Test_ComparisonStrings_Equals_False() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.String, Svalue = "value" };
        var token = new Token(4, "==", TokenType.Operator, operatorKind: OperatorKind.Eq);

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonStrings_NotEquals_True() {
        var left = new Value { DataType = DataType.String, Svalue = "hello" };
        var right = new Value { DataType = DataType.String, Svalue = "world" };
        var token = new Token(5, "!=", TokenType.Operator, operatorKind: OperatorKind.NotEq);

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonStrings_NotEquals_False() {
        var left = new Value { DataType = DataType.String, Svalue = "same" };
        var right = new Value { DataType = DataType.String, Svalue = "same" };
        var token = new Token(4, "!=", TokenType.Operator, operatorKind: OperatorKind.NotEq);

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonStrings_EmptyStrings() {
        var left = new Value { DataType = DataType.String, Svalue = "" };
        var right = new Value { DataType = DataType.String, Svalue = "" };
        var token = new Token(0, "==", TokenType.Operator, operatorKind: OperatorKind.Eq);

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonStrings_LeftDataTypeMismatch() {
        var left = new Value { DataType = DataType.Number, Nvalue = 42 };
        var right = new Value { DataType = DataType.String, Svalue = "test" };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected String data type", result.Message);
        Assert.Contains("Left operand data type Number mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonStrings_RightDataTypeMismatch() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token(4, "!=", TokenType.Operator, operatorKind: OperatorKind.NotEq);

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected String data type", result.Message);
        Assert.Contains("Right operand data type Boolean mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonStrings_LeftNullValue() {
        var left = new Value { DataType = DataType.String, Svalue = null };
        var right = new Value { DataType = DataType.String, Svalue = "test" };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonStrings_RightNullValue() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.String, Svalue = null };
        var token = new Token(4, "==", TokenType.Operator, operatorKind: OperatorKind.Eq);

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonStrings_BothNullValues() {
        var left = new Value { DataType = DataType.String, Svalue = null };
        var right = new Value { DataType = DataType.String, Svalue = null };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonStrings_UnexpectedOperator() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.String, Svalue = "value" };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 4, OperatorKind = OperatorKind.Lt };
        AssertThrows<Exception>(() => ComparisonOperations.PerformComparisonOperationOnStrings(left, right, token));
    }

    #endregion

    #region PerformComparisonOperationOnNumbers Tests

    [TestMethod]
    public void Test_ComparisonNumbers_LessThan_True() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 10 };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Lt };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
        Assert.AreEqual(DataType.Boolean, result.Value.DataType);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_LessThan_False() {
        var left = new Value { DataType = DataType.Number, Nvalue = 10 };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Lt };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_GreaterThan_True() {
        var left = new Value { DataType = DataType.Number, Nvalue = 15 };
        var right = new Value { DataType = DataType.Number, Nvalue = 10 };
        var token = new Token { Key = ">", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Gt };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_GreaterThan_False() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 10 };
        var token = new Token { Key = ">", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Gt };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_LessThanOrEqual_True_Less() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 10 };
        var token = new Token { Key = "<=", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.LtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_LessThanOrEqual_True_Equal() {
        var left = new Value { DataType = DataType.Number, Nvalue = 10 };
        var right = new Value { DataType = DataType.Number, Nvalue = 10 };
        var token = new Token { Key = "<=", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.LtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_LessThanOrEqual_False() {
        var left = new Value { DataType = DataType.Number, Nvalue = 15 };
        var right = new Value { DataType = DataType.Number, Nvalue = 10 };
        var token = new Token { Key = "<=", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.LtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_GreaterThanOrEqual_True_Greater() {
        var left = new Value { DataType = DataType.Number, Nvalue = 15 };
        var right = new Value { DataType = DataType.Number, Nvalue = 10 };
        var token = new Token { Key = ">=", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.GtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_GreaterThanOrEqual_True_Equal() {
        var left = new Value { DataType = DataType.Number, Nvalue = 10 };
        var right = new Value { DataType = DataType.Number, Nvalue = 10 };
        var token = new Token { Key = ">=", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.GtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_GreaterThanOrEqual_False() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 10 };
        var token = new Token { Key = ">=", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.GtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_Equals_True() {
        var left = new Value { DataType = DataType.Number, Nvalue = 42 };
        var right = new Value { DataType = DataType.Number, Nvalue = 42 };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_Equals_False() {
        var left = new Value { DataType = DataType.Number, Nvalue = 42 };
        var right = new Value { DataType = DataType.Number, Nvalue = 43 };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_NotEquals_True() {
        var left = new Value { DataType = DataType.Number, Nvalue = 10 };
        var right = new Value { DataType = DataType.Number, Nvalue = 20 };
        var token = new Token { Key = "!=", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.NotEq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_NotEquals_False() {
        var left = new Value { DataType = DataType.Number, Nvalue = 15 };
        var right = new Value { DataType = DataType.Number, Nvalue = 15 };
        var token = new Token { Key = "!=", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.NotEq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_LeftDataTypeMismatch() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Lt };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Number data type", result.Message);
        Assert.Contains("Left operand data type String mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_RightDataTypeMismatch() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = ">", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Gt };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Number data type", result.Message);
        Assert.Contains("Right operand data type Boolean mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_LeftNullValue() {
        var left = new Value { DataType = DataType.Number, Nvalue = null };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_RightNullValue() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = null };
        var token = new Token { Key = "!=", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.NotEq };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_BothNullValues() {
        var left = new Value { DataType = DataType.Number, Nvalue = null };
        var right = new Value { DataType = DataType.Number, Nvalue = null };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Lt };

        var result = ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonNumbers_UnexpectedOperator() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 3 };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Add };
        AssertThrows<Exception>(() => ComparisonOperations.PerformComparisonOperationOnNumbers(left, right, token));
    }

    #endregion

    #region PerformComparisonOperationOnBooleans Tests

    [TestMethod]
    public void Test_ComparisonBooleans_Equals_TrueTrue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 5, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
        Assert.AreEqual(DataType.Boolean, result.Value.DataType);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_Equals_FalseFalse() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = false };
        var right = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_Equals_TrueFalse() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_NotEquals_TrueFalse() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token(4, "!=", TokenType.Operator, operatorKind: OperatorKind.NotEq);

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_NotEquals_TrueTrue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "!=", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.NotEq };

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_LeftDataTypeMismatch() {
        var left = new Value { DataType = DataType.Number, Nvalue = 1 };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Boolean data type", result.Message);
        Assert.Contains("Left operand data type Number mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_RightDataTypeMismatch() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.String, Svalue = "true" };
        var token = new Token { Key = "!=", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.NotEq };

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Boolean data type", result.Message);
        Assert.Contains("Right operand data type String mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_LeftNullValue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = null };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token(4, "==", TokenType.Operator, operatorKind: OperatorKind.Eq);

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_RightNullValue() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = false };
        var right = new Value { DataType = DataType.Boolean, Bvalue = null };
        var token = new Token { Key = "!=", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.NotEq };

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_BothNullValues() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = null };
        var right = new Value { DataType = DataType.Boolean, Bvalue = null };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonBooleans_UnexpectedOperator() {
        var left = new Value { DataType = DataType.Boolean, Bvalue = true };
        var right = new Value { DataType = DataType.Boolean, Bvalue = false };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Lt };
        AssertThrows<Exception>(() => ComparisonOperations.PerformComparisonOperationOnBooleans(left, right, token));
    }

    #endregion

    #region PerformComparisonOperationOnDates Tests

    [TestMethod]
    public void Test_ComparisonDates_LessThan_True() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Lt };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
        Assert.AreEqual(DataType.Boolean, result.Value.DataType);
    }

    [TestMethod]
    public void Test_ComparisonDates_LessThan_False() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Lt };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_GreaterThan_True() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = ">", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Gt };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_GreaterThan_False() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = ">", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Gt };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_LessThanOrEqual_True_Less() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = "<=", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.LtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_LessThanOrEqual_True_Equal() {
        var date = new DateTimeOffset(2024, 5, 15, 12, 30, 0, TimeSpan.Zero);
        var left = new Value { DataType = DataType.Date, Dvalue = date };
        var right = new Value { DataType = DataType.Date, Dvalue = date };
        var token = new Token { Key = "<=", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.LtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_LessThanOrEqual_False() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = "<=", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.LtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_GreaterThanOrEqual_True_Greater() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = ">=", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.GtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_GreaterThanOrEqual_True_Equal() {
        var date = new DateTimeOffset(2024, 7, 4, 15, 45, 30, TimeSpan.Zero);
        var left = new Value { DataType = DataType.Date, Dvalue = date };
        var right = new Value { DataType = DataType.Date, Dvalue = date };
        var token = new Token { Key = ">=", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.GtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_GreaterThanOrEqual_False() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = ">=", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.GtEq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_Equals_True() {
        var date = new DateTimeOffset(2024, 3, 15, 10, 20, 30, TimeSpan.FromHours(2));
        var left = new Value { DataType = DataType.Date, Dvalue = date };
        var right = new Value { DataType = DataType.Date, Dvalue = date };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_Equals_False() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_NotEquals_True() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 6, 16, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = "!=", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.NotEq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_NotEquals_False() {
        var date = new DateTimeOffset(2024, 9, 20, 8, 15, 45, TimeSpan.FromHours(-5));
        var left = new Value { DataType = DataType.Date, Dvalue = date };
        var right = new Value { DataType = DataType.Date, Dvalue = date };
        var token = new Token { Key = "!=", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.NotEq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Value.Bvalue);
    }

    [TestMethod]
    public void Test_ComparisonDates_LeftDataTypeMismatch() {
        var left = new Value { DataType = DataType.Number, Nvalue = 42 };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 5, OperatorKind = OperatorKind.Lt };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Date data type", result.Message);
        Assert.Contains("Left operand data type Number mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonDates_RightDataTypeMismatch() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.String, Svalue = "2024-01-01" };
        var token = new Token { Key = ">", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Gt };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Date data type", result.Message);
        Assert.Contains("Right operand data type String mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonDates_LeftNullValue() {
        var left = new Value { DataType = DataType.Date, Dvalue = null };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Eq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonDates_RightNullValue() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = null };
        var token = new Token { Key = "!=", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.NotEq };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonDates_BothNullValues() {
        var left = new Value { DataType = DataType.Date, Dvalue = null };
        var right = new Value { DataType = DataType.Date, Dvalue = null };
        var token = new Token { Key = "<", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Lt };

        var result = ComparisonOperations.PerformComparisonOperationOnDates(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ComparisonDates_UnexpectedOperator() {
        var left = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var right = new Value { DataType = DataType.Date, Dvalue = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero) };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 10, OperatorKind = OperatorKind.Add };
        AssertThrows<Exception>(() => ComparisonOperations.PerformComparisonOperationOnDates(left, right, token));
    }

    #endregion
}



