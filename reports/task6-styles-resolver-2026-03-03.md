# Task 6 Styles Resolver + StylePlan Evidence

Date: 2026-03-03
Owner: Codex

## Read Set

Reviewed before implementation:

- `Design/Styles/Styles_DetailDesign.md`
- `Design/BasicDesign_v1.md` (Styles section)
- `reports/phase3-styles-expression-plan-2026-03-03.md`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StylesAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleRefAst.cs`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs` (`ResolveStyleRefs` and style index validation)

Applied skills:

- `coding-principles`
- `implementation-approach`

## Implementation Summary

Added a new runtime `ExcelReportLib.Styles` module:

- `ExcelReport/ExcelReportLib/Styles/IStyleResolver.cs`
- `ExcelReport/ExcelReportLib/Styles/StyleResolver.cs`
- `ExcelReport/ExcelReportLib/Styles/StylePlan.cs`
- `ExcelReport/ExcelReportLib/Styles/ResolvedStyle.cs`

Added tests:

- `ExcelReport/ExcelReportLib.Tests/StyleResolverTests.cs`

## Behavior Implemented

- Builds a global style dictionary from `StylesAst`, including recursive `styleImport`, with later definitions overwriting earlier ones inside the resolver dictionary.
- Resolves named styles by `styleRef` name and emits `IssueKind.UndefinedStyle` on unknown references.
- Validates runtime usage scope (`cell` / `grid`) and emits `IssueKind.StyleScopeViolation` warnings.
- On scope violation, only `border` is dropped; non-border properties remain available.
- On `cell` targets, `border mode="outer"` and `mode="all"` are filtered as invalid for cell-level application and logged as warnings.
- Produces a final merged `StylePlan` with precedence:
  - workbook default
  - sheet default
  - referenced styles (in DSL order)
  - inline styles (in DSL order)
- `StylePlan` exposes:
  - final merged values (`font`, `fill`, `numberFormat`, `border`)
  - ordered applied styles
  - per-property trace metadata (`StyleValueTrace`)
  - per-border trace metadata

## Important Technical Note

`StyleAst` typed accessors return `0` / `false` for missing value-type properties because they read through a generic helper.  
`ResolvedStyle` avoids that bug by reading `StyleAst.RawProperties` directly, so missing `font.size`, `font.bold`, `font.italic`, and `font.underline` remain `null` and do not overwrite higher-priority values.

## Verification

Direct `dotnet test` on the repository project could not run in this environment because the installed SDK is `.NET 8`, while the repo targets `net10.0`.

Observed error:

- `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0`

Validation performed instead:

- Created a temporary `net8.0` wrapper project under `/tmp`
- Compiled the current library source into that wrapper
- Executed six runtime checks matching the requested test cases:
  - `Resolve_ByName_ReturnsStyle`
  - `Resolve_UnknownName_ReturnsError`
  - `Resolve_ScopeViolation_ReturnsWarning`
  - `BuildPlan_InlineOverridesRef_CorrectPriority`
  - `BuildPlan_SheetDefault_AppliedWhenNoExplicit`
  - `BuildPlan_MultipleBorders_AllResolved`
- Result: all six checks passed

Existing nullable warnings from unrelated AST files were observed during the temporary build, but they predate this task and were not modified here.
