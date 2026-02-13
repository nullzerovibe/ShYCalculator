<div align="center">

<img src="ShYCalculator.Wasm/wwwroot/nlo/nlo-banner.jpg" width="100%" alt="nullzerovibe banner" />

# N u l l Z e r o V i b e
<pre>
▄▀▀▄░█▀▀█░█▀▀▄░█▀▀ 
█░░░░█░░█░█░░█░█▀▀ 
▀▄▄▀░█▄▄█░█▄▄▀░█▄▄ 
</pre>

` ░▒▒▓▓ LIFETIME OF SYNTAX // AGENTIC EVOLUTION ▓▓▒▒░ `

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![Vibe: Agentic](https://img.shields.io/badge/Vibe-Agentic-blueviolet)
![Mode: Vibecoding](https://img.shields.io/badge/Mode-Vibecoding-cyan)

</div>

---

# ShYCalculator

![Build Status](https://img.shields.io/github/actions/workflow/status/nullzerovibe/ShYCalculator/dotnet.yml?branch=main)
![Nuget](https://img.shields.io/nuget/v/ShYCalculator)
![License](https://img.shields.io/badge/license-MIT-green)

**ShYCalculator** is a high-performance, thread-safe, and extensible mathematical expression evaluator for .NET, based on the Shunting-Yard algorithm.

## Features

- 🚀 **High Performance**: Optimized for low allocation (Zero-Allocation paths where possible).
- 🧵 **Thread Safe**: Stateless execution model designed for high-concurrency environments (Web APIs, Services).
- 🧩 **Extensible**: Add custom functions, operators, and constants.
- 🏗️ **Builder Pattern**: Fluent API for easy configuration.
- 🛡️ **Safe**: No `eval()` or dynamic compilation risks; strictly parsed.
- 🔁 **Compiled Mode**: Parse once, execute many times for maximum performance.
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

For scenarios where the formula is constant but variables change (e.g., bulk processing), use `Compile` to avoid reparsing.

```csharp
// 1. Compile the expression once
var compiled = ShYCalculator.Compile("score * multiplier");
if (compiled.Success) {
    var runner = compiled.Value;

    // 2. Execute many times with different data
    foreach (var user in users) {
        var context = new Dictionary<string, double> {
            { "score", user.Score },
            { "multiplier", 1.5 }
        };
        var result = runner.Calculate(context);
        Console.WriteLine($"User {user.Id}: {result.Value.Nvalue}");
    }
}
```

## Documentation

See the [Documentation Tests](ShYCalculator/ShYCalculator.Test/UT_Documentation_Stateless.cs) for more executable examples.

## License

MIT

---

<div align="center">

### ◈ THE MANIFESTO

> **Legacy meets Autonomy.**
> After a lifetime of building the stack, I've moved beyond the keyboard. 
> I don't just write lines; I orchestrate intent. 
> This is **Vibecoding**: High-level reasoning, agentic execution, and zero friction.

</div>

### 🛠️ ARCHITECTURAL STACK
* **The Core:** Lifetime of Full-Stack Engineering & System Architecture.
* **The Shift:** 1 Year of Agentic Development & LLM Orchestration.
* **The Output:** 100% Open Source (MIT). I build for the commons.

### 🤖 CURRENT AGENTIC FOCUS
* **Autonomous Workflows:** Self-healing CI/CD and agent-led refactoring.
* **Vibe-Driven UI:** Rapid prototyping where the intent is the documentation.
* **Neural Tooling:** Building the next generation of developer experience.

---

### 📡 CONNECT / COLLABORATE
* **GitHub:** [nullzerovibe](https://github.com/nullzerovibe)
* **Email:** [nullzerovibe@gmail.com](mailto:nullzerovibe@gmail.com)
* **Status:** `[■■■■■■■■■□] Orchestrating the next vibe...`

---

<div align="center">

*Everything you find here is yours to fork, break, and build upon.*
**KEEP THE VIBE ALIVE.**

</div>
