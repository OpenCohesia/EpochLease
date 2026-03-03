# NuGet Packaging and Publishing Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Set up independent versioning for both NuGet packages and create manual-dispatch publishing workflows.

**Architecture:** Each source project gets a `<Version>` property. `dotnet pack` natively converts ProjectReferences to NuGet dependencies. Two GitHub Actions workflows handle publishing: one for core+hosting (when core changes), one for hosting-only.

**Tech Stack:** MSBuild/NuGet packaging, GitHub Actions workflow_dispatch

**Design doc:** `docs/plans/2026-03-03-nuget-packaging-and-publishing-design.md`

---

### Task 1: Add Version and IsPackable to Core Project

**Files:**
- Modify: `src/EpochLease/EpochLease.csproj:2-8`

**Step 1: Add Version and IsPackable properties**

Add `<Version>` and `<IsPackable>` to the existing `<PropertyGroup>` in `src/EpochLease/EpochLease.csproj`:

```xml
<PropertyGroup>
    <Version>0.1.0</Version>
    <IsPackable>true</IsPackable>
    <RootNamespace>OpenCohesia.EpochLease</RootNamespace>
    <!-- ...rest of existing properties unchanged... -->
</PropertyGroup>
```

**Step 2: Verify the solution builds**

Run: `mise run dotnet:build`
Expected: Build succeeds with no errors.

**Step 3: Commit**

```bash
git add src/EpochLease/EpochLease.csproj
git commit -m "chore: add Version and IsPackable to core package"
```

---

### Task 2: Add Version and IsPackable to Hosting Project

**Files:**
- Modify: `src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj:2-8`

**Step 1: Add Version and IsPackable properties**

Add `<Version>` and `<IsPackable>` to the existing `<PropertyGroup>` in `src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj`:

```xml
<PropertyGroup>
    <Version>0.1.0</Version>
    <IsPackable>true</IsPackable>
    <RootNamespace>OpenCohesia.EpochLease.Extensions.Hosting</RootNamespace>
    <!-- ...rest of existing properties unchanged... -->
</PropertyGroup>
```

**Step 2: Verify the solution builds**

Run: `mise run dotnet:build`
Expected: Build succeeds with no errors.

**Step 3: Commit**

```bash
git add src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj
git commit -m "chore: add Version and IsPackable to hosting package"
```

---

### Task 3: Verify NuGet Pack Behavior

This is a critical verification step — confirm that `dotnet pack` correctly converts the ProjectReference into a NuGet dependency.

**Step 1: Pack the core package**

Run: `dotnet pack src/EpochLease/EpochLease.csproj -c Release -o ./nupkgs`
Expected: Successfully creates `./nupkgs/OpenCohesia.EpochLease.0.1.0.nupkg`

**Step 2: Pack the hosting package**

Run: `dotnet pack src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj -c Release -o ./nupkgs`
Expected: Successfully creates `./nupkgs/OpenCohesia.EpochLease.Extensions.Hosting.0.1.0.nupkg`

**Step 3: Inspect the hosting package to verify dependencies**

Run: `dotnet nuget inspect ./nupkgs/OpenCohesia.EpochLease.Extensions.Hosting.0.1.0.nupkg`

If `dotnet nuget inspect` is not available, use:
```bash
unzip -p ./nupkgs/OpenCohesia.EpochLease.Extensions.Hosting.0.1.0.nupkg "*.nuspec" | cat
```

Expected: The `.nuspec` inside the package should contain a `<dependency>` entry for `OpenCohesia.EpochLease` version `0.1.0`, NOT a bundled DLL. It should look similar to:
```xml
<dependency id="OpenCohesia.EpochLease" version="0.1.0" exclude="Build,Analyzers" />
```

**Step 4: Clean up generated packages**

Run: `rm -rf ./nupkgs`

No commit needed — this is a verification step.

---

### Task 4: Create Publish Core + Hosting Workflow

**Files:**
- Create: `.github/workflows/publish-core.yml`

**Step 1: Create the workflow file**

Create `.github/workflows/publish-core.yml`:

```yaml
name: Publish Core + Hosting

on:
  workflow_dispatch:

permissions:
  contents: read
  packages: write

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release --verbosity minimal

      - name: Pack Core
        run: dotnet pack src/EpochLease/EpochLease.csproj --no-build -c Release -o ./nupkgs

      - name: Pack Hosting
        run: dotnet pack src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj --no-build -c Release -o ./nupkgs

      - name: Publish to NuGet
        run: dotnet nuget push ./nupkgs/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

**Step 2: Commit**

```bash
git add .github/workflows/publish-core.yml
git commit -m "ci: add publish-core workflow for both packages"
```

---

### Task 5: Create Publish Hosting Workflow

**Files:**
- Create: `.github/workflows/publish-hosting.yml`

**Step 1: Create the workflow file**

Create `.github/workflows/publish-hosting.yml`:

```yaml
name: Publish Hosting

