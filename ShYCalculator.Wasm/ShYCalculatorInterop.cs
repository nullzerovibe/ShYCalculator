using Microsoft.JSInterop;
using ShYCalculator.Classes;

namespace ShYCalculator.Wasm;

/// <summary>
/// Exposes ShYCalculator functionality to WebAssembly via JS Interop.
/// </summary>
public class ShYCalculatorInterop {
    private readonly ShYCalculator _calculator = new ShYCalculator();

    /// <summary>
    /// Simple ping method to verify interop connection.
    /// </summary>
    /// <returns>A pong message from the instance.</returns>
    [JSInvokable]
    public string Ping() => "Pong from ShYCalculator.Wasm Instance";

    /// <summary>
    /// Configures the date format and culture for the calculator.
    /// </summary>
    /// <param name="format">Date format string (e.g. "dd/MM/yyyy").</param>
    /// <param name="culture">Culture name (e.g. "en-US").</param>
    /// <returns>Success message.</returns>
    [JSInvokable]
    public string ConfigureDates(string format, string culture) {
        var options = new ShYCalculatorOptions {
            DateFormat = format,
            CultureName = culture
        };
        _calculator.Environment.ResetFunctions(options);
        return $"Dates configured: {format} ({culture})";
    }

    /// <summary>
    /// Calculates the result of a mathematical expression.
    /// This method is callable from JavaScript.
    /// </summary>
    /// <param name="expression">The expression string to evaluate.</param>
    /// <param name="includeAst">Whether to return the AST in the result.</param>
    /// <returns>The result of the calculation.</returns>
    [JSInvokable]
    public CalculationResult Calculate(string expression, bool includeAst = false) {
        return _calculator.Calculate(expression, (IDictionary<string, Value>?)null, includeAst);
    }

    /// <summary>
    /// Validates an expression without executing it.
    /// Checks for syntax errors and compilation issues.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <returns>A JSON string representing the Result (Success, Message, Errors).</returns>
    [JSInvokable]
    public string ValidateExpression(string expression) {
        var result = ShYCalculator.Compile(expression, _calculator.Environment);
        // We return a serialized Result object to the frontend
        return System.Text.Json.JsonSerializer.Serialize(new {
            result.Success,
            result.Message,
            Errors = result.Errors?.Select(e => new {
                e.Code,
                e.Message,
                e.StartIndex,
                e.Length,
                Severity = e.Severity.ToString()
            })
        });
    }

    /// <summary>
    /// Calculates the result of a mathematical expression using provided variables.
    /// Supports numbers, booleans, and strings.
    /// </summary>
    /// <param name="expression">The mathematical expression to evaluate.</param>
    /// <param name="variables">A dictionary of variable names and their values (can be number, bool, string).</param>
    /// <param name="includeAst">Whether to return the AST in the result.</param>
    /// <returns>The result of the calculation.</returns>
    [JSInvokable]
    public CalculationResult CalculateWithVars(string expression, Dictionary<string, object> variables, bool includeAst = false) {
        var context = new Dictionary<string, Value>();

        foreach (var kvp in variables) {
            context[kvp.Key] = ConvertToValue(kvp.Value);
        }

        return _calculator.Calculate(expression, context, includeAst);
    }

    private Value ConvertToValue(object obj) {
        if (obj is System.Text.Json.JsonElement json) {
            switch (json.ValueKind) {
                case System.Text.Json.JsonValueKind.Number:
                return Value.Number(json.GetDouble());
                case System.Text.Json.JsonValueKind.True:
                return Value.Boolean(true);
                case System.Text.Json.JsonValueKind.False:
                return Value.Boolean(false);
                case System.Text.Json.JsonValueKind.String:
                return Value.String(json.GetString()!);
                default:
                return Value.String(json.ToString());
            }
        }
        else if (obj is double d) return Value.Number(d);
        else if (obj is float f) return Value.Number(f);
        else if (obj is int i) return Value.Number(i);
        else if (obj is long l) return Value.Number(l);
        else if (obj is bool b) return Value.Boolean(b);
        else if (obj is string s) return Value.String(s);

        return Value.String(obj?.ToString() ?? "");
    }

