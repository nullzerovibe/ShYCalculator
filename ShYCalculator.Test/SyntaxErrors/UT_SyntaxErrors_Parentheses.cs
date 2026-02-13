using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;

namespace ShYCalculator.Test.SyntaxErrors;

[TestClass]
public class UT_SyntaxErrors_Parentheses {
    private ShYCalculator m_shyCalculator = null!;

    [TestInitialize]
    public void TestInitialize() {
        m_shyCalculator = new ShYCalculator();
        m_shyCalculator.Environment.RegisterFunctions(new CalcArithmeticalFunctions());
    }

    [TestMethod]
    public void MissingClosingParenthesis() {
        // Valid: (1 + 2) * 3
        var result = m_shyCalculator.Calculate("(1 + 2 * 3");
        
        Assert.IsFalse(result.Success, "Expected failure for missing closing parenthesis");
        Assert.IsTrue(result.Errors.Any(e => e.Code == ErrorCode.MismatchedParentheses), "Expected MismatchedParentheses error");
    }

    [TestMethod]
    public void MissingOpeningParenthesis() {
        // Valid: 1 + (2 * 3)
        var result = m_shyCalculator.Calculate("1 + 2 * 3)");

        Assert.IsFalse(result.Success, "Expected failure for missing opening parenthesis");
        Assert.IsTrue(result.Errors.Any(e => e.Code == ErrorCode.MismatchedParentheses), "Expected MismatchedParentheses error");
    }

    [TestMethod]
    public void EmptyParentheses() {
        // Valid: (1)
        var result = m_shyCalculator.Calculate("()");

        Assert.IsFalse(result.Success, "Expected failure for empty parentheses");
        // Expecting some error, potentially InvalidSyntax or similar
        Assert.IsTrue(result.Errors.Count > 0);
    }
}
