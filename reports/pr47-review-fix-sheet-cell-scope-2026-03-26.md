# PR #47 Review Fix: Sheet Cell Local Scope

- Date: 2026-03-26
- PR: https://github.com/ssaattww/ExcelReport/pull/47

## Review Finding

- P1: Top-level sheet children were assigned `/sheet/node-{index}` scope uniformly, which isolated sibling leaf `<cell>` nodes and regressed local `formulaRef` resolution across direct sheet children.

## Fix

- Updated `LayoutEngine.ExpandSheet` scope assignment:
  - direct `CellAst` child => `/sheet`
  - non-`CellAst` child => `/sheet/node-{index}`

This preserves existing top-level container isolation while restoring direct sheet-cell sibling local visibility.

## Tests Added

- `LayoutEngineTests.Expand_SheetCellSiblings_ShareLocalScopePath`
- `ReportGeneratorTests.Generate_SheetCellSiblingFormula_ResolvesLocalFormulaRef`

## Verification

- `dotnet test --no-restore --filter "FullyQualifiedName~Expand_SheetCellSiblings_ShareLocalScopePath|FullyQualifiedName~Generate_SheetCellSiblingFormula_ResolvesLocalFormulaRef"`  
  - Passed: 2
- `dotnet test --no-restore --filter "WorksheetStateTests|ReportGeneratorTests|LayoutEngineTests"`  
  - Passed: 83
