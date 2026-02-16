// -----------------------------------------------------------------------------
// <summary>
//     Represents a pre-compiled expression ready for repeated execution.
//     Optimizes performance by avoiding reparsing of the same formula.
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Calculator;
using ShYCalculator.Classes;

namespace ShYCalculator;

internal class ShYCompiledCalculator : ICompiledCalculator {
    #region Members
    internal readonly IExpressionTokenizer m_expTokenizer;
    internal readonly IShuntingYardGenerator m_shyGenerator;
    internal readonly IShuntingYardParser m_shyParser;

    private OpResult<IEnumerable<Token>> m_tokenizerResult;
    private OpResult<IEnumerable<Token>> m_generatorResult;

    private string m_expression;
    private bool m_compiled;

    private IEnumerable<Token> m_cachedExpressionTokens = [];
    private IEnumerable<Token> m_cachedRPNTokens = [];
    #endregion Members

    #region Constructor
    internal ShYCompiledCalculator(IGlobalScope? globalScope = null, ShYCalculatorOptions? options = null) {
        // If globalScope is null, create new Environment (which is IGlobalScope)
        Environment = globalScope ?? new Calculator.Environment(null, options);

        m_expTokenizer = new ExpressionTokenizer(Environment);
        m_shyGenerator = new ShuntingYardGenerator(Environment);
        m_shyParser = new ShuntingYardParser(Environment);
        m_tokenizerResult = default;
        m_generatorResult = default;
        m_expression = "";
        m_compiled = false;
    }
    #endregion Constructor

    #region Properties
    public IGlobalScope Environment { get; internal set; }

    public string Message {
        get {
            if (!m_tokenizerResult.Success) return m_tokenizerResult.Message;
            if (!m_generatorResult.Success) return m_generatorResult.Message;
            return "";
        }
    }

    /// <inheritdoc />
    public AstNode? Ast { get; private set; }

    public IEnumerable<CalcError> Errors {
        get {
            if (!m_tokenizerResult.Success) return m_tokenizerResult.Errors;
            if (!m_generatorResult.Success) return m_generatorResult.Errors;
            return [];
        }
    }
    #endregion Properties

    #region Methods
    public bool Compile(string expression, bool includeAst = false) {
        m_expression = expression;
        m_compiled = false;
        m_tokenizerResult = default;
        m_generatorResult = default;

        try {
            m_tokenizerResult = m_expTokenizer.Tokenize(m_expression);
            if (!m_tokenizerResult.Success) {
                return false;
            }

            m_generatorResult = m_shyGenerator.Generate(m_tokenizerResult.Value!);
            if (!m_generatorResult.Success) {
                return false;
            }

            m_cachedExpressionTokens = m_tokenizerResult.Value!;
            m_cachedRPNTokens = m_generatorResult.Value!;

            if (includeAst) {
                var builder = new AstBuilder(Environment);
                // We don't have runtime variables at compile time, so we pass null for context
                // This means the AST won't have fully evaluated values for variables, which is expected
                Ast = builder.Build(m_generatorResult.Value!, null);
                
                if (Ast.Type == "error") {
                     // Should we fail compilation if AST fails? 
                     // The generator succeeded, but AST builder found an issue (e.g. stack mismatch not caught by generator?)
                     // For now, let's treat it as a success but AST indicates error
                }
            } else {
                Ast = null;
            }

            m_compiled = true;
            return true;
        }
        catch (BaseCalcException) {
            return false;
        }
        catch (Exception) {
            return false;
        }
    }

    public void Reset() {
        m_tokenizerResult = default;
        m_generatorResult = default;
        m_expression = "";
        m_compiled = false;
        Ast = null;
    }

    public CalculationResult Calculate() {
        return Calculate((Dictionary<string, Value>?)null);
    }

    public CalculationResult Calculate(IEnumerable<KeyValuePair<string, double>> variables) {
        var contextWrapper = new DoubleDictionaryWrapper(variables);
        return Calculate(contextWrapper);
    }

    public CalculationResult Calculate(Dictionary<string, double> variables) {
        return Calculate((IEnumerable<KeyValuePair<string, double>>)variables);
    }

    public CalculationResult Calculate(IDictionary<string, Value>? contextVariables) {
        try {
            if (m_compiled == false || !m_generatorResult.Success) {
                return new CalculationResult() {
                    Success = false,
                    Message = "Expression not compiled or compilation failed.",
                    Expression = m_expression,
                };
            }

            // Fallback logic for compiled calculator too
            IDictionary<string, Value>? effectiveContext = contextVariables;
            // Only check for variables if the scope supports them (is IContext)
            if (Environment is IContext context && context.Variables.Count > 0) {
                if (contextVariables == null) {
                    effectiveContext = context.Variables;
                }
                else {
                    effectiveContext = new CompositeDictionary(contextVariables, context.Variables);
                }
            }

            var evaluationResult = m_shyParser.Evaluate(m_generatorResult.Value!, m_expression, effectiveContext);

            return new CalculationResult() {
                Success = evaluationResult.Success,
                Message = evaluationResult.Message,
                Errors = evaluationResult.Errors?.ToArray() ?? [],
                Expression = m_expression,
                InternalExpressionTokens = m_cachedExpressionTokens,
                InternalRPNTokens = m_cachedRPNTokens,
                Value = evaluationResult.Value
            };
        }
        catch (BaseCalcException ex) {
            return new CalculationResult() {
                Success = false,
                Message = ex.Message,
                Expression = m_expression,
            };
        }
        catch (Exception ex) {
            return new CalculationResult() {
                Success = false,
                Message = ex.Message,
                Expression = m_expression,
            };
        }
    }
    #endregion Methods
}


