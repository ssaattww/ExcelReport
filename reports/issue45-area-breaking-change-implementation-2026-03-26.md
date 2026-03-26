# issue #45 Area Breaking Change Implementation Report (2026-03-26)

## Summary

- Implemented fully breaking unification of named target attributes to `area`.
- Replaced target attributes:
  - `repeat@name` -> `repeat@area`
  - `use@instance` -> `use@area`
  - added `grid@area`
- Unified named target handling via `INamedAreaTarget.AreaName`.
- Kept conditional formatting resolution behavior:
  - named area first
  - formulaRef series resolution (local scope non-leak maintained)
  - direct range/address fallback

## Breaking Behavior

- Legacy target attributes are no longer supported:
  - `<repeat name="...">`
  - `<use instance="...">`
  - `<grid name="...">`
- In non-schema validation paths, legacy attributes now emit parser `Error` (`IssueKind.InvalidAttributeValue`) to enforce full break.

## Main Code Changes

- XSD
  - `Design/DslDefinition/DslDefinition_v1.xsd`
  - `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`
- AST / Parser / Layout
  - `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/RepeatAst.cs`
  - `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs`
  - `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/GridAst.cs`
  - `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
  - `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
- Test DSL fixture sample
  - `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_Sample_v1.xml`
  - `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml`
- Docs
  - `Design/DslDefinition/DslDefinition_DetailDesign_v1.md`
  - `Design/DslParser/DslParser_DetailDesign_v1.md`
  - `Design/Logger/Logger_DetailDesign.md`
  - `Design/BreakingChanges.md`
- Auxiliary runtime sample output
  - `ExcelReport/ExcelReportExe/Program.cs`

## Tests Added/Updated

- `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs`
  - repeat area target E2E
  - use area target E2E
  - grid area target E2E
  - local formulaRef non-leak behavior E2E
  - formulaRef series and sheet-level cases retained
- `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs`
  - named areas generated from `use@area`, `repeat@area`, `grid@area`
- `ExcelReport/ExcelReportLib.Tests/SheetAstTests.cs`
  - parse coverage for area attributes on grid/use/repeat
- `ExcelReport/ExcelReportLib.Tests/WorksheetStateTests.cs`
  - named area precedence over formulaRef series
- `ExcelReport/ExcelReportLib.Tests/ValidateDslTests.cs`
  - legacy attributes rejected
  - sheetOptions targeting `grid@area` accepted
- `ExcelReport/ExcelReportLib.Tests/LayoutNodeTests.cs`
  - `use@area` and `grid@area` parsing assertions

## Test Execution Notes

Sandbox restrictions blocked default .NET/NuGet profile paths. Tests were executed by redirecting environment variables:

- `APPDATA` -> workspace local
- `DOTNET_CLI_HOME` -> workspace local
- `NUGET_PACKAGES` -> workspace local

Commands and results:

1. `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore --filter "FullyQualifiedName~ReportGeneratorTests.Generate_ConditionalFormatting_"`
   - Passed: 14, Failed: 0
2. `dotnet test ... --no-restore --filter "FullyQualifiedName~LayoutEngineTests.Expand_UseAreaAndRepeatAreaAndGridArea_GeneratesNamedAreas|FullyQualifiedName~SheetAstTests.Parse_Sheet_LayoutNodesWithAreaAttributes_ExposeNamedTargets|FullyQualifiedName~WorksheetStateTests.Build_ConditionalFormatting_Target_NamedArea_PrecedesFormulaRefSeries|FullyQualifiedName~ValidateDslTests.ValidateDsl_LegacyNamedTargetAttributes_ReturnErrors|FullyQualifiedName~ValidateDslTests.ValidateDsl_SheetOptions_TargetGridArea_NoErrors"`
   - Passed: 5, Failed: 0
3. `dotnet test ... --no-restore --filter "FullyQualifiedName~LayoutNodeTests.Parse_Use_HasAreaAttribute|FullyQualifiedName~LayoutNodeTests.Parse_Grid_HasAreaAttribute"`
   - Passed: 2, Failed: 0
4. `dotnet test ... --no-restore --filter "FullyQualifiedName~FullTemplate"`
   - Passed: 8, Failed: 0
5. `dotnet test ... --no-build --no-restore --filter "FullyQualifiedName~DslParserTests|FullyQualifiedName~ValidateDslTests|FullyQualifiedName~LayoutNodeTests|FullyQualifiedName~SheetAstTests|FullyQualifiedName~LayoutEngineTests|FullyQualifiedName~WorksheetStateTests|FullyQualifiedName~RendererTests|FullyQualifiedName~ReportGeneratorTests"`
   - Passed: 127, Failed: 0
6. `dotnet test ... --no-build --no-restore`
   - Passed: 161, Failed: 0

