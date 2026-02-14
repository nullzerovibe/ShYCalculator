// -----------------------------------------------------------------------------
// <summary>
//     Extension methods for registering ShYCalculator with Microsoft.Extensions.DependencyInjection.
// </summary>
// -----------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;
using ShYCalculator.Classes;
using ShYCalculator.Calculator;

namespace ShYCalculator.DependencyInjection;

/// <summary>
/// Extension methods for registering ShYCalculator services.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Adds ShYCalculator services to the IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configure">Optional action to configure the calculator (e.g. add extensions, constants).</param>
    /// <returns>The IServiceCollection for chaining.</returns>
    public static IServiceCollection AddShYCalculator(this IServiceCollection services, Action<ShYCalculatorBuilder>? configure = null) {
        // Create a builder and configure it
        var builder = new ShYCalculatorBuilder();
        configure?.Invoke(builder);

        // Build a temporary calculator to extract the configured environment
        // Ideally, the builder should expose the environment or configuration without building the whole calculator,
        // but for now, we build it to get the fully configured Environment instance.
        var tempCalc = builder.Build();
        var configuredEnvironment = tempCalc.Environment;

        // Register the configured GlobalScope (Environment) as Singleton
        // This means all injected calculators will share the same functions/constants/operators definition.
        // If users need different scopes, they should use different containers or manual instantiation.
        services.AddSingleton<IGlobalScope>(configuredEnvironment);

        // Also register IEnvironment if needed, but IGlobalScope is the preferred interface for definitions
        services.AddSingleton<IEnvironment>(sp => (IEnvironment)sp.GetRequiredService<IGlobalScope>());

        // Register Core Components using factory delegates because implementations are internal
        services.AddTransient<IExpressionTokenizer>(sp => new ExpressionTokenizer(sp.GetRequiredService<IGlobalScope>()));
        services.AddTransient<IShuntingYardGenerator>(sp => new ShuntingYardGenerator(sp.GetRequiredService<IGlobalScope>()));
        services.AddTransient<IShuntingYardParser>(sp => new ShuntingYardParser(sp.GetRequiredService<IGlobalScope>()));

        // Register the Calculator itself
        // It's lightweight enough to be Transient, but Scoped is also fine.
        // Since it holds no state other than the dependencies (which are Singletons/Transients) and potentially 
        // a default context if we added one (but we didn't), Transient is safest.
        services.AddTransient<ShYCalculator>();

        return services;
    }
}
