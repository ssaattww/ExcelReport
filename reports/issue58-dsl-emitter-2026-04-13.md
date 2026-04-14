# issue #58 DslEmitter

Date: 2026-04-13

## Summary
- 対象 task: `R58-03 DslEmitter の基本出力を実装する`
- あわせて `R58-04 DslEmitter で cell@formula / styleOverflow / direction="down" を反映する` まで同時に完了
- 方針: `DslEmitter` は `XmlTemplateSerializer` の DSL 互換 XML を再利用し、UTF-8 declaration 付き text 出力の責務に絞る

## TDD
1. Red
   - `DslEmitterTests` を追加し、`DslEmitter` 未定義の compile error を確認
2. Green
   - `DslEmitter` を追加
   - `XmlTemplateSerializer` の `XDocument` を UTF-8 declaration 付き XML text へ整形出力
3. Verify
   - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter DslEmitterTests`
     - Passed 2
   - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "ExcelTemplate|XmlTemplateSerializerTests|DslEmitterTests"`
     - Passed 26
   - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`
     - Passed 242

## Fixed Behavior
- XML declaration: `<?xml version="1.0" encoding="utf-8"?>`
- DSL root/body: `workbook/component/grid/sheet/cell/use/repeat`
- `cell@formula` を text 出力へ保持
- explicit `use@styleOverflow` を text 出力へ保持
- `repeat@direction="down"` を text 出力へ保持
- emitted text は `DslParser + XSD validation` で parse 可能

## Files
- `ExcelReport/ExcelReportLib/ExcelTemplate/DslEmitter.cs`
- `ExcelReport/ExcelReportLib.Tests/DslEmitterTests.cs`

## Residual Risk
- `DslEmitter` は serializer の薄い wrapper なので、text 形状の安定化は `R58-06` の snapshot で固定する必要がある
