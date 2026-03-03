# OpenCohesia Org Prefix Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add `OpenCohesia.` prefix to all NuGet PackageIds, assembly names, and C# namespaces while keeping folder names unchanged.

**Architecture:** Direct per-file edits to csproj properties and namespace declarations. No MSBuild indirection. See `docs/plans/2026-03-03-org-prefix-design.md` for design rationale.

**Tech Stack:** .NET 10, C# latest, xUnit v3, coverlet

---

### Task 1: Update core library csproj

**Files:**
- Modify: `src/EpochLease/EpochLease.csproj:3-4` (RootNamespace, PackageId)

**Step 1: Edit csproj**

Replace the PropertyGroup contents:

```xml
<RootNamespace>OpenCohesia.EpochLease</RootNamespace>
<PackageId>OpenCohesia.EpochLease</PackageId>
<AssemblyName>OpenCohesia.EpochLease</AssemblyName>
```

**Step 2: Verify build**

Run: `mise run dotnet:build`
Expected: Build succeeds (namespace mismatch warnings are OK at this point — source files not yet updated)

**Step 3: Commit**

```bash
git add src/EpochLease/EpochLease.csproj
git commit -m "chore: add OpenCohesia prefix to core library csproj"
```

---

### Task 2: Update hosting library csproj

**Files:**
- Modify: `src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj:3-4` (RootNamespace, PackageId)

**Step 1: Edit csproj**

Replace the PropertyGroup contents:

```xml
<RootNamespace>OpenCohesia.EpochLease.Extensions.Hosting</RootNamespace>
<PackageId>OpenCohesia.EpochLease.Extensions.Hosting</PackageId>
<AssemblyName>OpenCohesia.EpochLease.Extensions.Hosting</AssemblyName>
```

**Step 2: Verify build**

