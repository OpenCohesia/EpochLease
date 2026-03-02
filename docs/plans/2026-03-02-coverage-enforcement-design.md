# CI Coverage Enforcement

## Goal

Enforce a minimum branch coverage threshold of 90% in CI so PRs that drop below are blocked.

## Approach

Use Coverlet MSBuild's built-in threshold mechanism. The `coverlet.msbuild` package is already referenced in both test projects. Adding Coverlet MSBuild properties to the `dotnet test` step in CI enables coverage collection and threshold enforcement with zero additional dependencies.

## Changes

**Single file:** `.github/workflows/build-and-test.yml`

Update the Test step to pass Coverlet properties:

- `/p:CollectCoverage=true` — enable coverage collection
- `/p:Threshold=90` — minimum percentage required
- `/p:ThresholdType=branch` — enforce on branch coverage
- `/p:ThresholdStat=total` — apply to total across all assemblies
- `/p:CoverletOutputFormat=cobertura` — standard output format
- `/p:SkipAutoProps=true` — exclude auto-properties from metrics

## Behavior

If total branch coverage falls below 90%, `dotnet test` returns a non-zero exit code and the `build-and-test` CI check fails, blocking the PR.

## Current State

- Current branch coverage: 71.7%
- Tests will need to be added to reach the 90% threshold