on:
  workflow_dispatch:

permissions:
  contents: read
  packages: write

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release --verbosity minimal

      - name: Pack Hosting
        run: dotnet pack src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj --no-build -c Release -o ./nupkgs

      - name: Publish to NuGet
        run: dotnet nuget push ./nupkgs/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

**Step 2: Commit**

```bash
git add .github/workflows/publish-hosting.yml
git commit -m "ci: add publish-hosting workflow for hosting-only releases"
```

---

### Task 6: Remove Old Publish Workflow

**Files:**
- Remove: `.github/workflows/publish.yml`

**Step 1: Remove the old workflow**

Run: `git rm .github/workflows/publish.yml`

**Step 2: Commit**

```bash
git commit -m "ci: remove release-triggered publish workflow

Replaced by manual-dispatch workflows publish-core.yml and publish-hosting.yml."
```

---

### Task 7: Update CONTRIBUTING.md with Publishing Documentation

**Files:**
- Modify: `CONTRIBUTING.md`

**Step 1: Add Publishing Packages section**

Append the following section to `CONTRIBUTING.md` after the "Testing Requirements" section:

```markdown
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
2. Merge the PR to `main`
3. Go to **Actions → "Publish Core + Hosting" → Run workflow**

### When only hosting changes (publish hosting only)

1. Bump `<Version>` in `src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj` only
2. Merge the PR to `main`
3. Go to **Actions → "Publish Hosting" → Run workflow**

### Version bumping rules

- Follow [Semantic Versioning](https://semver.org/): MAJOR for breaking changes, MINOR for new features, PATCH for bug fixes
- When publishing both: core gets the appropriate bump, hosting gets at least PATCH
- Versions are code-reviewed as part of the PR
```

**Step 2: Commit**

```bash
git add CONTRIBUTING.md
git commit -m "docs: add publishing packages section to CONTRIBUTING.md"
```

---

### Task 8: Fix README NuGet Badge and Install Instructions

The PackageId was previously renamed to `OpenCohesia.EpochLease` but the README still references the old names.

**Files:**
- Modify: `README.md:4` (NuGet badge)
- Modify: `README.md:23-24` (install instructions)

**Step 1: Update NuGet badge**

In `README.md` line 4, change:
```markdown
[![NuGet](https://img.shields.io/nuget/v/EpochLease.svg)](https://www.nuget.org/packages/EpochLease)
```
To:
```markdown
[![NuGet](https://img.shields.io/nuget/v/OpenCohesia.EpochLease.svg)](https://www.nuget.org/packages/OpenCohesia.EpochLease)
```

**Step 2: Update install instructions**

In `README.md` lines 23-24, change:
```bash
dotnet add package EpochLease
dotnet add package EpochLease.Extensions.Hosting
```
To:
```bash
dotnet add package OpenCohesia.EpochLease
dotnet add package OpenCohesia.EpochLease.Extensions.Hosting
```

**Step 3: Verify no other stale package name references**

Run: `grep -rn "dotnet add package EpochLease" README.md`
Expected: No matches (all updated).

**Step 4: Commit**

```bash
git add README.md
git commit -m "docs: update NuGet badge and install instructions to OpenCohesia prefix"
```

---

### Task 9: Final Verification

**Step 1: Run all tests**

Run: `mise run dotnet:test`
Expected: All tests pass.

**Step 2: Verify pack produces correct packages**

Run:
```bash
dotnet pack src/EpochLease/EpochLease.csproj -c Release -o ./nupkgs
dotnet pack src/EpochLease.Extensions.Hosting/EpochLease.Extensions.Hosting.csproj -c Release -o ./nupkgs
```

Expected: Two `.nupkg` files in `./nupkgs/`:
- `OpenCohesia.EpochLease.0.1.0.nupkg`
- `OpenCohesia.EpochLease.Extensions.Hosting.0.1.0.nupkg`

**Step 3: Verify hosting package dependency**

Run:
```bash
unzip -p ./nupkgs/OpenCohesia.EpochLease.Extensions.Hosting.0.1.0.nupkg "*.nuspec"
```

Expected: Contains `<dependency id="OpenCohesia.EpochLease" version="0.1.0" ...>`

**Step 4: Clean up**

Run: `rm -rf ./nupkgs`

**Step 5: Verify workflow YAML is valid**

Run:
```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/publish-core.yml')); print('publish-core.yml: valid')"
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/publish-hosting.yml')); print('publish-hosting.yml: valid')"
```

Expected: Both files are valid YAML.
