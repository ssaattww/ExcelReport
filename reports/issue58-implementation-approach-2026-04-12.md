# issue #58 実装方針レポート

- 作成日: 2026-04-12
- 対象: `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
- 目的: ExcelTemplate 対応の実装順序、統合ポイント、既存コードとの差分を固定する

## 1. 調査結果

現行の実行経路は次のとおり。

```text
ReportGenerator
  -> DslParser
  -> LayoutEngine
  -> WorksheetStateBuilder
  -> XlsxRenderer
```

確認した主要ファイル:
- `ExcelReport/ExcelReportLib/ReportGenerator.cs`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
- `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
- `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs`
- `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs`

判断:
- 後段パイプラインは既に成立しているため、Issue #58 は「前段に ExcelTemplate 変換層を追加する」実装として進めるのが最小リスク。
- `ReportGenerator` を直接 ExcelTemplate 対応へ書き換えるより、新しい facade を追加して既存 DSL 経路を再利用する方が安全。

## 2. 現行コードとの差分

設計をそのまま実装するには、次の差分がある。

1. ExcelTemplate 変換層が存在しない
   - `ExcelTemplateExtractor`
   - `XmlTemplateSerializer`
   - `DslEmitter`
   - `ExcelTemplateReportGenerator`
2. `cell@formula` が DSL 契約に存在しない
   - 現行は `cell@value` が `=` で始まると数式扱い
   - 設計は `cell@formula` 正規化を要求
3. `use@styleOverflow` が DSL 契約/runtime に存在しない
   - XSD / `UseAst` / `LayoutEngine` の拡張が必要
4. ExcelTemplate 専用 validation が存在しない
   - component 範囲
   - merged cell 境界
   - unsupported feature
   - overflow warning
5. `repeat` は `direction` 必須
   - converter は `direction="down"` を明示出力しないと現行 DSL では parse error になる

## 3. 実装方針

### 3.1 方針の軸
- additive change を優先する
- ExcelTemplate は DSL へ変換してから既存後段へ流す
- runtime 契約の不足は先に埋める
- 初版対象外は validator で明示検出する

### 3.2 実装順序
1. DSL 契約拡張
2. レイアウト runtime 補完
3. ExcelTemplate 中間モデル
4. OpenXML extractor + validator
5. DslEmitter / XmlTemplateSerializer
6. ExcelTemplate facade API
7. E2E 固定

### 3.3 この順序にする理由
- `cell@formula` と `styleOverflow` が未実装のままでは、変換器が正しい DSL を吐いても downstream が処理できない
- `repeat` に `direction` を明示しないと emitted DSL 自体が現行 parser で失敗する
- Excel 読み取りと DSL 生成を分離すると、debug XML / DSL snapshot / runtime の責務が崩れない
- facade を最後に載せることで、単体検証が済んだ部品を組み合わせる形にできる

## 4. 具体的な実装単位

### 4.1 DSL 契約拡張
対象:
- `Design/DslDefinition/DslDefinition_v2.xsd`
- `ExcelReport/ExcelReportLib.Tests/TestDsl/DslDefinition_v2.xsd`
- `ExcelReport/ExcelReportLib/DSL/DslContract.cs`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs`
- `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`

作業:
- `cell@formula` を XSD/AST に追加
- runtime が読む埋め込み schema と test fixture schema の両方を更新
- `CellAst` は `formula` と `value` を明確に分離
- `DslParser.ValidateDsl` に no-schema mode 用の契約検証を追加
- `LayoutEngine` は `formula` を優先して `LayoutCell.Formula` へ流す
- `use@styleOverflow="none|edge"` を XSD/AST に追加
- `repeat` は converter が `direction="down"` を明示出力する前提で統一

### 4.2 レイアウト runtime 補完
対象:
- `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
- `ExcelReport/ExcelReportLib/LayoutEngine/LayoutCell.cs`

作業:
- `styleOverflow=edge` は `ExpandUse` / `ExpandRepeat` の後段処理として実装する
- 挿入先 seed 書式は「値なしでも style を持つ `LayoutCell`」として保持する
- trailing edge copy は row / col / corner ごとに style-onlyセルを合成する
- 競合解決は 11章の border/style ルールへ委譲する

