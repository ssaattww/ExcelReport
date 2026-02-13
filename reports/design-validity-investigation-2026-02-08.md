# 設計書妥当性調査レポート

- 調査日: 2026-02-08
- 対象: `Design/*.md` と `ExcelReport/ExcelReportLib/**/*.cs`
- 目的: 設計書と現行実装の整合性確認（設計書本文への反映は行わない）

## 結論

1. 現行実装の中心は `DslParser` と `DSL AST` 周辺。
2. `ExpressionEngine / LayoutEngine / Styles / WorksheetState / Renderer / Logger / ReportGenerator` は未実装（設計先行）。
3. 設計書には、現行実装と不一致な記述が複数存在する。

## 主要不整合（優先度高）

1. `use` のインスタンス属性
- 設計書: `name`
- 実装: `instance`
- 根拠: `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs`

2. `rowspan/colspan` の属性名
- 設計書: `rowspan`, `colspan`
- 実装: `rowSpan`, `colSpan`
- 根拠: `ExcelReport/ExcelReportLib/DSL/AST/Common.cs`

3. 外部スタイル取込タグ
- 設計書: `<import ...>`
- 実装: `<styleImport ...>`
- 根拠: `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs`

4. 重複定義の解決方針
- 設計書: 後勝ち
- 実装: Issue 追加 + 先勝ち
- 根拠: `ExcelReport/ExcelReportLib/DSL/DslParser.cs`

5. `DslParser` API 定義
- 設計書: `IDslParser` / `XmlDslParser` 前提
- 実装: `static class DslParser` + `ParseFromFile/ParseFromText/ParseFromStream`
- 根拠: `ExcelReport/ExcelReportLib/DSL/DslParser.cs`

6. XSD 検証の扱い
- 設計書: 有効前提
- 実装: 現状無効（コメントアウト状態）
- 根拠: `ExcelReport/ExcelReportLib/DSL/DslParser.cs`

## 追加観測事項

1. `SheetAst` は `rows/cols` を保持していない。
- 根拠: `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs`

2. `@styleRef` は `CellAst.StyleRefShortcut` へ未反映の状態。
- 根拠: `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs`

3. 外部コンポーネント定義内の `<styles>` は現行実装では読み取り対象外（component のみ解析）。
- 根拠: `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs`

## 判定

1. 設計書は「将来設計」としては有効。
2. ただし「現行実装仕様」として読むと誤解が生じる状態。
3. 実装準拠版と将来設計版の切り分けが必要。

## 補足

- 本レポートは調査結果の記録であり、設計書本文には未反映。
