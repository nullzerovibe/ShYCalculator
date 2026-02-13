using ShYCalculator.Classes;

namespace ShYCalculator.Test.SyntaxErrors;

[TestClass]
public class UT_SyntaxErrors_Values {
    private ShYCalculator m_shyCalculator = null!;

    [TestInitialize]
    public void TestInitialize() {
        m_shyCalculator = new ShYCalculator();
    }

    [TestMethod]
    public void InvalidNumberFormat_MultipleDots() {
        // Valid: 1.2
        var result = m_shyCalculator.Calculate("1.2.3");

        Assert.IsFalse(result.Success);
        // Tokenizer likely treats this as "1.2" then ".3" or similar, or just UnknownToken
        Assert.IsTrue(result.Errors.Count > 0);
    }

    [TestMethod]
    public void InvalidNumberFormat_InvalidExponent() {
        // Valid: 1E+2
        var result = m_shyCalculator.Calculate("1E++2");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Count > 0);
    }

    [TestMethod]
    public void UnknownVariable() {
        // Valid: 1 + 1
        var result = m_shyCalculator.Calculate("1 + unknown_var");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Code == ErrorCode.VariableNotFound));
    }

    [TestMethod]
    public void UnterminatedString() {
        // Valid: "string"
        var result = m_shyCalculator.Calculate("\"unterminated string");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Code == ErrorCode.UnknownToken || e.Code == ErrorCode.InvalidSyntax));
    }
}
