# Chart 詳細設計書

## Status
- As-Is (New): グラフ機能は未実装。
- To-Be (Planned): DSL から Excel グラフを生成できるようにし、初版で `barStacked`（積み上げ横棒）および `line`（折れ線）を必須対応とする。
- To-Be (Planned): 系列色だけでなく、各データ点単位の色指定をサポートする。
- To-Be (Planned): `colorKey` / `colorBy` により、同一意味キーは同一色になる色解決を導入する。
- To-Be (Planned): `chartPalette` により Workbook 単位の色キー辞書を定義できるようにする。
- To-Be (Planned): Chart はセル・結合セル・条件付き書式とは別レイヤの Drawing 要素として扱う。

---

# 1. 概要・位置づけ

## 1.1 モジュール名 / アセンブリ

| 項目 | 内容 |
|---|---|
| 機能名 | `Chart` |
| 想定アセンブリ | `ExcelReport.Core` |
| 実装形態 | DslParser / LayoutEngine / WorksheetState / Renderer / Styles にまたがる横断機能 |

Chart は独立単一モジュールというより、既存のレポート生成パイプラインへ新たに追加される横断機能として設計する。

## 1.2 位置づけ

Chart 機能は、DSL に定義された `<chart>` 要素を入力として受け取り、  
最終的に Excel(OpenXML) の `ChartPart` / `DrawingsPart` / `WorksheetDrawing` として物理出力する機能である。

既存の ExcelReport では以下の責務分離が明確である。

- DslParser: XML / DSL → AST
- LayoutEngine: 参照解決、レイアウト計画、最終判断
- WorksheetState: Excel 出力直前の完全な最終状態保持
- Renderer: OpenXML への機械的写像
- ReportGenerator: 全体統合・呼び出し順制御

Chart 機能もこの原則に従い、**Renderer に判断を持ち込まない**ことを最重要方針とする。

## 1.3 役割（高粒度）

Chart 機能の主な役割は以下のとおり。

1. `<chart>` / `<series>` / `<chartPalette>` の DSL 定義を構文解析する
2. `category` / `value` / `colorBy` の参照をシート上の最終セル範囲に解決する
3. `color` / `colorKey` / `colorBy` の優先順位に基づいて各データ点の最終色を決定する
4. グラフ種別、系列、アンカー位置、サイズなどを論理レイアウトとして確定する
5. Renderer が追加判断なしで出力できる `ChartState` を構築する
6. OpenXML の Drawing / ChartPart を生成する

## 1.4 設計上の基本方針

1. **Renderer は判断しない**  
   グラフ種別ごとの意味解釈、系列長整合性、色解決、参照解決、既定値補完は Renderer ではなく上流で完了する。既存 Renderer の方針と一致させる。

2. **LayoutEngine が最終判断を持つ**  
   既存設計ではスタイル適用順序・競合解決・最終スタイル値の決定は LayoutEngine の責務である。Chart においても、色や参照解決の最終決定を LayoutEngine に集約する。

3. **WorksheetState は完全な最終状態を保持する**  
   `ChartState` を Renderer に渡す最終成果物とし、Renderer 側での再推論を不要にする。既存 WorksheetState の責務に整合する。

4. **Chart はセルとは別レイヤで扱う**  
   Chart は `CellState` / `MergedRange` / `NamedArea` と同列ではなく、シート上の Drawing 要素として独立管理する。セル占有マップには入れない。

5. **初版は `barStacked` と `line` を必須対応とする**  
   今回の要件上、積み上げ横棒が主目的である。一方で折れ線も初版で対応し、棒系・折れ線系双方の基盤を先に設計する。

---

# 2. 責務・非責務

## 2.1 責務 (IN)

Chart 機能が担う責務は次のとおり。

### 2.1.1 DslParser
- `<chart>` 要素の構文解析
- `<series>` 子要素の構文解析
- `<chartPalette>` / `<color>` の構文解析
- Chart 系 AST の構築
- DSL レベルの必須属性検証
- 列挙値・数値・色文字列の基本妥当性検証

