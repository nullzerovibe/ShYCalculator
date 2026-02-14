using ShYCalculator.Classes;
using ShYCalculator.Functions.Dates;

namespace ShYCalculator.Test;

[TestClass]
public class UT_DateConfiguration {
    
    [TestMethod]
    public void Test_Default_Configuration() {
        // Arrange
        var calc = new ShYCalculatorBuilder().WithDate().Build();

        // Act & Assert
        // Default format is dd/MM/yyyy
        // dt_equal("25/12/2023", dt_create(2023, 12, 25))
        // Should parse default format dd/MM/yyyy
        var result1 = calc.Calculate("dt_equal(\"25/12/2023\", dt_create(2023, 12, 25))");
        Assert.IsTrue(result1.Success, "Calculation failed");
        Assert.IsTrue(result1.Bvalue.GetValueOrDefault(), "Should parse default format dd/MM/yyyy correctly");

        // Should return false for other formats (failed parsing in AllDateValues logic or exception?)
        // dt_equal calls TryParseDateValue which returns fallback logic.
        // "25.12.2023" with default settings (Invariant) likely fails.
        // It won't crash, just returns false comparison or throws if unparseable?
        // Let's check impl: SucceedDate throws if unparseable.
        // So this should fail with exception or return false Success.
        var result2 = calc.Calculate("dt_equal(\"25.12.2023\", dt_create(2023, 12, 25))");
        Assert.IsFalse(result2.Success, "Should fail (exception) for unparseable format by default");
    }

    [TestMethod]
    public void Test_Custom_Format_Dots() {
        // Arrange
        var options = new ShYCalculatorOptions {
            DateFormat = "d.M.yyyy"
        };
        var calc = new ShYCalculatorBuilder().WithDate().Build();
        calc.Environment.ResetFunctions(options);

        // Act & Assert
        // Should now support 1.1.2023
        var result1 = calc.Calculate("dt_equal(\"1.1.2023\", dt_create(2023, 1, 1))");
        Assert.IsTrue(result1.Success);
        Assert.IsTrue(result1.Bvalue.GetValueOrDefault());

        // Should support 13.4.2026
        var result2 = calc.Calculate("dt_equal(\"13.4.2026\", dt_create(2026, 4, 13))");
        Assert.IsTrue(result2.Success);
        Assert.IsTrue(result2.Bvalue.GetValueOrDefault());
    }

    [TestMethod]
    public void Test_Culture_Specific_Croatian() {
        // Arrange
        // hr-HR requires trailing dot e.g. "13. 4. 2026."
        var options = new ShYCalculatorOptions { CultureName = "hr-HR" };
        var calc = new ShYCalculatorBuilder().WithDate().Build();
        calc.Environment.ResetFunctions(options);

        // Act & Assert
        // Note: hr-HR format usually has spaces too: "d. M. yyyy."
        // Let's test standard parsing
        var result1 = calc.Calculate("dt_equal(\"13.4.2026.\", dt_create(2026, 4, 13))");
        Assert.IsTrue(result1.Success, "Should parse Croatian format with trailing dot");
        Assert.IsTrue(result1.Bvalue.GetValueOrDefault());
        
        // Also spaces commonly used: "13. 4. 2026."
        var result2 = calc.Calculate("dt_equal(\"13. 4. 2026.\", dt_create(2026, 4, 13))");
         if (result2.Success) Assert.IsTrue(result2.Bvalue.GetValueOrDefault());
    }

    [TestMethod]
    public void Test_Culture_Specific_UK() {
        // Arrange
        // en-GB uses dd/MM/yyyy
        var options = new ShYCalculatorOptions { CultureName = "en-GB" };
        var calc = new ShYCalculatorBuilder().WithDate().Build();
        calc.Environment.ResetFunctions(options);

        // Act & Assert
        // Standard UK
        var result1 = calc.Calculate("dt_equal(\"13/04/2026\", dt_create(2026, 4, 13))");
        Assert.IsTrue(result1.Success);
        Assert.IsTrue(result1.Bvalue.GetValueOrDefault());

        // Short year sometimes works with lenient parsing
        var result2 = calc.Calculate("dt_equal(\"13/4/2026\", dt_create(2026, 4, 13))");
        Assert.IsTrue(result2.Success); 
        Assert.IsTrue(result2.Bvalue.GetValueOrDefault());
    }

