# PR #47 Follow-up Fixes (Findings 3-6)

- Date: 2026-03-26
- Scope: Review findings #3-#6

## Implemented

1. `FindNamedArea` global vs unique-descendant collision handling
- File: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs`
- Behavior:
  - When both global and unique descendant local match the same name, prefer global.
  - Emit `IssueKind.FormulaRefResolutionFallback` warning.

2. Sheet-scope conditional `formulaRef` fallback leakage tightening
- File: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs`
- Behavior:
  - Keep target-intersection-based local selection behavior.
  - Restrict fallback unique-local scan by `ShouldResolveLocalScopeCandidate` to avoid sheet-scope descendant leakage in fallback path.

3. Import root strict validation
- Files:
  - `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs`
  - `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs`
- Behavior:
  - `styleImport` requires `<styles>` root.
  - `componentImport` requires `<components>` root.
  - Violations produce `Fatal` `SchemaViolation`.

4. Test quality improvements
- Files:
  - `ExcelReport/ExcelReportLib.Tests/WorksheetStateTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/ValidateDslTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/ComponentImportTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/StyleImportTests.cs`
- Added/updated:
  - global/local collision behavior pinning (+warning)
  - sheet-scope conditional formulaRef fallback non-leak behavior
  - strict `NoErrors` assertion in ValidateDsl
  - missing-area negative case in ValidateDsl
  - invalid import-root negative tests for style/component import

## Verification

- Command:
  - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore --filter "WorksheetStateTests|ReportGeneratorTests|ValidateDslTests|ComponentImportTests|StyleImportTests"`
- Result:
  - Passed: 76
  - Failed: 0
