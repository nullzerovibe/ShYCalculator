# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]
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
