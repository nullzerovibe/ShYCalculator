using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;

namespace ShYCalculator.Test.SyntaxErrors;

[TestClass]
public class UT_SyntaxErrors_Functions {
    private ShYCalculator m_shyCalculator = null!;

    [TestInitialize]
    public void TestInitialize() {
        m_shyCalculator = new ShYCalculator();
        m_shyCalculator.Environment.RegisterFunctions(new CalcScientificFunctions());
    }

    [TestMethod]
    public void UnknownFunction() {
        // Valid: sin(1)
        var result = m_shyCalculator.Calculate("unknown_func(1)");

        Assert.IsFalse(result.Success);
        // Usually reports as VariableNotFound if it looks like an identifier
        Assert.Contains(e => e.Code == ErrorCode.VariableNotFound || e.Code == ErrorCode.UnknownToken, result.Errors);
    }

    [TestMethod]
    public void ArgumentCount_TooMany() {
        // Valid: sin(1)
        var result = m_shyCalculator.Calculate("sin(1, 2)");

        Assert.IsFalse(result.Success);
        Assert.Contains(e => e.Code == ErrorCode.InvalidFunctionArgument, result.Errors);
    }

    [TestMethod]
    public void ArgumentCount_TooFew() {
        // Valid: sin(1)
        var result = m_shyCalculator.Calculate("sin()");

        Assert.IsFalse(result.Success);
        // Might be InvalidFunctionArgument or InvalidSyntax depending on parser
        Assert.Contains(e => e.Code == ErrorCode.InvalidFunctionArgument || e.Code == ErrorCode.InvalidSyntax, result.Errors);
    }

    [TestMethod]
    public void InvalidArgumentSyntax_MissingComma() {
        // Valid: max(1, 2)
        var result = m_shyCalculator.Calculate("max(1 2)");

        Assert.IsFalse(result.Success);
        var error = result.Errors.FirstOrDefault();
        Assert.IsTrue(
            error != null && (error.Code == ErrorCode.InvalidSyntax || error.Code == ErrorCode.MissingOperand || error.Code == ErrorCode.UnknownToken || error.Code == ErrorCode.InvalidFunctionArgument),
            $"Actual error: {error?.Code}, Msg: {error?.Message}"
        );
    }
}