### 2.1.2 LayoutEngine
- `category` / `value` / `colorBy` / `colorKey` の参照解決
- `formulaRef` / `area` / 直接範囲 (`A1:B10`) の解決
- 系列長・カテゴリ長の検証
- 色解決（固定色 / キー色 / デフォルト色）
- グラフのアンカー位置・サイズの論理確定
- `LayoutChart` の生成
- Chart 関連 Issue の生成

### 2.1.3 WorksheetState
- `LayoutChart` から `ChartState` を構築
- シート範囲外や最終整合性の検証
- Renderer 向けの最終状態固定
- Workbook 全体のグラフ Issue 統合

### 2.1.4 Renderer
- `ChartState` から `ChartPart` / `WorksheetDrawing` を構築
- シートへのアンカー配置
- 系列・データ点色の物理適用
- タイトル・凡例・軸などの機械的写像

### 2.1.5 Styles（補助）
- `chartPalette` の保持元として利用
- Workbook 単位の色キー辞書の提供
- 色定義の正規化

## 2.2 非責務 (OUT)

### 2.2.1 DslParser の非責務
- 参照解決
- 色の最終決定
- 系列長整合性の最終判定
- OpenXML 構造生成

### 2.2.2 LayoutEngine の非責務
- OpenXML 物理出力
- DrawingPart / ChartPart の作成
- Excel API 呼び出し

### 2.2.3 WorksheetState の非責務
- 色解決ロジック
- 参照解決ロジック
- グラフレイアウトの再計算

### 2.2.4 Renderer の非責務
- `colorKey` / `colorBy` の意味解釈
- 系列整合性検証
- 値系列の長さ調整
- デフォルト色の決定

Renderer は既存方針通り、**判断をしない**。

---

# 3. DSL 仕様

## 3.1 配置可能位置

初版では `<chart>` は **`<sheet>` 直下のみ** 許可する。

理由:
- component / repeat / grid 配下まで初版から許容すると、繰り返し展開・相対位置・重複アンカー・ローカルスコープ解決が複雑化するため
- 条件付き書式はより柔軟な配置を許しているが、Chart はセル外描画を伴うため初版は制約を強めるほうが安全である

## 3.2 chart 要素

### 3.2.1 例

```xml
<chart
    type="barStacked"
    title="Progress"
    name="ProgressChart"
    r="2" c="8"
    width="10" height="16"
    category="Task.Name"
    legend="right"
    showDataLabels="false">
  <series name="Done"  value="Task.Done"  colorKey="Done"/>
  <series name="Doing" value="Task.Doing" colorKey="Doing"/>
  <series name="Todo"  value="Task.Todo"  colorKey="Todo"/>
</chart>
```

### 3.2.2 属性一覧

| 属性 | 必須 | 説明 |
|---|---:|---|
| `type` | Yes | グラフ種別。初版は `barStacked` / `line` 必須 |
| `title` | No | グラフタイトル |
| `name` | No | 論理名。Issue / Logger 用 |
| `r` | Yes | 左上アンカー行（1-based） |
| `c` | Yes | 左上アンカー列（1-based） |
| `width` | No | 幅（論理列数ベース）。既定 8 |
| `height` | No | 高さ（論理行数ベース）。既定 15 |
| `category` | Yes | カテゴリ系列参照 |
| `legend` | No | `none / right / left / top / bottom` |
| `showDataLabels` | No | データラベル表示有無 |
| `when` | No | 表示条件式。false の場合は出力しない |

### 3.2.3 `type` の候補

初版で仕様定義および実装必須とする列挙値:

- `barStacked`
- `line`

将来拡張候補:

- `bar`
- `column`
- `pie`
- `barStacked100`

## 3.3 series 要素

### 3.3.1 例

```xml
<series name="Done" value="Task.Done" colorKey="Done"/>
<series name="SegmentA" value="Task.ValueA" colorBy="Task.ColorKeyA"/>
<series name="Warn" value="Task.Warn" color="#FF0000"/>
```

### 3.3.2 属性一覧

