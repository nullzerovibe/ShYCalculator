using ShYCalculator.Classes;

namespace ShYCalculator.Test;

[TestClass]
public class UT_ShYCalculator_API {
    private ShYCalculator m_shyCalculator = null!;

    [TestInitialize]
    public void TestInitialize() {
        m_shyCalculator = new ShYCalculator();
    }

    [TestMethod]
    public void Calculate_WithDictionaryDouble_ShouldSucceed() {
        var variables = new Dictionary<string, double> {
            { "a", 10.5 },
            { "b", 20.5 }
        };

        var result = m_shyCalculator.Calculate("a + b", variables);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(31.0, result.Nvalue);
    }

    [TestMethod]
    public void Calculate_WithIEnumerableKeyValuePair_ShouldSucceed() {
        var variables = new List<KeyValuePair<string, double>> {
            new("x", 5),
            new("y", 5)
        };

        // Explicit cast to ensure we pick the IEnumerable overload if possible, 
        // though strictly it matches the same overload as Dictionary<string, double> would if mapped to IEnumerable
        var result = m_shyCalculator.Calculate("x * y", variables);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(25.0, result.Nvalue);
    }

    [TestMethod]
    public void Compile_ValidExpression_ShouldReturnSuccess() {
        var result = ShYCalculator.Compile("1 + 1");
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Value);

        var calcResult = result.Value.Calculate();
        Assert.IsTrue(calcResult.Success);
        Assert.AreEqual(2.0, calcResult.Nvalue);
    }

    [TestMethod]
    public void Compile_InvalidExpression_ShouldReturnFailure() {
        var result = ShYCalculator.Compile("(1 + 2"); // Mismatched parentheses fails Generator
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Errors);
        Assert.IsTrue(result.Errors.Any());
    }
}
