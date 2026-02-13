using ShYCalculator.Classes;
using ShYCalculator.Functions.Dates;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions.Text;
using ShYCalculator.Functions;

namespace ShYCalculator.Test;

[TestClass]
public class UT_Errors {
    // Fix nullable warning
    private ShYCalculator m_shyCalculator = null!;

    [TestInitialize]
    public void TestInitialize() {
        m_shyCalculator = new ShYCalculator();
        m_shyCalculator.Environment.SetVariables(new Dictionary<string, Value> {
            { "$one", new Value() { Nvalue = 1, DataType = DataType.Number } },
            { "$two", new Value() { Nvalue = 2, DataType = DataType.Number } },
            { "$date", new Value() { Dvalue = DateTimeOffset.Now, DataType = DataType.Date } },
            { "$str", new Value() { Svalue = "text", DataType = DataType.String } }
        });

        // Register functions
        m_shyCalculator.Environment.RegisterFunctions(new CalcDateFunctions());
        m_shyCalculator.Environment.RegisterFunctions(new CalcStringFunctions());
        m_shyCalculator.Environment.RegisterFunctions(new CalcArithmeticalFunctions());
        m_shyCalculator.Environment.RegisterFunctions(new CalcNumericFunctions());
    }

    [TestMethod]
    public void Error_InvalidExpression_Empty() {
        var result = m_shyCalculator.Calculate("");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.InvalidExpression, result.Errors.Count > 0 ? result.Errors[0].Code : default);
    }

    [TestMethod]
    public void Error_UnknownToken() {
        var result = m_shyCalculator.Calculate("1 + @");

        Assert.IsFalse(result.Success);
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;
        Assert.IsNotNull(error);
        Assert.AreEqual(ErrorCode.UnknownToken, error.Code);
        Assert.AreEqual(4, error.StartIndex);
    }

    [TestMethod]
    public void Error_MismatchedParentheses() {
        var result = m_shyCalculator.Calculate("(1 + 2");

        Assert.IsFalse(result.Success);
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;
        Assert.IsNotNull(error);
        Assert.AreEqual(ErrorCode.MismatchedParentheses, error.Code);
        Assert.AreEqual(0, error.StartIndex);
    }

    [TestMethod]
    public void Error_MissingOperand() {
        var result = m_shyCalculator.Calculate("1 +");
        // "1 +" -> Tokenize OK. Generator: "1" to output, "+" to stack. End: "+" pop.
        // Parser: "+" needs 2 operands. Stack has "1".

        Assert.IsFalse(result.Success);
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;
        Assert.IsNotNull(error);
        Assert.AreEqual(ErrorCode.MissingOperand, error.Code);
    }

    [TestMethod]
    public void Error_Ternary_MissingColon() {
        var result = m_shyCalculator.Calculate("1 ? 2");

        Assert.IsFalse(result.Success);
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;
        Assert.IsNotNull(error);
        Assert.AreEqual(ErrorCode.InvalidSyntax, error.Code);
    }

    [TestMethod]
    public void Error_UnknownFunction() {
        var result = m_shyCalculator.Calculate("foobar(1)");

        Assert.IsFalse(result.Success);
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;
        Assert.IsNotNull(error);
        Assert.AreEqual(ErrorCode.VariableNotFound, error.Code);
    }

    [TestMethod]
    public void Error_BaseFunctions_ConsecutiveOperators() {
        var result = m_shyCalculator.Calculate("1 * * 2");

        Assert.IsFalse(result.Success);
        var errorCodes = result.Errors.Select(e => e.Code).ToList();
        Assert.IsTrue(errorCodes.Contains(ErrorCode.MissingOperand) || errorCodes.Contains(ErrorCode.InvalidSyntax));
    }

    [TestMethod]
    public void Error_BaseFunctions_StartingOperator() {
        var result = m_shyCalculator.Calculate("* 1");
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Count > 0 && result.Errors.Any(e => e.Code == ErrorCode.MissingOperand));
    }

    [TestMethod]
    public void Error_ExtendedFunctions_ArgCount() {
        var result = m_shyCalculator.Calculate("sin(1, 2)");
        Assert.IsFalse(result.Success);
        var errorCodes = result.Errors.Select(e => e.Code).ToList();
        Assert.IsTrue(errorCodes.Contains(ErrorCode.InvalidFunctionArgument) || errorCodes.Contains(ErrorCode.InvalidSyntax));
    }

    [TestMethod]
    public void Error_ExtendedFunctions_ArgCount_Less() {
        var result = m_shyCalculator.Calculate("max()");
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void Error_Logical_Negative() {
        var result = m_shyCalculator.Calculate("1 <");
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.MissingOperand, error?.Code);
    }

    [TestMethod]
    public void Error_Ternary_EmptyBranch() {
        var result = m_shyCalculator.Calculate("true ? : 3");
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.InvalidSyntax, error?.Code);
    }

    [TestMethod]
    public void Error_TypeMismatch_Date() {
        var result = m_shyCalculator.Calculate("GetYear(123)");
        Assert.IsFalse(result.Success);
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void Error_DivisionByZero() {
        var result = m_shyCalculator.Calculate("1 / 0");
        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.DivisionByZero, result.Errors.Count > 0 ? result.Errors[0].Code : default);
    }

    [TestMethod]
    public void Error_String_Unterminated() {
        var result = m_shyCalculator.Calculate("\"test");
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.UnknownToken, error?.Code);
    }

    [TestMethod]
    public void Error_UnknownVariable() {
        var result = m_shyCalculator.Calculate("1 + INVALID");
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.VariableNotFound, error?.Code);
    }

    [TestMethod]
    public void Error_FunctionException() {
        var extension = new ThrowerExtension();

        m_shyCalculator.Environment.RegisterFunctions(extension);

        var result = m_shyCalculator.Calculate("ThrowNow()");
        var error = result.Errors.Count > 0 ? result.Errors[0] : null;

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.InvalidFunctionArgument, error?.Code);
        Assert.Contains("Forced error", error?.Message ?? "");
    }

    private class ThrowerExtension : ICalcFunctionsExtension {
        public string Name => "Thrower";
        public IEnumerable<CalcFunction> GetFunctions() => [
            new() { Name = "ThrowNow", Arguments = [] }
        ];
        public Value ExecuteFunction(string functionName, ReadOnlySpan<Value> parameters) {
            if (functionName == "ThrowNow") throw new Exception("Forced error");
            return new Value();
        }
    }
}
