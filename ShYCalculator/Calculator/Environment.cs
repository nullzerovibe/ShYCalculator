// -----------------------------------------------------------------------------
// <summary>
//     Manages the execution context (variables, constants, functions).
//     Acts as a symbol table and state container for the calculator.
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;
using ShYCalculator.Functions;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Functions.Dates;
using ShYCalculator.Functions.Text;
using ShYCalculator.Functions.Logical;

namespace ShYCalculator.Calculator;

/// <summary>
/// Manages the execution context (variables, constants, functions).
/// Acts as a symbol table and state container for the calculator.
/// </summary>
public class Environment : IEnvironment {
    #region Members
    #endregion Members

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="Environment"/> class.
    /// </summary>
    /// <param name="operatorProvider">Optional provider for operators. if null, uses default registry.</param>
    /// <param name="options">Optional configuration options.</param>
    public Environment(IOperatorProvider? operatorProvider = null, ShYCalculatorOptions? options = null) {
        // Use OperatorRegistry as the default single source of truth
        Operators = operatorProvider?.GetOperators() ?? OperatorRegistry.Operators;

        ResetEnvironment(options);
    }
    #endregion Constructor

    #region Properties
    /// <summary>
    /// Gets the registered function extensions.
    /// </summary>
    public IDictionary<string, ICalcFunctionsExtension> Functions { get; private set; } = new Dictionary<string, ICalcFunctionsExtension>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets the registered global constants.
    /// </summary>
    public IDictionary<string, Value> Constants { get; private set; } = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets the current variables in scope.
    /// </summary>
    public IDictionary<string, Value> Variables { get; private set; } = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets the registered operators.
    /// </summary>
    public IDictionary<string, Operator> Operators { get; private set; } = new Dictionary<string, Operator>(StringComparer.OrdinalIgnoreCase);
    #endregion Properties

    #region Reset Methods
    /// <summary>
    /// Resets the environment to its default state (clears variables, constants, functions).
    /// </summary>
    public void ResetEnvironment() {
        ResetEnvironment(null);
    }

    /// <summary>
    /// Resets the environment with specific options.
    /// </summary>
    /// <param name="options">Configuration options for resetting (e.g., re-registering standard functions).</param>
    public void ResetEnvironment(ShYCalculatorOptions? options) {
        ResetVariables();
        ResetConstants();
        ResetFunctions(options);
    }

    /// <summary>
    /// Clears all variables from the context.
    /// </summary>
    public void ResetVariables() {
        Variables = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resets constants to default values (pi, e, true, false).
    /// </summary>
    public void ResetConstants() {
        Constants = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase) {
            ["pi"] = new Value() { Nvalue = Math.PI, DataType = DataType.Number },
            ["e"] = new Value() { Nvalue = Math.E, DataType = DataType.Number },
            ["true"] = new Value() { Bvalue = true, DataType = DataType.Boolean },
            ["false"] = new Value() { Bvalue = false, DataType = DataType.Boolean },
        };
    }

    /// <summary>
    /// Resets registered functions based on provided options.
    /// </summary>
    /// <param name="options">Options determining which function sets to register.</param>
    public void ResetFunctions(ShYCalculatorOptions? options = null) {
        Functions = new Dictionary<string, ICalcFunctionsExtension>(StringComparer.OrdinalIgnoreCase);
        // Default to All if no options provided
        var extensions = options?.EnabledExtensions ?? FunctionExtensions.All;

        if (extensions.HasFlag(FunctionExtensions.Mathematics)) {
            RegisterFunctions(new CalcScientificFunctions());
            RegisterFunctions(new CalcArithmeticalFunctions());
            RegisterFunctions(new CalcNumericFunctions());
        }

        if (extensions.HasFlag(FunctionExtensions.Date)) {
            RegisterFunctions(new CalcDateFunctions());
        }

        if (extensions.HasFlag(FunctionExtensions.Text)) {
            RegisterFunctions(new CalcStringFunctions());
        }

        if (extensions.HasFlag(FunctionExtensions.Logical)) {
            RegisterFunctions(new CalcLogicalFunctions());
        }
    }
    #endregion Reset Methods

    #region Main Public Methods
    /// <summary>
    /// Sets the variables context, replacing existing ones.
    /// </summary>
    /// <param name="variables">Dictionary of variables to set.</param>
    public void SetVariables(Dictionary<string, Value> variables) {
        Variables = variables;
    }

    /// <summary>
    /// Sets the constants, replacing existing ones.
    /// </summary>
    /// <param name="constants">Dictionary of constants to set.</param>
    public void SetConstants(Dictionary<string, Value> constants) {
        Constants = constants;
    }

    /// <summary>
    /// Registers a function extension.
    /// </summary>
    /// <param name="extension">The extension to register.</param>
    /// <param name="overrideExistingFunctions">Whether to override existing functions with same names.</param>
    public void RegisterFunctions(ICalcFunctionsExtension extension, bool overrideExistingFunctions = true) {
        try {
            var functions = extension.GetFunctions() ?? throw new CalcEnvironmentException($"Function issue");

            if (!functions.Any()) {
                throw new CalcEnvironmentException($"There are no Functions present durring RegisterFunctions on {extension.Name}");
            }

            foreach (var function in functions) {
                if (function.Name == null) {
                    throw new CalcEnvironmentException($"Function without Name present durring RegisterFunctions on {extension.Name}");
                }

                if (Functions.ContainsKey(function.Name)) {
                    if (!overrideExistingFunctions) {
                        throw new CalcEnvironmentException($"RegisterFunctions issue. Function with Name {function.Name} already registered. {extension.Name}");
                    }

                    // Removing old function so we can add the new one
                    Functions.Remove(function.Name);
                }

                Functions.Add(function.Name, extension);
            }
        }
        catch (Exception ex) {
            throw new CalcEnvironmentException($"Unexpected Exception Registering Functions from {extension.Name}", ex);
        }

    }
    #endregion Main Public Methods

