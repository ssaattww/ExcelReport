# issue #16: シート間参照（sheet repeat 対応）設計・実装記録

Date: 2026-04-05
Branch: feature/issue-16-dynamic-sheet-reference
Issue: https://github.com/ssaattww/ExcelReport/issues/16

## 背景

- issue #16 は「シート間のアドレス参照」がテーマ。
- 固定シート名なら `='Detail'!A1` のように書けるが、`sheet@from` で動的生成したシート名を式へ埋め込むのが難しい。

## 設計方針

- DSL 要素/属性は追加しない（既存テンプレート互換を優先）。
- `cell@value` が `@( ... )` のとき、評価結果が `string` かつ先頭 `=` なら数式扱いにする。
- これにより、`sheet repeat` の `it`/`root` を使ってシート間参照式を動的生成可能にする。

## 設計反映

- 追加: `Design/SheetReference/SheetReference_DetailDesign.md`
- 更新: `Design/DslDefinition/DslDefinition_DetailDesign.md`
  - `cell@value` の評価後数式判定ルールを追記
  - `sheet repeat + シート間参照` の具体例を追記
- 更新: `Design/BreakingChanges.md`
  - `cell@value` 式評価で `=` 先頭文字列を数式扱いへ変更する挙動差分を記録（conditionally compatible）

## 実装内容

- `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
  - `EvaluateCellValue` 内で、`@( ... )` 評価結果が `string` かつ `=` 始まりの場合、`RenderedValue(..., Formula: ...)` を設定

## テスト追加

1. `LayoutEngineTests.Expand_CellValueExpressionReturningFormula_TreatedAsFormula`
   - 式評価結果 `='Detail'!A1` が `LayoutCell.Formula` へ入ることを確認
2. `ReportGeneratorTests.Generate_SheetRepeat_CrossSheetFormulaFromExpression_E2E`
   - `sheet@from` 展開で生成したシートに、動的シート間参照式が出力されることを確認

## 検証

- 追加テスト2件:
  - Passed: 2, Failed: 0
- 関連回帰（`LayoutEngineTests|ReportGeneratorTests`）:
  - Passed: 74, Failed: 0

## 補足

- 評価結果が `=` で始まる文字列をあえて「文字列」として出したい場合は、`'=...` 形式を利用する。
