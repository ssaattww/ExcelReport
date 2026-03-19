# sheet repeat 調査レポート (2026-03-19)

## 1. 依頼背景

- ユーザー要望: `sheet` の `repeat` 対応を行いたい。
- 今回の先行成果物: 実装前に「調査レポート」「設計書」「先行テスト」を作成する。

## 2. 調査範囲

- 既存 DSL 契約:
  - `Design/DslDefinition/DslDefinition_v1.xsd`
  - `Design/DslDefinition/DslDefinition_DetailDesign_v1.md`
- 実装:
  - `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs`
  - `ExcelReport/ExcelReportLib/DSL/AST/WorkBookAst.cs`
  - `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
  - `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
- テスト:
  - `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/ValidateDslTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs`

## 3. 現状整理 (As-Is)

- `repeat` は LayoutNode (`cell/use/grid/repeat`) としてのみ実装済み。
  - 証跡: `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/RepeatAst.cs`
  - 証跡: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs` (`ExpandRepeat`)
- `sheet` は `name/rows/cols` のみを持つ単一シート定義。
  - 証跡: `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs`
  - 証跡: `Design/DslDefinition/DslDefinition_v1.xsd` (`SheetType`)
- `LayoutEngine` は `WorkbookAst.Sheets` を 1 件ずつ固定展開し、シート増殖ロジックを持たない。
  - 証跡: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs` (`foreach (var sheet in workbook.Sheets)`)

## 4. ギャップ

- 要望: 1つの `sheet` 定義から、コレクション件数分のワークシートを生成したい。
- 現状: `sheet` は繰り返せず、`repeat` はシート内レイアウトに限定される。

## 5. 方針候補比較

### 候補A: `sheet` 属性拡張 (`from` / `var`) で繰り返し化

- 例:
  - `<sheet name="@(it.Name)" from="@(root.Sections)" var="it">...</sheet>`
- 長所:
  - 既存 DSL と自然に整合（`repeat` の概念を sheet に水平展開）。
  - 既存 `workbook > sheet` 構造を維持できる。
- 短所:
  - `sheet@name` の式評価と重複名検証をレイアウト時に追加する必要がある。

### 候補B: 新要素 `sheetRepeat` を追加

- 例:
  - `<sheetRepeat from="..." var="it"><sheet .../></sheetRepeat>`
- 長所:
  - `sheet` の責務を増やさずに済む。
- 短所:
  - XSD/AST/パーサ/設計書の変更点が増え、導入コストが高い。
  - 既存読者にとって DSL 学習コストが上がる。

## 6. 採用方針 (To-Be)

- 候補Aを採用する。
- `sheet` に以下を追加する:
  - `from` (optional): `IEnumerable` を返す式
  - `var` (optional): 反復変数名。省略時 `item`
- `from` が指定された `sheet` は反復シートとして扱う。
- `sheet@name` は式 (`@( ... )`) を許可し、反復毎に評価した結果をシート名として採用する。

## 7. 影響範囲

- DSL 契約:
  - XSD の `SheetType` 属性拡張
  - 設計書 (`DslDefinition`) 追記
- AST/パーサ:
  - `SheetAst` へ `FromExprRaw` / `VarName` を追加
  - `ValidateDsl` に sheet-repeat 制約追加
- LayoutEngine:
  - シート展開処理を単発→反復対応へ変更
  - 反復後シート名の重複検出
- テスト:
  - ValidateDsl / LayoutEngine / ReportGenerator に追加

## 8. リスクと対策

- リスク: シート名重複で Excel 側が不正になる。
  - 対策: `LayoutEngine` で重複名を `IssueKind.DuplicateSheetName` (Error) として検出。
- リスク: `from` がコレクションでない場合の曖昧動作。
  - 対策: 既存 repeat と同様に `InvalidAttributeValue` を返す。
- リスク: `sheetOptions` の named area 解決が反復で崩れる。
  - 対策: 各反復シートを独立 `LayoutSheet` として扱い、既存ロジックをそのまま適用。

## 9. 先行テスト戦略

- 実装前に Red テストを追加:
  - `ValidateDsl`: `sheet@var` 指定時に `from` 必須制約
  - `LayoutEngine`: `sheet from` による複数シート展開 + `var` バインド
  - `LayoutEngine`: 反復後シート名重複検知
  - `ReportGenerator`: 実 XLSX 上で複数シート出力

## 10. 結論

- 現在は `sheet repeat` 未対応。
- 実装前準備として、設計書と先行テストを作成し、要件を固定化する方針が妥当。