    #region Variable Helper Public Methods
    /// <summary>
    /// Adds numeric variables to the context.
    /// </summary>
    /// <param name="variables">Dictionary of numeric variables.</param>
    public void AddVariables(Dictionary<string, double> variables) {
        foreach (var variable in variables) {
            AddVariable(variable.Key, variable.Value);
        }
    }

    /// <summary>
    /// Adds boolean variables to the context.
    /// </summary>
    /// <param name="variables">Dictionary of boolean variables.</param>
    public void AddVariables(Dictionary<string, bool> variables) {
        foreach (var variable in variables) {
            AddVariable(variable.Key, variable.Value);
        }
    }

    /// <summary>
    /// Adds string variables to the context.
    /// </summary>
    /// <param name="variables">Dictionary of string variables.</param>
    public void AddVariables(Dictionary<string, string> variables) {
        foreach (var variable in variables) {
            AddVariable(variable.Key, variable.Value);
        }
    }

    /// <summary>
    /// Adds date variables to the context.
    /// </summary>
    /// <param name="variables">Dictionary of date variables.</param>
    public void AddVariables(Dictionary<string, DateTimeOffset> variables) {
        foreach (var variable in variables) {
            AddVariable(variable.Key, variable.Value);
        }
    }

    /// <summary>
    /// Adds a single numeric variable.
    /// </summary>
    /// <param name="key">Variable name.</param>
    /// <param name="value">Numeric value.</param>
    public void AddVariable(string key, double value) {
        Variables[key] = new Value() {
            DataType = DataType.Number,
            Nvalue = value,
        };
    }

    /// <summary>
    /// Adds a single boolean variable.
    /// </summary>
    /// <param name="key">Variable name.</param>
    /// <param name="value">Boolean value.</param>
    public void AddVariable(string key, bool value) {
        Variables[key] = new Value() {
            DataType = DataType.Boolean,
            Bvalue = value,
        };
    }

    /// <summary>
    /// Adds a single string variable.
    /// </summary>
    /// <param name="key">Variable name.</param>
    /// <param name="value">String value.</param>
    public void AddVariable(string key, string value) {
        Variables[key] = new Value() {
            DataType = DataType.String,
            Svalue = value,
        };
    }

    /// <summary>
    /// Adds a single date variable.
    /// </summary>
    /// <param name="key">Variable name.</param>
    /// <param name="value">Date value.</param>
    public void AddVariable(string key, DateTimeOffset value) {
        Variables[key] = new Value() {
            DataType = DataType.Date,
            Dvalue = value,
        };
    }
    #endregion Variable Helper Public Methods

    #region Constant Helper Public Methods
    /// <summary>
    /// Adds a collection of typed constants.
    /// </summary>
    /// <param name="constants">Dictionary of typed constants.</param>
    public void AddConstants(Dictionary<string, Value> constants) {
        foreach (var constant in constants) {
            Constants[constant.Key] = constant.Value;
        }
    }

    /// <summary>
    /// Adds a collection of numeric constants.
    /// </summary>
    /// <param name="constants">Dictionary of numeric constants.</param>
    public void AddConstants(Dictionary<string, double> constants) {
        foreach (var constant in constants) {
            AddConstant(constant.Key, constant.Value);
        }
    }

    /// <summary>
    /// Adds a collection of boolean constants.
    /// </summary>
    /// <param name="constants">Dictionary of boolean constants.</param>
    public void AddConstants(Dictionary<string, bool> constants) {
        foreach (var constant in constants) {
            AddConstant(constant.Key, constant.Value);
        }
    }

    /// <summary>
    /// Adds a collection of string constants.
    /// </summary>
    /// <param name="constants">Dictionary of string constants.</param>
    public void AddConstants(Dictionary<string, string> constants) {
        foreach (var constant in constants) {
            AddConstant(constant.Key, constant.Value);
        }
    }

    /// <summary>
    /// Adds a collection of date constants.
    /// </summary>
    /// <param name="constants">Dictionary of date constants.</param>
    public void AddConstants(Dictionary<string, DateTimeOffset> constants) {
        foreach (var constant in constants) {
            AddConstant(constant.Key, constant.Value);
        }
    }

    /// <summary>
    /// Adds a single numeric constant.
    /// </summary>
    /// <param name="key">Constant name.</param>
    /// <param name="value">Numeric value.</param>
    public void AddConstant(string key, double value) {
        Constants[key] = new Value() {
            DataType = DataType.Number,
            Nvalue = value,
        };
    }

    /// <summary>
    /// Adds a single boolean constant.
    /// </summary>
    /// <param name="key">Constant name.</param>
    /// <param name="value">Boolean value.</param>
    public void AddConstant(string key, bool value) {
        Constants[key] = new Value() {
            DataType = DataType.Boolean,
            Bvalue = value,
        };
    }

    /// <summary>
    /// Adds a single string constant.
    /// </summary>
    /// <param name="key">Constant name.</param>
    /// <param name="value">String value.</param>
    public void AddConstant(string key, string value) {
        Constants[key] = new Value() {
            DataType = DataType.String,
            Svalue = value,
        };
    }

    /// <summary>
    /// Adds a single date constant.
    /// </summary>
    /// <param name="key">Constant name.</param>
    /// <param name="value">Date value.</param>
    public void AddConstant(string key, DateTimeOffset value) {
        Constants[key] = new Value() {
            DataType = DataType.Date,
            Dvalue = value,
        };
    }
    #endregion Constant Helper Public Methods
}
