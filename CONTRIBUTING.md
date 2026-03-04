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

## Publishing Packages

This repository produces two NuGet packages with independent versions:

| Package | Project |
|---------|---------|
| `OpenCohesia.EpochLease` | `src/EpochLease/EpochLease.csproj` |
| `OpenCohesia.EpochLease.Extensions.Hosting` | `src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj` |

The hosting package depends on the core package. Versions are stored in each project's `<Version>` property.

### When core changes (publish both packages)

The core package is a dependency of the hosting package, so both must be published together.

1. Bump `<Version>` in **both** source `.csproj` files (hosting needs at least a patch bump)
2. Merge the PR to `master`
3. Go to **Actions → "Publish Core + Hosting" → Run workflow**

### When only hosting changes (publish hosting only)

1. Bump `<Version>` in `src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj` only
2. Merge the PR to `master`
3. Go to **Actions → "Publish Hosting" → Run workflow**

### Version bumping rules

- Follow [Semantic Versioning](https://semver.org/): MAJOR for breaking changes, MINOR for new features, PATCH for bug fixes
- When publishing both: core gets the appropriate bump, hosting gets at least PATCH
- Versions are code-reviewed as part of the PR
