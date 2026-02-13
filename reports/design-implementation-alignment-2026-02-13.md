# 設計-実装整合性チェックレポート

- 調査日: 2026-02-13
- 対象:
- 設計: `Design/`
- 実装: `ExcelReport/ExcelReportLib/**/*.cs`, `ExcelReport/ExcelReportLibTest/TestDsl/*`
- 既存レポート参照: `reports/design-validity-investigation-2026-02-08.md`
- 目的: 設計と実装の一致/不一致を根拠付きで更新判定する

## 1. 総括

1. 2026-02-08時点の主要不整合は概ね継続。
2. 追加で、設計内部（DslDefinition本文・XSD・サンプル）の不整合が確認された。
3. 実装未着手モジュールが多いため、整合性評価の中心は DslDefinition/DslParser 周辺。

## 2. モジュール別整合性マトリクス

| モジュール | 設計状態 | 実装状態 | 整合判定 |
|---|---|---|---|
| DslDefinition | 仕様・XSD・サンプルあり | パーサ実装あり | 部分不一致（仕様揺れあり） |
| DslParser | 詳細設計あり | 実装あり（部分） | 部分一致（未実装項目あり） |
| ExpressionEngine | 詳細設計あり | 実装なし | 未整合（設計先行） |
| LayoutEngine | 詳細設計あり | 実装なし | 未整合（設計先行） |
| Styles | 詳細設計あり | ASTのみ実装 | 未整合（Resolver未実装） |
| WorksheetState | 詳細設計あり | 実装なし | 未整合（設計先行） |
| Renderer | 詳細設計あり | 実装なし | 未整合（設計先行） |
| Logger | 詳細設計あり | 実装なし | 未整合（設計先行） |
| ReportGenerator | 詳細設計あり | 実装なし | 未整合（設計先行） |

証跡:
- 設計側: `Design/*.md`
- 実装側: `ExcelReport/ExcelReportLib/DSL/*.cs`, `ExcelReport/ExcelReportLib/DSL/AST/**/*.cs`

## 3. 主要不整合（継続課題の更新）

### 3.1 `use` インスタンス属性名

- 旧指摘: 設計 `name` vs 実装 `instance`
- 今回判定: 「設計内部でも揺れ」。
- DslDefinition本文は `instance` を説明する一方、サンプルXMLとXSDは `name` 記法が残る。
- 実装は `instance` を読み取る。

証跡:
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:169`
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:177`
- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml:14`
- `Design/DslDefinition/DslDefinition_v1.xsd:230`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs:37`

### 3.2 `rowSpan/colSpan` と `rowspan/colspan`

- 旧指摘: 設計と実装でキャメルケース差異
- 今回判定: 継続 + 設計内部不整合
- DslDefinition本文は `rowSpan/colSpan`、XSDは `rowspan/colspan`、実装は `rowSpan/colSpan`。

証跡:
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:123`
- `Design/DslDefinition/DslDefinition_v1.xsd:200`
- `ExcelReport/ExcelReportLib/DSL/AST/Common.cs:65`
- `ExcelReport/ExcelReportLib/DSL/AST/Common.cs:70`

### 3.3 外部スタイル取込タグ

- 旧指摘: 設計 `<import>` vs 実装 `<styleImport>`
- 今回判定: 継続 + テスト資産側も `<styleImport>` に寄っている
- DslDefinition本文の一部では `<styleImport>`、XSDは `<import>`、Designサンプルは `<import>`、実装/テストサンプルは `<styleImport>`。

証跡:
- `Design/DslDefinition/DslDefinition_v1.xsd:84`
- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml:5`
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:455`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs:11`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_Sample_v1.xml:5`

### 3.4 重複定義の解決方針（後勝ち vs 先勝ち）

- 旧指摘: 設計「後勝ち」、実装は重複Issue + 先に登録した定義を維持
- 今回判定: 継続

証跡:
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:506`
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:627`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:87`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:142`

### 3.5 DslParser API設計と現実装

- 旧指摘: `IDslParser/XmlDslParser` 前提と実装差異
- 今回判定: 改善（設計に現実装メモが追加）だが完全一致ではない
- 詳細設計に `static` 実装メモがある一方、API章には `IDslParser` が残る。

証跡:
- `Design/DslParser/DslParser_DetailDesign_v1.md:136`
- `Design/DslParser/DslParser_DetailDesign_v1.md:149`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:11`

### 3.6 XSD検証有効前提と現実装

- 旧指摘: 設計有効前提、実装は無効
- 今回判定: 継続（設計に無効化メモあり）
- `EnableSchemaValidation` は定義されるが検証処理はコメントアウト。

証跡:
- `Design/DslParser/DslParser_DetailDesign_v1.md:151`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:47`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:282`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:318`

## 4. 追加観測（今回更新）

### 4.1 `sheet@rows/cols` の扱い

- XSDおよび設計側には `rows/cols` がある
- `SheetAst` は `Name`/`StyleRefs`/`Children`/`Options` を保持し、`rows/cols` は保持しない

証跡:
- `Design/DslDefinition/DslDefinition_v1.xsd:166`
- `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs:12`
- `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs:15`

### 4.2 `cell@styleRef` ショートカット未実装

- `CellAst` に `StyleRefShortcut` はあるが、属性読み取り処理がない

証跡:
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs:12`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs:17`

### 4.3 `componentImport` 内 `<styles>` の取り込み未実装

- `ComponentImportAst` は `Styles` プロパティを持つが、実体構築していない
- `DslParser` 側に取り込み意図コメントがあるが未接続

証跡:
- `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs:19`
- `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs:76`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:169`

## 5. 設計先行モジュールの整合性判定

`ExpressionEngine / LayoutEngine / Styles(Resolver) / WorksheetState / Renderer / Logger / ReportGenerator` は、設計書に対する実装コードが未確認（不在）であるため、現時点では「不一致」ではなく「未実装による未整合」と分類する。

証跡:
- `ExcelReport/ExcelReportLib/ExcelReportLib.csproj:10`
- `ExcelReport/ExcelReportLib/DSL/`（実装領域がDSL中心）
- `Design/ExpressionEngine/ExpressionEngine.md`
- `Design/LayoutEngine/LayoutEngine.md`
- `Design/Styles/Styles_DetailDesign.md`
- `Design/WorkSheetState/WorksheetState_DetailDesign.md`
- `Design/Renderer/Renderer_DetailDesign.md`
- `Design/Logger/Logger_DetailDesign.md`
- `Design/ReportGenerator/ReportGenerator_DetailDesign.md`

## 6. フェーズ3結果要約

1. 既存レポートの高優先不整合は現在も有効。
2. 2026-02-13時点では「設計内部の記法混在」が主要な追加論点。
3. 修正対象は実装差分だけでなく、設計書同士（本文/XSD/サンプル）の正規化が必要。
