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

    /// <summary>
    /// Evaluates the given expression using a set of variables.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">A collection of variables (name/value pairs) to use in the calculation.</param>
    /// <param name="includeAst">If true, the result will include the AST.</param>
    public CalculationResult Calculate(string expression, IEnumerable<KeyValuePair<string, double>> variables, bool includeAst = false) {
        var contextWrapper = new DoubleDictionaryWrapper(variables);
        return Calculate(expression, contextWrapper, includeAst);
    }

    // Retained for backward compatibility but implemented efficiently
    /// <summary>
    /// Evaluates the given expression using a dictionary of numeric variables.
    /// Retained for backward compatibility.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">Dictionary of numeric variables.</param>
    /// <param name="includeAst">If true, the result will include the AST.</param>
    public CalculationResult Calculate(string expression, Dictionary<string, double> variables, bool includeAst = false) {
        // Re-use the interface-based overload to avoid code duplication, 
        // though specific optimization for exact Dictionary type could be done if needed.
        // For now, wrappers are struct-based so overhead is minimal.
        return Calculate(expression, (IEnumerable<KeyValuePair<string, double>>)variables, includeAst);
    }

    /// <summary>
    /// Evaluates the given expression using a full context of variables (including mixed types).
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="contextVariables">A dictionary of <see cref="Value"/> objects representing variables.</param>
    /// <param name="includeAst">If true, the result will include the AST.</param>
    public CalculationResult Calculate(string expression, IDictionary<string, Value>? contextVariables, bool includeAst = false) {
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

            // Normal evaluation path if AST is not requested
            if (!includeAst) {
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

                return new CalculationResult() {
                    Success = true,
                    Message = evaluationResult.Message,
                    Expression = expression,
                    InternalExpressionTokens = expTokenizerResult.Value,
                    InternalRPNTokens = shyGeneratorResult.Value,
                    Value = evaluationResult.Value,
                };
            } 
            else {
                // AST requests use the builder which also evaluates
                var builder = new AstBuilder(Environment);
                var rootNode = builder.Build(shyGeneratorResult.Value!, effectiveContext);
                
                if (rootNode.Type == "error") {
                     return new CalculationResult() {
                        Success = false,
                        Message = (string)rootNode.EvaluatedValue!,
                        Expression = expression,
                        InternalExpressionTokens = expTokenizerResult.Value,
                        InternalRPNTokens = shyGeneratorResult.Value,
                    };
                }
                
                // Convert AST evaluated value back to Value struct
                Value finalValue = CreateValueFromObject(rootNode.EvaluatedValue);

                return new CalculationResult() {
                    Success = true,
                    Expression = expression,
                    InternalExpressionTokens = expTokenizerResult.Value,
                    InternalRPNTokens = shyGeneratorResult.Value,
                    Value = finalValue,
                    Ast = rootNode
                };
            }
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
    
    private Value CreateValueFromObject(object? obj) {
        if (obj == null) return Value.Null(DataType.Number);
        if (obj is double d) return Value.Number(d);
        if (obj is bool b) return Value.Boolean(b);
        if (obj is string s) return Value.String(s);
        if (obj is DateTimeOffset dt) return Value.Date(dt);
        if (obj is int i) return Value.Number(i);
        return Value.Null(DataType.Number);
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

            var builder = new AstBuilder(Environment);
            var rootNode = builder.Build(shyGeneratorResult.Value!, effectiveContext);
            
            if (rootNode.Type == "error") {
                 return Result<AstNode>.Fail((string)rootNode.EvaluatedValue!);
            }

            return Result<AstNode>.Ok(rootNode);
        }
        catch (Exception ex) {
            return Result<AstNode>.Fail(ex.Message, [new CalcError(ErrorCode.UnexpectedException, ex.Message)]);
        }
    }
    #endregion Methods
}


