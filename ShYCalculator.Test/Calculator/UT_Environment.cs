using ShYCalculator.Classes;
using ShYCalculator.Functions;
using ShYCalculator.Functions.Dates;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions.Text;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Calculator;

[TestClass]
public class UT_Environment
{
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;

    [TestInitialize]
    public void Setup()
    {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        // Register default functions as some tests expect them
        m_environment.RegisterFunctions(new CalcDateFunctions());
        m_environment.RegisterFunctions(new CalcArithmeticalFunctions());
        m_environment.RegisterFunctions(new CalcNumericFunctions());
        m_environment.RegisterFunctions(new CalcStringFunctions());
    }

    #region Environment Tests

    [TestMethod]
    public void Environment_SetConstants_Coverage()
    {
        var constants = new Dictionary<string, Value> {
            { "test_const", new Value { Nvalue = 123, DataType = DataType.Number } }
        };
        m_environment.SetConstants(constants);
        Assert.AreEqual(123.0, m_environment.Constants["test_const"].Nvalue);
    }

    [TestMethod]
    public void Environment_AddVariables_Overloads_Coverage()
    {
        m_environment.AddVariables(new Dictionary<string, bool> { { "$b1", true } });
        m_environment.AddVariables(new Dictionary<string, string> { { "$s1", "hello" } });
        m_environment.AddVariables(new Dictionary<string, DateTimeOffset> { { "$d1", DateTimeOffset.Now } });

        Assert.IsTrue(m_environment.Variables["$b1"].Bvalue);
        Assert.AreEqual("hello", m_environment.Variables["$s1"].Svalue);
        Assert.IsNotNull(m_environment.Variables["$d1"].Dvalue);

        // Verify usability in calculation
        var result = m_shyCalculator.Calculate("$s1 == \"hello\"");
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Bvalue);
    }

    [TestMethod]
    public void Environment_AddConstants_Overloads_Coverage()
    {
        m_environment.AddConstants(new Dictionary<string, double> { { "c1", 1.1 } });
        m_environment.AddConstants(new Dictionary<string, bool> { { "c2", true } });
        m_environment.AddConstants(new Dictionary<string, string> { { "c3", "const" } });
        m_environment.AddConstants(new Dictionary<string, DateTimeOffset> { { "c4", DateTimeOffset.Now } });

        Assert.AreEqual(1.1, m_environment.Constants["c1"].Nvalue);
        Assert.IsTrue(m_environment.Constants["c2"].Bvalue);
        Assert.AreEqual("const", m_environment.Constants["c3"].Svalue);
        Assert.IsNotNull(m_environment.Constants["c4"].Dvalue);
    }

    [TestMethod]
    public void Environment_RegisterFunctions_EmptyList_Throws()
    {
        var extension = new EmptyExtension();
        try
        {
            m_environment.RegisterFunctions(extension);
            Assert.Fail("Expected CalcEnvironmentException was not thrown.");
        }
        catch (CalcEnvironmentException)
        {
            // Success
        }
    }

    private class EmptyExtension : ICalcFunctionsExtension
    {
        public string Name => "Empty";
        public IEnumerable<CalcFunction> GetFunctions() => [];
        public Value ExecuteFunction(string functionName, ReadOnlySpan<Value> parameters) => new();
    }

    #endregion Environment Tests
}
