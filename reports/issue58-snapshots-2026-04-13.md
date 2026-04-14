# issue #58 Snapshot Tests

Date: 2026-04-13

## Summary
- 対象 task:
  - `R58-05 xlsx -> xml snapshot テストを追加する`
  - `R58-06 xlsx -> dsl snapshot テストを追加する`
- 目的: `XmlTemplateSerializer` / `DslEmitter` の shape を外部 fixture で固定し、後続 API 実装で text/structure が揺れないようにする

## Implemented
- `ExcelTemplateSnapshotTests` を追加
- serializer snapshot:
  - `ExcelReport/ExcelReportLib.Tests/TestDsl/Issue58_StandardTemplate_Debug.xml`
  - `XmlTemplateSerializer.Serialize(contract).ToString()` と比較
- DSL snapshot:
  - `ExcelReport/ExcelReportLib.Tests/TestDsl/Issue58_StandardTemplate_Dsl.xml`
  - `DslEmitter.Emit(contract)` と比較
- 比較時は line ending を正規化して cross-platform で安定化

## Verification
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter ExcelTemplateSnapshotTests`
  - Passed 2
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "ExcelTemplate|XmlTemplateSerializerTests|DslEmitterTests|ExcelTemplateSnapshotTests"`
  - Passed 28
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`
  - Passed 244

## Files
- `ExcelReport/ExcelReportLib.Tests/ExcelTemplateSnapshotTests.cs`
- `ExcelReport/ExcelReportLib.Tests/TestDsl/Issue58_StandardTemplate_Debug.xml`
- `ExcelReport/ExcelReportLib.Tests/TestDsl/Issue58_StandardTemplate_Dsl.xml`

## Residual Risk
- snapshot review が終わるまでは、fixture 粒度や deterministic policy の見落としが残る可能性がある
