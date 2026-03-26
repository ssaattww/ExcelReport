# PR #47 Review (Part 3): CI / Release / Versioning / Reporting Pipeline

- Date: 2026-03-26
- Scope:
  - `.github/workflows/publish-nuget.yml`
  - `ExcelReport/ExcelReportLib/ReportGenerator.cs`
  - `ExcelReport/ExcelReportLib/ExcelReportLib.csproj`
  - `Design/BreakingChanges.md`
  - Relevant consistency notes from existing reports

## Prioritized Findings

### P1: `workflow_dispatch` can publish arbitrary-ref packages with an outdated fallback version line
- File: `.github/workflows/publish-nuget.yml:10`, `.github/workflows/publish-nuget.yml:79`, `.github/workflows/publish-nuget.yml:111-117`
- Risk:
  - Manual dispatch is enabled, and publish runs without branch/release guard.
  - For `workflow_dispatch`, version is forced to `${VersionPrefix}-ci.${GITHUB_RUN_NUMBER}`; current `VersionPrefix` is `0.1.0` (`ExcelReport/ExcelReportLib/ExcelReportLib.csproj:8`), which is inconsistent with current release line expectations.
  - This allows accidental or intentional publication from non-release refs with unexpected versioning.
- Suggested fix:
  - Split into two jobs:
    - `build-pack` for all triggers.
    - `publish` only for `release.published` and optionally `push(master)` with explicit `if`.
  - Or keep one job but gate `dotnet nuget push` with `if: github.event_name == 'release' || (github.event_name == 'push' && github.ref_name == 'master')`.
  - For `workflow_dispatch`, default to dry-run (pack only) unless an explicit input like `publish=true` is provided and protected.

### P1: Pre-release base tag selection is not restricted to tags reachable from `HEAD`
- File: `.github/workflows/publish-nuget.yml:57-60`
- Risk:
  - `git tag --sort=-v:refname` selects the global latest stable tag, not necessarily one merged into `master`.
  - If a higher stable tag exists on another branch, pre-release version calculation can jump to the wrong base and produce incorrect `major.minor.patch-pre`.
- Suggested fix:
  - Select only reachable stable tags:
    - `git tag --merged HEAD --sort=-v:refname | grep -E '^[0-9]+\\.[0-9]+\\.[0-9]+$' | head -n 1`
  - Add a guard that fails when no semver-stable merged tag exists and policy requires one.

### P2: Token/secret scope is broader than necessary for all triggers
- File: `.github/workflows/publish-nuget.yml:15-19`, `.github/workflows/publish-nuget.yml:121`
- Risk:
  - `contents: write` is granted at job scope for all triggers, though only pre-release creation requires it.
  - `NUGET_API_KEY` is exposed at job scope, so non-publish steps receive publish credentials.
- Suggested fix:
  - Split jobs by responsibility and grant `contents: write` only to the release-creation job.
  - Move `NUGET_API_KEY` from job-level `env` to the `Publish to NuGet.org` step `env`.
  - Keep default minimal permissions (`contents: read`) for build/pack-only paths.

### P2: PR reference note generation masks API/permission failures as “not found”
- File: `.github/workflows/publish-nuget.yml:136`, `.github/workflows/publish-nuget.yml:144`
- Risk:
  - `gh api ... 2>/dev/null || true` suppresses failure cause.
  - Release notes may incorrectly claim no related PR when API permissions/endpoints fail, reducing traceability.
- Suggested fix:
  - Capture stderr and exit code explicitly.
  - Emit a warning section in notes like “PR lookup failed (API error)” with the error text.
  - Optionally `exit 1` for release events when PR-link completeness is mandatory.

### P2: Unhandled exceptions are logged as `Rendering` phase regardless of actual failure phase
- File: `ExcelReport/ExcelReportLib/ReportGenerator.cs:87`, `ExcelReport/ExcelReportLib/ReportGenerator.cs:147`
- Risk:
  - Both top-level catch blocks hardcode `ReportPhase.Rendering`.
  - Parsing/load failures can be mislabeled as rendering failures, degrading diagnostics and triage quality.
- Suggested fix:
  - Track current phase in a local variable (`phase = Parsing -> StyleResolving -> LayoutExpanding -> Rendering`) and log with that value in catch.
  - Include richer exception text (`ex.ToString()`) so stack information is preserved in logs.

### P2: Unhandled exception path does not add a fatal `Issue`, so issue-only consumers can miss hard failures
- File: `ExcelReport/ExcelReportLib/ReportGenerator.cs:88-89`, `ExcelReport/ExcelReportLib/ReportGenerator.cs:148-149`
- Risk:
  - On generic exceptions, result returns `UnhandledException` but does not append a fatal `Issue`.
  - Downstream pipelines that rely primarily on `Issues`/`LogEntries` may under-report failures.
- Suggested fix:
  - In generic catch, add a fatal issue (dedicated kind if available, or a documented fallback kind), then return result with both `Issues` and `UnhandledException`.
  - Add tests asserting fatal issue presence for unexpected exceptions in `Generate`/`GenerateFromFile`.

### P3: Breaking change version boundary is likely inconsistent with current release progression
- File: `Design/BreakingChanges.md:31-33`, `Design/BreakingChanges.md:42-44`
- Risk:
  - Both entries use `Version before change: 1.2.4` and `Planned effective version: after 1.2.4`.
  - Current repository tag set already includes later stable tags (e.g., `1.3.0` observed locally), so these boundaries may mislead release-note consumers unless branch/release-line context is explicitly stated.
- Suggested fix:
  - Align “Version before change” with the actual target release line used for publishing, or add an explicit branch/release-line qualifier in each entry.
  - At release time, ensure “Confirmed effective version” is updated from GitHub Releases `tagName` as required by the file’s own rules.

## Consistency Note (Relevant Reports)

- `reports/release-nuget-sync-investigation-2026-03-19.md:17` documents `-pre.<GITHUB_RUN_NUMBER>` behavior, while current workflow uses `-pre` (`.github/workflows/publish-nuget.yml:47`).
- This is likely intentional drift from later design decisions, but it should be reconciled to avoid operator confusion.
