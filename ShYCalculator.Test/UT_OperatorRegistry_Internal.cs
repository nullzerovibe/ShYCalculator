using ShYCalculator.Calculator;
using ShYCalculator.Classes;

namespace ShYCalculator.Test;

[TestClass]
public class UT_OperatorRegistry_Internal {
    [TestMethod]
    public void OperatorDef_PropertyCoverage() {
        // 1. Instantiate (covers constructor and initial setters)
        var op = new OperatorRegistry.OperatorDef(
            "Test",
            "Test",
            OperatorKind.None,
            1,
            Associativity.Left,
            Category.Grouping,
            DataType.Boolean,
            IsUnaryPrefix: true,
            IsUnaryPostfix: true
        );

        // 2. Getters coverage
        Assert.AreEqual("Test", op.Key);
        Assert.AreEqual(OperatorKind.None, op.Kind);
        Assert.AreEqual(1, op.Precedence);
        Assert.AreEqual(Associativity.Left, op.Associativity);
        Assert.AreEqual(Category.Grouping, op.Category);
        Assert.AreEqual(DataType.Boolean, op.ValidDataTypes);
        Assert.IsTrue(op.IsUnaryPrefix);
        Assert.IsTrue(op.IsUnaryPostfix);

        // 3. Setters coverage (via 'with' expressions for init-only properties)
        // Each 'with' triggers the specific property setter and copy constructor mechanism
        var op2 = op with { Key = "Test2" };
        Assert.AreEqual("Test2", op2.Key);

        var op3 = op with { Kind = OperatorKind.Add };
        Assert.AreEqual(OperatorKind.Add, op3.Kind);

        var op4 = op with { Precedence = 100 };
        Assert.AreEqual(100, op4.Precedence);

        var op5 = op with { Associativity = Associativity.Right };
        Assert.AreEqual(Associativity.Right, op5.Associativity);

        var op6 = op with { Category = Category.Arithmetic };
        Assert.AreEqual(Category.Arithmetic, op6.Category);

        var op7 = op with { ValidDataTypes = DataType.Number };
        Assert.AreEqual(DataType.Number, op7.ValidDataTypes);

        var op8 = op with { IsUnaryPrefix = false };
        Assert.IsFalse(op8.IsUnaryPrefix);

        var op9 = op with { IsUnaryPostfix = false };
        Assert.IsFalse(op9.IsUnaryPostfix);
    }
}