    /// <summary>
    /// Retrieves the documentation for functions and operators.
    /// </summary>
    /// <returns>A JSON string containing the documentation.</returns>
    [JSInvokable]
    public string GetDocumentation() {
        var functions = _calculator.Environment.Functions.Values
            .Distinct()
            .SelectMany(ext => {
                // Extract category from extension name (e.g. CalcArithmeticalFunctions -> Arithmetical)
                var category = ext.Name.Replace("Calc", "").Replace("Functions", "");
                return ext.GetFunctions().Select(f => new { Func = f, Category = category });
            })
            .Select(x => new {
                x.Func.Name,
                x.Func.Description,
                x.Func.Examples,
                Arguments = x.Func.Arguments?.Select(a => $"{a.Type}{(a.Optional ? "?" : "")}").ToList(),
                x.Category
            })
            .OrderBy(f => f.Name)
            .ToList();

        // Manual injection: if function
        functions.Add(new {
            Name = (string?)"if",
            Description = (string?)"Evaluates a condition and returns the first value if true, otherwise the second value.",
            Examples = (List<string>?)new List<string> { "if(5 > 3, 10, 0)", "if(x, 'Yes', 'No')" },
            Arguments = (List<string>?)new List<string> { "boolean", "any", "any" },
            Category = "Logical"
        });

        functions = functions.OrderBy(f => f.Name).ToList();

        var operators = _calculator.Environment.Operators
            .Select(o => new {
                Symbol = o.Key,
                o.Value.Name,
                o.Value.Precedence,
                Associativity = o.Value.Associativity.ToString(),
                Category = o.Value.Category.ToString()
            })
            .OrderByDescending(o => o.Precedence)
            .ToList();

        var doc = new {
            functions,
            operators
        };

        return System.Text.Json.JsonSerializer.Serialize(doc);
    }
    /// <summary>
    /// Retrieves the AST of an expression as a JSON string.
    /// </summary>
    /// <param name="expression">The mathematical expression to analyze.</param>
    /// <returns>A JSON string representing the AST node.</returns>
    [JSInvokable]
    public string GetAst(string expression) {
        var result = _calculator.GetAst(expression);
        
        if (!result.Success) {
            return System.Text.Json.JsonSerializer.Serialize(new {
                error = result.Message,
                details = result.Errors
            });
        }
        
        // Use a permissive serializer options to handle object? types correctly if needed,
        // though default might be fine given AstNode structure.
        return System.Text.Json.JsonSerializer.Serialize(result.Value, new System.Text.Json.JsonSerializerOptions { 
            WriteIndented = false, 
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Retrieves the AST of an expression as a JSON string, using provided variables for evaluation.
    /// </summary>
    /// <param name="expression">The mathematical expression to analyze.</param>
    /// <param name="variables">A dictionary of variable names and their values.</param>
    /// <returns>A JSON string representing the AST node.</returns>
    [JSInvokable]
    public string GetAstWithVars(string expression, Dictionary<string, object> variables) {
        var context = new Dictionary<string, Value>();

        foreach (var kvp in variables) {
            context[kvp.Key] = ConvertToValue(kvp.Value);
        }

        var result = _calculator.GetAst(expression, context);
        
        if (!result.Success) {
            return System.Text.Json.JsonSerializer.Serialize(new {
                error = result.Message,
                details = result.Errors
            });
        }
        
        return System.Text.Json.JsonSerializer.Serialize(result.Value, new System.Text.Json.JsonSerializerOptions { 
            WriteIndented = false, 
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Retrieves the version of the assembly.
    /// </summary>
    /// <returns>The version string (e.g., "1.0.0.0").</returns>
    [JSInvokable]
    public string GetVersion() {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
    }
}
