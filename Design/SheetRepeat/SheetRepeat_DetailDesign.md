# Sheet Repeat 詳細設計書

最終更新: 2026-03-19

## Status

- As-Is: `sheet` は単発展開のみで、`repeat` はシート内レイアウト要素に限定される。
- To-Be: `sheet` に `from` / `var` を追加し、コレクションから複数シートを展開可能にする。

## 1. 目的

- 1つの `sheet` 定義を、入力データのコレクション件数に応じて複数ワークシートへ展開する。
- 既存の `repeat` 概念と整合する DSL を追加し、既存 DSL 互換性は維持する。

## 2. DSL 仕様 (To-Be)

### 2.1 `sheet` 追加属性

- `from` (optional)
  - C# 式文字列。`IEnumerable` を返す必要がある。
- `var` (optional)
  - 反復変数名。`from` 指定時のみ意味を持つ。
  - 省略時は `item`。

### 2.2 記述例

```xml
<workbook xmlns="urn:excelreport:v1">
  <sheet name="@(it.Name)" from="@(root.Items)" var="it">
    <cell r="1" c="1" value="@(it.Name)" />
  </sheet>
</workbook>
```

### 2.3 互換性

- `from` 未指定の `sheet` は現行どおり単発展開。
- 既存テンプレート (`name` 固定文字列の `sheet`) は無変更で動作。

## 3. 実行仕様

### 3.1 シート展開

- `from` 未指定:
  - 1回だけ展開。
- `from` 指定:
  - `from` 式を評価して `IEnumerable` を取得。
  - 要素ごとに `var` へ束縛してシート展開。

### 3.2 シート名解決

- `sheet@name` が `@( ... )` の場合は式評価結果を文字列化してシート名に採用。
- `sheet@name` が通常文字列の場合はそのまま採用。

### 3.3 変数スコープ

- `root`: 既存どおり。
- `var`: シート反復の各要素。
- シート内の `cell/use/repeat` から `@(varName.xxx)` を参照可能。

### 3.4 sheetOptions

- 反復で生成された各シートは独立の `LayoutSheet` として扱う。
- `sheetOptions` 解決処理は既存ロジックをそのまま適用する。

## 4. バリデーション仕様

### 4.1 DslParser 検証

- `sheet@var` が指定されていて `sheet@from` が空の場合:
  - `IssueKind.UndefinedRequiredAttribute` (Error)
- `sheet@from` が指定されていて `sheet@name` が空の場合:
  - `IssueKind.UndefinedRequiredAttribute` (Error)

### 4.2 LayoutEngine 検証

- `sheet@from` 評価結果が `IEnumerable` 以外:
  - `IssueKind.InvalidAttributeValue` (Error)
- 反復展開後にシート名重複:
  - `IssueKind.DuplicateSheetName` (Error)

## 5. 実装対象

- `Design/DslDefinition/DslDefinition_v1.xsd`
  - `SheetType` に `from` / `var` 属性を追加。
- `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs`
  - `FromExprRaw` / `VarName` を追加。
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
  - sheet repeat 制約検証を追加。
- `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
  - シート展開ループを repeat 対応へ拡張。
  - シート名評価と重複名検出を追加。

## 6. 先行テスト方針

- ValidateDsl: `sheet@var` 単独指定は Error。
- LayoutEngine: `sheet@from` で複数シートへ展開される。
- LayoutEngine: 重複シート名を Error 検知する。
- ReportGenerator: 実 XLSX に複数シートが出力される。

## 7. 受け入れ条件

- 反復シートが想定件数で生成される。
- 反復変数で `sheet@name` と `cell@value` が評価される。
- 不正入力 (`from` 非コレクション、重複シート名) が Issue として記録される。
- 既存 DSL テストが回帰しない。
