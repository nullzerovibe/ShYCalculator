// -----------------------------------------------------------------------------
// <summary>
//     Helper methods for loading function configurations and validating function arguments.
// </summary>
// -----------------------------------------------------------------------------
using System.Text.Json;
using ShYCalculator.Classes;
using System.Reflection;

namespace ShYCalculator.Functions;

internal static class CalcFunctionsHelper {
    // Cache JsonSerializerOptions instance for reuse
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    #region Load Json
    // Load Json configuration from embeded resource
    public static List<CalcFunction> ReadFunctionsConfiguration(string extensionName, Type extensionType) {
        try {
            var assembly = extensionType.Assembly;
            var resourceName = $"{extensionType.Namespace}.{extensionName}.json";

            using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new ConfigException($"Unexpected Exception on GetManifestResourceStream reading resource {resourceName}");
            using StreamReader reader = new(stream);
            string jsonFile = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<CalcFunction>>(jsonFile, CachedJsonSerializerOptions)!;
        }
        catch (Exception ex) {
            throw new ConfigException("Unexpected ReadConfig Exception", ex);
        }
    }
    #endregion Load Json

    #region Check Configuration
    // Check Calculation Functions Configuration for validity
    public static void CheckFunctionsConfiguration(List<CalcFunction> functions) {
        try {
            if (functions.Count == 0) {
                throw new ConfigException($"No Function present in provided configuration");
            }

            foreach (var function in functions) {
                if (String.IsNullOrEmpty(function.Name)) {
                    throw new ConfigException($"No Function name present in provided function configuration {function}");
                }

                var hasOptional = false;
                var hasArray = false;

                if (function.Arguments != null) {
                    foreach (var argument in function.Arguments) {
                        if (hasArray == true) {
                            throw new ConfigException($"Function {function.Name} contains an argument after an array argument");
                        }

                        if (hasOptional == true && !argument.Optional) {
                            throw new ConfigException($"Function {function.Name} contains a non Optional argument after an optional one");
                        }

                        if (String.IsNullOrEmpty(argument.Type) || !IsValidType(argument.Type)) {
                            throw new ConfigException($"Unsupported argument type {argument.Type} on function {function.Name}");
                        }

                        if (argument.Optional) {
                            hasOptional = true;
                        }

                        // Repeating arguments
                        if (argument.Type == "array") {
                            hasArray = true;

                            if (argument.Min == null || argument.Max == null || argument.Min < 0 || argument.Max < 0 || argument.Min > argument.Max) {
                                throw new ConfigException($"Invalid Min/Max values in argument type {argument.Type} on function {function.Name}");
                            }

                            // Repeating argument must have at least one sub argument
                            if (argument.Arguments == null || argument.Arguments.Count == 0) {
                                throw new ConfigException($"No sub arguments in argument type {argument.Type} on function {function.Name}");
                            }

                            foreach (var subArguments in argument.Arguments) {
                                // Check for unsuported argument type
                                if (String.IsNullOrEmpty(subArguments.Type) || !IsValidType(subArguments.Type) || subArguments.Type == "array") {
                                    throw new ConfigException($"Unsupported sub argument type {subArguments.Type} in argument type {argument.Type} on function {function.Name}");
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) {
            throw new ConfigException("Unexpected CheckConfig Exception", ex);
        }
    }

    #endregion Check Configuration

    #region Check Function Arguments
    // Check arguments provided to the function against Function Configuration
    public static void CheckFunctionArguments(CalcFunction function, ReadOnlySpan<Value> arguments, string extension) {
        var maxArgCount = GetMaxArgCount(function.Arguments);
        var minArgCount = GetMinArgCount(function.Arguments);

        if (maxArgCount < minArgCount) {
            throw new FunctionException($"Invalid argument count; max arguments count {maxArgCount} is less than min {minArgCount} provided {maxArgCount} in extension {extension} on function {function.Name}");
        }

        if (arguments.Length < minArgCount) {
            throw new FunctionException($"Invalid argument count; min {minArgCount} arguments provided {arguments.Length} in extension {extension} on function {function.Name}");
        }

        if (arguments.Length > maxArgCount) {
            throw new FunctionException($"Invalid argument count; max {maxArgCount} arguments provided {arguments.Length} in extension {extension} on function {function.Name}");
        }

        CheckArgTypes(function, arguments, extension);
    }

    internal static int GetMaxArgCount(List<CalcFunctionArgument>? arguments) {
        var maxArgCount = 0;
        for (int i = 0; i < arguments?.Count; i++) {
            var argument = arguments[i];
            if (argument.Type == "array") {
                if (argument.Arguments != null) {
                    foreach (var subArgument in argument.Arguments) {
                        maxArgCount += argument.Max ?? 0;
                    }
                }
                break;
            }

            maxArgCount++;
        }

        return maxArgCount;
    }

    internal static int GetMinArgCount(List<CalcFunctionArgument>? arguments) {
        var minArgCount = 0;
        for (int i = 0; i < arguments?.Count; i++) {
            var argument = arguments[i];

            if (argument.Optional) {
                break;
            }

            if (argument.Type == "array") {
                if (argument.Arguments != null) {
                    foreach (var subArgument in argument.Arguments) {
                        minArgCount += argument.Min ?? 0;
                    }
                }
                break;
            }

            minArgCount++;
        }

        return minArgCount;
    }

    internal static void CheckArgTypes(CalcFunction function, ReadOnlySpan<Value> arguments, string extension) {
        var argsConfig = function.Arguments;
        if (argsConfig == null) {
            return;
        }

        var i = 0;
        for (int ci = 0; ci < argsConfig.Count; ci++) {
            var argConfig = argsConfig[ci];

            if (argConfig.Min == 0 && arguments.Length == 0) {
                continue;
            }

            // No more arguments
            if (arguments.Length - 1 < ci) {
                if (argConfig.Optional) { // Nothing more expected
                    return;
                }

                // Expected an argument that is not provided
                throw new FunctionException($"Invalid argument count; expected {argConfig.Type} provided undefined in extension {extension} on function {function.Name}");
            }

            var argument = arguments[ci];

            // Repeating Arguments
            if (argConfig.Type == "array" && argConfig.Arguments != null) {
                var repArgsConfig = argConfig.Arguments;

                var argumentsLeft = arguments.Length - ci;
                // Note: argumentsLeft is always > 0 here because we passed the check on line 171
                // which ensures arguments.Count - 1 >= ci, therefore arguments.Count > ci

                var obsoleteArguments = argumentsLeft % repArgsConfig.Count;
                if (obsoleteArguments > 0) {
                    string expectedCount;
                    int minVal = argConfig.Min ?? 0;
                    int maxVal = argConfig.Max ?? 0;
                    int argCount = arguments.Length;
                    if (argConfig.Min != argConfig.Max) {
                        expectedCount = $"{minVal * argCount} - {maxVal * argCount}";
                    }
                    else {
                        expectedCount = $"{minVal * argCount}";
                    }
                    throw new FunctionException($"Invalid argument count; expected {expectedCount} {argConfig.Type} arguments provided {obsoleteArguments} obsolete arguments. Note, repeating arguments must be provided in chunks of {repArgsConfig.Count} for this function. In extension {extension} on function {function.Name}");
                }

                for (int l = 0; l < argumentsLeft / repArgsConfig.Count; l++) {
                    for (int rai = 0; rai < repArgsConfig.Count; rai++) {
                        var repArgConfig = repArgsConfig[rai];
                        var currentArg = arguments[ci + (l * repArgsConfig.Count) + rai];

                        if (!IsValidArgType(repArgConfig.Type ?? "string", currentArg)) {
                            throw new FunctionException($"Invalid argument type; expected {repArgConfig.Type} provided {currentArg} in extension {extension} on function {function.Name}");
                        }
                    }
                }

                // Advance ci by the number of consumed repeating arguments (minus 1, as the loop and i will advance further)
                ci += argumentsLeft - 1;
            }
            else {
                // Check regular Argument
                if (!IsValidArgType(argConfig.Type ?? "string", argument)) {
                    throw new FunctionException($"Invalid argument type; expected {argConfig.Type} provided {argument} in extension {extension} on function {function.Name}");
                }
            }

            i++;
        }
    }

    internal static bool IsValidArgType(string type, Value value) {
        if (string.Equals(type, "number", StringComparison.OrdinalIgnoreCase)) return value.DataType.HasFlag(DataType.Number);
        if (string.Equals(type, "string", StringComparison.OrdinalIgnoreCase)) return value.DataType.HasFlag(DataType.String);
        // Date can be provided as DataType.Date OR DataType.String (which will be parsed later)
        if (string.Equals(type, "date", StringComparison.OrdinalIgnoreCase)) return value.DataType.HasFlag(DataType.Date) || value.DataType.HasFlag(DataType.String);
        if (string.Equals(type, "boolean", StringComparison.OrdinalIgnoreCase)) return value.DataType.HasFlag(DataType.Boolean);
        if (string.Equals(type, "any", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static bool IsValidType(string type) {
        if (string.IsNullOrEmpty(type)) {
            return false;
        }
        return string.Equals(type, "number", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(type, "array", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(type, "string", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(type, "date", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(type, "boolean", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(type, "any", StringComparison.OrdinalIgnoreCase);
    }
    #endregion Check Function Arguments
}