// -----------------------------------------------------------------------------
// <summary>
//     The main entry point for the ShYCalculator library.
//     Provides a thread-safe, stateless expression evaluator based on the Shunting-Yard algorithm.
// </summary>
// -----------------------------------------------------------------------------
using ShY.Calculator;
using ShY.Classes;

namespace ShY;

/// <summary>
/// The main thread-safe Shunting-Yard Calculator engine.
/// Provides methods to evaluate mathematical expressions using a configured environment.
/// </summary>
/// <remarks>
/// <see cref="Calculate(string)"/> and its overloads are safe to call concurrently. Mutating <see cref="Environment"/>
/// (adding variables or constants, registering or resetting functions) is not, and must not overlap with calculations.
/// Configure the environment up front and pass per-call data as context variables.
/// </remarks>
public class ShYCalculator {
    private readonly IExpressionTokenizer m_tokenizer;
    private readonly IShuntingYardGenerator m_generator;
    private readonly IShuntingYardParser m_parser;

    /// <summary>
    /// Gets the Global Scope (Functions, Constants) used by this calculator.
    /// </summary>
    public IEnvironment Environment { get; private set; }

    /// <summary>
    /// Gets the options this calculator was created with (limits, date format, culture).
    /// </summary>
    public ShYCalculatorOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShYCalculator"/> class.
    /// Recommended to use <see cref="ShYCalculatorBuilder"/> instead.
    /// </summary>
    /// <param name="environment">Functions, constants and variables; a default environment is created when null.</param>
    /// <param name="options">Limits, date format, culture and clock; defaults apply when null.</param>
    public ShYCalculator(IEnvironment? environment = null, ShYCalculatorOptions? options = null)
        : this(environment, null, null, null, options) {
    }

    // WHY: pipeline components are implementation details; substitution is kept for internal tests only.
    internal ShYCalculator(IEnvironment? environment, IExpressionTokenizer? tokenizer, IShuntingYardGenerator? generator, IShuntingYardParser? parser, ShYCalculatorOptions? options) {
        Environment = environment ?? new Calculator.CalculatorEnvironment(null, options);
        Options = options ?? new ShYCalculatorOptions();
        m_tokenizer = tokenizer ?? new ExpressionTokenizer(Environment, Options);
        m_generator = generator ?? new ShuntingYardGenerator(Environment, Options);
        m_parser = parser ?? new ShuntingYardParser(Environment, Options);
    }

    #region Methods
    /// <summary>
    /// Compiles an expression into an efficient <see cref="ICompiledCalculator"/> for repeated execution.
    /// </summary>
    /// <param name="expression">The mathematical expression to compile.</param>
    /// <param name="globalScope">Optional scope definitions. Defaults to a standard environment.</param>
    /// <param name="options">Optional calculator options.</param>
    /// <param name="includeAst">If true, the compiled result will include the AST.</param>
    /// <returns>A result containing the compiled calculator instance.</returns>
    public static Result<ICompiledCalculator> Compile(string expression, IGlobalScope? globalScope = null, ShYCalculatorOptions? options = null, bool includeAst = false) {
        var compiledCalc = new ShYCompiledCalculator(globalScope, options);
        if (!compiledCalc.Compile(expression, includeAst)) {
            return Result<ICompiledCalculator>.Fail(compiledCalc.Message, compiledCalc.Errors);
        }
        return Result<ICompiledCalculator>.Ok(compiledCalc);
    }

    /// <summary>
    /// Evaluates the given expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate (e.g., "2 + 2").</param>
    /// <returns>The result of the calculation.</returns>
    public CalculationResult Calculate(string expression) {
        return Calculate(expression, (IDictionary<string, Value>?)null);
    }

    // WHY: no overload carries optional parameters; explicit pairs keep overload resolution unambiguous and let future
    // parameters be added compatibly (Roslyn RS0026/RS0027).
    /// <summary>
    /// Evaluates the given expression, optionally including the AST.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="includeAst">If true, the result will include the AST.</param>
    public CalculationResult Calculate(string expression, bool includeAst) =>
        Calculate(expression, (IDictionary<string, Value>?)null, includeAst);

    /// <summary>
    /// Evaluates the given expression using a full context of variables (including mixed types).
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="contextVariables">A dictionary of <see cref="Value"/> objects representing variables.</param>
    public CalculationResult Calculate(string expression, IDictionary<string, Value>? contextVariables) =>
        Calculate(expression, contextVariables, false);

    /// <summary>
    /// Evaluates the given expression using a set of numeric variables.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">A collection of variables (name/value pairs) to use in the calculation.</param>
    public CalculationResult Calculate(string expression, IEnumerable<KeyValuePair<string, double>> variables) =>
        Calculate(expression, variables, false);

    /// <summary>
    /// Evaluates the given expression using a set of numeric variables.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">A collection of variables (name/value pairs) to use in the calculation.</param>
    /// <param name="includeAst">If true, the result will include the AST.</param>
    public CalculationResult Calculate(string expression, IEnumerable<KeyValuePair<string, double>> variables, bool includeAst) =>
        Calculate(expression, new DoubleDictionaryWrapper(variables), includeAst);

