# issue #58 Output Contract Builder

Date: 2026-04-13

## Summary
- 対象 task: `R58-01 変換出力の最小契約を固定する`
- 目的: `XmlTemplateSerializer` / `DslEmitter` の前段で使う共通の出力契約を、component/sheet 分類・cell/use/repeat-use 正規化・issue 集約の形で固定する

## TDD
1. Red
   - `ExcelTemplateOutputContractBuilderTests` を追加し、未実装状態で compile error (`ExcelTemplateOutputContractBuilder` など未定義) を確認した
2. Green
   - `ExcelTemplateOutputContractBuilder` と `ExcelTemplateOutputContract` 系モデルを追加
   - resolver / validator / use trigger parser を束ねて contract を組み立てる最小実装を追加
3. Verify
   - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter ExcelTemplateOutputContractBuilderTests`
     - Passed 2
   - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "ExcelTemplate"`
     - Passed 19

## Implemented Contract
- component sheet は `__component_` 接頭辞で判定し、resolved range を持つものだけ contract の `Components` へ入れる
- 通常 sheet は `Sheets` へ入れる
- cell は row/column 順で並べる
- `{{use:...}}` は `ExcelTemplateOutputUse` または `ExcelTemplateOutputRepeatUse` に正規化する
- 数式セルは `ExcelTemplateOutputCell.Formula` へ保持する
- component range resolver と validator の `Issues` を contract へ集約する
- range 解決に失敗した component sheet も contract へ残し、未解決文脈を downstream へ渡す
- テスト fixture は `ExcelTemplateOutputContractFixture` にまとめ、後続 snapshot テストで再利用できる形にした

## Files
- `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs`
- `ExcelReport/ExcelReportLib/ExcelTemplate/Model/ExcelTemplateOutputContract.cs`
- `ExcelReport/ExcelReportLib.Tests/ExcelTemplateOutputContractBuilderTests.cs`
- `ExcelReport/ExcelReportLib.Tests/ExcelTemplateOutputContractFixture.cs`

## Residual Risk
- 現時点では Excel 側から `styleOverflow` を入力する新しい source syntax は未導入のため、contract 上の `use` / `repeat-use` は `StyleOverflow = null` で「未指定」を保持している
- `R58-02` / `R58-03` ではこの contract をそのまま serializer / emitter に接続し、snapshot で出力表現を固定する必要がある
