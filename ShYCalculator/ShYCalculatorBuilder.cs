// -----------------------------------------------------------------------------
// <summary>
//     Implements the Builder pattern for configuring ShYCalculator instances.
//     Allows fluent configuration of extensions, constants, and options.
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;
using ShY.Functions;
using ShY.Functions.Dates;
using ShY.Functions.Logical;
using ShY.Functions.Mathematics;
using ShY.Functions.Text;

namespace ShY;

/// <summary>
/// A fluent builder for configuring and creating <see cref="ShYCalculator"/> instances.
/// Allows setting up available functions (Math, Date, Text, Logic) and defining global constants.
/// </summary>
public class ShYCalculatorBuilder {
    // WHY: configuration is recorded and replayed into a fresh environment on every Build(), so built calculators never
    // observe later builder calls, and options (e.g. date culture) apply no matter where WithOptions appears in the chain.
    private readonly List<Action<Calculator.CalculatorEnvironment, ShYCalculatorOptions>> _steps = [];
    private readonly List<Action<ShYCalculatorOptions>> _optionSteps = [];

    /// <summary>
    /// Initializes a new instance of the builder with a clean environment (default constants, no functions).
    /// </summary>
    public ShYCalculatorBuilder() { }

    /// <summary>
    /// Enables all standard extension modules (Mathematics, Text, Date, Logical).
    /// </summary>
    public ShYCalculatorBuilder WithAllExtensions() => Step((env, options) => {
        env.RegisterFunctions(new CalcScientificFunctions());
        env.RegisterFunctions(new CalcArithmeticalFunctions());
        env.RegisterFunctions(new CalcNumericFunctions());
        env.RegisterFunctions(new CalcDateFunctions(options.DateFormat, options.CultureName, options.TimeProvider));
        env.RegisterFunctions(new CalcStringFunctions());
        env.RegisterFunctions(new CalcLogicalFunctions());
        env.AddConstant("pi", Math.PI);
        env.AddConstant("e", Math.E);
        env.AddConstant("true", true);
        env.AddConstant("false", false);
    });

    /// <summary>
    /// Enables mathematical functions (sin, cos, abs, etc.) and constants (pi, e).
    /// </summary>
    public ShYCalculatorBuilder WithMathematics() => Step((env, _) => {
        env.RegisterFunctions(new CalcScientificFunctions());
        env.RegisterFunctions(new CalcArithmeticalFunctions());
        env.RegisterFunctions(new CalcNumericFunctions());
        env.AddConstant("pi", Math.PI);
        env.AddConstant("e", Math.E);
    });

    /// <summary>
    /// Enables date manipulation functions.
    /// </summary>
    public ShYCalculatorBuilder WithDate() => Step((env, options) => env.RegisterFunctions(new CalcDateFunctions(options.DateFormat, options.CultureName, options.TimeProvider)));

    /// <summary>
    /// Enables string manipulation functions.
    /// </summary>
    public ShYCalculatorBuilder WithText() => Step((env, _) => env.RegisterFunctions(new CalcStringFunctions()));

    /// <summary>
    /// Enables logical functions (any, all, not) and constants (true, false).
    /// </summary>
    public ShYCalculatorBuilder WithLogic() => Step((env, _) => {
        env.RegisterFunctions(new CalcLogicalFunctions());
        env.AddConstant("true", true);
        env.AddConstant("false", false);
    });

    /// <summary>
    /// Adds a custom constant to the global scope.
    /// </summary>
    public ShYCalculatorBuilder WithConstant(string name, double value) => Step((env, _) => env.AddConstant(name, value));

    /// <summary>
    /// Adds a custom constant to the global scope.
    /// </summary>
    public ShYCalculatorBuilder WithConstant(string name, string value) => Step((env, _) => env.AddConstant(name, value));

    /// <summary>
    /// Adds a custom constant to the global scope.
    /// </summary>
    public ShYCalculatorBuilder WithConstant(string name, bool value) => Step((env, _) => env.AddConstant(name, value));

    /// <summary>
    /// Adds a custom constant to the global scope.
    /// </summary>
    public ShYCalculatorBuilder WithConstant(string name, DateTimeOffset value) => Step((env, _) => env.AddConstant(name, value));

    /// <summary>
    /// Registers a custom function extension module.
    /// </summary>
    public ShYCalculatorBuilder WithFunction(ICalcFunctionsExtension extension) => Step((env, _) => env.RegisterFunctions(extension));

    /// <summary>
    /// Configures calculator options such as limits, date format and culture.
    /// Applied at <see cref="Build"/> time, so the position in the chain does not matter.
    /// </summary>
    public ShYCalculatorBuilder WithOptions(Action<ShYCalculatorOptions> configure) {
        ArgumentNullException.ThrowIfNull(configure);
        _optionSteps.Add(configure);
        return this;
    }

    /// <summary>
    /// Creates a new <see cref="ShYCalculator"/> with its own snapshot of the configured environment.
    /// Later changes to this builder do not affect calculators already built.
    /// </summary>
    public ShYCalculator Build() {
        var options = new ShYCalculatorOptions();
        foreach (var configure in _optionSteps) configure(options);

        var environment = new Calculator.CalculatorEnvironment(null, new ShYCalculatorOptions { EnabledExtensions = FunctionExtensions.None });
        foreach (var step in _steps) step(environment, options);

        return new ShYCalculator(environment, options: options);
    }

    private ShYCalculatorBuilder Step(Action<Calculator.CalculatorEnvironment, ShYCalculatorOptions> step) {
        _steps.Add(step);
        return this;
    }
}
