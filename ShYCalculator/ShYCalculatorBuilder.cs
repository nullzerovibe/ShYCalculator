// -----------------------------------------------------------------------------
// <summary>
//     Implements the Builder pattern for configuring ShYCalculator instances.
//     Allows fluent configuration of extensions, constants, and options.
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Calculator;
using ShYCalculator.Classes;
using ShYCalculator.Functions;
using ShYCalculator.Functions.Dates;
using ShYCalculator.Functions.Logical;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions.Text;

namespace ShYCalculator;

/// <summary>
/// A fluent builder for configuring and creating <see cref="ShYCalculator"/> instances.
/// Allows setting up available functions (Math, Date, Text, Logic) and defining global constants.
/// </summary>
public class ShYCalculatorBuilder {
    private readonly Calculator.Environment _environment;

    /// <summary>
    /// Initializes a new instance of the builder with a clean environment.
    /// </summary>
    public ShYCalculatorBuilder() {
        _environment = new Calculator.Environment();
        // Start with clean slate for builder
        _environment.ResetFunctions(new ShYCalculatorOptions { EnabledExtensions = FunctionExtensions.None });
        _environment.ResetConstants();
        _environment.ResetVariables();
    }

    /// <summary>
    /// Enables all standard extension modules (Mathematics, Text, Date, Logical).
    /// </summary>
    public ShYCalculatorBuilder WithAllExtensions() {
        _environment.RegisterFunctions(new CalcScientificFunctions());
        _environment.RegisterFunctions(new CalcArithmeticalFunctions());
        _environment.RegisterFunctions(new CalcNumericFunctions());
        _environment.RegisterFunctions(new CalcDateFunctions());
        _environment.RegisterFunctions(new CalcStringFunctions());
        _environment.RegisterFunctions(new CalcLogicalFunctions());

        // Add default constants
        _environment.AddConstant("pi", Math.PI);
        _environment.AddConstant("e", Math.E);
        _environment.AddConstant("true", true);
        _environment.AddConstant("false", false);
        return this;
    }

    /// <summary>
    /// Enables mathematical functions (sin, cos, abs, etc.) and constants (pi, e).
    /// </summary>
    public ShYCalculatorBuilder WithMathematics() {
        _environment.RegisterFunctions(new CalcScientificFunctions());
        _environment.RegisterFunctions(new CalcArithmeticalFunctions());
        _environment.RegisterFunctions(new CalcNumericFunctions());
        _environment.AddConstant("pi", Math.PI);
        _environment.AddConstant("e", Math.E);
        return this;
    }

    /// <summary>
    /// Enables date manipulation functions.
    /// </summary>
    public ShYCalculatorBuilder WithDate() {
        _environment.RegisterFunctions(new CalcDateFunctions());
        return this;
    }

    /// <summary>
    /// Enables string manipulation functions.
    /// </summary>
    public ShYCalculatorBuilder WithText() {
        _environment.RegisterFunctions(new CalcStringFunctions());
        return this;
    }

    /// <summary>
    /// Enables logical functions (any, all, not) and constants (true, false).
    /// </summary>
    public ShYCalculatorBuilder WithLogic() {
        _environment.RegisterFunctions(new CalcLogicalFunctions());
        _environment.AddConstant("true", true);
        _environment.AddConstant("false", false);
        return this;
    }

    /// <summary>
    /// Adds a custom constant to the global scope.
    /// </summary>
    public ShYCalculatorBuilder WithConstant(string name, double value) {
        _environment.AddConstant(name, value);
        return this;
    }

    /// <summary>
    /// Adds a custom constant to the global scope.
    /// </summary>
    public ShYCalculatorBuilder WithConstant(string name, string value) {
        _environment.AddConstant(name, value);
        return this;
    }

    /// <summary>
    /// Adds a custom constant to the global scope.
    /// </summary>
    public ShYCalculatorBuilder WithConstant(string name, bool value) {
        _environment.AddConstant(name, value);
        return this;
    }

    /// <summary>
    /// Adds a custom constant to the global scope.
    /// </summary>
    public ShYCalculatorBuilder WithConstant(string name, DateTimeOffset value) {
        _environment.AddConstant(name, value);
        return this;
    }

    /// <summary>
    /// Registers a custom function extension module.
    /// </summary>
    public ShYCalculatorBuilder WithFunction(ICalcFunctionsExtension extension) {
        _environment.RegisterFunctions(extension);
        return this;
    }

    /// <summary>
    /// Creates a new <see cref="ShYCalculator"/> instance with the configured environment.
    /// </summary>
    public ShYCalculator Build() {
        return new ShYCalculator(_environment);
    }
}
