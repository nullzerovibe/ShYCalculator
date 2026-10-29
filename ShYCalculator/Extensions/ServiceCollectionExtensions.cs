// -----------------------------------------------------------------------------
// <summary>
//     Extension methods for registering ShYCalculator with Microsoft.Extensions.DependencyInjection.
// </summary>
// -----------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;
using ShY.Classes;
using ShY.Calculator;

namespace ShY.DependencyInjection;

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
        var builder = new ShYCalculatorBuilder();
        configure?.Invoke(builder);
        var configured = builder.Build();

        // WHY: definitions (functions, constants, options) are shared singletons; calculators are cheap transient facades.
        // Mutating the shared IEnvironment after startup affects every consumer and is not thread-safe.
        services.AddSingleton<IGlobalScope>(configured.Environment);
        services.AddSingleton<IEnvironment>(sp => (IEnvironment)sp.GetRequiredService<IGlobalScope>());
        services.AddSingleton(configured.Options);

        services.AddTransient<ShYCalculator>();

        return services;
    }
}
