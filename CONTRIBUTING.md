# Contributing to EpochLease

Thank you for your interest in contributing to EpochLease! This document provides guidelines and information for contributors.

## Reporting Bugs

Please use the [Bug Report](https://github.com/OpenCohesia/EpochLease/issues/new?template=bug_report.yml) issue template. Include:

- A clear description of the bug
- Minimal reproduction steps or code
- Expected vs actual behavior
- Package version and .NET version

## Suggesting Features

Use the [Feature Request](https://github.com/OpenCohesia/EpochLease/issues/new?template=feature_request.yml) issue template. Describe:

- The problem you're trying to solve
- Your proposed solution
- Alternatives you've considered

## Development Setup

1. Fork and clone the repository
2. Ensure you have [.NET 10 SDK](https://dotnet.microsoft.com/download) installed
3. Build: `dotnet build`
4. Run tests: `dotnet test`

## Pull Request Process

1. Fork the repository
2. Create a feature branch from `main`
3. Make your changes
4. Ensure all tests pass (`dotnet test`)
5. Ensure no build warnings (`dotnet build`)
6. Submit a pull request

## Code Style

- Follow the `.editorconfig` settings in the repository
- Run `dotnet format` before submitting
- Use C# 14 features where appropriate
- Keep lines under 120 characters

## Testing Requirements

- All new code must have tests
- TDD is preferred: write failing tests first, then implement
- Use xUnit v3 for test framework
- Follow the AAA pattern (Arrange, Act, Assert)
- One behavior per test method

## Commit Message Conventions

We use [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` — new features
- `fix:` — bug fixes
- `docs:` — documentation changes
- `chore:` — maintenance tasks
- `test:` — test additions or modifications
- `ci:` — CI/CD changes

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
