# PR xUnit Workflow Investigation (2026-03-24)

## Request

Ensure xUnit tests run automatically when a pull request targets `master`.

## Current State

- Existing workflow: `publish-nuget.yml` (release trigger only)
- No CI workflow for pull requests.

## Implementation

Added a new workflow:

- `.github/workflows/pr-xunit-tests.yml`

Key configuration:

- Trigger: `pull_request` on `master`
- Runner: `ubuntu-latest`
- Steps:
  - `actions/checkout@v4`
  - `actions/setup-dotnet@v4` (`8.0.x`)
  - `dotnet restore` for `ExcelReportLib.Tests`
  - `dotnet test` for `ExcelReportLib.Tests`

## Effect

Any PR targeting `master` now executes xUnit tests in GitHub Actions before merge.