### 4.3 ExcelTemplate モデルと extractor
新規追加候補:
- `ExcelTemplate/Model/ExcelTemplateWorkbook.cs`
- `ExcelTemplate/Model/ExcelTemplateSheet.cs`
- `ExcelTemplate/Model/ExcelTemplateCell.cs`
- `ExcelTemplate/Model/ExcelTemplateStyle.cs`
- `ExcelTemplate/Model/ExcelTemplateComponentRange.cs`
- `ExcelTemplate/ExcelTemplateExtractor.cs`
- `ExcelTemplate/ExcelTemplateValidator.cs`
- `ExcelTemplate/ComponentRangeResolver.cs`
- `ExcelTemplate/UseTriggerParser.cs`

役割:
- extractor: OpenXML 読み取り
- validator: 初版対象外/不正入力の集約
- resolver: component 定義範囲の決定
- parser: `{{use:...}}` trigger を DSL 相当へ正規化

### 4.4 出力器
新規追加候補:
- `ExcelTemplate/DslEmitter.cs`
- `ExcelTemplate/XmlTemplateSerializer.cs`
- `ExcelTemplate/ExcelTemplateConverter.cs`

役割:
- XML debug 出力
- DSL text 出力
- 変換入口の統一

### 4.5 facade
新規追加候補:
- `ExcelTemplate/ExcelTemplateReportGenerator.cs`
- `ExcelTemplate/ExcelTemplateGenerateOptions.cs`

役割:
- ExcelTemplate -> DSL 変換
- 既存 `ReportGenerator` 呼び出し
- logger / issues の統合

API 方針:
- `ConvertToDsl` / `ConvertToXmlTemplate` は `string` ではなく result object を返す
- result object は `Text` と `Issues` を持ち、conversion-only 呼び出しでも座標付き Warning/Error を失わない

## 5. テスト方針

### 5.1 Unit
- `cell@formula` parser/runtime
- `styleOverflow=edge`
- `UseTriggerParser`
- `ComponentRangeResolver`
- `repeat@direction="down"` 明示出力
- extractor の値/数式/スタイル/defined name/merged cell
- 後方互換: `cell@value="=..."` が従来どおり数式として動くこと

### 5.2 Integration
- xlsx -> xml snapshot
- xlsx -> dsl snapshot

### 5.3 E2E
- xlsx -> dsl -> final xlsx
- `GroupBlock` / `ItemRow`
- `styleOverflow=edge`
- `cell@formula`
- merged cell violation
- unsupported conditional formatting

## 6. リスクと対策

| リスク | 影響 | 対策 |
|---|---|---|
| `cell@formula` と現行 `value=\"=...\"` の二重契約 | 互換崩れ | `formula` 優先、既存 `value=\"=...\"` は後方互換として残し、回帰テストで固定する |
| `styleOverflow` 実装が広がりすぎる | 実装膨張 | 初版は right/down/right-down corner のみ |
| Excel 自由入力の未対応機能 | silent corruption | validator で Warning/Error を強制 |
| merged cell 境界崩れ | 出力破損 | 初版は矩形内完結のみ許可 |
| schema validation 無効時に契約逸脱を見逃す | 実行時不整合 | `ValidateDsl` に追加検証を実装する |

## 7. 実装開始条件

以下を満たしたら実装着手してよい。

1. 設計書 13章の方針に reviewer 合意がある
2. `cell@formula` / `styleOverflow` の DSL 契約拡張を先行タスクに置く
3. `styleOverflow=edge` を `LayoutEngine` の post-expand 処理として実装する方針に合意がある
4. 初版対象外を validator で止める方針にブレがない

## 8. 推奨着手順

1. DSL 契約拡張と unit test
2. runtime 補完 (`styleOverflow=edge`) と unit test
3. extractor/model と unit test
4. emitter/snapshot test
5. facade/E2E

この順で進めると、途中段階でも差分が小さく、レビュー単位を保ちやすい。
