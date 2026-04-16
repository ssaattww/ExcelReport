# issue #61: ExcelTemplate の sheet repeat 定義方式 設計検討（2026-04-16）

## 1. 背景

- 対象 Issue: https://github.com/ssaattww/ExcelReport/issues/61
- 要望:
  - ExcelTemplate で `sheet` レベルの repeat 定義を記述したい。
  - ユーザー意向として「シェイプに定義を書く方式」を優先したい。
  - ただし他方式（セル記述 / 既存 xmltemplate 併用）も比較して決める。

## 2. 現状（As-Is）

### 2.1 DSL runtime 側

- `sheet@from` / `sheet@var` は既に DSL と runtime が対応済み。
  - `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs`
  - `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
  - `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs` (`Expand_SheetRepeat_ExpandsMultipleSheets`)
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs` (`Generate_SheetRepeat_ProducesMultipleSheets`)

### 2.2 ExcelTemplate 変換側

- `ExcelTemplateExtractor` はセル/definedName/merge のみ抽出し、shape/drawing は未抽出。
  - `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateExtractor.cs`
- `ExcelTemplateOutputSheet` は `name` と `items` だけで、`from/var` を保持できない。
  - `ExcelReport/ExcelReportLib/ExcelTemplate/Model/ExcelTemplateOutputContract.cs`
- `XmlTemplateSerializer` の `<sheet>` 出力も `name` のみ。
  - `ExcelReport/ExcelReportLib/ExcelTemplate/XmlTemplateSerializer.cs`

## 3. 方式比較

| 方式 | 概要 | 実装コスト | テンプレ可読性 | 運用リスク | 備考 |
|---|---|---:|---:|---:|---|
| 案1.1 セル定義 | 特殊シート上のセルに `sheet repeat` 定義を書く | 低 | 中-低 | 中 | 既存セル抽出を再利用できるが、配置ルールが増えて見づらくなりやすい |
| 案1.2 シェイプ定義 | 特殊シート上のシェイプのテキストに定義を書く | 中 | 高 | 中-低 | パーサ追加は必要だが、1定義1シェイプで視認性が高い |
| 案2 xmltemplate踏襲 | `xlsx` 以外に xmltemplate 側へ sheet repeat 定義を置く | 中-高 | 低（定義分散） | 高 | 入力源が分かれ、ExcelTemplate 単体完結性が下がる |

## 4. 採用方針（To-Be）

- 採用: **案1.2（シェイプ定義）**
- 理由:
  1. ユーザー意向（シェイプ推し）と整合する。
  2. 案1.1より視認性が高く、複数定義を増やしても可読性を維持しやすい。
  3. 案2の「定義分散」を回避し、ExcelTemplate 単体で完結できる。

## 5. 仕様案（初版）

### 5.1 メタ定義シート

- シート名: `__sheet_meta`
- **shape 名は固定**: `__workbook_meta`
- `__sheet_meta` 上の `__workbook_meta` shape に書かれたテキストを `sheet repeat` 定義として扱う。

### 5.2 shape テキスト記法

- 1 shape（`__workbook_meta`）に workbook 階層定義を集約する。
- **Workbook 階層の XML 断片**で記述する（JSONは使わない）。

```xml
<workbook>
  <sheets>
    <sheet templateSheet="InvoiceTemplate"
           name="@(grp.Name)"
           from="@(root.Groups)"
           var="grp" />
  </sheets>
</workbook>
```

- 属性仕様:
  - `templateSheet`: レイアウト元シート名（通常シート）
  - `name`: 出力シート名式（`sheet@name` へ）
  - `from`: 反復元式（`sheet@from` と同じ `@(...)` 記法）
  - `var`: 反復変数名（省略時 `item`）
- 目的:
  - 今回実装の `sheet repeat` だけでなく、将来の workbook 階層拡張を同じ記法で受けられるようにする。
  - 例（今回は非対応）: 外部コンポーネントロード、workbook repeat。

### 5.3 DSL マッピング

- shape 内 `workbook/sheets/sheet` のうち、`templateSheet` を持つノードを今回の対象とする。
- `templateSheet` で指定したシートを 1 件だけテンプレート定義として読み、出力では次を付与する。
  - `<sheet name="..." from="..." var="..."> ... </sheet>`
- shape に書く `from` / `name` は xmlテンプレートと同じ `@(...)` 形を基本記法とする。

### 5.4 検証ルール

- `__sheet_meta` に `__workbook_meta` shape が存在しない: Error
- `__workbook_meta` 以外の shape を定義入力として扱わない（無視または Warning）
- shape XML 断片のルートが `<workbook>` でない: Error
- `workbook/sheets/sheet` が存在しない: Error
- `templateSheet` が存在しない: `IssueKind.InvalidAttributeValue` (Error)
- 同一 `templateSheet` に対する `sheet repeat` 定義が複数: Error
- shape XML 断片パース失敗: Error（shape識別情報を message に含める）
- `var` あり `from` なし: Error（DSL と同じ制約）
- `name` 省略: `templateSheet` 名を既定値として使用

## 6. 実装影響（次フェーズ）

1. 抽出モデル拡張
- `ExcelTemplateWorkbook` / `ExcelTemplateSheet` に shape メタ情報を追加

2. Extractor 拡張
- `WorksheetPart.DrawingsPart` から shape テキストを抽出
- `__sheet_meta` 上の `__workbook_meta` shape を `workbook` 断片として解析し、今回対応分（`sheets/sheet`）を取り込む

3. 出力契約拡張
- `ExcelTemplateOutputSheet` に `FromExpression` / `VariableName` を追加

4. Serializer 拡張
- `<sheet>` に `from` / `var` 属性を出力

5. テスト
- unit: shape XML parse / validation
- integration: xlsx -> dsl snapshot（sheet@from/var 出力）
- e2e: ExcelTemplateReportGenerator で複数シート生成確認

## 7. 非採用方針

- 案1.1（セル定義）は初版非採用。
  - 理由: 定義密度が高くなるほど、どのセルが何の設定か追跡しづらい。
- 案2（xmltemplate 分離）は初版非採用。
  - 理由: 1テンプレート1入力の運用から外れ、保守点が増える。

## 8. 結論

- issue #61 は、ExcelTemplate 側で未対応の `sheet repeat` 定義入力を追加する設計課題。
- 初版は **`__sheet_meta` + shape(Workbook階層XML) 方式**で進める。
- 記法を workbook 階層に合わせることで、将来の外部コンポーネントロード / workbook repeat へ同じ拡張ポイントで接続可能にする。
- 実装フェーズでは、Extractor/OutputContract/Serializer とテストを順に拡張する。