| 属性 | 必須 | 説明 |
|---|---:|---|
| `name` | No | 系列名 |
| `value` | Yes | 値系列参照 |
| `color` | No | 固定色 (`#RRGGBB`) |
| `colorKey` | No | 系列全体に適用する色キー |
| `colorBy` | No | 各データ点ごとの色キー系列参照 |

### 3.3.3 色関連属性の意味

- `color`: 当該系列の全データ点を固定色で描画する
- `colorKey`: 当該系列の全データ点を、同一キー対応色で描画する
- `colorBy`: 各データ点ごとにキーを与え、そのキーに応じて色を決定する

`color` / `colorKey` / `colorBy` は同時指定を許容するが、解決優先順位は後述する。

## 3.4 chartPalette

### 3.4.1 例

```xml
<chartPalette>
  <color key="Done" value="#4CAF50"/>
  <color key="Doing" value="#FF9800"/>
  <color key="Todo" value="#BDBDBD"/>
</chartPalette>
```

### 3.4.2 配置位置

初版では `workbook` 直下に 0 または 1 個のみ許可する。

### 3.4.3 意味

- `key` に対応する既定色を定義する
- 同一 key は Workbook 単位で同一色に解決される
- 重複 key は後勝ちとする

## 3.5 参照の解決対象

`category` / `value` / `colorBy` は次のいずれかを指定可能とする。

1. `formulaRef` 系列名  
   例: `Task.Done`
2. `area` 名  
   例: `DetailRows`
3. 直接セル範囲  
   例: `A2:A10`, `Summary!$B$2:$B$10`

ただし、**最終的に 1 次元連続範囲へ解決できること** を要求する。

---

# 4. データモデル

## 4.1 DslParser / AST モデル

### 4.1.1 ChartPaletteAst

```csharp
public sealed class ChartPaletteAst
{
    public IReadOnlyList<ChartColorAst> Colors { get; init; } = Array.Empty<ChartColorAst>();
    public SourceSpan? Span { get; init; }
}

public sealed class ChartColorAst
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty; // #RRGGBB
    public SourceSpan? Span { get; init; }
}
```

### 4.1.2 ChartAst

```csharp
public sealed class ChartAst : LayoutNodeAst
{
    public string ChartType { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Name { get; init; }

    public int Width { get; init; } = 8;
    public int Height { get; init; } = 15;

    public string CategoryRef { get; init; } = string.Empty;
    public string? Legend { get; init; }
    public bool ShowDataLabels { get; init; }

    public IReadOnlyList<ChartSeriesAst> Series { get; init; } = Array.Empty<ChartSeriesAst>();
}
```

### 4.1.3 ChartSeriesAst

```csharp
public sealed class ChartSeriesAst
{
    public string? Name { get; init; }
    public string ValueRef { get; init; } = string.Empty;

    public string? Color { get; init; }      // #RRGGBB
    public string? ColorKey { get; init; }   // palette key
    public string? ColorByRef { get; init; } // formulaRef / area / range

    public SourceSpan? Span { get; init; }
}
```

## 4.2 LayoutEngine モデル

### 4.2.1 LayoutChart

```csharp
public sealed class LayoutChart
{
    public string ChartType { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Name { get; init; }

    public int TopRow { get; init; }
    public int LeftColumn { get; init; }
    public int WidthColumns { get; init; }
    public int HeightRows { get; init; }

    public string CategoryFormula { get; init; } = string.Empty;
    public string? Legend { get; init; }
    public bool ShowDataLabels { get; init; }

    public IReadOnlyList<LayoutChartSeries> Series { get; init; } = Array.Empty<LayoutChartSeries>();
}
```

### 4.2.2 LayoutChartSeries

```csharp
public sealed class LayoutChartSeries
{
    public string? Name { get; init; }
    public string ValueFormula { get; init; } = string.Empty;

    public string? FixedColor { get; init; } // #RRGGBB
    public string? ColorKey { get; init; }
    public string? ColorByFormula { get; init; }

    public IReadOnlyList<string>? PointColors { get; init; } // 最終決定済み #RRGGBB
}
```

