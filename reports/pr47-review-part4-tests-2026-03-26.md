# PR #47 Review (Part 4 Retry): Test Quality & Coverage Sanity

- Date: 2026-03-26
- Scope reviewed:
  - `ExcelReport/ExcelReportLib.Tests/*` changed files
  - `ExcelReport/ExcelReportLibTest/TestDsl/*v2*` changed files

## Prioritized Findings

### P1: Missing regression test for `formulaRef` global-vs-unique-descendant collision (silent retarget risk)
- Evidence:
  - `ExcelReport/ExcelReportLib.Tests/WorksheetStateTests.cs:655-752` covers ambiguous descendant locals and global fallback, but there is no case with **exactly one descendant-local match + existing global same name**.
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs:1030-1090` and `:1257-1297` cover local resolution success paths, but not the collision case above.
- Why this is high risk:
  - This is the easiest path for silent behavior change (formula placeholder unexpectedly rebinding from global to descendant local).
- Concrete test additions:
  - Add `WorksheetStateTests.Build_FormulaRefPlaceholders_UniqueDescendantLocal_WithExistingGlobal_DoesNotSilentlyOverrideGlobal`:
    - Setup one global `RowData`, one descendant local `RowData`, and a sheet-scope `=#{RowData}`.
    - Assert resolved reference is the intended one (global, or at minimum warning + deterministic documented behavior).
  - Add `ReportGeneratorTests.Generate_FormulaPlaceholder_UniqueDescendantLocal_WithExistingGlobal_BehaviorPinned_E2E`:
    - Assert generated formula anchor cell and expected warning presence/absence explicitly.

### P1: No test for sheet-scope conditional `formulaRef` local-leak path
- Evidence:
  - `ExcelReport/ExcelReportLib.Tests/WorksheetStateTests.cs:598-620` validates non-leak for `at="RowData"` target resolution only.
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs:991-1024` also verifies only the target side (rule disappearance), not `formulaRef` resolution behavior.
  - No test asserts sheet-scope `conditionalFormatting formulaRef="..."` cannot bind descendant-local candidates.
- Why this is high risk:
  - A leak on `formulaRef` side can change generated condition formulas while target non-leak tests still pass.
- Concrete test additions:
  - Add `WorksheetStateTests.Build_ConditionalFormatting_FormulaRef_FromSheetScope_DoesNotResolveDescendantLocal`:
    - Use sheet-scope rule with fixed target range (e.g., `A1:A1`) and descendant locals for the formulaRef name.
    - Assert resolved formulaRef does not use descendant-local cell.
  - Add `ReportGeneratorTests.Generate_ConditionalFormatting_FormulaRef_FromSheetScope_DoesNotLeak_E2E`:
    - Verify final OpenXML formula text references expected cell (global or raw fallback), and never descendant-local anchor.

### P2: Assertion quality issue: “NoErrors” test can pass with unrelated errors/fatal
- Evidence:
  - `ExcelReport/ExcelReportLib.Tests/ValidateDslTests.cs:149-169` (`ValidateDsl_SheetOptions_TargetGridArea_NoErrors`) only asserts absence of `SheetOptionsTargetNotFound`.
- Why this matters:
  - Test can pass even if other `Error`/`Fatal` issues are present (false positive).
- Concrete test additions:
  - Strengthen this test (or add companion test) to assert:
    - `result.HasFatal == false`
    - no `IssueSeverity.Error`/`IssueSeverity.Fatal` at all.
  - Add negative counterpart:
    - `ValidateDsl_SheetOptions_TargetMissingArea_ReturnsSheetOptionsTargetNotFound`.

### P2: Naming/intent mismatch in repeat-scope conditional-formatting E2E
- Evidence:
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs:605-617` method name says `DefinedOnRepeat`, but DSL defines `<conditionalFormatting .../>` directly under `<sheet>`, not inside `<repeat>`.
- Why this matters:
  - Creates false confidence that repeat-scope definition path is covered by this test.
- Concrete test additions:
  - Either rename current test to reflect actual intent (targeting repeat area from sheet scope), or
  - Move rule under `<repeat>` and assert per-iteration expansion explicitly.
  - Keep/extend `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs:943-985` as the true repeat-defined coverage path.

### P3: Duplicate E2E scenario reduces net coverage value
- Evidence:
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs:784-806` and `:1096-1118` are effectively the same scenario/assertion (`at="Detail.Value"` => `$B$2:$B$3`).
- Why this matters:
  - Adds maintenance cost without increasing behavior coverage.
- Concrete test additions:
  - Replace one duplicate with a missing negative/e2e case, e.g.:
    - sheet-scope `formulaRef` local-leak prevention,
    - or global/local same-name collision + warning propagation check.

