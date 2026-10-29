using Microsoft.Extensions.DependencyInjection;
using ShY.Classes;
using ShY.DependencyInjection;

namespace ShY.Test;

[TestClass]
public class UT_07_DependencyInjection {
    [TestMethod]
    public void TestAddShYCalculator_ResolvesServices() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddShYCalculator(builder => {
            builder.WithMathematics(); // Configure with Math functions
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var calculator = provider.GetService<ShYCalculator>();
        Assert.IsNotNull(calculator);

        var globalScope = provider.GetService<IGlobalScope>();
        Assert.IsNotNull(globalScope);

        // Verify Math functions are registered
        Assert.IsTrue(globalScope.Functions.ContainsKey("sin"));

        Assert.IsNotNull(provider.GetService<ShYCalculatorOptions>());
        Assert.AreEqual(1, calculator.Calculate("sin(pi / 2)").Nvalue);
    }

    [TestMethod]
    public void TestAddShYCalculator_TransientCalculator_SingletonScope() {
        // Arrange
        var services = new ServiceCollection();
        services.AddShYCalculator();
        var provider = services.BuildServiceProvider();

        // Act
        var calc1 = provider.GetRequiredService<ShYCalculator>();
        var calc2 = provider.GetRequiredService<ShYCalculator>();
        var scope1 = provider.GetRequiredService<IGlobalScope>();

        // Assert
        Assert.AreNotSame(calc1, calc2); // Calculator is transient
        Assert.AreSame(calc1.Environment, calc2.Environment); // Environment is singleton
        Assert.AreSame(calc1.Environment, scope1);
    }
}