## 4.3 WorksheetState モデル

### 4.3.1 ChartState

```csharp
public sealed class ChartState
{
    public string ChartType { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Name { get; init; }

    public int TopRow { get; init; }
    public int LeftColumn { get; init; }
    public int WidthColumns { get; init; }
    public int HeightRows { get; init; }

    public string CategoryFormula { get; init; } = string.Empty;
    public string? Legend { get; init; }
    public bool ShowDataLabels { get; init; }

    public IReadOnlyList<ChartSeriesState> Series { get; init; } = Array.Empty<ChartSeriesState>();
}
```

### 4.3.2 ChartSeriesState

```csharp
public sealed class ChartSeriesState
{
    public string? Name { get; init; }
    public string ValueFormula { get; init; } = string.Empty;
    public IReadOnlyList<string>? PointColors { get; init; } // #RRGGBB
}
```

## 4.4 WorksheetState / WorkbookState への追加

### 4.4.1 WorksheetState

```csharp
public sealed class WorksheetState
{
    public string Name { get; init; } = string.Empty;
    public int Rows { get; init; }
    public int Cols { get; init; }

    public IReadOnlyList<CellState> Cells { get; init; } = Array.Empty<CellState>();
    public IReadOnlyList<MergedRange> MergedRanges { get; init; } = Array.Empty<MergedRange>();
    public IReadOnlyDictionary<string, Area> NamedAreas { get; init; } = new Dictionary<string, Area>();
    public IReadOnlyDictionary<string, FormulaSeries> FormulaSeriesMap { get; init; } = new Dictionary<string, FormulaSeries>();

    public IReadOnlyList<ChartState> Charts { get; init; } = Array.Empty<ChartState>();

    public SheetOptions SheetOptions { get; init; } = new();
    public IReadOnlyList<Issue> Issues { get; init; } = Array.Empty<Issue>();
}
```

### 4.4.2 WorkbookAst への追加

```csharp
public sealed class WorkbookAst
{
    public StylesAst? Styles { get; init; }
    public ChartPaletteAst? ChartPalette { get; init; }

    public IReadOnlyList<ComponentAst> Components { get; init; } = Array.Empty<ComponentAst>();
    public IReadOnlyList<SheetAst> Sheets { get; init; } = Array.Empty<SheetAst>();

    public SourceSpan? Span { get; init; }
}
```

---

# 5. 列挙型・補助モデル

## 5.1 ChartType

```csharp
public enum ChartType
{
    BarStacked,
    Line,
    Bar,
    Column,
    Pie,
    Err
}
```

## 5.2 ChartLegendPosition

```csharp
public enum ChartLegendPosition
{
    None,
    Right,
    Left,
    Top,
    Bottom,
    Err
}
```

## 5.3 ChartReferenceKind

```csharp
public enum ChartReferenceKind
{
    FormulaRefSeries,
    Area,
    DirectRange,
    Err
}
```

## 5.4 ColorResolutionSource

```csharp
public enum ColorResolutionSource
{
    Fixed,
    ColorBy,
    ColorKey,
    DefaultPalette
}
```

## 5.5 ChartReferenceResolved

```csharp
public sealed class ChartReferenceResolved
{
    public ChartReferenceKind Kind { get; init; }
    public string A1Formula { get; init; } = string.Empty; // Summary!$B$2:$B$10 等
    public int Length { get; init; }
}
```

---

# 6. 公開 API

## 6.1 IChartReferenceResolver

```csharp
public interface IChartReferenceResolver
{
    ChartReferenceResolved Resolve(
        string rawReference,
        LayoutSheet sheet,
        IReadOnlyDictionary<string, AreaLayout> areas,
        IReadOnlyDictionary<string, FormulaSeriesLayout> formulaSeries,
        IList<Issue> issues);
}
```

### 役割
- `rawReference` を `formulaRef` / `area` / 直接範囲として解決する
- 1 次元連続範囲か検証する
- A1 形式の最終参照へ変換する

## 6.2 IChartColorResolver