Run: `mise run dotnet:build`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj
git commit -m "chore: add OpenCohesia prefix to hosting library csproj"
```

---

### Task 3: Update core library namespaces (8 files)

**Files:**
- Modify: `src/EpochLease/ILeaseable.cs:1`
- Modify: `src/EpochLease/LeaseableExtensions.cs:1`
- Modify: `src/EpochLease/ILeaseStore.cs:1`
- Modify: `src/EpochLease/ILeaseExpirationHandler.cs:1`
- Modify: `src/EpochLease/ScanResult.cs:1`
- Modify: `src/EpochLease/Lease.cs:1`
- Modify: `src/EpochLease/LeaseExpirationScanner.cs:1`
- Modify: `src/EpochLease/InMemoryLeaseStore.cs:3`

**Step 1: Replace namespaces**

In all 8 files, change:
```csharp
namespace EpochLease;
```
to:
```csharp
namespace OpenCohesia.EpochLease;
```

**Step 2: Verify build**

Run: `mise run dotnet:build`
Expected: Build succeeds (test projects may show warnings — they still reference old namespace)

**Step 3: Commit**

```bash
git add src/EpochLease/*.cs
git commit -m "refactor: rename core library namespace to OpenCohesia.EpochLease"
```

---

### Task 4: Update hosting library namespaces (3 files)

**Files:**
- Modify: `src/EpochLease.Extensions.Hosting/LeaseExpirationWatcherOptions.cs:3`
- Modify: `src/EpochLease.Extensions.Hosting/LeaseExpirationWatcher.cs:5`
- Modify: `src/EpochLease.Extensions.Hosting/ServiceCollectionExtensions.cs:5`

**Step 1: Replace namespaces**

In all 3 files, change:
```csharp
namespace EpochLease.Extensions.Hosting;
```
to:
```csharp
namespace OpenCohesia.EpochLease.Extensions.Hosting;
```

**Step 2: Verify build**

Run: `mise run dotnet:build`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/EpochLease.Extensions.Hosting/*.cs
git commit -m "refactor: rename hosting library namespace to OpenCohesia.EpochLease.Extensions.Hosting"
```

---

### Task 5: Update core test namespaces and coverage filter (4 files + 1 csproj)

**Files:**
- Modify: `tests/EpochLease.Tests/EpochLease.Tests.csproj:4` (Include filter)
- Modify: `tests/EpochLease.Tests/LeaseTests.cs:3`
- Modify: `tests/EpochLease.Tests/LeaseableExtensionsTests.cs:3`
- Modify: `tests/EpochLease.Tests/InMemoryLeaseStoreTests.cs:3`
- Modify: `tests/EpochLease.Tests/LeaseExpirationScannerTests.cs:5`

**Step 1: Update coverage filter in csproj**

Change:
```xml
<Include>[EpochLease]*</Include>
```
to:
```xml
<Include>[OpenCohesia.EpochLease]*</Include>
```

**Step 2: Replace test namespaces**

In all 4 test files, change:
```csharp
namespace EpochLease.Tests;
```
to:
```csharp
namespace OpenCohesia.EpochLease.Tests;
```

**Step 3: Run core tests**

Run: `dotnet test tests/EpochLease.Tests --verbosity minimal --consoleLoggerParameters:ErrorsOnly`
Expected: All tests pass

**Step 4: Commit**

```bash
git add tests/EpochLease.Tests/
git commit -m "refactor: rename core test namespace to OpenCohesia.EpochLease.Tests"
```

---

### Task 6: Update hosting test namespaces and coverage filter (3 files + 1 csproj)

**Files:**
- Modify: `tests/EpochLease.Extensions.Hosting.Tests/EpochLease.Extensions.Hosting.Tests.csproj:4` (Include filter)
- Modify: `tests/EpochLease.Extensions.Hosting.Tests/LeaseExpirationWatcherOptionsTests.cs:3`
- Modify: `tests/EpochLease.Extensions.Hosting.Tests/LeaseExpirationWatcherTests.cs:8`
- Modify: `tests/EpochLease.Extensions.Hosting.Tests/ServiceCollectionExtensionsTests.cs:6`

**Step 1: Update coverage filter in csproj**

Change:
```xml
<Include>[EpochLease.Extensions.Hosting]*</Include>
```
to:
```xml
<Include>[OpenCohesia.EpochLease.Extensions.Hosting]*</Include>
```

**Step 2: Replace test namespaces**

In all 3 test files, change:
```csharp
namespace EpochLease.Extensions.Hosting.Tests;
```
to:
```csharp
namespace OpenCohesia.EpochLease.Extensions.Hosting.Tests;
```

**Step 3: Run hosting tests**

Run: `dotnet test tests/EpochLease.Extensions.Hosting.Tests --verbosity minimal --consoleLoggerParameters:ErrorsOnly`
Expected: All tests pass

**Step 4: Commit**

```bash
git add tests/EpochLease.Extensions.Hosting.Tests/
git commit -m "refactor: rename hosting test namespace to OpenCohesia.EpochLease.Extensions.Hosting.Tests"
```

---

### Task 7: Full verification

**Step 1: Run full test suite**

Run: `mise run dotnet:test`
Expected: All tests pass

**Step 2: Verify package metadata**

Run: `dotnet pack src/EpochLease/EpochLease.csproj --configuration Release --output /tmp/nupkg-verify --no-build 2>/dev/null || dotnet pack src/EpochLease/EpochLease.csproj --configuration Release --output /tmp/nupkg-verify`
Expected: Produces `OpenCohesia.EpochLease.*.nupkg`

Run: `dotnet pack src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj --configuration Release --output /tmp/nupkg-verify --no-build 2>/dev/null || dotnet pack src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj --configuration Release --output /tmp/nupkg-verify`
Expected: Produces `OpenCohesia.EpochLease.Extensions.Hosting.*.nupkg`

**Step 3: Clean up**

Run: `rm -rf /tmp/nupkg-verify`
