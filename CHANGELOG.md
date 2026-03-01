# Changelog

All notable changes to this project will be documented in this file.

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
