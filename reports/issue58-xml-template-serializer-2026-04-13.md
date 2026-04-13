# issue #58 XmlTemplateSerializer

Date: 2026-04-13

## Summary
- 対象 task: `R58-02 XmlTemplateSerializer の workbook/component/sheet 出力を実装する`
- 方針: user 指摘に合わせ、debug XML であっても DSL 互換を優先し、`DslParser` で parse 可能な `<workbook xmlns="urn:excelreport:v2">` を直接出力する

## TDD
1. Red
   - `XmlTemplateSerializerTests` を追加し、`XmlTemplateSerializer` 未実装の compile error を作った
   - テストでは構造確認に加えて `DslParser.ParseFromText` まで行い、DSL 互換を先に固定した
2. Green
   - `XmlTemplateSerializer` を追加
   - `component` は `<grid>` body に正規化し、resolved range がある場合のみ `rows/cols` を付与
   - `sheet` は直下に `cell` / `use` / `repeat` を配置
   - unresolved component や issue は schema を壊さないよう XML comment へ退避し、未解決 component 自体は DSL 本体から除外
3. Verify
   - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter XmlTemplateSerializerTests`
     - Passed 4
   - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "ExcelTemplate|XmlTemplateSerializerTests"`
     - Passed 24
   - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`
     - Passed 240

## Serialization Rules Fixed In This Step
- root: `<workbook xmlns="urn:excelreport:v2">`
- component: `<component name=\"...\"><grid ...>...</grid></component>`
- component item placement: `grid` 配下の `cell` / `repeat` / `use` に `r` / `c` を付与
- repeat-use: `<repeat from=\"...\" var=\"...\" direction=\"down\"><use component=\"...\" /></repeat>`
- formula cell: `cell@formula`
- simple use: `use@component`
- unresolved component / conversion issues: XML comment として保持
- schema compatibility: `DslParser` + embedded XSD validation を有効にした parse までテストで固定
- serializer branch coverage: style-only empty cell と explicit `styleOverflow` 出力を追加テストで固定

## Files
- `ExcelReport/ExcelReportLib/ExcelTemplate/XmlTemplateSerializer.cs`
- `ExcelReport/ExcelReportLib.Tests/XmlTemplateSerializerTests.cs`
- `ExcelReport/ExcelReportLib.Tests/ExcelTemplateOutputContractFixture.cs`

## Residual Risk
- style index はまだ DSL 表現へ落としていないため、debug XML 上では書式 source 情報を comment 以上には出していない
- 次の `R58-03` / `R58-04` では serializer と emitter の責務重複を避けつつ、text 出力の整形規則を固定する必要がある
