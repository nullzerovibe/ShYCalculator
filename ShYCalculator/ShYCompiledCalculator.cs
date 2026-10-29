// -----------------------------------------------------------------------------
// <summary>
//     Represents a pre-compiled expression ready for repeated execution.
//     Optimizes performance by avoiding reparsing of the same formula.
// </summary>
// -----------------------------------------------------------------------------
using ShY.Calculator;
using ShY.Classes;

namespace ShY;

internal class ShYCompiledCalculator : ICompiledCalculator {
    #region Members
    internal readonly IExpressionTokenizer m_expTokenizer;
    internal readonly IShuntingYardGenerator m_shyGenerator;
    internal readonly IShuntingYardParser m_shyParser;

    private OpResult<IEnumerable<Token>> m_tokenizerResult;
    private OpResult<IEnumerable<Token>> m_generatorResult;

    private string m_expression;
    private bool m_compiled;
    private readonly ShYCalculatorOptions? m_options;
    private CalcError? m_compileError;

    private IEnumerable<Token> m_cachedExpressionTokens = [];
    private IEnumerable<Token> m_cachedRPNTokens = [];
    #endregion Members

    #region Constructor
    internal ShYCompiledCalculator(IGlobalScope? globalScope = null, ShYCalculatorOptions? options = null) {
        m_options = options;
        // If globalScope is null, create new Environment (which is IGlobalScope)
        Environment = globalScope ?? new Calculator.CalculatorEnvironment(null, options);

        m_expTokenizer = new ExpressionTokenizer(Environment, options);
        m_shyGenerator = new ShuntingYardGenerator(Environment, options);
        m_shyParser = new ShuntingYardParser(Environment, options);
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
            if (m_compileError != null) return m_compileError.Message;
            if (!m_tokenizerResult.Success) return m_tokenizerResult.Message ?? "";
            if (!m_generatorResult.Success) return m_generatorResult.Message ?? "";
            return "";
        }
    }

    /// <inheritdoc />
    public AstNode? Ast { get; private set; }

    public IEnumerable<CalcError> Errors {
        get {
            if (m_compileError != null) return [m_compileError];
            if (!m_tokenizerResult.Success) return m_tokenizerResult.Errors ?? [];
            if (!m_generatorResult.Success) return m_generatorResult.Errors ?? [];
            return [];
        }
    }
    #endregion Properties

    #region Methods
    public bool Compile(string expression, bool includeAst = false) {
        m_expression = expression;
        m_compiled = false;
        m_compileError = null;
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

            // WHY: no runtime variables exist at compile time, so variable nodes are annotated with null.
            Ast = includeAst ? new AstBuilder(Environment, m_options).Build(m_generatorResult.Value!, null) : null;

            m_compiled = true;
            return true;
        }
        catch (Exception ex) {
            m_compileError = CalcError.FromException(ex);
            return false;
        }
    }

    public void Reset() {
        m_tokenizerResult = default;
        m_generatorResult = default;
        m_expression = "";
        m_compiled = false;
        m_compileError = null;
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
                    Errors = [new CalcError(ErrorCode.InvalidExpression, "Expression not compiled or compilation failed.")],
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
        catch (Exception ex) {
            var error = CalcError.FromException(ex);
            return new CalculationResult() {
                Success = false,
                Message = error.Message,
                Errors = [error],
                Expression = m_expression,
            };
        }
    }
    #endregion Methods
}


