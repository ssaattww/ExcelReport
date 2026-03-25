# Issue #35 E2E確認レポート（repeat + local formulaRefScope）

- 日付: 2026-03-25
- 対象: #35 の修正が実xlsx生成（E2E）で有効かどうかの確認

## 背景

これまで #35 対応として `formulaRefScope=local` と `scopePath` ベース解決を実装し、
主に `WorksheetStateTests` / `LayoutEngineTests` で検証していた。
本レポートでは、`ReportGenerator` 経由で実際に `.xlsx` を生成するE2Eテストを追加し、
repeat反復単位で local 参照が分離されることを確認した。

## 追加したE2Eテスト

- 追加先: `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs`
- テスト名: `Generate_RepeatWithLocalFormulaRefScope_ResolvesPerIteration`

### シナリオ

- `repeat` で2行展開
- 各行で `B列` を `formulaRef="RowData" formulaRefScope="local"` で登録
- 同じ行の `C列` で `=SUM(#{RowData:RowDataEnd})` を評価

### 期待値

- 1行目: `SUM(B1:B1)`
- 2行目: `SUM(B2:B2)`

global集約の不具合が残っている場合は、2行目も `SUM(B1:B2)` 等になり得るため、
この期待値で #35 の再発有無を判定できる。

## 結果

`dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj` を実行し、
追加テストを含め全件通過した。

