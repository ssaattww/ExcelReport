# Task 2: DslParser Fixes Evidence Report

## Scope

This report covers the DslParser fixes requested for Task 2:

- AST attribute intake gaps in `RepeatAst`, `CellAst`, `SheetAst`
- `componentImport` external `<styles>` intake
- `sheetOptions` XML shape fix for `<groups>/<groupCols>`
- `StyleAst` border parsing shape fix
- XSD contract fixes for `style@scope` and `border@mode="cell"`
- Converting duplicate-placement `ToDictionary` failures into `Issue`-based handling

## Fixes Implemented

### 1. `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/RepeatAst.cs`

- Change:
  - `repeat@from` is now read and assigned to `RepeatAst.FromExprRaw`.
- Reason:
  - `DslParser_DetailDesign_v1.md` maps `repeat@from` to `FromExprRaw`.
  - The sample DSL uses `from="@(root.Lines)"`, and the previous implementation always left `FromExprRaw` empty.

### 2. `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs`

- Change:
  - `cell@styleRef` is now assigned to `CellAst.StyleRefShortcut`.
  - `cell@formulaRef` is now assigned to `CellAst.FormulaRef`.
- Reason:
  - `DslParser_DetailDesign_v1.md` and `DslDefinition_DetailDesign_v1.md` both define these attributes.
  - The external component sample uses both forms, and the previous implementation ignored them.

### 3. `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs`

- Change:
  - When loading an external `<components>` file, the optional child `<styles>` section is now parsed into `ComponentImportAst.Styles`.
  - Relative `styleImport` paths inside that imported file are resolved from the imported file's directory.
- Reason:
  - `DslDefinition_DetailDesign_v1.md` explicitly calls out `componentImport`-side `<styles>` support.
  - `DslParser.ResolveStyleRefs` already expects `ComponentImportAst.Styles`; the AST was the missing link.

### 4. `ExcelReport/ExcelReportLib/DSL/AST/SheetOptionsAst.cs`

- Change:
  - `GroupCols` is now read from `<sheetOptions><groups><groupCols .../></groups></sheetOptions>`.
  - `GroupRows` and `GroupCols` now share the same `<groups>` parent lookup.
- Reason:
  - `Design/DslDefinition/DslDefinition_v1.xsd` defines `groupRows` and `groupCols` under `GroupsType`.
  - The previous implementation incorrectly searched `groupCols` directly under `sheetOptions`.

### 5. `ExcelReport/ExcelReportLib/DSL/AST/StyleAst.cs`

- Change:
  - Border parsing now reads repeated direct child `<border .../>` elements.
  - The broken lookup for nested `<borders>` children was removed.
- Reason:
  - Both XSD and design samples define border entries as direct repeated `<border>` elements.
  - This was the remaining Task 1 mismatch noted in `reports/task1-dsl-unification-2026-03-03.md`.

### 6. `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs`

- Change:
  - Added `SheetAst.Rows` and `SheetAst.Cols`.
  - `sheet@rows` and `sheet@cols` are now parsed and retained.
  - Missing or invalid values now add `Issue`s instead of silently disappearing.
- Reason:
  - `Design/DslDefinition/DslDefinition_v1.xsd` marks both attributes as required.
  - The project survey report flagged these values as currently dropped.

### 7. `ExcelReport/ExcelReportLib/DSL/AST/Common.cs`

- Change:
  - Added `AstDictionaryBuilder.BuildLayoutNodeMap(...)` to construct child-placement maps without throwing.
  - Duplicate placements now add an `Issue` and keep the first entry instead of failing with `ToDictionary`.
- Reason:
  - This addresses the quality issue called out for duplicate coordinates in AST construction.
  - The change centralizes the behavior used by both `SheetAst` and `GridAst`.

### 8. `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/GridAst.cs`

- Change:
  - Replaced direct `ToDictionary(...)` with `AstDictionaryBuilder.BuildLayoutNodeMap(...)`.
- Reason:
  - Prevents `ArgumentException` on duplicate child placement and converts it to controlled `Issue` handling.

### 9. `Design/DslDefinition/DslDefinition_v1.xsd`

- Change:
  - Added `cell` to `BorderModeEnum`.
  - Added `StyleScopeEnum` (`cell`, `grid`, `both`).
  - Added `style@scope` to `StyleType`.
- Reason:
  - The design samples already use `scope="cell"` / `scope="grid"` and `border mode="cell"`.
  - This closes the contract gap identified in Task 1 and the survey report.

### 10. `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`

- Change:
  - Mirrored the same XSD contract changes made to the design-side XSD.
- Reason:
  - Keeps the test DSL contract aligned with the design contract and sample files.

## Design Alignment Check

### Aligned After This Task

- `repeat@from` -> `RepeatAst.FromExprRaw`
  - Matches `Design/DslParser/DslParser_DetailDesign_v1.md`.
- `cell@styleRef` and `cell@formulaRef`
  - Matches both parser detail design and DSL definition detail design.
- `componentImport` with child `<styles>`
  - Matches `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` section 10 / section 11 examples.
- `sheetOptions/groups/groupCols`
  - Matches `Design/DslDefinition/DslDefinition_v1.xsd`.
- `StyleAst` direct repeated `<border>` parsing
  - Matches `Design/DslDefinition/DslDefinition_v1.xsd`, parser detail design pseudocode, and current samples.
- `style@scope` and `border@mode="cell"` in XSD
  - Matches current sample XML and `StyleAst` parsing behavior.

### Remaining Design Nuance

- `SheetAst.Rows` / `SheetAst.Cols` are now retained to match the XSD and the survey findings.
- `Design/DslParser/DslParser_DetailDesign_v1.md` still does not list these two fields in the `SheetAst` snippet/table, so the code now follows the stronger contract source (`DslDefinition_v1.xsd`) and the project survey expectation.

## Verification Evidence

- Source review completed for:
  - target AST files
  - `Design/DslParser/DslParser_DetailDesign_v1.md`
  - `Design/DslDefinition/DslDefinition_DetailDesign_v1.md`
  - design/test XSD files
  - design/test sample XML files
  - referenced reports (`excel-report-project-survey-2026-03-03.md`, `task1-dsl-unification-2026-03-03.md`)
- `xmllint --noout` passed for:
  - `Design/DslDefinition/DslDefinition_v1.xsd`
  - `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`
- `dotnet build` could not be completed in this environment because the projects target `net10.0`, while the installed SDK is `.NET SDK 8.0.416` (`NETSDK1045`).

## Remaining Known Out-of-Scope Items

- `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
  - `ValidateDsl(...)` remains a stub.
  - XSD validation is still commented out and not executed even when `EnableSchemaValidation=true`.
  - `TreatExpressionSyntaxErrorAsFatal` is still unused.
- Expression validation remains unimplemented.
- `formulaRef` series validation remains unimplemented.
- Style scope validation (`StyleScopeViolation`) remains unimplemented.
- Static layout validation (coordinate bounds, overlap policy beyond duplicate dictionary insertion handling) remains incomplete.
- `ParseFromFile(...)` still has the existing `RootFilePath` explicit-options caveat noted in the survey report.
