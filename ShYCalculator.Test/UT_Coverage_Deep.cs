using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Classes;
using System.Collections.Generic;
using Moq;
using ShYCalculator.Functions;
using System.Linq;

namespace ShYCalculator.Test;

[TestClass]
public class UT_Coverage_Deep {
    private Mock<global::ShYCalculator.Classes.IEnvironment> m_envMock = null!;
    private global::ShYCalculator.Calculator.ShuntingYardParser m_parser = null!;

    [TestInitialize]
    public void TestInitialize() {
        m_envMock = new Mock<global::ShYCalculator.Classes.IEnvironment>();
        m_parser = new global::ShYCalculator.Calculator.ShuntingYardParser(m_envMock.Object);
    }

    [TestMethod]
    public void Test_ProcessToken_UnknownType() {
        var stack = new Value[10];
        int count = 0;
        // Manually creating a token with an invalid type to trigger 'default' case in ProcessToken
        var token = new Token(0, "???", (TokenType)999);
        
        // We can call it directly if it's internal.
        var result = (OpResult<Value>)m_parser.ProcessToken(token, stack, ref count, null!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.UnknownToken, result.Errors.FirstOrDefault()?.Code);
    }

    [TestMethod]
    public void Test_ProcessToken_InvalidNumber() {
        var stack = new Value[10];
        int count = 0;
        var token = new Token(0, "not_a_number", TokenType.Number);
        
        var result = (OpResult<Value>)m_parser.ProcessToken(token, stack, ref count, null!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.InvalidNumberFormat, result.Errors.FirstOrDefault()?.Code);
    }

    [TestMethod]
    public void Test_ProcessFunction_MissingInfo() {
        var stack = new Value[10];
        int count = 0;
        var token = new Token(0, "sin", TokenType.Function); // FunctionInfo is null by default
        
        // Mock environment to return the extension
        var extension = new SinExtension();
        var functions = new Dictionary<string, ICalcFunctionsExtension> { { "sin", extension } };
        m_envMock.Setup(e => e.Functions).Returns(functions);

        var result = (OpResult<Value>)m_parser.ProcessFunction(token, stack, ref count);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.UnexpectedException, result.Errors.FirstOrDefault()?.Code);
        Assert.Contains("FunctionInfo is missing", result.Message);
    }

    [TestMethod]
    public void Test_EvaluateInternal_StackSizeMismatch() {
        // EvaluateInternal returns error if stack count != 1 at the end
        // Trigger by passing two numbers but no operator (in RPN)
        var tokens = new List<Token> {
            new(0, "1", TokenType.Number),
            new(2, "2", TokenType.Number)
        };

        var result = (OpResult<Value>)m_parser.EvaluateInternal(tokens, null!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.InvalidSyntax, result.Errors.FirstOrDefault()?.Code);
        Assert.Contains("stack should have exactly one value", result.Message);
    }

    [TestMethod]
    public void Test_ProcessTernary_MissingBranches() {
        var stack = new Value[10];
        int count = 0;
        stack[count++] = new Value { DataType = DataType.Boolean, Bvalue = true };
        
        var token = new Token(0, "?", TokenType.Ternary) {
            TernaryBranches = null // Should trigger error
        };

        var result = (OpResult<Value>)m_parser.ProcessTernary(token, stack, ref count, null!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.TernaryBranchError, result.Errors.FirstOrDefault()?.Code);
    }

    private class SinExtension : ICalcFunctionsExtension {
        public string Name => "Sin";
        public IEnumerable<CalcFunction> GetFunctions() => [];
        public Value ExecuteFunction(string functionName, ReadOnlySpan<Value> parameters) => new();
    }
}
