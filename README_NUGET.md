# ShYCalculator

[![Build Status](https://img.shields.io/github/actions/workflow/status/nullzerovibe/ShYCalculator/dotnet.yml?branch=main)](https://github.com/nullzerovibe/ShYCalculator/actions)
[![Nuget](https://img.shields.io/nuget/v/ShYCalculator)](https://www.nuget.org/packages/ShYCalculator)
[![License](https://img.shields.io/badge/license-MIT-green)](https://github.com/nullzerovibe/ShYCalculator/blob/main/LICENSE)
[![Powered By: Antigravity](https://img.shields.io/badge/Powered%20By-Antigravity-orange)](https://antigravity.google/)

**ShYCalculator** is a high-performance, thread-safe, and extensible mathematical expression evaluator for .NET, based on the Shunting-Yard algorithm.

## Features

- 🚀 **High Performance**: Optimized for low allocation (Zero-Allocation paths where possible).
- 🧵 **Thread Safe**: Stateless execution model designed for high-concurrency environments (Web APIs, Services).
- 🧩 **Extensible**: Add custom functions, operators, and constants.
- 🏗️ **Builder Pattern**: Fluent API for easy configuration.
- 🛡️ **Safe**: No `eval()` or dynamic compilation risks; strictly parsed.
- 🔁 **Compiled Mode**: Parse once, execute many times for maximum performance.
- 📅 **Date & Time**: Robust support for date parsing, culture-specific formats, and time adjustments.
- 🌳 **Deep Nesting**: Supports complex nested expressions (ifs, ternaries, functions).

## Quick Start

### Installation

```bash
dotnet add package ShYCalculator
```

### Basic Usage (Stateless / Thread-Safe)

Recommended for Web APIs or shared services.

```csharp
var calculator = new ShYCalculatorBuilder()
    .WithAllExtensions()
    .Build();

var context = new Dictionary<string, double> {
    { "x", 10 },
    { "y", 20 }
};

var result = calculator.Calculate("x * y + sqrt(x)", context);
Console.WriteLine($"Result: {result.Value.Nvalue}"); // Output: 203.16...
```

### Advanced Usage (Custom Functions)

```csharp
var calculator = new ShYCalculatorBuilder()
    .WithMathematics()
    .WithConstant("TaxRate", 0.2)
    .Build();

var result = calculator.Calculate("100 * (1 + TaxRate)");
```

### Dependency Injection

```csharp
// Program.cs
builder.Services.AddShYCalculator(options => {
    options.WithAllExtensions();
});

// MyService.cs
public class MyService(ShYCalculator calculator) {
    public double CalculatePrice(double basePrice) {
        var context = new Dictionary<string, double> { { "basePrice", basePrice } };
        return calculator.Calculate("basePrice * 1.2", context).Value.Nvalue.Value;
    }
}
```

### Compiled Mode (High Performance)

For scenarios where the formula is constant but variables change, use `Compile` to avoid reparsing.

```csharp
var compiled = ShYCalculator.Compile("score * multiplier");
if (compiled.Success) {
    var runner = compiled.Value;
    var result = runner.Calculate(new Dictionary<string, double> { { "score", 100 }, { "multiplier", 1.5 } });
}
```

### AST & Expression Validation

Retrieve the parsed AST for analysis or validate syntax without execution.

```csharp
// AST
var result = calculator.Calculate("1 + 2 * 3", includeAst: true);
var nodeType = result.Ast?.Type; // "binary"

// Validation
var validation = ShYCalculator.Compile("1 + (2"); 
if (!validation.Success) Console.WriteLine(validation.Errors[0].Message);
```

## License

Distributed under the MIT License. See [LICENSE](https://github.com/nullzerovibe/ShYCalculator/blob/main/LICENSE) for more information.
