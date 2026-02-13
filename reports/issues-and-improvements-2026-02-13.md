# 問題点・改善点整理レポート

- 調査日: 2026-02-13
- 入力:
- `reports/design-inventory-2026-02-13.md`
- `reports/implementation-inventory-2026-02-13.md`
- `reports/design-implementation-alignment-2026-02-13.md`
- 目的: 不整合の優先順位付けと改善候補の明確化（設計修正方針決定の判断材料）

## 1. 優先度付き課題一覧

| Priority | 区分 | 問題 | 影響 |
|---|---|---|---|
| High | 仕様整合 | `DslDefinition` の属性/タグ仕様が本文・XSD・サンプルで不一致 | 実装者/利用者の誤読、互換性事故 |
| High | 実装整合 | 重複定義の解決ルールが設計（後勝ち）と実装（先勝ち+Issue）で不一致 | 期待挙動と実行結果の乖離 |
| High | 実装欠落 | XSD検証が無効、DSL固有検証がスタブ | 不正DSLの見逃し、下流障害誘発 |
| Medium | 実装欠落 | `cell@styleRef` ショートカット、`sheet@rows/cols` 反映不足 | DSL仕様の実利用制約 |
| Medium | 実装欠落 | `componentImport` 内 `<styles>` の取り込み未接続 | 外部化設計の利用制限 |
| Medium | アーキ整合 | 主要モジュール（LayoutEngine等）が設計先行で未実装 | パイプライン全体検証不可 |
| Low | 運用品質 | テストコード不在（DSL素材のみ） | 変更時の回帰検知遅延 |

## 2. 詳細分析（課題ごと）

### 2.1 High: DslDefinition の仕様混在

- 内容:
- `use` の識別属性: `name` と `instance` が混在
- style importタグ: `<import>` と `<styleImport>` が混在
- span属性: `rowspan/colspan` と `rowSpan/colSpan` が混在
- 根拠:
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:169`
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:177`
- `Design/DslDefinition/DslDefinition_v1.xsd:84`
- `Design/DslDefinition/DslDefinition_v1.xsd:200`
- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml:5`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs:37`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs:11`
- `ExcelReport/ExcelReportLib/DSL/AST/Common.cs:65`
- 改善案:
1. 仕様正を1つに決め、本文・XSD・全サンプル・ASTパーサを同時更新する。
2. 互換対応が必要なら「正式属性 + 互換属性（deprecated）」を明記する。
3. 正規化後に DSL fixture を再生成し、以降は fixture を単一ソース化する。

### 2.2 High: 重複定義ルール不一致

- 内容:
- 設計は後勝ち、実装は重複Issueを上げて先に登録した定義を維持（後続定義は無視）
- 根拠:
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:506`
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md:627`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:87`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:142`
- 改善案:
1. 仕様優先か実装優先かを決定し、反対側を同期する。
2. 同名重複時の Severity と継続可否（Error/Fatal）を固定する。
3. `style`/`component` 双方で同一ルールを定義する。

### 2.3 High: 検証機能の未完成

- 内容:
- XSD検証呼び出しがコメントアウト
- DSL固有検証メソッドが空実装
- 根拠:
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:47`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:282`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:308`
- 改善案:
1. 最低限の検証レベルを定義（L1: XML妥当, L2: XSD, L3: DSL参照整合）。
2. `ValidateDsl` を論点別（参照、repeat、sheetOptions、座標）に分離実装する。
3. 検証ON/OFF方針を options だけでなく運用デフォルトに明記する。

### 2.4 Medium: AST反映不足（部分実装）

- 内容:
- `cell@styleRef` 未読込
- `sheet@rows/cols` 非保持
- `componentImport` 内 `<styles>` 未接続
- 根拠:
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs:12`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs:17`
- `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs:12`
- `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs:19`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:169`
- 改善案:
1. AST契約を XSD対応表に基づき再点検し、欠落属性を埋める。
2. 未対応属性は設計上「未サポート」と明示する。
3. 仕様利用度が高い項目（`styleRef` shortcut 等）を優先実装する。

### 2.5 Medium: モジュール設計先行による検証不能領域

- 内容:
- ExpressionEngine, LayoutEngine, WorksheetState, Renderer, Logger, ReportGenerator が未実装
- 根拠:
- `ExcelReport/ExcelReportLib/ExcelReportLib.csproj:10`
- `ExcelReport/ExcelReportLib/DSL/`
- 改善案:
1. 実装着手前に「設計版（To-Be）」と「実装版（As-Is）」の読み分けを明示する。
2. 依存順（Parser→Expression→Styles→Layout→WorksheetState→Renderer→ReportGenerator）で段階導入する。
3. 各段階で最小統合テストを固定化する。

### 2.6 Low: テストコード不在

- 内容:
- DSL素材はあるがテストコードなし
- 根拠:
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_Sample_v1.xml`
- 改善案:
1. まず DslParser のスナップショット系テストを整備する。
2. 不整合論点（属性名/タグ名/重複）を回帰テスト化する。

## 3. 改善着手順（フェーズ5検討用の入力）

1. 仕様正規化（DslDefinition本文/XSD/サンプル統一）
2. DslParser挙動同期（重複解決・XSD検証・DSL検証）
3. AST欠落属性の補完
4. 最小テスト整備
5. 後段モジュール実装計画へ接続

## 4. フェーズ4結果要約

1. 直近で最もリスクが高いのは「仕様混在」と「検証未完成」。
2. 設計修正だけでなく、設計の正を実装側に同期する作業が不可欠。
3. 今回の結果は、次フェーズ（設計書修正方針）で「何を正にするか」を決めるための根拠として十分。
