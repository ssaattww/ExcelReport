# PR Follow-up: conditionalFormatting local scope + boolean literal parsing (2026-03-25)

## 指摘事項
1. `conditionalFormatting` の `fontBold/fontItalic/fontUnderline` は XSD 上 `xs:boolean` のため `1/0` も有効。
2. `sheetOptions/conditionalFormatting@formulaRef` が `formulaRefScope="local"` を解決できず、未解決名のまま式生成される。

## 対応内容
- `SheetOptionsAst.NormalizeOptionalBool` で `true/false` に加えて `1/0` を受理する仕様を反映。
- `WorksheetStateBuilder` の `ResolveConditionalFormulaRefTarget` を拡張し、
  - まず global / named area を解決
  - 次に `conditionalFormatting@at` の解決結果レンジと交差する local scope の `formulaRef` を優先解決
  - 交差候補がない場合は local scope 全体で一意な定義があれば解決
  - それでも解決不能なら元の文字列を維持
- 回帰テストを追加:
  - `SheetAstTests.Parse_Sheet_ConditionalFormatting_BooleanLiterals_ParsesNumericBooleans`
  - `WorksheetStateTests.Build_ConditionalFormatting_FormulaRef_LocalScope_ResolvedFromTargetScope`

## 実行結果
- `dotnet` コマンドが環境に存在しないため、テスト実行は未実施（環境制約）。
