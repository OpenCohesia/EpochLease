# Add OpenCohesia Org Prefix to NuGet Packages

## Summary

Add the `OpenCohesia.` prefix to PackageId, AssemblyName, and C# namespaces for all projects. Folder names and solution structure remain unchanged.

## Approach

Direct per-file changes (no MSBuild indirection). Each csproj explicitly declares its new identity. This is a one-time rename that doesn't warrant centralized prefix composition.

## Changes

### csproj updates (4 files)

**`src/EpochLease/EpochLease.csproj`**
- `PackageId` -> `OpenCohesia.EpochLease`
- `RootNamespace` -> `OpenCohesia.EpochLease`
- `AssemblyName` -> `OpenCohesia.EpochLease`

**`src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj`**
- `PackageId` -> `OpenCohesia.EpochLease.Extensions.Hosting`
- `RootNamespace` -> `OpenCohesia.EpochLease.Extensions.Hosting`
- `AssemblyName` -> `OpenCohesia.EpochLease.Extensions.Hosting`

**`tests/EpochLease.Tests/EpochLease.Tests.csproj`**
- Coverage `Include` filter -> `[OpenCohesia.EpochLease]*`

**`tests/EpochLease.Extensions.Hosting.Tests/EpochLease.Extensions.Hosting.Tests.csproj`**
- Coverage `Include` filter -> `[OpenCohesia.EpochLease.Extensions.Hosting]*`

### C# namespace changes

Source files under `src/EpochLease/` (8 files):
- `namespace EpochLease;` -> `namespace OpenCohesia.EpochLease;`

Source files under `src/EpochLease.Extensions.Hosting/` (3 files):
- `namespace EpochLease.Extensions.Hosting;` -> `namespace OpenCohesia.EpochLease.Extensions.Hosting;`

Test files under `tests/EpochLease.Tests/` (4 files):
- `namespace EpochLease.Tests;` -> `namespace OpenCohesia.EpochLease.Tests;`

Test files under `tests/EpochLease.Extensions.Hosting.Tests/` (3 files):
- `namespace EpochLease.Extensions.Hosting.Tests;` -> `namespace OpenCohesia.EpochLease.Extensions.Hosting.Tests;`

### Unchanged

- Folder names
- Solution file (`EpochLease.slnx`)
- `Directory.Build.props`
- CI workflows
- README, LICENSE, logo assets
