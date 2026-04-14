# issue #58 ExcelTemplate Facade Implementation Record

- Date: 2026-04-14
- Scope: Phase 13 / R58-09, R58-10
- Reference: `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`

## Summary

- Added `ExcelTemplateReportGenerator` as the facade from `xlsx` directly to final `xlsx`.
- Added `ExcelTemplateGenerateOptions` to separate conversion options from downstream `ReportGeneratorOptions`.
- Kept the existing `ReportGenerator` public API unchanged and implemented the ExcelTemplate entry point as a thin composition layer.
- Preserved conversion issues on the final `ReportGeneratorResult`.
- Short-circuited the facade before DSL execution when conversion returns a fatal issue.

## Implemented Behavior

- `GenerateFromExcelTemplate(string xlsxPath, object? data, ExcelTemplateGenerateOptions? options = null, CancellationToken cancellationToken = default)`
  - converts ExcelTemplate workbook to DSL text
  - logs conversion issues into the same logger used by downstream report generation
  - aborts early on fatal conversion issues
  - otherwise calls the existing `ReportGenerator.Generate`
  - returns a final `ReportGeneratorResult` that includes both conversion issues and report-generation issues

## TDD

- Added `ExcelTemplateReportGeneratorTests`
  - valid workbook -> final xlsx output
  - corrupt workbook -> fatal load issue, no output
  - non-fatal conversion issue -> output preserved and issue merged
- Extended `ExcelTemplateTestWorkbookFactory`
  - report workbook fixture
  - report workbook fixture with non-fatal conversion issue

## Verification

- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter ExcelTemplateReportGeneratorTests`
  - Passed 3
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "ExcelTemplate|ReportGeneratorTests|ExcelTemplateReportGeneratorTests|ExcelTemplateConverterTests|XmlTemplateSerializerTests|DslEmitterTests|ExcelTemplateSnapshotTests"`
  - Passed 81
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`
  - Passed 251
