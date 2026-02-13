# Contributing to ShYCalculator

Thank you for your interest in contributing! We welcome bug reports, feature requests, and pull requests.

## Getting Started
1. **Fork** the repository.
2. **Clone** your fork locally.
3. Install **.NET 10.0 SDK**.
4. Run tests to ensure everything is working: `dotnet test`.

## Development Workflow
1. Create a new branch for your feature or fix: `git checkout -b my-feature-branch`.
2. Make your changes.
3. **Add Unit Tests** for any new logic.
4. Run `dotnet test` again to ensure no regressions.
5. Push your branch and open a **Pull Request**.

## Coding Standards
- Use modern C# features (records, patterns, etc.).
- Follow standard .NET naming conventions.
- Ensure zero-allocation paths where possible (critical for this project).

## Reporting Bugs
Please open an issue with:
- A clear description of the bug.
- A minimal reproduction code snippet (or failing test case).
- Expected vs. Actual behavior.

Thank you!
