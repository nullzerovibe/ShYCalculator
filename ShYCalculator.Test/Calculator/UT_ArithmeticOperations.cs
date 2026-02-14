using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Calculator.Operations;
using ShYCalculator.Classes;

namespace ShYCalculator.Test.Calculator;

[TestClass]
public class UT_ArithmeticOperations {
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

    #region PerformArithmeticOperationOnNumbers Tests

    [TestMethod]
    public void Test_ArithmeticNumbers_Addition() {
        var left = new Value { DataType = DataType.Number, Nvalue = 10 };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(15.0, result.Value.Nvalue);
        Assert.AreEqual(DataType.Number, result.Value.DataType);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_Subtraction() {
        var left = new Value { DataType = DataType.Number, Nvalue = 10 };
        var right = new Value { DataType = DataType.Number, Nvalue = 3 };
        var token = new Token { Key = "-", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Sub };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(7.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_Subtraction_UnicodeMinusSign() {
        var left = new Value { DataType = DataType.Number, Nvalue = 10 };
        var right = new Value { DataType = DataType.Number, Nvalue = 3 };
        var token = new Token { Key = "−", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Sub };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(7.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_Multiplication() {
        var left = new Value { DataType = DataType.Number, Nvalue = 6 };
        var right = new Value { DataType = DataType.Number, Nvalue = 7 };
        var token = new Token { Key = "*", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Mul };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(42.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_Multiplication_UnicodeTimesSign() {
        var left = new Value { DataType = DataType.Number, Nvalue = 6 };
        var right = new Value { DataType = DataType.Number, Nvalue = 7 };
        var token = new Token { Key = "×", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Mul };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(42.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_Division() {
        var left = new Value { DataType = DataType.Number, Nvalue = 20 };
        var right = new Value { DataType = DataType.Number, Nvalue = 4 };
        var token = new Token { Key = "/", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Div };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(5.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_Division_UnicodeDivideSign() {
        var left = new Value { DataType = DataType.Number, Nvalue = 20 };
        var right = new Value { DataType = DataType.Number, Nvalue = 4 };
        var token = new Token { Key = "÷", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Div };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(5.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_DivisionByZero() {
        var left = new Value { DataType = DataType.Number, Nvalue = 10 };
        var right = new Value { DataType = DataType.Number, Nvalue = 0 };
        var token = new Token { Key = "/", Type = TokenType.Operator, Index = 5, OperatorKind = OperatorKind.Div };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("Division by zero") && result.Message.Contains("position 5"), $"Got: {result.Message}");
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_DivisionByZero_UnicodeDivideSign() {
        var left = new Value { DataType = DataType.Number, Nvalue = 10 };
        var right = new Value { DataType = DataType.Number, Nvalue = 0 };
        var token = new Token { Key = "÷", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Div };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("Division by zero") && result.Message.Contains("position 3"), $"Got: {result.Message}");
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_Modulo() {
        var left = new Value { DataType = DataType.Number, Nvalue = 17 };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "%", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Mod };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(2.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_Power() {
        var left = new Value { DataType = DataType.Number, Nvalue = 2 };
        var right = new Value { DataType = DataType.Number, Nvalue = 8 };
        var token = new Token { Key = "^", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Pow };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(256.0, result.Value.Nvalue);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_LeftDataTypeMismatch() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Number data type", result.Message);
        Assert.Contains("Left operand data type String mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_RightDataTypeMismatch() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "*", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Mul };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Number data type", result.Message);
        Assert.Contains("Right operand data type Boolean mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_BothDataTypeMismatch() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected Number data type", result.Message);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_LeftNullValue() {
        var left = new Value { DataType = DataType.Number, Nvalue = null };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_RightNullValue() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = null };
        var token = new Token { Key = "-", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Sub };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_BothNullValues() {
        var left = new Value { DataType = DataType.Number, Nvalue = null };
        var right = new Value { DataType = DataType.Number, Nvalue = null };
        var token = new Token { Key = "*", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Mul };

        var result = ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Left operand value is NULL", result.Message);
        Assert.Contains("Right operand value is NULL", result.Message);
    }

    [TestMethod]
    public void Test_ArithmeticNumbers_UnexpectedOperator() {
        var left = new Value { DataType = DataType.Number, Nvalue = 5 };
        var right = new Value { DataType = DataType.Number, Nvalue = 3 };
        var token = new Token { Key = "==", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Eq };

        AssertThrows<Exception>(() => {
            ArithmeticOperations.PerformArithmeticOperationOnNumbers(left, right, token);
        });
    }

    #endregion

    #region PerformArithmeticOperationOnStrings Tests

    [TestMethod]
    public void Test_ArithmeticStrings_Concatenation() {
        var left = new Value { DataType = DataType.String, Svalue = "Hello" };
        var right = new Value { DataType = DataType.String, Svalue = " World" };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 5, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("Hello World", result.Value.Svalue);
        Assert.AreEqual(DataType.String, result.Value.DataType);
    }

    [TestMethod]
    public void Test_ArithmeticStrings_EmptyStrings() {
        var left = new Value { DataType = DataType.String, Svalue = "" };
        var right = new Value { DataType = DataType.String, Svalue = "" };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 0, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("", result.Value.Svalue);
    }

    [TestMethod]
    public void Test_ArithmeticStrings_LeftDataTypeMismatch() {
        // Now valid: Number + String = String
        var left = new Value { DataType = DataType.Number, Nvalue = 42 };
        var right = new Value { DataType = DataType.String, Svalue = "test" };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("42test", result.Value.Svalue);
        Assert.AreEqual(DataType.String, result.Value.DataType);
    }

    [TestMethod]
    public void Test_ArithmeticStrings_RightDataTypeMismatch() {
        // Now valid: String + Number = String
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Number, Nvalue = 42 };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 4, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("test42", result.Value.Svalue);
        Assert.AreEqual(DataType.String, result.Value.DataType);
    }

    [TestMethod]
    public void Test_ArithmeticStrings_LeftNullValue() {
        // Null is treated as empty string
        var left = new Value { DataType = DataType.String, Svalue = null };
        var right = new Value { DataType = DataType.String, Svalue = "test" };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 1, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("test", result.Value.Svalue);
    }

    [TestMethod]
    public void Test_ArithmeticStrings_RightNullValue() {
        // Null is treated as empty string
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.String, Svalue = null };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 3, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("test", result.Value.Svalue);
    }

    [TestMethod]
    public void Test_ArithmeticStrings_BothNullValues() {
        // Null + Null = Empty String
        var left = new Value { DataType = DataType.String, Svalue = null };
        var right = new Value { DataType = DataType.String, Svalue = null };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("", result.Value.Svalue);
    }

    [TestMethod]
    public void Test_ArithmeticStrings_UnexpectedOperator() {
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.String, Svalue = "value" };
        var token = new Token { Key = "-", Type = TokenType.Operator, Index = 4, OperatorKind = OperatorKind.Sub };

        AssertThrows<Exception>(() => {
            ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);
        });
    }

    [TestMethod]
    public void Test_ArithmeticStrings_Concatenation_Boolean() {
        var left = new Value { DataType = DataType.String, Svalue = "Val: " };
        var right = new Value { DataType = DataType.Boolean, Bvalue = true };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 0, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("Val: True", result.Value.Svalue); // Boolean.ToString() returns "True" or "False"
    }

    [TestMethod]
    public void Test_ArithmeticStrings_Concatenation_Date() {
        var dateVal = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var left = new Value { DataType = DataType.Date, Dvalue = dateVal };
        var right = new Value { DataType = DataType.String, Svalue = " is the date" };
        var token = new Token { Key = "+", Type = TokenType.Operator, Index = 0, OperatorKind = OperatorKind.Add };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(dateVal.ToString() + " is the date", result.Value.Svalue);
    }

    [TestMethod]
    public void Test_ArithmeticStrings_NonAdd_DataTypeMismatch() {
        // Trying to Subtract a Number from a String should fail with type mismatch, 
        // NOT concatenation and NOT unexpected operator (failed before unexpected operator check)
        var left = new Value { DataType = DataType.String, Svalue = "test" };
        var right = new Value { DataType = DataType.Number, Nvalue = 5 }; // Mismatch for Sub
        var token = new Token { Key = "-", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Sub };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected String data type", result.Message);
        Assert.Contains("Right operand data type Number mismatch", result.Message);
    }

    [TestMethod]
    public void Test_ArithmeticStrings_NonAdd_LeftDataTypeMismatch() {
        // Trying to Subtract a String from a Number should fail with type mismatch
        var left = new Value { DataType = DataType.Number, Nvalue = 5 }; // Mismatch for Sub
        var right = new Value { DataType = DataType.String, Svalue = "test" };
        var token = new Token { Key = "-", Type = TokenType.Operator, Index = 2, OperatorKind = OperatorKind.Sub };

        var result = ArithmeticOperations.PerformArithmeticOperationOnStrings(left, right, token);

        Assert.IsFalse(result.Success);
        Assert.Contains("Expected String data type", result.Message);
        Assert.Contains("Left operand data type Number mismatch", result.Message);
    }

    [TestMethod]
    public void Test_GetValueString_Coverage_EdgeCases() {
        // Test DataType.None
        var valNone = new Value { DataType = (DataType)0 };
        // We can't call GetValueString directly as it is private, but we can trigger it via Add
        // 0 + 0 (with types masked as None? No, Value defaults to Number/Null if not specified usually)
        // Let's force it by manually constructing values that might theoretically exist or by using reflection if needed?
        // Actually, we can just pass them to PerformArithmeticOperationOnStrings with "+" operator.

        // Case 1: DataType.None -> Should return ""
        var res1 = ArithmeticOperations.PerformArithmeticOperationOnStrings(valNone, valNone, new Token { OperatorKind = OperatorKind.Add });
        Assert.IsTrue(res1.Success);
        Assert.AreEqual("", res1.Value.Svalue);

        // Case 2: DataType.Number but Null value -> Should return ""
        // We need to manually construct this because Value constructor tries to set flags safely.
        // But we can use the struct default init.
        var valNumNull = new Value { DataType = DataType.Number }; // Nvalue is property getting from null union -> null
        var res2 = ArithmeticOperations.PerformArithmeticOperationOnStrings(valNumNull, valNumNull, new Token { OperatorKind = OperatorKind.Add });
        Assert.IsTrue(res2.Success);
        Assert.AreEqual("", res2.Value.Svalue);

        // Case 3: DataType.Boolean but Null value
        var valBoolNull = new Value { DataType = DataType.Boolean };
        var res3 = ArithmeticOperations.PerformArithmeticOperationOnStrings(valBoolNull, valBoolNull, new Token { OperatorKind = OperatorKind.Add });
        Assert.IsTrue(res3.Success);
        Assert.AreEqual("", res3.Value.Svalue);

         // Case 4: DataType.Date but Null value
        var valDateNull = new Value { DataType = DataType.Date };
        var res4 = ArithmeticOperations.PerformArithmeticOperationOnStrings(valDateNull, valDateNull, new Token { OperatorKind = OperatorKind.Add });
        Assert.IsTrue(res4.Success);
        Assert.AreEqual("", res4.Value.Svalue);
    }

    #endregion
}



