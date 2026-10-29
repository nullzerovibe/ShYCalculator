# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]
### Breaking Changes & Migration
| Before | After | Action |
|---|---|---|
| `namespace ShYCalculator` (`ShYCalculator.ShYCalculator.Compile`) | `namespace ShY` (`ShYCalculator.Compile`) | Replace `using ShYCalculator...;` with `using ShY...;` (e.g. `ShYCalculator.Classes` → `ShY.Classes`, `ShYCalculator.DependencyInjection` → `ShY.DependencyInjection`). Package id and assembly name are unchanged. |
| `ShYCalculator.Calculator.Environment` | `ShY.Calculator.CalculatorEnvironment` | Rename type references. The `ShYCalculator.Environment` property keeps its name. |
| `new ShYCalculator(env, tokenizer, generator, parser, options)` | `new ShYCalculator(env, options)` | Pipeline components are no longer substitutable. |
| Public `Token`, `TokenKey`, `TokenType`, `OperatorKind`, `FunctionInfo`, `TernaryBranches`, `OpResult<T>`, `IExpressionTokenizer`, `IShuntingYardGenerator`, `IShuntingYardParser` | Internal | Implementation details; no replacement needed for `Calculate`/`Compile` users. |
| DI registered `IExpressionTokenizer`/`IShuntingYardGenerator`/`IShuntingYardParser` | Not registered; `ShYCalculatorOptions` is | Resolve `ShYCalculator`, `IGlobalScope`/`IEnvironment` or `ShYCalculatorOptions`. |
| `Calculate(expr, vars, includeAst = false)` optional parameters | Explicit overloads (`Calculate(expr)`, `(expr, includeAst)`, `(expr, vars)`, `(expr, vars, includeAst)`) | Source-compatible for all existing call shapes; recompile. |
| `CalcDateFunctions(string? format = null, string? culture = null)` | Explicit `()`, `(format)`, `(format, culture)`, `(format, culture, timeProvider)` | Source-compatible; recompile. |
| Extensions using `Func<ReadOnlySpan<Value>, Value>` on net8.0 | Not available before .NET 9 | Declare a delegate, e.g. `delegate Value Body(ReadOnlySpan<Value> args)` (see `ExampleFunctions`). |

Every public API change is now tracked in `ShYCalculator/PublicAPI.*.txt` by Microsoft.CodeAnalysis.PublicApiAnalyzers; an unrecorded change fails the build.

### Security
- **Stack Exhaustion DoS**: Deeply nested `if()`/ternaries (~400 levels, ~4.5 KB of input) or nested `&&`/`||` crashed the host process with an uncatchable `StackOverflowException`. Nesting is now limited by `ShYCalculatorOptions.MaxNestingDepth` (default 64), and every recursive stage has an execution-stack guard, so any remaining overflow surfaces as `ErrorCode.LimitExceeded`.
- **Input Size**: New `ShYCalculatorOptions.MaxExpressionLength` (default unlimited).

### Changed (behavior)
- **Builder Isolation**: `ShYCalculatorBuilder.Build()` returns a calculator with its own snapshot of the configuration; later builder calls no longer leak into calculators already built, and two builds no longer share one environment.
- **Function Registration**: `RegisterFunctions` reports the real reason (e.g. "already registered") and is atomic: a conflict no longer leaves the environment half-registered.
- **Operator Precedence**: Now follows C# conventions. `%` binds like `*` and `/` (`10 % 3 * 2` is `2`, was `4`); `&&` binds tighter than `||` (`true || false && false` is `true`, was `false`); equality binds looser than relational operators; shifts bind tighter than comparisons.
- **Short-Circuit Logic**: `&&` and `||` skip their right operand when the left one decides the result (`$x != 0 && 10 / $x > 1` no longer fails for `$x = 0`).
- **Bitwise Operators**: Operate on 64-bit integers (was 32-bit, overflowing to an unexpected exception). Shift counts outside 0–63 are rejected.
- **Modulo by Zero**: `x % 0` fails with `DivisionByZero` (was `NaN`).
- **Boolean Ordering**: `<=` and `>=` on booleans fail with `TypeMismatch`, consistent with `<` and `>`.
- **Date Concatenation**: Dates concatenated to strings use ISO 8601 (`o`) instead of the current culture.
- **Operations Contract**: Internal operations return `OperationNotSupported` instead of throwing for unsupported operators.

