# NuGet Packaging and Publishing Design

## Problem

The solution has two NuGet packages with a dependency relationship:
- **OpenCohesia.EpochLease** (core)
- **OpenCohesia.EpochLease.Extensions.Hosting** (depends on core)

Requirements:
1. Development uses direct `ProjectReference` for seamless debugging and navigation
2. Published NuGet packages use proper NuGet package dependencies (not bundled assemblies)
3. Independent versioning — each package has its own version
4. Two publishing scenarios: core+hosting together, or hosting-only
5. Manual workflow dispatch for publishing

## Key Insight: Native MSBuild Behavior

`dotnet pack` automatically converts `ProjectReference`s to packable projects into NuGet package dependencies. No MSBuild conditions or workarounds needed. The dependency version is resolved from the referenced project's `<Version>` property at pack time.

Reference: [MSBuild pack targets — Project to project references](https://learn.microsoft.com/nuget/reference/msbuild-targets#pack-scenarios)

## Design

### Project File Changes

Each source project gets a `<Version>` property and explicit `<IsPackable>` marker:

**`src/EpochLease/EpochLease.csproj`** — Add:
```xml
<Version>0.1.0</Version>
<IsPackable>true</IsPackable>
```

**`src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj`** — Add:
```xml
<Version>0.1.0</Version>
<IsPackable>true</IsPackable>
```

The existing `ProjectReference` from hosting → core remains unchanged.

**Test projects** — Add to prevent accidental packing:
```xml
<IsPackable>false</IsPackable>
```

### Workflows

#### `publish-core.yml` — Publish Both Packages

Triggered manually when core changes. Always publishes both packages since hosting depends on core.

- No version inputs — versions come from csproj `<Version>` properties
- Build → Test → Pack Core → Pack Hosting → Publish all
- Sequential packing ensures correct dependency resolution

#### `publish-hosting.yml` — Publish Hosting Only

Triggered manually when only hosting code changes.

- No version inputs — version comes from hosting csproj
- Core dependency version comes from core's existing `<Version>` (last published version)
- Build → Test → Pack Hosting → Publish

#### Remove `publish.yml`

The existing release-triggered workflow is removed and replaced by the two new manual-dispatch workflows.

### Versioning Strategy

- Independent versions per package (not lock-step)
- Version is stored in each project's `<Version>` property in csproj
- Version bumps are part of the PR (code-reviewed)
- When core changes: bump both versions (hosting gets at least a patch bump)
- When only hosting changes: bump only hosting version

### Release Process

**Core changed → publish both:**
1. Bump `<Version>` in both source csproj files
2. Merge PR to main
3. Trigger "Publish Core + Hosting" workflow

**Only hosting changed → publish hosting:**
1. Bump `<Version>` in hosting csproj only
2. Merge PR to main
3. Trigger "Publish Hosting" workflow

### Documentation

Add a "Publishing Packages" section to `CONTRIBUTING.md` documenting:
- The two workflows and when to use each
- The version bump process (which csproj files to update for each scenario)
- The release checklist for both scenarios

## Files Changed

| File | Change |
|---|---|
| `src/EpochLease/EpochLease.csproj` | Add `<Version>`, `<IsPackable>` |
| `src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj` | Add `<Version>`, `<IsPackable>` |
| `tests/EpochLease.Tests/EpochLease.Tests.csproj` | Add `<IsPackable>false</IsPackable>` |
| `tests/EpochLease.Extensions.Hosting.Tests/EpochLease.Extensions.Hosting.Tests.csproj` | Add `<IsPackable>false</IsPackable>` |
| `.github/workflows/publish.yml` | Remove |
| `.github/workflows/publish-core.yml` | New — publishes both packages |
| `.github/workflows/publish-hosting.yml` | New — publishes hosting only |
| `CONTRIBUTING.md` | Add "Publishing Packages" section |
