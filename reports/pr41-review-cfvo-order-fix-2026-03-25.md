# PR #41 レビュー指摘対応（cfvo/color 順序）(2026-03-25)

## 指摘内容
- `colorScale` の子要素順序が `cfvo` と `color` で交互になっており、`CT_ColorScale` の順序要件（cfvoを先に全て、その後color）に反する可能性がある。

## 対応内容
- `XlsxRenderer` の colorScale 生成順序を修正。
  - 修正前: `cfvo -> color -> cfvo -> color ...`
  - 修正後: `cfvo... -> color...`
- `RendererTests.Render_SheetOptionsWithNamedTargets_AppliedAfterStateBuild` に子要素順序アサーションを追加。

## 結果
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj` 実行。
- Passed: 137, Failed: 0
