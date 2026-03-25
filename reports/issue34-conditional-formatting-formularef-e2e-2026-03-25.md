# issue #34 追加対応レポート（formulaRef + E2E）(2026-03-25)

## 追加要望
- expression 条件付き書式で `formulaRef` を指定できるようにする。
- E2Eテストを追加（2カラー / 3カラー / 条件一致書式変更）。

## 実装
- `conditionalFormatting` に `formulaRef` 属性を追加。
- `formula` 未指定かつ `formulaRef` 指定時は、`NOT(ISBLANK(<resolvedRef>))` を自動条件式として生成。
- `formulaRef` が NamedArea 名の場合は `WorksheetStateBuilder` でセル参照へ解決。

## E2Eテスト
`ReportGeneratorTests` に以下を追加:
1. `Generate_ConditionalFormatting_TwoColorScale_E2E`
2. `Generate_ConditionalFormatting_ThreeColorScale_E2E`
3. `Generate_ConditionalFormatting_ExpressionWithFormulaRef_E2E`

## テスト結果
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`:
  - Passed: 136
  - Failed: 0