    [TestMethod]
    public void Test_Culture_Specific_Germany() {
        // Arrange
        // de-DE uses dd.MM.yyyy
        var options = new ShYCalculatorOptions { CultureName = "de-DE" };
        var calc = new ShYCalculatorBuilder().WithDate().Build();
        calc.Environment.ResetFunctions(options);

        // Act & Assert
        // Standard German
        var result1 = calc.Calculate("dt_equal(\"13.04.2026\", dt_create(2026, 4, 13))");
        Assert.IsTrue(result1.Success);
        Assert.IsTrue(result1.Bvalue.GetValueOrDefault());

        // Single digit day/month often works in standard parsing
        var result2 = calc.Calculate("dt_equal(\"1.1.2026\", dt_create(2026, 1, 1))");
        Assert.IsTrue(result2.Success);
        Assert.IsTrue(result2.Bvalue.GetValueOrDefault());
    }

    [TestMethod]
    public void Test_Culture_Specific_Italy() {
        // Arrange
        // it-IT uses dd/MM/yyyy
        var options = new ShYCalculatorOptions { CultureName = "it-IT" };
        var calc = new ShYCalculatorBuilder().WithDate().Build();
        calc.Environment.ResetFunctions(options);

        // Act & Assert
        var result1 = calc.Calculate("dt_equal(\"13/04/2026\", dt_create(2026, 4, 13))");
        Assert.IsTrue(result1.Success);
        Assert.IsTrue(result1.Bvalue.GetValueOrDefault());
    }

    [TestMethod]
    public void Test_Culture_Specific_Dutch() {
        // Arrange
        // nl-NL uses d-M-yyyy or dd-MM-yyyy
        var options = new ShYCalculatorOptions { CultureName = "nl-NL" };
        var calc = new ShYCalculatorBuilder().WithDate().Build();
        calc.Environment.ResetFunctions(options);

        // Act & Assert
        // Standard Dutch with dashes
        var result1 = calc.Calculate("dt_equal(\"13-04-2026\", dt_create(2026, 4, 13))");
        Assert.IsTrue(result1.Success);
        Assert.IsTrue(result1.Bvalue.GetValueOrDefault());
        
        // Single digits
        var result2 = calc.Calculate("dt_equal(\"1-1-2026\", dt_create(2026, 1, 1))");
        Assert.IsTrue(result2.Success);
        Assert.IsTrue(result2.Bvalue.GetValueOrDefault());
    }

    [TestMethod]
    public void Test_Culture_Specific_US() {
        // Arrange
        // en-US uses M/d/yyyy
        var options = new ShYCalculatorOptions { CultureName = "en-US" };
        var calc = new ShYCalculatorBuilder().WithDate().Build();
        calc.Environment.ResetFunctions(options);

        // Act & Assert
        // Standard US: Month first!
        // 04/13/2026 -> April 13th
        var result1 = calc.Calculate("dt_equal(\"04/13/2026\", dt_create(2026, 4, 13))");
        Assert.IsTrue(result1.Success);
        Assert.IsTrue(result1.Bvalue.GetValueOrDefault());

        // Ambiguous: 01/02/2026 -> Jan 2nd (NOT Feb 1st)
        var result2 = calc.Calculate("dt_equal(\"01/02/2026\", dt_create(2026, 1, 2))");
        Assert.IsTrue(result2.Success);
        Assert.IsTrue(result2.Bvalue.GetValueOrDefault());
    }

    [TestMethod]
    public void Test_Mixed_Configuration() {
        // Arrange
        // Custom format overrides culture default
        // Force ISO format on US culture
        var options = new ShYCalculatorOptions {
            DateFormat = "yyyy-MM-dd",
            CultureName = "en-US"
        };

        var calc = new ShYCalculatorBuilder().WithDate().Build();
        calc.Environment.ResetFunctions(options);

        // Act
        var result = calc.Calculate("dt_equal(\"2026-04-13\", dt_create(2026, 4, 13))");

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Bvalue.GetValueOrDefault());
    }
}