```csharp
public interface IChartColorResolver
{
    IReadOnlyList<string>? ResolvePointColors(
        ChartSeriesAst series,
        int pointCount,
        Func<string, IReadOnlyList<string>?> resolveKeySeries,
        IReadOnlyDictionary<string, string> palette,
        IList<Issue> issues);
}
```

### 役割
- `color` / `colorBy` / `colorKey` / default の優先順位に従い色を決定する
- pointCount 件分の色配列を返す

## 6.3 LayoutEngine 統合 API

LayoutEngine 自体の公開 API は既存の `ILayoutEngine.Build(...)` を維持する。Chart 機能はその内部拡張として扱う。

## 6.4 Renderer 統合 API

Renderer 自体の公開 API は既存の WorkbookState 入力を維持し、`WorksheetState.Charts` を追加読み取り対象とする。

---

# 7. 色解決仕様

## 7.1 色解決の目的

積み上げ横棒において、Excel の既定動作では系列ごとに色が割り当たるが、要件としては

- 系列名が同じ意味なら同じ色にしたい
- 一部は各要素ごとに色を変えたい
- ただし明示指定があればそれを優先したい

という性質がある。

このため、Chart 機能は **系列色ではなく、各データ点の最終色列を保持できる設計** とする。

## 7.2 優先順位

各系列・各データ点について、色の決定優先順位は次の通り。

1. `color`
2. `colorBy`
3. `colorKey`
4. デフォルトパレット

### 7.2.1 `color`
- 指定されていれば、その系列の全データ点に同一色を適用する

### 7.2.2 `colorBy`
- 指定されていれば、色キー系列参照を解決し、各データ点ごとにキー→色へ変換する

### 7.2.3 `colorKey`
- 指定されていれば、当該系列全体に対し key→色 を適用する

### 7.2.4 デフォルトパレット
- どれも指定されていない場合に適用する
- Workbook 単位で固定順序の既定色を使用する

## 7.3 同一キー保証

同一 Workbook 内で同一 `colorKey` は同一色に解決される。

## 7.4 擬似コード

```text
for each series:
  for each point:
    if series.color exists:
        pointColor = series.color
    else if series.colorBy exists:
        key = colorBySeries[point]
        pointColor = palette[key] or default
    else if series.colorKey exists:
        pointColor = palette[series.colorKey] or default
    else:
        pointColor = defaultPalette(seriesIndex)
```

## 7.5 line における色の扱い

折れ線グラフでは「系列線色」と「点マーカー色」を区別する必要がある。

- `PointColors` が全点同一色であれば、その色を系列線色として適用する
- `PointColors` が点ごとに異なる場合は、系列線色は先頭点色を既定線色として適用し、各 `DataPoint` にマーカー色を設定する
- 初版では線そのものを点ごとに分割着色することはしない

---

# 8. DslParser 設計

## 8.1 XSD 追加方針

`workbook` 直下に `chartPalette` を追加し、`sheet` 直下の choice に `chart` を追加する。

### 8.1.1 workbook

```xml
<workbook>
  <styles>...</styles>
  <chartPalette>...</chartPalette>
  <component ... />
  <sheet ... />
</workbook>
```

### 8.1.2 sheet

```xml
<sheet name="Summary">
  <cell ... />
  <repeat ... />
  <chart ... />
  <sheetOptions>...</sheetOptions>
</sheet>
```

## 8.2 AST 構築ルール

- `chart@type` は `ChartType` に変換
- `chart@legend` は `ChartLegendPosition` に変換
- `series@color` は `#RRGGBB` 形式を要求
- `width/height` 省略時は既定値適用
- `series` が 0 個の場合は Error
- `chart@category` 未指定は Fatal

## 8.3 DslParser Issue 方針

### Fatal
- `chart@type` 不正
- `chart@category` 未指定
- `series@value` 未指定
- `r/c` 未指定

### Error
- 色文字列不正
- `width/height <= 0`

### Warning
- `name` 重複
- `chartPalette` 重複 key（後勝ち）

---

# 9. LayoutEngine 設計

## 9.1 役割

