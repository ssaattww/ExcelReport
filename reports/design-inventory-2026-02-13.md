# Design インベントリ調査レポート

- 調査日: 2026-02-13
- 対象: `Design/`
- 目的: 設計書の構造・記述範囲・モジュール境界を把握し、後続の実装整合性チェックに使える棚卸し情報を作る

## 1. ディレクトリ構造

- 基本設計: `Design/BasicDesign_v1.md`
- DSL定義: `Design/DslDefinition/`
- モジュール詳細設計:
- `Design/DslParser/DslParser_DetailDesign_v1.md`
- `Design/ExpressionEngine/ExpressionEngine.md`
- `Design/LayoutEngine/LayoutEngine.md`
- `Design/Styles/Styles_DetailDesign.md`
- `Design/WorkSheetState/WorksheetState_DetailDesign.md`
- `Design/Renderer/Renderer_DetailDesign.md`
- `Design/Logger/Logger_DetailDesign.md`
- `Design/ReportGenerator/ReportGenerator_DetailDesign.md`

証跡:
- `Design/BasicDesign_v1.md`
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md`
- `Design/DslDefinition/DslDefinition_v1.xsd`
- `Design/DslParser/DslParser_DetailDesign_v1.md`
- `Design/ExpressionEngine/ExpressionEngine.md`
- `Design/LayoutEngine/LayoutEngine.md`
- `Design/Styles/Styles_DetailDesign.md`
- `Design/WorkSheetState/WorksheetState_DetailDesign.md`
- `Design/Renderer/Renderer_DetailDesign.md`
- `Design/Logger/Logger_DetailDesign.md`
- `Design/ReportGenerator/ReportGenerator_DetailDesign.md`

## 2. 設計書のカバレッジ（モジュール別）

| モジュール | 設計書 | 主な記述内容 |
|---|---|---|
| DslDefinition | `Design/DslDefinition/DslDefinition_DetailDesign_v1.md`, `Design/DslDefinition/DslDefinition_v1.xsd` | 要素仕様、属性仕様、サンプルDSL、XSD |
| DslParser | `Design/DslParser/DslParser_DetailDesign_v1.md` | Parse API、Issueモデル、XSD⇔AST対応、検証フェーズ |
| ExpressionEngine | `Design/ExpressionEngine/ExpressionEngine.md` | `IExpressionEvaluator`、EvaluationContext、評価/キャッシュ方針 |
| LayoutEngine | `Design/LayoutEngine/LayoutEngine.md` | `ILayoutEngine`、`LayoutPlan`、FinalStyle決定 |
| Styles | `Design/Styles/Styles_DetailDesign.md` | `IStyleResolver`、StylePlan、scope違反・優先順位 |
| WorksheetState | `Design/WorkSheetState/WorksheetState_DetailDesign.md` | `IWorksheetStateBuilder`、Workbook/WorksheetState契約 |
| Renderer | `Design/Renderer/Renderer_DetailDesign.md` | 入出力I/F、OpenXML写像、Issue/_Auditシート |
| Logger | `Design/Logger/Logger_DetailDesign.md` | `IReportLogger`、進捗・監査モデル |
| ReportGenerator | `Design/ReportGenerator/ReportGenerator_DetailDesign.md` | オーケストレーション、キャッシュ、GenerateAsyncフロー |

## 3. 基本設計との整合（設計同士）

`Design/BasicDesign_v1.md` では、各モジュール責務と依存方向（Parser→Layout→WorksheetState→Renderer）が定義されている。個別詳細設計も同じ大枠を採用しており、モジュール単位の責務分割は概ね一貫している。

証跡:
- `Design/BasicDesign_v1.md:200`
- `Design/BasicDesign_v1.md:221`
- `Design/LayoutEngine/LayoutEngine.md:67`
- `Design/Renderer/Renderer_DetailDesign.md:46`
- `Design/ReportGenerator/ReportGenerator_DetailDesign.md:65`

## 4. 設計書内で確認した論点（後続フェーズ向け）

### 4.1 DslDefinition の記述揺れ

1. `use` の識別属性
- 詳細設計本文は `instance` を採用
- ただしサンプルXMLには `name` 記述も残存

証跡:
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:169`
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:177`
- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml:14`

2. `styleImport` と `import` の混在
- 詳細設計本文では `styleImport` の例あり
- `DslDefinition_v1.xsd` は `<import>` を定義
- サンプルXMLにも `<import>` が残る

証跡:
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:455`
- `Design/DslDefinition/DslDefinition_v1.xsd:84`
- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml:5`

3. `rowSpan/colSpan` と `rowspan/colspan` の混在
- 詳細設計本文は `rowSpan`, `colSpan`
- XSDは `rowspan`, `colspan`
- 外部componentサンプルは `colspan`

証跡:
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:123`
- `Design/DslDefinition/DslDefinition_v1.xsd:200`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml:12`

4. 同名定義の競合ルール
- 詳細設計には「後勝ち」記述あり
- 競合解決ルールは実装照合が必要（フェーズ3で評価）

証跡:
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:506`
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:627`

### 4.2 DslParser 詳細設計の補足記述

`DslParser` 詳細設計は「実装状況メモ」を含み、`static` クラス提供・XSD検証無効化が明記されているため、設計そのものが「将来仕様」と「現実装メモ」を混在させる構造になっている。

証跡:
- `Design/DslParser/DslParser_DetailDesign_v1.md:139`
- `Design/DslParser/DslParser_DetailDesign_v1.md:149`
- `Design/DslParser/DslParser_DetailDesign_v1.md:151`

## 5. フェーズ1結果要約

1. `Design/` は全主要モジュールを網羅しており、ドキュメント量は十分。
2. 一方で、`DslDefinition` 系には仕様表記の揺れ（属性名・タグ名・サンプル記法）が存在。
3. 後続の整合性評価は「設計書同士の整合」も含めて実施する必要がある。
