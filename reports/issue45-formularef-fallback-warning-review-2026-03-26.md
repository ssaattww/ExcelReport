# Issue #45 Review: FormulaRef Fallback Warning

- Date: 2026-03-26
- Scope: local `formulaRef` ambiguity fallback warning

## Summary

Implemented and reviewed the requirement: whenever local `formulaRef` resolution falls back due to ambiguity (or uses deterministic tie-break), a warning is emitted.

## Code Changes Reviewed

1. Worksheet-state issue sink
- `IWorksheetStateBuilder.Build(LayoutPlan, IList<Issue>? issues = null)` added.
- `WorksheetStateBuilder` now accepts optional issue sink and reports non-fatal warnings.

2. Warning emission points
- Ambiguous descendant local match in placeholder resolution (`FindNamedArea`) now records warning before fallback.
- Conditional-formatting `formulaRef` resolution now records warning on:
  - multiple scoped candidates with deterministic tie-break selection
  - multiple local candidates forcing fallback to global/named/raw target

3. Result/log plumbing
- `ReportGenerator` now collects worksheet-state warnings, appends them to `ReportGeneratorResult.Issues`, and logs them in `ReportPhase.LayoutExpanding`.

4. Issue kind
- Added `IssueKind.FormulaRefResolutionFallback`.

## Verification

- Command:
  - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "WorksheetStateTests|ReportGeneratorTests" --no-restore`
- Result:
  - Passed: 57
  - Failed: 0

## Added Assertions

- `WorksheetStateTests`:
  - ambiguous descendant local fallback emits warning and falls back.
  - conditional formulaRef tie-break emits warning.
  - ambiguous local candidates fallback-to-global emits warning.
- `ReportGeneratorTests`:
  - worksheet-state fallback warning appears in both `result.Issues` and `result.LogEntries`.