LayoutEngine は Chart に関して次を行う。

1. 表示条件 `when` の評価
2. 参照解決
3. 系列長の整合性検証
4. 色の最終決定
5. `LayoutChart` の生成

既存設計上、LayoutEngine はスタイルや最終レイアウトの判断を持つため、Chart の最終判断もここで行う。

## 9.2 参照解決の対象

- `category`
- `series.value`
- `series.colorBy`

## 9.3 解決手順

### 9.3.1 formulaRef 系列名
- `FormulaSeriesLayout` から解決
- 1 次元連続セル列であることを確認
- シート名付き A1 範囲へ変換

### 9.3.2 area 名
- `AreaLayout` から解決
- 幅 1 または高さ 1 のみ許可
- 1 次元でない場合 Error

### 9.3.3 直接範囲
- `A1:B10` や `Summary!$B$2:$B$10` を解析
- 1 次元連続を確認

## 9.4 系列整合性

`category` の長さを `N` としたとき、

- 各 `value` 系列長は `N`
- 各 `colorBy` 系列長も `N`

でなければならない。

## 9.5 グラフ配置

Chart はセルとは別レイヤで扱い、以下を保持する。

- `TopRow`
- `LeftColumn`
- `WidthColumns`
- `HeightRows`

セル占有マップには含めない。

## 9.6 LayoutSheet への追加

```csharp
public sealed class LayoutSheet
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<LayoutCell> Cells { get; init; } = Array.Empty<LayoutCell>();
    public IReadOnlyList<AreaLayout> Areas { get; init; } = Array.Empty<AreaLayout>();
    public IReadOnlyList<FormulaSeriesLayout> FormulaSeries { get; init; } = Array.Empty<FormulaSeriesLayout>();
    public IReadOnlyList<LayoutChart> Charts { get; init; } = Array.Empty<LayoutChart>();
}
```

## 9.7 擬似コード

```text
for each sheet:
  for each chartAst:
    if when == false:
      continue

    category = resolve(chartAst.CategoryRef)
    assert category is 1D

    for each seriesAst:
      value = resolve(seriesAst.ValueRef)
      assert value.Length == category.Length

      if colorBy exists:
        colorBy = resolve(seriesAst.ColorByRef)
        assert colorBy.Length == category.Length

      pointColors = resolveColors(seriesAst)

    emit LayoutChart
```

---

# 10. WorksheetState 設計

## 10.1 役割

WorksheetState は `LayoutChart` を受け取り、Renderer が直接使える `ChartState` を構築する。

既存方針どおり、ここは最終状態固定の層である。

## 10.2 Build フローへの追加

既存の Build フローに以下を追加する。

1. 各 `LayoutSheet.Charts` を走査
2. `LayoutChart` → `ChartState` に変換
3. シート範囲外・不整合を検証
4. `WorksheetState.Charts` に格納

## 10.3 検証項目

### Fatal
- アンカー位置がシート範囲外
- `WidthColumns <= 0` または `HeightRows <= 0`

### Error
- 系列 0 件
- `CategoryFormula` 空
- `ValueFormula` 空
- `PointColors` 長さ不整合

### Warning
- タイトル空
- 凡例位置不正時の既定値補正

## 10.4 変換規則

- `LayoutChart.ChartType` → `ChartState.ChartType`
- `LayoutChart.Series[*].PointColors` をそのまま保持
- `CategoryFormula` / `ValueFormula` は Renderer 用の A1 範囲文字列として保持

---

# 11. Renderer 設計

## 11.1 役割

Renderer は `ChartState` をそのまま OpenXML の Drawing / Chart に変換する。  
既存 Renderer 方針に従い、再解釈や追加判断は行わない。

## 11.2 出力先

各シートについて

- `DrawingsPart`
- `WorksheetDrawing`
- `TwoCellAnchor` または `OneCellAnchor`
- `ChartPart`
- `ChartSpace`

を生成する。

## 11.3 グラフ種別の写像

### 初版必須

| `ChartType` | OpenXML |
|---|---|
| `BarStacked` | `BarChart` + `BarDirection=Bar` + `BarGrouping=Stacked` |
| `Line` | `LineChart` |

