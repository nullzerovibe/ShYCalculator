// -----------------------------------------------------------------------------
// <summary>
//     Contains all core interfaces for the calculator components (Tokenizer, Parser, Generator).
//     Facilitates dependency injection and testing mocking.
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;
using ShYCalculator.Functions;

namespace ShYCalculator.Classes;

/// <summary>
/// Defines the contract for tokenizing expression strings.
/// </summary>
public interface IExpressionTokenizer {
    /// <summary>
    /// Converts a string expression into a sequence of tokens.
    /// </summary>
    /// <param name="expression">The expression string to tokenize.</param>
    /// <returns>A result containing the list of tokens or error details.</returns>
    OpResult<IEnumerable<Token>> Tokenize(string expression);
}

/// <summary>
/// Defines the contract for converting infix tokens to RPN (Reverse Polish Notation) using the Shunting-Yard algorithm.
/// </summary>
public interface IShuntingYardGenerator {
    /// <summary>
    /// Generates RPN tokens from infix tokens.
    /// </summary>
    /// <param name="expressionTokens">The sequence of infix tokens.</param>
    /// <returns>A result containing the RPN tokens or error details.</returns>
    OpResult<IEnumerable<Token>> Generate(IEnumerable<Token> expressionTokens);
}

/// <summary>
/// Defines the contract for parsing/evaluating RPN tokens to produce a result.
/// </summary>
public interface IShuntingYardParser {
    /// <summary>
    /// Evaluates the RPN tokens against the global scope and context.
    /// </summary>
    /// <param name="rpnTokens">The RPN tokens to evaluate.</param>
    /// <param name="expression">The original expression string (for error context).</param>
    /// <param name="contextVariables">Optional runtime variables to use during evaluation.</param>
    /// <returns>The result of the evaluation.</returns>
    OpResult<Value> Evaluate(IEnumerable<Token> rpnTokens, string expression, IDictionary<string, Value>? contextVariables = null);
}

/// <summary>
/// Represents a compiled calculator instance that can execute a pre-parsed expression.
/// </summary>
public interface ICompiledCalculator {
    /// <summary>
    /// Gets the global environment scope (functions, constants) associated with this calculator.
    /// </summary>
    IGlobalScope Environment { get; }

    /// <summary>
    /// Calculates the result of the compiled expression using current environment state.
    /// </summary>
    /// <returns>The calculation result.</returns>
    CalculationResult Calculate();

    /// <summary>
    /// Calculates the result using provided variables.
    /// </summary>
    /// <param name="variables">Key-value pairs of variable names and values.</param>
    /// <returns>The calculation result.</returns>
    CalculationResult Calculate(IEnumerable<KeyValuePair<string, double>> variables);

    /// <summary>
    /// Calculates the result using provided variables dictionary.
    /// </summary>
    /// <param name="variables">Dictionary of variable names and values.</param>
    /// <returns>The calculation result.</returns>
    CalculationResult Calculate(Dictionary<string, double> variables);

    /// <summary>
    /// Calculates the result using a typed context dictionary.
    /// </summary>
    /// <param name="contextVariables">Dictionary of typed values.</param>
    /// <returns>The calculation result.</returns>
    CalculationResult Calculate(IDictionary<string, Value>? contextVariables);
}
/// <summary>
/// Defines a provider for mathematical and logical operators.
/// </summary>
public interface IOperatorProvider {
    /// <summary>
    /// Retrieves the dictionary of registered operators.
    /// </summary>
    /// <returns>A dictionary mapping operator symbols to Operator definitions.</returns>
    IDictionary<string, Operator> GetOperators();
}

/// <summary>
/// Defines the global scope containing functions, constants, and operators.
/// </summary>
public interface IGlobalScope {
    /// <summary>
    /// Gets the registered function extensions.
    /// </summary>
    IDictionary<string, ICalcFunctionsExtension> Functions { get; }

    /// <summary>
    /// Gets the registered global constants.
    /// </summary>
    IDictionary<string, Value> Constants { get; }

    /// <summary>
    /// Gets the registered operators.
    /// </summary>
    IDictionary<string, Operator> Operators { get; }

    /// <summary>
    /// Registers a function extension.
    /// </summary>
    /// <param name="extension">The extension to register.</param>
    /// <param name="overrideExistingFunctions">Whether to override existing functions with same names.</param>
    void RegisterFunctions(ICalcFunctionsExtension extension, bool overrideExistingFunctions = true);

    /// <summary>
    /// Adds a collection of typed constants.
    /// </summary>
    void AddConstants(Dictionary<string, Value> constants);

    /// <summary>
    /// Adds a collection of numeric constants.
    /// </summary>
    void AddConstants(Dictionary<string, double> constants);

    /// <summary>
    /// Adds a collection of boolean constants.
    /// </summary>
    void AddConstants(Dictionary<string, bool> constants);

    /// <summary>
    /// Adds a collection of string constants.
    /// </summary>
    void AddConstants(Dictionary<string, string> constants);

    /// <summary>
    /// Adds a collection of date constants.
    /// </summary>
    void AddConstants(Dictionary<string, DateTimeOffset> constants);

    /// <summary>
    /// Adds a single numeric constant.
    /// </summary>
    void AddConstant(string key, double value);

    /// <summary>
    /// Adds a single boolean constant.
    /// </summary>
    void AddConstant(string key, bool value);

    /// <summary>
    /// Adds a single string constant.
    /// </summary>
    void AddConstant(string key, string value);

    /// <summary>
    /// Adds a single date constant.
    /// </summary>
    void AddConstant(string key, DateTimeOffset value);
}

/// <summary>
/// Defines a context for runtime variable storage.
/// </summary>
public interface IContext {
    /// <summary>
    /// Gets the current variables in scope.
    /// </summary>
    IDictionary<string, Value> Variables { get; }

    /// <summary>
    /// Sets the variables context, replacing existing ones.
    /// </summary>
    void SetVariables(Dictionary<string, Value> variables);

    /// <summary>
    /// Adds numeric variables to the context.
    /// </summary>
    void AddVariables(Dictionary<string, double> variables);

    /// <summary>
    /// Adds boolean variables to the context.
    /// </summary>
    void AddVariables(Dictionary<string, bool> variables);

    /// <summary>
    /// Adds string variables to the context.
    /// </summary>
    void AddVariables(Dictionary<string, string> variables);

    /// <summary>
    /// Adds date variables to the context.
    /// </summary>
    void AddVariables(Dictionary<string, DateTimeOffset> variables);

    /// <summary>
    /// Adds a single numeric variable.
    /// </summary>
    void AddVariable(string key, double value);

    /// <summary>
    /// Adds a single boolean variable.
    /// </summary>
    void AddVariable(string key, bool value);

    /// <summary>
    /// Adds a single string variable.
    /// </summary>
    void AddVariable(string key, string value);

    /// <summary>
    /// Adds a single date variable.
    /// </summary>
    void AddVariable(string key, DateTimeOffset value);

    /// <summary>
    /// Clears all variables from the context.
    /// </summary>
    void ResetVariables();
}

/// <summary>
/// Represents the full execution environment comprising global scope and runtime context.
/// </summary>
public interface IEnvironment : IGlobalScope, IContext {
    /// <summary>
    /// Resets the entire environment (constants, functions, variables).
    /// </summary>
    void ResetEnvironment();

    /// <summary>
    /// Resets only the constants.
    /// </summary>
    void ResetConstants();

    /// <summary>
    /// Resets functions to defaults or configured options.
    /// </summary>
    /// <param name="options">Optional configuration for function reset.</param>
    void ResetFunctions(ShYCalculatorOptions? options = null);
}
