using ShY.Functions.Dates;
using ShY.Functions.Mathematics;
using ShY.Functions.Text;
using ShY.Calculator;
using Environment = ShY.Calculator.CalculatorEnvironment;

namespace ShY.Test.Calculator;

[TestClass]
public class UT_ExpressionTokenizer {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        // Register default functions as some tests expect them
        m_environment.RegisterFunctions(new CalcDateFunctions());
        m_environment.RegisterFunctions(new CalcArithmeticalFunctions());
        m_environment.RegisterFunctions(new CalcNumericFunctions());
        m_environment.RegisterFunctions(new CalcStringFunctions());
    }

    #region ExpressionTokenizer Tests

    [TestMethod]
    public void Tokenizer_ScientificNotation_Positive_Exponent() {
        var tokenizer = new ExpressionTokenizer(m_environment);
        var opResult = tokenizer.Tokenize("1.2e+3");
        Assert.IsTrue(opResult.Success);
        var tokens = opResult.Value.ToList();
        Assert.HasCount(1, tokens);
        Assert.AreEqual("1.2e+3", tokens[0].Key);
    }

    [TestMethod]
    public void Tokenizer_ScientificNotation_Negative_Exponent() {
        var tokenizer = new ExpressionTokenizer(m_environment);
        var opResult = tokenizer.Tokenize("1.2e-3");
        Assert.IsTrue(opResult.Success);
        var tokens = opResult.Value.ToList();
        Assert.HasCount(1, tokens);
        Assert.AreEqual("1.2e-3", tokens[0].Key);
    }

    [TestMethod]
    public void Tokenizer_ScientificNotation_NoSign_Exponent() {
        var tokenizer = new ExpressionTokenizer(m_environment);
        var opResult = tokenizer.Tokenize("1.2e3");
        Assert.IsTrue(opResult.Success);
        var tokens = opResult.Value.ToList();
        Assert.HasCount(1, tokens);
        Assert.AreEqual("1.2e3", tokens[0].Key);
    }

    #endregion ExpressionTokenizer Tests
}