### Added
- **Decimal Mode**: `ShYCalculatorOptions.NumericMode = NumericMode.Decimal` evaluates literals, variables, constants and operators in exact `decimal`, with exact rounding/aggregate functions; transcendental functions compute in double and convert back. New `Value.Number(decimal)`, `Value.Mvalue`, `Value.IsDecimal`, `CalculationResult.Mvalue`. Double mode is unchanged and `Value` stays 32 bytes.
- **.NET 8 Support**: The library multi-targets `net8.0` and `net10.0` (span lookup fast paths on .NET 9+); tests run on both in CI.
- **Builder Options**: `ShYCalculatorBuilder.WithOptions(...)` for limits, date format, culture and time provider, honored by `AddShYCalculator` too.
- **Injectable Time**: `ShYCalculatorOptions.TimeProvider` drives `dt_now()`/`dt_today()`.
- **Trim/AOT Ready**: The library is marked `IsAotCompatible`; function definitions load through source-generated JSON.
- **Symbols**: NuGet releases ship a symbol package (`.snupkg`) and are built deterministically in CI.
- **Error Codes**: `NullValue`, `ValueOutOfRange` and `LimitExceeded`. Every operator failure now carries an error code and the operator's position.
- **String Escapes**: `\\ \" \' \` \n \r \t \0 \uXXXX`, with `InvalidSyntax` for unknown escapes and `UnterminatedString` for unclosed literals.
- `Calculate(expression, includeAst: true)` overload resolution (the `contextVariables` parameter now defaults to `null`).

### Fixed
- **Thread Safety**: `random()` used a shared, non-thread-safe `System.Random`; it now uses `Random.Shared`.
- **Custom Comparers**: Variable and constant dictionaries (and custom operator providers) using a custom `IEqualityComparer` failed every lookup with an unexpected exception.
- **Silent Truncation**: Input after a stray `)` or top-level `,` was ignored (`abs(-3)) + 1` returned `3`); it is now an error.
- **Deep Expressions**: Expressions holding more than 128 pending values failed with an unexpected exception.
- **AST Mode**: `includeAst: true` reported success with a null value when evaluation failed, and could not compare dates. Results are now identical with and without the AST.
- **Lazy `if`**: `if (...)` with whitespace before `(` evaluated every branch.
- **String Literals**: `"a\\"` failed to tokenize; escape handling no longer uses `Regex.Unescape`.
- **Date Functions**: Getters and adders (`dt_year`, `dt_addyears`, `dt_add`, ...) ignored dates passed as strings.
- **WASM Cultures**: Culture-specific month names (fr-FR, es-ES, ru-RU) failed to parse because the app ran in invariant globalization mode.
- **Compiled Expressions**: Compile-time exceptions and uncompiled use now report an error code and message.
- **Error Messages**: Unexpected evaluation exceptions keep the underlying message.

### Performance
- **Construction**: Function definitions are parsed once per process: `new ShYCalculator()` 412 µs → 17 µs, `Compile()` 430 µs → 22 µs, builder 813 µs → 17 µs.
- **Variables**: Lookups through `Dictionary<string, double>` no longer allocate per variable read.
- Compiled evaluation ~30% faster with ~30% fewer allocations; full parse-and-evaluate ~20% faster with ~23% fewer allocations (numbers parsed once at tokenization, allocation-free function lookups, leaner generator).

## [0.9.3] - 2026-03-01
### Added
- **SyntaxEditor v2**: Robust scroll synchronization, auto-expanding height, and bit-perfect character alignment using `scrollbar-gutter: stable`.
- **Deep Linking**: Reactive URL state synchronization (`e` and `v` parameters) for sharing calculations.
- **Advanced Result UX**: Replaced Export dropdown with minimalist "Copy" and "Share" icon buttons.
- **Shortcuts Tab**: New documentation tab with industrial `.kbd-badge` styling and standardized hotkeys.
- **UoM Aesthetics**: Premium glassmorphism with fixed backgrounds and linear-gradient hover effects for links.

### Fixed
- **Layout Stability**: Eliminated "scrollbar flicker" layout shift on dialog open using `scrollbar-gutter`.
- **Dialog Jumping**: Fixed `SyntaxEditor` jump and initial sizing issues in dialogs using `ResizeObserver`.
- **History Wrapping**: Fixed long expression overflow in history items (`overflow-wrap: anywhere`).
- **Functions Tab**: Resolved rendering delay when first opening the documentation dialog.

## [0.9.1] - 2026-02-15
### Added
- **Settings UI**: Overhauled with 3-column chip grid, clear buttons, and sticky headers.
- **Culture Support**: Added ES, FR, RU cultures and new date formats (`dd.MM.yyyy`, etc).
- **Date Safety**: Robust handling for Year 1 underflows and invalid culture fallbacks.

### Fixed
- **Date Parsing**: Resolved UTC vs Local time zone inconsistencies in `dt_create` and `dt_parse`.
- **UI Glitches**: Fixed sticky header gaps and double borders in Reference Guide.

## [0.9.0] - 2026-02-12

### Added
- **Compiled Mode**: `ShYCalculator.Compile` for high-performance reuse of expressions.
- **Builder Pattern**: `ShYCalculatorBuilder` for fluent configuration.
- **Deep Nesting Support**: Improved parsing for deeply nested expressions.
- **Dependency Injection**: `AddShYCalculator` extension method.
- **Zero-Allocation**: improved performance for dictionary lookups.