    /// <summary>
    /// Evaluates the given expression using a dictionary of numeric variables.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">Dictionary of numeric variables.</param>
    public CalculationResult Calculate(string expression, Dictionary<string, double> variables) =>
        Calculate(expression, variables, false);

    /// <summary>
    /// Evaluates the given expression using a dictionary of numeric variables.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">Dictionary of numeric variables.</param>
    /// <param name="includeAst">If true, the result will include the AST.</param>
    public CalculationResult Calculate(string expression, Dictionary<string, double> variables, bool includeAst) =>
        Calculate(expression, new DoubleDictionaryWrapper(variables), includeAst);

    /// <summary>
    /// Evaluates the given expression using a full context of variables (including mixed types).
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="contextVariables">A dictionary of <see cref="Value"/> objects representing variables.</param>
    /// <param name="includeAst">If true, the result will include the AST.</param>
    public CalculationResult Calculate(string expression, IDictionary<string, Value>? contextVariables, bool includeAst) {
        try {
            var expTokenizerResult = m_tokenizer.Tokenize(expression);
            if (!expTokenizerResult.Success) {
                return new CalculationResult() {
                    Success = false,
                    Message = expTokenizerResult.Message,
                    Errors = expTokenizerResult.Errors?.ToArray() ?? [],
                    Expression = expression,
                };
            }

            var shyGeneratorResult = m_generator.Generate(expTokenizerResult.Value!);
            if (!shyGeneratorResult.Success) {
                return new CalculationResult() {
                    Success = false,
                    Message = shyGeneratorResult.Message,
                    Errors = shyGeneratorResult.Errors?.ToArray() ?? [],
                    Expression = expression,
                    InternalExpressionTokens = expTokenizerResult.Value,
                    InternalRPNTokens = shyGeneratorResult.Value,
                };
            }

            // Fallback logic: If Environment has variables, include them in the context
            IDictionary<string, Value>? effectiveContext = contextVariables;
            if (Environment.Variables.Count > 0) {
                if (contextVariables == null) {
                    effectiveContext = Environment.Variables;
                }
                else {
                    effectiveContext = new CompositeDictionary(contextVariables, Environment.Variables);
                }
            }

            var evaluationResult = m_parser.Evaluate(shyGeneratorResult.Value!, expression, effectiveContext);

            return new CalculationResult() {
                Success = evaluationResult.Success,
                Message = evaluationResult.Message,
                Errors = evaluationResult.Errors?.ToArray() ?? [],
                Expression = expression,
                InternalExpressionTokens = expTokenizerResult.Value,
                InternalRPNTokens = shyGeneratorResult.Value,
                Value = evaluationResult.Value,
                Ast = includeAst ? new AstBuilder(Environment, Options).Build(shyGeneratorResult.Value!, effectiveContext) : null,
            };
        }
        catch (Exception ex) {
            var error = CalcError.FromException(ex);
            return new CalculationResult() {
                Success = false,
                Message = error.Message,
                Errors = [error],
                Expression = expression,
            };
        }
    }

    /// <summary>
    /// Generates an Abstract Syntax Tree (AST) for the given expression, including position metadata and evaluated values.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <param name="contextVariables">Optional variables to use for evaluation.</param>
    /// <returns>A result containing the root AST node.</returns>
    public Result<AstNode> GetAst(string expression, IDictionary<string, Value>? contextVariables = null) {
        try {
            var expTokenizerResult = m_tokenizer.Tokenize(expression);
            if (!expTokenizerResult.Success) {
                return Result<AstNode>.Fail(expTokenizerResult.Message, expTokenizerResult.Errors);
            }

            var shyGeneratorResult = m_generator.Generate(expTokenizerResult.Value!);
            if (!shyGeneratorResult.Success) {
                return Result<AstNode>.Fail(shyGeneratorResult.Message, shyGeneratorResult.Errors);
            }

            // Fallback logic: If Environment has variables, include them in the context
            IDictionary<string, Value>? effectiveContext = contextVariables;
            if (Environment.Variables.Count > 0) {
                if (contextVariables == null) {
                    effectiveContext = Environment.Variables;
                }
                else {
                    effectiveContext = new CompositeDictionary(contextVariables, Environment.Variables);
                }
            }

            var builder = new AstBuilder(Environment, Options);
            var rootNode = builder.Build(shyGeneratorResult.Value!, effectiveContext);
            
            if (rootNode.Type == "error") {
                 return Result<AstNode>.Fail((string)rootNode.EvaluatedValue!);
            }

            return Result<AstNode>.Ok(rootNode);
        }
        catch (Exception ex) {
            var error = CalcError.FromException(ex);
            return Result<AstNode>.Fail(error.Message, [error]);
        }
    }
    #endregion Methods
}


