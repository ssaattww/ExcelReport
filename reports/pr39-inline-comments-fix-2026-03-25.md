# PR #39 指摘対応レポート（追加2点）

- 日付: 2026-03-25
- 対象PR: https://github.com/ssaattww/ExcelReport/pull/39

## 指摘1（親スコープ探索）

- 内容: local 解決時に親スコープ探索が欠落する可能性の指摘。
- 対応: `FindNamedArea` は `currentScope` をそのまま順に縮退探索する実装を維持し、即時親→上位親→global の順で解決されることを確認。

## 指摘2（`formulaRefScope` typo）

- 内容: `formulaRefScope` が自由文字列だと typo で意図せず global 扱いになる。
- 対応:
  1. XSD の `formulaRefScope` を `FormulaRefScopeType(local|global)` に制約。
  2. パーサ側でも防御実装を追加し、無効値は Warning (`InvalidAttributeValue`) を記録したうえで `global` に正規化。
  3. 回帰テスト `Parse_Cell_InvalidFormulaRefScope_FallsBackToGlobalWithWarning` を追加。

## 変更ファイル

- `Design/DslDefinition/DslDefinition_v1.xsd`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs`
- `ExcelReport/ExcelReportLib.Tests/LayoutNodeTests.cs`
