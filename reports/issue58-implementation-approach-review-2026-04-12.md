# issue #58 実装方針レビュー記録

- 作成日: 2026-04-12
- reviewer: sub-agent `gpt-5.4` / `high`
- 対象:
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
  - `reports/issue58-implementation-approach-2026-04-12.md`

## 指摘事項

1. High: runtime schema 更新対象が不足
   - test fixture XSD だけでなく、runtime が読む `Design/DslDefinition/DslDefinition_v2.xsd` と埋め込み schema 経路も対象化する必要がある
2. High: `repeat@direction` 必須との契約不一致
   - design example / emitted DSL に `direction="down"` を明示する必要がある
3. High: `styleOverflow=edge` の実装位置が未固定
   - `LayoutEngine` 内のどの段で seed 書式を増分領域へコピーするかを先に決める必要がある
4. Medium: conversion-only API が診断情報を返せていない
   - DSL/XML 文字列だけでなく `Issues` 返却が必要
5. Medium: `cell@value="=..."` 後方互換の回帰テストが未明記
6. Medium: schema validation 無効時の `ValidateDsl` 補完検証が未明記

## 対応内容

1. 実装方針へ runtime schema の更新対象を追加
   - `Design/DslDefinition/DslDefinition_v2.xsd`
   - `ExcelReport/ExcelReportLib.Tests/TestDsl/DslDefinition_v2.xsd`
   - `DslContract` / `DslParser.ValidateDsl`
2. design example を `direction="down"` 明示へ統一
3. `styleOverflow=edge` は `LayoutEngine` の post-expand 処理として固定
   - style-only seed cell を `LayoutCell` として保持
   - row / col / corner 単位で trailing edge copy
4. `ConvertToDsl` / `ConvertToXmlTemplate` は result object + `Issues` 返却へ修正
5. unit test 計画に legacy `cell@value="=..."` 回帰を追加
6. `ValidateDsl` で no-schema mode 時の契約検証追加を明記

## 結論

重大指摘は実装方針へ反映済み。

実装着手前に固定された重要事項:
- `repeat` は converter が `direction="down"` を明示出力する
- `styleOverflow=edge` は `LayoutEngine` の post-expand で実装する
- conversion-only API でも `Issues` を返す
- schema validation 無効時も `ValidateDsl` で契約逸脱を止める
