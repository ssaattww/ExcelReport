# SheetReference 詳細設計（issue #16）

## 1. 背景

- 課題: `sheet` の `from` 反復で生成される複数シート間を、セル数式で参照したい。
- 現状: `cell@value` が文字列リテラルで `=` 始まりの場合は数式として出力できるが、`@( ... )` 式評価結果は常に値扱いとなる。
- 影響: 動的なシート名（`@(it.Name)` で生成）を使うシート間参照式を DSL だけで構築しづらい。

## 2. 要件

1. `sheet repeat` で生成されたシート名を使ったシート間参照式を記述できること。
2. DSL 仕様変更を最小化し、既存テンプレートへの影響を限定すること。
3. 既存の `value="=..."` 数式記法は変更しないこと。

## 3. 仕様（To-Be）

### 3.1 `cell@value` の式評価後判定

- `cell@value` が `@( ... )` の式だった場合、評価結果が `string` かつ先頭 `=` なら **Excel 数式として扱う**。
- それ以外は従来どおり値として扱う。

### 3.2 互換性

- 既存の `value="=SUM(...)"` は従来どおり数式扱い。
- 既存の `value="@( ... )"` は、評価結果が `=` で始まらない限り従来どおり値扱い。
- 評価結果が `=` で始まる文字列を「文字列そのもの」として出したいケースは、`'=...` のように先頭へ `'` を付ける運用で回避可能。

## 4. 完全な利用例（sheet repeat + 動的シート参照）

### 4.1 C# データモデル例

```csharp
public sealed class RootModel
{
    public IReadOnlyList<ReportItem> Items { get; init; } = Array.Empty<ReportItem>();
}

public sealed class ReportItem
{
    public string Name { get; init; } = string.Empty;
    public string SourceSheet { get; init; } = string.Empty;
    public int InputValue { get; init; }
}
```

### 4.2 C# 入力データ例

```csharp
var data = new RootModel
{
    Items =
    [
        new ReportItem { Name = "ReportA", SourceSheet = "Summary", InputValue = 10 },
        new ReportItem { Name = "ReportB", SourceSheet = "ReportA", InputValue = 20 },
        new ReportItem { Name = "ReportC", SourceSheet = "ReportB", InputValue = 30 },
    ],
};
```

### 4.3 DSL 全体例

```xml
<workbook xmlns="urn:excelreport:v2">
  <sheet name="Summary">
    <cell r="1" c="1" value="Sheet" />
    <cell r="1" c="2" value="SeedValue" />
    <repeat direction="down" from="@(root.Items)" var="it" r="2" c="1">
      <grid>
        <cell value="@(it.Name)" />
        <cell c="2" value="@(it.InputValue)" />
      </grid>
    </repeat>
  </sheet>

  <sheet name="@(it.Name)" from="@(root.Items)" var="it">
    <cell r="1" c="1">
      <value>@(it.Name)</value>
    </cell>
    <cell r="1" c="2">
      <value>@(it.InputValue)</value>
    </cell>

    <cell r="2" c="1" value="SourceA1" />
    <cell r="2" c="2">
      <value>@("='" + it.SourceSheet.Replace("'", "''") + "'!A1")</value>
    </cell>

    <cell r="3" c="1" value="SourceB1" />
    <cell r="3" c="2">
      <value>@("='" + it.SourceSheet.Replace("'", "''") + "'!B1")</value>
    </cell>
  </sheet>
</workbook>
```

### 4.4 C# 実行例

```csharp
var generator = new ReportGenerator();
var options = new ReportGeneratorOptions();
var result = generator.Generate(dsl, data, options);
```

### 4.5 展開結果イメージ

上記入力では `sheet repeat` により `ReportA`, `ReportB`, `ReportC` の3シートが生成される。

- `ReportA!B2` は `='Summary'!A1`
- `ReportA!B3` は `='Summary'!B1`
- `ReportB!B2` は `='ReportA'!A1`
- `ReportB!B3` は `='ReportA'!B1`
- `ReportC!B2` は `='ReportB'!A1`
- `ReportC!B3` は `='ReportB'!B1`

このように、`from="@(root.Items)"` の要素ごとに `it.SourceSheet` が評価され、動的に参照先シートを切り替えられる。

## 5. 実装方針

- 変更箇所を `LayoutEngine.EvaluateCellValue` に限定し、判定ロジックのみ拡張する。
- Renderer / WorksheetState / DSL AST の構造変更は行わない。

## 6. 検証方針

1. LayoutEngine 単体: `<value>@("='Detail'!A1")</value>` が `Formula` に入ること。
2. ReportGenerator E2E: `sheet repeat` で生成したシートに動的なシート間参照式が出力されること。