### 将来拡張

| `ChartType` | OpenXML |
|---|---|
| `Bar` | `BarChart` + `BarDirection=Bar` |
| `Column` | `BarChart` + `BarDirection=Column` |
| `Pie` | `PieChart` |

## 11.4 `barStacked` 出力仕様

- `BarDirectionValues.Bar`
- `BarGroupingValues.Stacked`
- 系列ごとに `BarChartSeries`
- 各データ点色は `DataPoint` の `ShapeProperties / SolidFill` に設定

## 11.5 `line` 出力仕様

- `LineChart` を使用する
- 系列ごとに `LineChartSeries` を出力する
- `category` を X 軸カテゴリとして設定する
- `value` を Y 値系列として設定する
- point color が指定されている場合は、各 `DataPoint` に対して `ShapeProperties / SolidFill` を設定する
- 系列線色は次の規則とする
  - その系列の `PointColors` が全点同一色なら、その色を系列線色として適用する
  - `PointColors` が点ごとに異なる場合、系列線色は先頭点色を既定値として適用し、各点マーカー色を `DataPoint` 単位で設定する
- 初版では以下は非対応とする
  - 二次軸
  - スムージング
  - 面塗りつぶし付き折れ線
  - エラーバー

## 11.6 タイトル・凡例

- `Title` が存在すれば `ChartTitle` を出力
- `Legend` に応じて凡例位置を設定
- `None` の場合は凡例を出力しない

## 11.7 アンカー配置

- `(TopRow, LeftColumn)` を左上セルに対応づける
- `WidthColumns / HeightRows` を右下セルオフセットへ変換する
- 詳細な pixel / EMU 変換は Renderer 内部実装とするが、意味決定は持たない

## 11.8 擬似コード

```text
for each worksheet:
  ensure DrawingsPart

  for each chart in ws.Charts:
    create ChartPart
    create chart root by chart.ChartType
    bind category formula
    bind series value formula
    apply point colors
    anchor chart to sheet drawing
```

---

# 12. エラーモデル

## 12.1 Issue 種別案

```csharp
public enum IssueKind
{
    // 既存...
    ChartTypeInvalid,
    ChartSeriesMissing,
    ChartCategoryMissing,
    ChartReferenceUnresolved,
    ChartReferenceNot1D,
    ChartSeriesLengthMismatch,
    ChartColorInvalid,
    ChartColorKeyUndefined,
    ChartAnchorOutOfRange,
}
```

## 12.2 Severity 方針

### Fatal
- グラフ構築不能
- 参照解決不能で出力意味が成立しない
- アンカーがシート外で OpenXML 構築不能

### Error
- 系列長不一致
- 1 次元でない参照
- 色値不正

### Warning
- `colorKey` がパレット未定義で既定色へフォールバック
- タイトル未設定
- `legend` 不正で既定位置へ補正

---

# 13. 初版制約

## 13.1 必須対応
- `barStacked`
- `line`
- `sheet` 直下配置
- 1 次元連続参照
- point color 出力

## 13.2 非対応
- `component` / `repeat` / `grid` 直下の `<chart>`
- 複合グラフ
- 二次軸
- 100% 積み上げ (`stacked100`)
- 系列並び替え
- 負値の特殊表示制御

## 13.3 将来拡張
- `barStacked100`
- `area`
- `scatter`
- component 内再利用可能 chart 定義
- palette のテーマ連携

---

# 14. 性能

## 14.1 設計前提

Chart 機能はセル出力ほど大量件数にはなりにくいが、以下のコストがある。

- 参照解決
- point color 配列生成
- OpenXML ChartPart 構築

## 14.2 時間計算量

- 参照解決: `O(S)`
- 系列整合性検証: `O(S * N)`
- 色解決: `O(S * N)`
- Renderer 出力: `O(S * N)`

ここで、
- `S`: 系列数
- `N`: 各系列のデータ点数

## 14.3 メモリ

- `PointColors` を保持するため `O(S * N)`
- 初版では point color 完全保持を優先する

