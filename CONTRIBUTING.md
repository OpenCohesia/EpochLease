# Contributing to EpochLease

For general contribution guidelines, see the [OpenCohesia contributing guide](https://github.com/OpenCohesia/.github/blob/main/CONTRIBUTING.md).

## Development Setup

1. Fork and clone the repository
2. Ensure you have [.NET 10 SDK](https://dotnet.microsoft.com/download) installed
3. Build: `dotnet build`
4. Run tests: `dotnet test`

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
