using ShY.Classes;

namespace ShY.Test;

[TestClass]
public class UT_DecimalMode {
    private static readonly ShYCalculatorOptions DecimalOptions = new() { NumericMode = NumericMode.Decimal };
    private ShYCalculator m_decimal = null!;
    private ShYCalculator m_double = null!;

    [TestInitialize]
    public void TestInitialize() {
        m_decimal = new ShYCalculator(options: DecimalOptions);
        m_double = new ShYCalculator();
    }

    private decimal Exact(string expression, IDictionary<string, Value>? vars = null) {
        var result = m_decimal.Calculate(expression, vars);
        Assert.IsTrue(result.Success, result.Message);
        Assert.IsTrue(result.Value.IsDecimal, $"'{expression}' should produce a decimal");
        return result.Mvalue!.Value;
    }

    #region Exact arithmetic
    [TestMethod]
    [DataRow("0.1 + 0.2", "0.3")]
    [DataRow("19.99 * 3", "59.97")]
    [DataRow("1.1 ^ 2", "1.21")]
    [DataRow("1.5 ^ -2", "0.4444444444444444444444444444")]
    [DataRow("10.5 % 3", "1.5")]
    [DataRow("100 - 99.99", "0.01")]
    [DataRow("-0.1 - 0.2", "-0.3")]
    [DataRow("1e3 + 0.5", "1000.5")]
    [DataRow("20!", "2432902008176640000")]
    [DataRow("6 & 3", "2")]
    [DataRow("~5", "-6")]
    [DataRow("4 ^ 0.5", "2")]
    public void Arithmetic_IsExact(string expression, string expected) {
        Assert.AreEqual(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), Exact(expression));
    }

    [TestMethod]
    [DataRow("0.1 + 0.2 == 0.3", true)]
    [DataRow("0.1 + 0.2 > 0.3", false)]
    [DataRow("1.10 == 1.1", true)]
    [DataRow("0.1 * 3 != 0.3", false)]
    public void Comparison_IsExact(string expression, bool expected) {
        var result = m_decimal.Calculate(expression);

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(expected, result.Bvalue);
    }

    [TestMethod]
    public void DoubleMode_IsUnchanged() {
        var result = m_double.Calculate("0.1 + 0.2");

        Assert.IsFalse(result.Value.IsDecimal);
        Assert.AreEqual(0.1 + 0.2, result.Nvalue);
        Assert.IsFalse(m_double.Calculate("0.1 + 0.2 == 0.3").Bvalue);
        Assert.AreEqual(1.2100000000000002, m_double.Calculate("1.1 ^ 2").Nvalue);
    }
    #endregion

    #region Functions
    [TestMethod]
    [DataRow("round(2.675, 2)", "2.68")]
    [DataRow("round(1.255, 2)", "1.26")]
    [DataRow("round(2.5)", "2")]
    [DataRow("sum(0.1, 0.2, 0.3)", "0.6")]
    [DataRow("avg(0.1, 0.2)", "0.15")]
    [DataRow("min(0.3, 0.1, 0.2)", "0.1")]
    [DataRow("max(0.3, 0.1, 0.2)", "0.3")]
    [DataRow("abs(-0.1)", "0.1")]
    [DataRow("floor(-2.5)", "-3")]
    [DataRow("ceiling(2.1)", "3")]
    [DataRow("trunc(-2.7)", "-2")]
    [DataRow("sign(-0.001)", "-1")]
    [DataRow("median(1.1, 3.3, 2.2)", "2.2")]
    [DataRow("median(0.1, 0.2)", "0.15")]
    [DataRow("mode(0.1, 0.2, 0.2)", "0.2")]
    [DataRow("count(1, 2, 3)", "3")]
    [DataRow("sin(0)", "0")]
    public void Functions_AreExactOrConvertedBack(string expression, string expected) {
        Assert.AreEqual(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), Exact(expression));
    }

    [TestMethod]
    public void Functions_DoubleMode_RoundShowsBinaryArtifact() {
        // WHY: 1.255 is stored as 1.25499999999999989... in binary, so double rounding cannot see the midpoint.
        Assert.AreEqual(1.25, m_double.Calculate("round(1.255, 2)").Nvalue);
    }

    [TestMethod]
    public void Functions_Transcendental_ProduceDecimal() {
        var root = Exact("sqrt(2)");

        Assert.AreEqual(1.4142135623731m, Math.Round(root, 13));
    }
    #endregion

    #region Inputs
    [TestMethod]
    public void Variables_DoubleDictionary_AreConvertedExactly() {
        var result = m_decimal.Calculate("$price * $qty", new Dictionary<string, double> { ["$price"] = 0.1, ["$qty"] = 3 });

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(0.3m, result.Mvalue);
    }

    [TestMethod]
    public void Variables_DecimalValues_StayExact() {
        Assert.AreEqual(100.5m, Exact("$x * 100", new Dictionary<string, Value> { ["$x"] = Value.Number(1.005m) }));
    }

    [TestMethod]
    public void Variables_DecimalValues_InDoubleMode_UseDecimalArithmetic() {
        var result = m_double.Calculate("$a + $b", new Dictionary<string, Value> { ["$a"] = Value.Number(0.1m), ["$b"] = Value.Number(0.2m) });

        Assert.IsTrue(result.Value.IsDecimal);
        Assert.AreEqual(0.3m, result.Mvalue);
    }

    [TestMethod]
    public void Constants_AreConverted() {
        Assert.AreEqual(3.14159265358979m, Math.Round(Exact("pi"), 14));
    }

    [TestMethod]
    public void Strings_ConcatenateInvariantDecimal() {
        var result = m_decimal.Calculate("\"total: \" + (0.1 + 0.2)");

        Assert.AreEqual("total: 0.3", result.Svalue);
    }
    #endregion

    #region Errors
    [TestMethod]
    [DataRow("79228162514264337593543950335 * 10")]
    [DataRow("1e30")]
    [DataRow("28!")]
    [DataRow("ln(-1)")]
    [DataRow("10 ^ 40")]
    public void OutOfDecimalRange_IsValueOutOfRange(string expression) {
        var result = m_decimal.Calculate(expression);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ErrorCode.ValueOutOfRange, result.Errors[0].Code, result.Message);
    }

    [TestMethod]
    public void DivisionByZero_IsReported() {
        var result = m_decimal.Calculate("1 / (0.1 - 0.1)");

        Assert.AreEqual(ErrorCode.DivisionByZero, result.Errors[0].Code);
    }

    [TestMethod]
    public void NonFiniteVariable_IsValueOutOfRange() {
        var result = m_decimal.Calculate("$x + 1", new Dictionary<string, double> { ["$x"] = double.NaN });

        Assert.AreEqual(ErrorCode.ValueOutOfRange, result.Errors[0].Code);
    }
    #endregion

    #region Entry points
    [TestMethod]
    public void Compiled_HonorsDecimalMode() {
        var compiled = ShYCalculator.Compile("0.1 + 0.2", options: DecimalOptions);

        Assert.AreEqual(0.3m, compiled.Value!.Calculate().Mvalue);
    }

    [TestMethod]
    public void Builder_HonorsDecimalMode() {
        var calc = new ShYCalculatorBuilder().WithMathematics().WithOptions(o => o.NumericMode = NumericMode.Decimal).Build();

        Assert.AreEqual(0.6m, calc.Calculate("sum(0.1, 0.2, 0.3)").Mvalue);
    }

    [TestMethod]
    public void Ast_CarriesDecimalValues() {
        var result = m_decimal.Calculate("0.1 + 0.2", (IDictionary<string, Value>?)null, includeAst: true);

        Assert.AreEqual(0.3m, result.Ast!.EvaluatedValue);
    }

    [TestMethod]
    public void Ternary_And_ShortCircuit_Work() {
        Assert.AreEqual(0.3m, Exact("0.1 + 0.2 == 0.3 ? 0.1 + 0.2 : 0"));
        Assert.AreEqual(1m, Exact("if(0.1 + 0.2 == 0.3 && 1 > 0, 1, 2)"));
    }

    [TestMethod]
    public void Dates_ReturnDecimalNumbers() {
        Assert.AreEqual(2026m, Exact("dt_year(dt_create(2026, 1, 15))"));
    }
    #endregion

    #region Value API
    [TestMethod]
    public void Value_Conversions() {
        Assert.AreEqual(0.1, Value.Number(0.1m).Nvalue);
        Assert.AreEqual(0.1m, Value.Number(0.1).Mvalue);
        Assert.IsNull(Value.Number(double.NaN).Mvalue);
        Assert.IsNull(Value.Number(1e30).Mvalue);
        Assert.IsTrue(Value.Number(1m).IsDecimal);
        Assert.IsFalse(Value.Number(1d).IsDecimal);
        Assert.IsNull(Value.Boolean(true).Mvalue);
    }
    #endregion
}
