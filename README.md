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
[![Vibe: Agentic](https://img.shields.io/badge/Vibe-Agentic-blueviolet)](AGENTS.md)
[![Mode: Vibecoding](https://img.shields.io/badge/Mode-Vibecoding-cyan)](VIBE.md)
[![Powered By: Antigravity](https://img.shields.io/badge/Powered%20By-Antigravity-orange)](https://antigravity.google/)

</div>

---

# ShYCalculator

[![Build Status](https://img.shields.io/github/actions/workflow/status/nullzerovibe/ShYCalculator/dotnet.yml?branch=main)](https://github.com/nullzerovibe/ShYCalculator/actions)
[![Nuget](https://img.shields.io/nuget/v/ShYCalculator)](https://www.nuget.org/packages/ShYCalculator)
[![Live Demo](https://img.shields.io/badge/Demo-WASM-brightgreen)](https://nullzerovibe.github.io/ShYCalculator/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

**ShYCalculator** is a high-performance, thread-safe, and extensible mathematical expression evaluator for .NET, based on the [Shunting-Yard algorithm](https://en.wikipedia.org/wiki/Shunting_yard_algorithm).

> **[👉 Try the Live WebAssembly Demo 👈](https://nullzerovibe.github.io/ShYCalculator/)**
> *Runs entirely in your browser. Architecture by NullZeroVibe.*

## Features

- 🚀 **High Performance**: Optimized for low allocation (Zero-Allocation paths where possible).
- 🧵 **Thread Safe**: Stateless execution model designed for high-concurrency environments (Web APIs, Services). Configure the environment once, then pass per-call data as context variables; mutating the environment while calculating is not thread-safe.
- 🧩 **Extensible**: Add custom functions and constants. The operator set is fixed; `IOperatorProvider` can re-weight precedence and accepted data types of the built-in operators.
- 🧱 **Hardened for untrusted input**: Configurable nesting and length limits, and a stack guard that turns pathological input into an error instead of crashing the process.
- 🏗️ **Builder Pattern**: Fluent API for easy configuration.
- 🛡️ **Safe**: No `eval()` or dynamic compilation risks; strictly parsed.
- 🎯 **Decimal Mode**: Opt-in exact base-10 arithmetic for financial calculations.
- 🎛️ **Targets**: .NET 8 (LTS) and .NET 10 (LTS), trim- and AOT-compatible.
- 🔁 **Compiled Mode**: Parse once, execute many times for maximum performance.
- 📅 **Date & Time**: Robust support for date parsing, culture-specific formats, and time adjustments.
- 🌳 **Deep Nesting**: Supports complex nested expressions (ifs, ternaries, functions).
- 🔗 **Deep Linking**: Reactive URL state synchronization for sharing calculations (WASM).

## Quick Start

### Installation

```bash
dotnet add package ShYCalculator
```

### Basic Usage (Stateless / Thread-Safe)

Recommended for Web APIs or shared services.

```csharp
using ShY;                       // ShYCalculator, ShYCalculatorBuilder
using ShY.Classes;               // Value, CalculationResult, options
using ShY.DependencyInjection;   // AddShYCalculator

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

## Operators

Precedence follows C# conventions, from lowest to highest:

| Precedence | Operators | Notes |
|---|---|---|
| 0 | `? :` | Ternary; only the selected branch is evaluated |
| 1 | `\|\|` | Short-circuit: right side skipped when left is `true` |
| 2 | `&&` | Short-circuit: right side skipped when left is `false` |
| 3 | `\|` | Bitwise OR |
| 4 | `^^` `⊕` | Bitwise XOR |
| 5 | `&` | Bitwise AND |
| 6 | `==` `!=` | Equality |
| 7 | `<` `>` `<=` `>=` | Relational (numbers and dates) |
| 8 | `<<` `>>` | Shifts (count 0–63) |
| 9 | `+` `-` `−` | `+` also concatenates strings |
| 10 | `*` `×` `/` `÷` `%` | `/ 0` and `% 0` fail with `DivisionByZero` |
| 11 | `^` | Power, right-associative |
| 12 | `!` `~` `√` (prefix), `!` (postfix factorial) | Unary |

Bitwise operators work on 64-bit integers. `if(cond, a, b)` is lazy like the ternary operator.

String literals use `"`, `'` or `` ` `` and support the escapes `\\` `\"` `\'` `` \` `` `\n` `\r` `\t` `\0` `\uXXXX`.

## Decimal Mode (Financial Precision)

Binary floating point cannot represent most decimal fractions (`0.1 + 0.2 == 0.3` is `false` in double). Opt into exact base-10 arithmetic:

```csharp
var calculator = new ShYCalculatorBuilder()
    .WithAllExtensions()
    .WithOptions(o => o.NumericMode = NumericMode.Decimal)
    .Build();

calculator.Calculate("0.1 + 0.2 == 0.3").Bvalue;   // true
calculator.Calculate("19.99 * 3").Mvalue;          // 59.97m (exact)
calculator.Calculate("round(1.255, 2)").Mvalue;    // 1.26m  (double mode: 1.25)
```

In decimal mode literals, constants, variables and every operator are exact `decimal`; `abs`, `min`, `max`, `sum`, `avg`, `round`, `floor`, `ceiling`, `trunc`, `sign`, `median` and `mode` compute exactly, while transcendental functions (`sin`, `ln`, `sqrt`, fractional `^`) compute in double and convert back. Results outside the decimal range (~±7.9e28, `28!`, `ln(-1)`) fail with `ErrorCode.ValueOutOfRange`. Read results through `Mvalue` (exact) or `Nvalue` (converted to double). `Value.Number(decimal)` variables are exact in either mode.

## Configuration & Limits

Options can be set through the builder (order in the chain does not matter), the DI registration, or the constructor:

```csharp
var calculator = new ShYCalculatorBuilder()
    .WithAllExtensions()
    .WithOptions(o => {
        o.MaxNestingDepth = 32;          // ternaries, if(), &&, || (default 64; 0 = no limit)
        o.MaxExpressionLength = 4_096;   // characters (default 0 = unlimited)
        o.CultureName = "fr-FR";         // date parsing culture
        o.DateFormat = "dd MMMM yyyy";
        o.TimeProvider = timeProvider;   // drives dt_now()/dt_today(); defaults to TimeProvider.System
    })
    .Build();
```

Exceeding a limit, or nesting deeper than the available execution stack allows, fails with `ErrorCode.LimitExceeded`; it never crashes the host process. Plain parentheses are not subject to the nesting limit.

Each `Build()` returns a calculator with its own snapshot of the configuration. With dependency injection, functions, constants and options are shared singletons: configure them at registration time and do not mutate the injected `IEnvironment` afterwards.

## Advanced Features

### Abstract Syntax Tree (AST)

You can retrieve the parsed AST for analysis or visualization.

```csharp
var result = calculator.Calculate("1 + 2 * 3", includeAst: true);
if (result.Success && result.Ast != null) {
    Console.WriteLine($"Root Node Type: {result.Ast.Type}"); // "binary"
    Console.WriteLine($"Operator: {result.Ast.Operator}");   // "+"
}
```

### Expression Validation

Validate syntax without executing the expression. Useful for UI feedback.

```csharp
var validation = ShYCalculator.Compile("1 + (2 * 3"); // Missing closing parenthesis
if (!validation.Success) {
    foreach (var error in validation.Errors) {
        Console.WriteLine($"Error at {error.StartIndex}: {error.Message}");
    }
}
```

### WebAssembly (WASM) Interop

When running in the browser, `ShYCalculator` exposes a JS-friendly API:

```javascript
// Calculate with AST
const result = await dotNetHelper.invokeMethodAsync('Calculate', '1 + 2', true);
console.log(result.ast); // { type: "binary", operator: "+", ... }

// Validate Expression
const validation = await dotNetHelper.invokeMethodAsync('ValidateExpression', '1 +');
console.log(JSON.parse(validation).errors); // [{ message: "Unexpected end of expression", ... }]
```


## Documentation

See the [Documentation Tests](ShYCalculator/ShYCalculator.Test/UT_Documentation_Stateless.cs) for more executable examples.

## License

Distributed under the MIT License. See [`LICENSE`](./LICENSE) for more information.

_For the vibe check, see [`VIBE.md`](./VIBE.md)._

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
