using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShYCalculator.Classes;

namespace ShYCalculator.Test
{
    [TestClass]
    public class UT_Validation
    {
        [TestMethod]
        public void Test_ValidExpression()
        {
            var result = ShYCalculator.Compile("1 + 2");
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public void Test_IncompleteExpression_TrailingOperator()
        {
            var result = ShYCalculator.Compile("4 +");
            Assert.IsFalse(result.Success, "Expression '4 +' should fail validation.");
            // We expect MissingOperand, but currently it might pass or fail differently
        }

        [TestMethod]
        public void Test_Mixed_Parentheses()
        {
            var result = ShYCalculator.Compile("4 + (1 +");
            Assert.IsFalse(result.Success);
        }
        [TestMethod]
        public void Test_InvalidSyntax_CorruptedExpressions()
        {
            var invalidExpressions = new Dictionary<string, string>
            {
                { "1 +", "Missing right operand" },
                { "1 + 2 +", "Trailing operator" },
                { "(1 + 2", "Missing closing parenthesis" },
                { "1 + 2)", "Missing opening parenthesis" },
                { "max(1, 2", "Missing closing parenthesis on function" },
                { "max 1, 2)", "Missing opening parenthesis on function" },
                { "min(1,)", "Trailing comma in function args" },
                { "1 < 2 ? 3 :", "Missing false branch" },
                { "1 < 2 ? : 4", "Missing true branch" },
                { "1 < 2 ? 3", "Missing colon and false branch" },
                { "* 2", "Missing left operand" },
                { "1 *", "Missing right operand" },
                { "1 + * 3", "Double operator (missing operand between)" },
                { "(1 + 2 * 3", "Mismatched parenthesis nested" },
                { "if(true, 1, 0", "Missing closing paren on if" },
                { "if(true, 1,)", "Missing false branch arg" },
                { "1 2", "Missing operator" },
                { "max(1 2)", "Missing comma" },
                { "+", "Operator only" },
                { "3 -", "Missing right operand" },
                { "7 %", "Missing right operand" },
                { "2 ^", "Missing right operand" },
                { "!", "Missing operand for unary" }
            };

            foreach (var entry in invalidExpressions)
            {
                var expression = entry.Key;
                var reason = entry.Value;
                var result = ShYCalculator.Compile(expression);
                Assert.IsFalse(result.Success, $"Expression '{expression}' should fail validation. Reason: {reason}. Got Success=true.");
            }
        }
    }
}
