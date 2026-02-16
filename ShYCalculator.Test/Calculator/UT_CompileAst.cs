namespace ShYCalculator.Test.Calculator;

[TestClass]
public class UT_CompileAst {
    [TestMethod]
    public void Compile_WithAst_ReturnsAst() {
        // Arrange
        var expression = "1 + 2";

        // Act
        var result = ShYCalculator.Compile(expression, includeAst: true);

        // Assert
        Assert.IsTrue(result.Success);
        var ast = result.Value!.Ast;
        Assert.IsNotNull(ast);
        Assert.AreEqual("binary", ast.Type);
        Assert.AreEqual("+", ast.Operator);
    }

    [TestMethod]
    public void Compile_WithoutAst_ReturnsNullAst() {
        // Arrange
        var expression = "1 + 2";

        // Act
        var result = ShYCalculator.Compile(expression, includeAst: false);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNull(result.Value!.Ast);
    }

    [TestMethod]
    public void Compile_WithAst_ComplexExpression() {
        // Arrange
        var expression = "sin(max(1, 2))";

        // Act
        var result = ShYCalculator.Compile(expression, includeAst: true);

        // Assert
        Assert.IsTrue(result.Success);
        var ast = result.Value!.Ast;
        Assert.IsNotNull(ast);
        Assert.AreEqual("function", ast.Type);
        Assert.AreEqual("sin", ast.Name);
        Assert.IsNotNull(ast.Arguments);
        Assert.AreEqual(1, ast.Arguments.Count);
        Assert.AreEqual("function", ast.Arguments[0].Type);
        Assert.AreEqual("max", ast.Arguments[0].Name);
    }
}
