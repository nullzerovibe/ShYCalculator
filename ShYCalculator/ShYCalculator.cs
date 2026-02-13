// -----------------------------------------------------------------------------
// <summary>
//     The main entry point for the ShYCalculator library.
//     Provides a thread-safe, stateless expression evaluator based on the Shunting-Yard algorithm.
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Calculator;
using ShYCalculator.Classes;

namespace ShYCalculator;

/// <summary>
/// The main thread-safe Shunting-Yard Calculator engine.
/// Provides methods to evaluate mathematical expressions using a configured environment.
/// </summary>
public class ShYCalculator {
    private readonly IExpressionTokenizer m_tokenizer;
    private readonly IShuntingYardGenerator m_generator;
    private readonly IShuntingYardParser m_parser;
    
    /// <summary>
    /// Gets the Global Scope (Functions, Constants) used by this calculator.
    /// </summary>
    public IEnvironment Environment { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShYCalculator"/> class.
    /// Recommended to use <see cref="ShYCalculatorBuilder"/> instead.
    /// </summary>
    public ShYCalculator(IEnvironment? environment = null, IExpressionTokenizer? tokenizer = null, IShuntingYardGenerator? generator = null, IShuntingYardParser? parser = null, ShYCalculatorOptions? options = null) {
        Environment = environment ?? new Calculator.Environment(null, options);
        m_tokenizer = tokenizer ?? new ExpressionTokenizer(Environment);
        m_generator = generator ?? new ShuntingYardGenerator(Environment);
        m_parser = parser ?? new ShuntingYardParser(Environment);
    }

    #region Methods
    /// <summary>
    /// Compiles an expression into an efficient <see cref="ICompiledCalculator"/> for repeated execution.
    /// </summary>
    /// <param name="expression">The mathematical expression to compile.</param>
    /// <param name="globalScope">Optional scope definitions. Defaults to a standard environment.</param>
    /// <param name="options">Optional calculator options.</param>
    /// <returns>A result containing the compiled calculator instance.</returns>
    public static Result<ICompiledCalculator> Compile(string expression, IGlobalScope? globalScope = null, ShYCalculatorOptions? options = null) {
        var compiledCalc = new ShYCompiledCalculator(globalScope, options);
        if (!compiledCalc.Compile(expression)) {
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

    /// <summary>
    /// Evaluates the given expression using a set of variables.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">A collection of variables (name/value pairs) to use in the calculation.</param>
    public CalculationResult Calculate(string expression, IEnumerable<KeyValuePair<string, double>> variables) {
        var contextWrapper = new DoubleDictionaryWrapper(variables);
        return Calculate(expression, contextWrapper);
    }

    // Retained for backward compatibility but implemented efficiently
    /// <summary>
    /// Evaluates the given expression using a dictionary of numeric variables.
    /// Retained for backward compatibility.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">Dictionary of numeric variables.</param>
    /// <returns>The result of the calculation.</returns>
    public CalculationResult Calculate(string expression, Dictionary<string, double> variables) {
         // Re-use the interface-based overload to avoid code duplication, 
         // though specific optimization for exact Dictionary type could be done if needed.
         // For now, wrappers are struct-based so overhead is minimal.
         return Calculate(expression, (IEnumerable<KeyValuePair<string, double>>)variables);
    }

    /// <summary>
    /// Evaluates the given expression using a full context of variables (including mixed types).
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="contextVariables">A dictionary of <see cref="Value"/> objects representing variables.</param>
    public CalculationResult Calculate(string expression, IDictionary<string, Value>? contextVariables) {
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
                } else {
                    effectiveContext = new CompositeDictionary(contextVariables, Environment.Variables);
                }
            }

            var evaluationResult = m_parser.Evaluate(shyGeneratorResult.Value!, expression, effectiveContext);
            if (!evaluationResult.Success) {
                return new CalculationResult() {
                    Success = false,
                    Message = evaluationResult.Message,
                    Errors = evaluationResult.Errors?.ToArray() ?? [],
                    Expression = expression,
                    InternalExpressionTokens = expTokenizerResult.Value,
                    InternalRPNTokens = shyGeneratorResult.Value,
                };
            }

            // We need to copy values from evaluationResult.Value to CalculationResult
            return new CalculationResult() {
                Success = true,
                Message = evaluationResult.Message,
                Expression = expression,
                InternalExpressionTokens = expTokenizerResult.Value,
                InternalRPNTokens = shyGeneratorResult.Value,
                Value = evaluationResult.Value, 
            };
        }
        catch (BaseCalcException ex) {
            return new CalculationResult() {
                Success = false,
                Message = ex.Message,
                Expression = expression,
            };
        }
        catch (Exception ex) {
            return new CalculationResult() {
                Success = false,
                Message = ex.Message,
                Errors = [new CalcError(ErrorCode.UnexpectedException, ex.Message)],
                Expression = expression,
            };
        }
    }
    #endregion Methods
}


