using ShYCalculator.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShYCalculator.Test.Integration.Operators;

[TestClass]
public class UT_Ternary {
    private readonly ShYCalculator m_calculator = new();

    private void CheckDoubleResult(string expression, double expected) {
        var result = m_calculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Expression '{expression}' failed: {result.Message}");
        Assert.AreEqual(expected, result.Value.Nvalue!.Value, 1e-10);
    }

    [TestMethod]
    public void Ternary_Basic() {
        CheckDoubleResult("1 < 2 ? 3 : 4", 3);
        CheckDoubleResult("1 > 2 ? 3 : 4", 4);
        CheckDoubleResult("true ? 3 : 4", 3);
        CheckDoubleResult("false ? 3 : 4", 4);
    }

    [TestMethod]
    public void Ternary_WithLogic() {
        CheckDoubleResult("1 < 2 && 5 > 3 ? 3 : 4", 3);
        CheckDoubleResult("1 < 2 && 5 < 3 ? 3 : 4", 4);
        CheckDoubleResult("1 > 2 || 5 < 3 ? 3 : 4", 4);
        CheckDoubleResult("3 / 3 < 2 && 5 - 1 > 3 && 1 + 2 == 3 ? 3 * 3 : 4", 9);
    }

    [TestMethod]
    public void Ternary_Nested_RightAssociativity() {
        // a ? b : c ? d : e  -> a ? b : (c ? d : e)
        CheckDoubleResult("true ? 1 : true ? 2 : 3", 1);
        CheckDoubleResult("false ? 1 : true ? 2 : 3", 2);
        CheckDoubleResult("false ? 1 : false ? 2 : 3", 3);

        CheckDoubleResult("1 < 2 ? 4 < 5 ? 3 : 4 : 5", 3);
        CheckDoubleResult("1 < 2 ? 4 > 5 ? 3 : 4 : 5", 4);
        CheckDoubleResult("1 > 2 ? 4 > 5 ? 3 : 4 : 5", 5);
    }
}