---

# 15. テスト観点

## 15.1 DslParser
- `<chart>` 正常解析
- `type="barStacked"` 正常
- `type="line"` 正常
- `series@value` 未指定 Error
- `color="#GGGGGG"` Error
- `chartPalette` 重複 key 後勝ち

## 15.2 LayoutEngine
- `formulaRef` 解決
- `area` 解決
- 直接範囲解決
- 1D でない area 拒否
- `category/value` 長さ不一致 Error
- `colorBy` 長さ不一致 Error
- `color` / `colorBy` / `colorKey` / default の優先順位確認

## 15.3 WorksheetState
- `ChartState` 構築
- シート外アンカー Fatal
- `PointColors` 長さ保持
- 複数 chart 保持

## 15.4 Renderer
- `barStacked` の `BarGrouping=Stacked`
- `BarDirection=Bar`
- `line` の `LineChart` 出力
- タイトル出力
- 凡例出力
- `DataPoint` ごとの色出力
- 折れ線で全点同一色の場合、系列線色がその色になること
- 折れ線で点ごとに色が異なる場合、各点マーカー色が出力されること
- OpenXML schema valid
- Excel 修復不要

---

# 16. 実装順序

## Step 1
DslParser / XSD / AST
- `ChartPaletteAst`
- `ChartAst`
- `ChartSeriesAst`

## Step 2
LayoutEngine
- 参照解決
- 色解決
- `LayoutChart`

## Step 3
WorksheetState
- `ChartState`
- `WorksheetState.Charts`

## Step 4
Renderer
- DrawingPart
- ChartPart
- `barStacked` 出力
- point color 出力
- `line` 出力

## Step 5
E2E テスト
- サンプル DSL
- OpenXML 検証
- Excel 表示確認

---

# 17. サンプル DSL

## 17.1 固定色

```xml
<chart type="barStacked" title="Progress" r="2" c="8" width="10" height="16" category="Task.Name">
  <series name="Done"  value="Task.Done"  color="#4CAF50"/>
  <series name="Doing" value="Task.Doing" color="#FF9800"/>
  <series name="Todo"  value="Task.Todo"  color="#BDBDBD"/>
</chart>
```

## 17.2 colorKey

```xml
<chartPalette>
  <color key="Done" value="#4CAF50"/>
  <color key="Doing" value="#FF9800"/>
  <color key="Todo" value="#BDBDBD"/>
</chartPalette>

<chart type="barStacked" title="Progress" r="2" c="8" width="10" height="16" category="Task.Name">
  <series name="Done"  value="Task.Done"  colorKey="Done"/>
  <series name="Doing" value="Task.Doing" colorKey="Doing"/>
  <series name="Todo"  value="Task.Todo"  colorKey="Todo"/>
</chart>
```

## 17.3 colorBy

```xml
<chart type="barStacked" title="Breakdown" r="2" c="8" width="10" height="16" category="Task.Name">
  <series name="Seg1" value="Task.Seg1" colorBy="Task.Seg1ColorKey"/>
  <series name="Seg2" value="Task.Seg2" colorBy="Task.Seg2ColorKey"/>
</chart>
```

## 17.4 line

```xml
<chart type="line" title="Trend" r="20" c="8" width="10" height="16" category="Task.Name">
  <series name="Actual" value="Task.Actual" colorKey="Actual"/>
  <series name="Plan" value="Task.Plan" color="#3366CC"/>
</chart>
```

---

# 18. 総括

本設計は、既存 ExcelReport の設計原則を維持したまま、  
Chart 機能、特に `barStacked` と point color 制御、および `line` を追加するための詳細設計である。

重要点は次のとおり。

1. **Renderer 非判断原則を維持する**
2. **色・参照・整合性の最終決定は LayoutEngine が担う**
3. **WorksheetState に `ChartState` を追加し、最終状態を完全保持する**
4. **初版要件として `barStacked` / `line` と point color を必須対応とする**
5. **`colorKey` / `colorBy` により「同一意味キーは同一色」を保証する**