## Additional Verification and Cleanup

- Searched code/tests/docs for remaining active legacy named-target usage in implementation paths; remaining mentions are only:
  - breaking-change/history notes
  - explicit negative test cases that validate legacy rejection
- Updated parser/logger detail design docs to align with `area` terminology (`UseAst.AreaName`, `INamedAreaTarget.AreaName`).
- Fixed `ExcelReportExe/Program.cs` debug walker to use `RepeatAst.AreaName` (removed obsolete `RepeatAst.Name` usage).
- Removed temporary test-run artifacts from repository root:
  - `.appdata`
  - `.nuget`
  - `.dotnet`
- Subsequent tests were executed with temp environment roots outside the repository (`%TEMP%/excelreport-codex-env`).

## Continuation Verification (Latest)

- Restored packages successfully with temp environment roots:
  - `dotnet restore ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`
  - Result: success (both `ExcelReportLib` and `ExcelReportLib.Tests` restored)
- Rebuilt + broad regression (parser/layout/renderer/report-generator scope):
  - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore --filter "FullyQualifiedName~DslParserTests|FullyQualifiedName~ValidateDslTests|FullyQualifiedName~LayoutNodeTests|FullyQualifiedName~SheetAstTests|FullyQualifiedName~LayoutEngineTests|FullyQualifiedName~WorksheetStateTests|FullyQualifiedName~RendererTests|FullyQualifiedName~ReportGeneratorTests"`
  - Result: Passed 127, Failed 0
- Rebuilt + full regression:
  - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore`
  - Result: Passed 161, Failed 0

## Upward Report Snapshot (2026-03-26)

- Status: Completed
- Scope completion:
  - breaking unification to `area` (`repeat@area`, `use@area`, `grid@area`)
  - common named target path (`INamedAreaTarget.AreaName`)
  - conditional formatting resolution order preserved
  - local formulaRef non-leak behavior preserved
  - legacy naming attrs rejected (`repeat@name`, `use@instance`, `grid@name`)
- Verification:
  - targeted + broad tests executed
  - latest full run: `Passed 161, Failed 0`
- Remaining items:
  - none in implementation scope
  - release-time confirmed version update in `Design/BreakingChanges.md` remains operational follow-up (replace `pending` with GitHub Release `tagName`)

## v2 Migration + P1 Fix Continuation (2026-03-26)

- Implemented full DSL contract migration to v2:
  - namespace: `urn:excelreport:v2`
  - schema: `DslDefinition_v2.xsd`
  - fixture/sample files renamed to `*_v2.xml`
  - parser schema resource switched to v2 (`ExcelReportLib.DSL.DslDefinition_v2.xsd`)
- Removed v1 compatibility in parser flow:
  - parser now rejects non-v2 namespace even when schema validation is disabled
  - `styleImport`/`componentImport` also reject non-v2 namespace
- Implemented P1 fix:
  - top-level sibling nodes now use isolated scope paths (`/sheet/node-{index}`)
  - prevents local formulaRef series mixing across sibling definitions
- Added new tests:
  - `LayoutEngineTests.Expand_TopLevelSiblings_IsolateLocalScopePath`
  - `WorksheetStateTests.Build_FormulaPlaceholder_LocalSeries_DoesNotCrossTopLevelSiblings`
  - `ReportGeneratorTests.Generate_ConditionalFormatting_TargetLocalFormulaRefSeries_TopLevelSiblings_DoNotMix`
  - plus v1 rejection test: `ValidateDslTests.ValidateDsl_V1Namespace_WithSchemaValidationDisabled_ReturnsFatalSchemaViolation`

Latest commands and results:

1. `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore --filter "FullyQualifiedName~LayoutEngineTests.Expand_TopLevelSiblings_IsolateLocalScopePath|FullyQualifiedName~WorksheetStateTests.Build_FormulaPlaceholder_LocalSeries_DoesNotCrossTopLevelSiblings|FullyQualifiedName~ReportGeneratorTests.Generate_ConditionalFormatting_TargetLocalFormulaRefSeries_TopLevelSiblings_DoNotMix|FullyQualifiedName~ValidateDslTests.ValidateDsl_V1Namespace_WithSchemaValidationDisabled_ReturnsFatalSchemaViolation"`
   - Passed: 4, Failed: 0
2. `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore --filter "FullyQualifiedName~DslParserTests|FullyQualifiedName~ValidateDslTests|FullyQualifiedName~LayoutNodeTests|FullyQualifiedName~SheetAstTests|FullyQualifiedName~LayoutEngineTests|FullyQualifiedName~WorksheetStateTests|FullyQualifiedName~RendererTests|FullyQualifiedName~ReportGeneratorTests"`
   - Passed: 131, Failed: 0
3. `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore`
   - Passed: 165, Failed: 0
