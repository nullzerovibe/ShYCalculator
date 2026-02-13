using ShYCalculator.Classes;
using ShYCalculator.Functions.Mathematics;

namespace ShYCalculator.Test.SyntaxErrors;

[TestClass]
public class UT_SyntaxErrors_Operators {
    private ShYCalculator m_shyCalculator = null!;

    [TestInitialize]
    public void TestInitialize() {
        m_shyCalculator = new ShYCalculator();
        m_shyCalculator.Environment.RegisterFunctions(new CalcArithmeticalFunctions());
    }

    [TestMethod]
    public void MissingRightOperand() {
        // Valid: 1 + 2
        var result = m_shyCalculator.Calculate("1 +");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Code == ErrorCode.MissingOperand));
    }

    [TestMethod]
    public void MissingLeftOperand() {
        // Valid: 1 + 2
        var result = m_shyCalculator.Calculate("* 2"); // '*' usually requires left operand, unary '-' might behave differently

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Code == ErrorCode.MissingOperand));
    }

    [TestMethod]
    public void DoubleOperators_Same() {
        // Valid: 1 + 2
        var result = m_shyCalculator.Calculate("1 ++ 2");

        Assert.IsFalse(result.Success);
        // This might interpret as 1 + (+2) which is valid if unary plus is supported, 
        // OR it might fail. Let's try '1 ** 2' which is definitely invalid (unless power operator)
        // ShY might support power as '^', so let's stick to '1 + * 2'
    }

    [TestMethod]
    public void DoubleOperators_Mixed() {
        // Valid: 1 + 2
        var result = m_shyCalculator.Calculate("1 + * 2");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Code == ErrorCode.MissingOperand || e.Code == ErrorCode.InvalidSyntax));
    }
    
    [TestMethod]
    public void InvalidTrailingOperator() {
        // Valid: 1 + 2
        var result = m_shyCalculator.Calculate("1 + 2 /");
        
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Code == ErrorCode.MissingOperand));
    }
}
