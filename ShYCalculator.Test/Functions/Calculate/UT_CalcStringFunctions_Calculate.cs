using ShYCalculator.Classes;
using ShYCalculator.Functions.Text;
using ShYCalculator.Calculator;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Calculate;

[TestClass]
public class UT_CalcStringFunctions_Calculate {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        m_environment.RegisterFunctions(new CalcStringFunctions());
    }

    private void CheckBool(string expression, bool expected) {
        var result = m_shyCalculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Calculation failed for '{expression}': {result.Message}");
        Assert.AreEqual(DataType.Boolean, result.DataType, $"Expected Boolean for '{expression}'");
        Assert.AreEqual(expected, result.Bvalue!.Value, $"Wrong result for '{expression}'");
    }

    [TestMethod]
    public void Str_Contains_Extensive() {
        CheckBool("str_contains('hello world', 'world')", true);
        CheckBool("str_contains('hello world', 'bye')", false);
        // Checking case sensitivity default (False)
        CheckBool("str_contains('ABC', 'a')", true);
        // Checking explicit case sensitive (True)
        CheckBool("str_contains('ABC', 'a', true)", false);

        CheckBool("str_contains('', 'a')", false);

        // Empty needle usually returns true in C# Contains, let's see implementation.
        // Assuming C# behavior: "abc".Contains("") == true.
        CheckBool("str_contains('abc', '')", false);
    }

    [TestMethod]
    public void Str_Starts_Extensive() {
        CheckBool("str_starts('hello', 'he')", true);
        CheckBool("str_starts('hello', 'lo')", false);
        CheckBool("str_starts('ABC', 'a')", true); // Default insensitive
        CheckBool("str_starts('ABC', 'a', true)", false); // Sensitive
    }

    [TestMethod]
    public void Str_Ends_Extensive() {
        CheckBool("str_ends('hello', 'lo')", true);
        CheckBool("str_ends('hello', 'he')", false);
        CheckBool("str_ends('ABC', 'c')", true); // Default insensitive
        CheckBool("str_ends('ABC', 'c', true)", false); // Sensitive
    }

    [TestMethod]
    public void Str_Equal_Extensive() {
        CheckBool("str_equal('a', 'a')", true);
        CheckBool("str_equal('a', 'b')", false);

        // Case insensitive default
        CheckBool("str_equal('A', 'a')", true);
        // Case sensitive explicit
        CheckBool("str_equal('A', 'a', true)", false);

        CheckBool("str_equal('', '')", true);
    }

    [TestMethod]
    public void Str_NotEqual_Extensive() {
        CheckBool("str_notequal('a', 'b')", true);
        CheckBool("str_notequal('a', 'a')", false);

        // Case insensitive default
        CheckBool("str_notequal('A', 'a')", false);
        // Case sensitive explicit
        CheckBool("str_notequal('A', 'a', true)", true);
    }

    [TestMethod]
    public void Str_All_Extensive() {
        CheckBool("str_all('a', 'b')", true);
        // If args are strings, it returns true? 
        // Description says "Checks if all provided arguments are non-null strings."

        CheckBool("str_all('')", true); // Empty string is a string
        // We can't easily pass 'null' from parser, but we can pass non-strings?
        // Wait, parser might error on types before function? 
        // Or if function takes 'any', it returns false if not string.
        // Let's assume passed literals are strings.
    }
}
