# issue #58 Conversion API Implementation Record

- Date: 2026-04-14
- Scope: Phase 12 / R58-07, R58-08
- Reference: `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`

## Summary

- Added `ExcelTemplateConverter` as the conversion-only entry point for `xlsx -> DSL text` and `xlsx -> XML template text`.
- Added `ExcelTemplateConversionResult` and `ExcelTemplateConvertOptions`.
- Aggregated conversion issues from extractor/contract/parser into `result.Issues`.
- Hardened the public API so corrupt `xlsx` input is returned as a fatal `IssueKind.LoadFile` instead of leaking `FileFormatException`.
- Rechecked remaining work against the detail design before implementation. No missing phase/task was found beyond `Phase 13-14 / R58-09..R58-12`.

## Implemented Behavior

- `ConvertToDsl(string xlsxPath, ExcelTemplateConvertOptions? options = null)`
  - extracts workbook
  - builds normalized output contract
  - emits DSL text
  - optionally parses emitted text with schema validation and appends parser issues
- `ConvertToXmlTemplate(string xlsxPath, ExcelTemplateConvertOptions? options = null)`
  - shares the same pipeline
  - emits DSL-compatible debug XML text
- Input validation
  - blank path -> fatal `LoadFile`
  - file not found -> fatal `LoadFile`
  - corrupt/invalid workbook container -> fatal `LoadFile`

## TDD

- Added `ExcelTemplateConverterTests`
  - valid workbook -> DSL text, no issues
  - issue workbook -> XML text, aggregated issues
  - corrupt workbook -> fatal load issue
  - schema validation opt-out -> conversion issues only
- Added `ExcelTemplateTestWorkbookFactory`
  - valid workbook fixture
  - issue workbook fixture

## Verification

- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter ExcelTemplateConverterTests`
  - Passed 4
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "ExcelTemplate|XmlTemplateSerializerTests|DslEmitterTests|ExcelTemplateSnapshotTests|ExcelTemplateConverterTests"`
  - Passed 32
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`
  - Passed 248
