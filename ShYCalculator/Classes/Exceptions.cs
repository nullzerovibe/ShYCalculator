// -----------------------------------------------------------------------------
// <summary>
//     Custom exception types for calculator errors.
//     Used for specific failure scenarios like missing functions or invalid configuration.
// </summary>
// -----------------------------------------------------------------------------
namespace ShYCalculator.Classes;

internal class CalcEnvironmentException : BaseCalcException {
    public CalcEnvironmentException(string message) : base(message) { }

    public CalcEnvironmentException(string message, Exception innerException) : base(message, innerException) { }
}

internal class ExpressionTokenizerException : BaseCalcException {
    public ExpressionTokenizerException(string message) : base(message) { }

    public ExpressionTokenizerException(string message, Exception innerException) : base(message, innerException) { }
}

internal class ShuntingYardGeneratorException : BaseCalcException {
    public ShuntingYardGeneratorException(string message) : base(message) { }

    public ShuntingYardGeneratorException(string message, Exception innerException) : base(message, innerException) { }
}

internal class ShuntingYardParserException : BaseCalcException {
    public ShuntingYardParserException(string message) : base(message) { }

    public ShuntingYardParserException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a function execution fails.
/// </summary>
public class FunctionException : BaseCalcException {
    /// <summary>Initializes a new instance of the <see cref="FunctionException"/> class.</summary>
    public FunctionException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="FunctionException"/> class with inner exception.</summary>
    public FunctionException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when configuration is invalid.
/// </summary>
public class ConfigException : BaseCalcException {
    /// <summary>Initializes a new instance of the <see cref="ConfigException"/> class.</summary>
    public ConfigException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="ConfigException"/> class with inner exception.</summary>
    public ConfigException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Base class for all calculator exceptions.
/// </summary>
public abstract class BaseCalcException : Exception {
    /// <summary>Initializes a new instance of the <see cref="BaseCalcException"/> class.</summary>
    public BaseCalcException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="BaseCalcException"/> class with inner exception.</summary>
    public BaseCalcException(string message, Exception innerException) : base(message, innerException) { }
}
