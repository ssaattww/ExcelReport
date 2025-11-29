# WorksheetState 詳細設計書 v1

## 1. 概要・責務

### 1.1 モジュール名

- モジュール名: `WorksheetState`
- 所属アセンブリ想定: `ExcelReport.Core`（仮）
- 役割:
  - LayoutEngine が生成した論理レイアウト（LayoutPlan）を受け取り、Excel 物理出力直前の「シート状態」を確定させて保持する。
  - セル占有・結合セル・スタイル・名前付き領域などを管理し、Renderer が単純なループで `.xlsx` を生成できるようにする。

## 1.2 上流・下流との境界

- 入力 (IN):
  - LayoutPlan（LayoutEngine の出力）
  - 各シートの行・列座標、セル値、数式、styleRef、結合セル、formulaRef 系列など
- 出力 (OUT):
  - WorksheetState（Renderer がそのまま参照して Excel に書き込む）

## 1.3 責務範囲

- セル占有管理（重複検出）
- 結合セル領域の整合性保証
- 最終スタイルの保持（論理 → 物理手前）
- 名前付き領域（Area）の保持
- formulaRef 系列の物理アドレス保持
- Issue(Error/Fatal/Warning) の生成

WorksheetState 自体は Excel 操作（ClosedXML）は行わない。

---

## 2. 公開 API

### 2.1 インターフェース

```csharp
public interface IWorksheetStateBuilder
{
    WorksheetWorkbookState Build(LayoutPlan layoutPlan);
}
```

### 2.2 オブジェクト構造（Workbook）

```csharp
public sealed class WorksheetWorkbookState
{
    public IReadOnlyList<WorksheetState> Sheets { get; }
    public IReadOnlyList<Issue> Issues { get; }

    public WorksheetWorkbookState(
        IReadOnlyList<WorksheetState> sheets,
        IReadOnlyList<Issue> issues)
    {
        Sheets = sheets;
        Issues = issues;
    }
}
```

### 2.3 オブジェクト構造（Sheet）

```csharp
public sealed class WorksheetState
{
    public string Name { get; }
    public int Rows { get; }
    public int Cols { get; }

    public IReadOnlyList<CellState> Cells { get; }
    public IReadOnlyList<MergedRange> MergedRanges { get; }
    public IReadOnlyDictionary<string, Area> NamedAreas { get; }
    public IReadOnlyDictionary<string, FormulaSeries> FormulaSeriesMap { get; }

    public IReadOnlyList<Issue> Issues { get; }

    public WorksheetState(
        string name,
        int rows,
        int cols,
        IReadOnlyList<CellState> cells,
        IReadOnlyList<MergedRange> mergedRanges,
        IReadOnlyDictionary<string, Area> namedAreas,
        IReadOnlyDictionary<string, FormulaSeries> formulaSeriesMap,
        IReadOnlyList<Issue> issues)
    {
        Name = name;
        Rows = rows;
        Cols = cols;
        Cells = cells;
        MergedRanges = mergedRanges;
        NamedAreas = namedAreas;
        FormulaSeriesMap = formulaSeriesMap;
        Issues = issues;
    }
}
```

---

## 3. データモデル

### 3.1 LayoutCell との対応

LayoutPlan 内の LayoutCell から WorksheetState の CellState が構築される。

### 3.2 CellState

```csharp
public enum CellValueKind
{
    Blank,
    Constant,
    Formula,
    Error
}

public sealed class CellState
{
    public int Row { get; }
    public int Col { get; }

    public CellValueKind ValueKind { get; }
    public object? ConstantValue { get; }
    public string? Formula { get; }
    public string? ErrorText { get; }

    public StyleSnapshot Style { get; }

    public bool IsMergedHead { get; }
    public MergedRange? MergedRange { get; }

    public string? FormulaRefName { get; }

    public CellState(
        int row,
        int col,
        CellValueKind valueKind,
        object? constantValue,
        string? formula,
        string? errorText,
        StyleSnapshot style,
        bool isMergedHead,
        MergedRange? mergedRange,
        string? formulaRefName)
    {
        Row = row;
        Col = col;
        ValueKind = valueKind;
        ConstantValue = constantValue;
        Formula = formula;
        ErrorText = errorText;
        Style = style;
        IsMergedHead = isMergedHead;
        MergedRange = mergedRange;
        FormulaRefName = formulaRefName;
    }
}
```

---

### 3.3 StyleSnapshot

```csharp
public sealed class StyleSnapshot
{
    public string? FontName { get; }
    public double? FontSize { get; }
    public bool? FontBold { get; }
    public bool? FontItalic { get; }
    public bool? FontUnderline { get; }

    public string? FillColor { get; }
    public string? NumberFormatCode { get; }

    public BorderSnapshot? Border { get; }

    public IReadOnlyList<string> AppliedStyleNames { get; }

    public StyleSnapshot(
        string? fontName,
        double? fontSize,
        bool? fontBold,
        bool? fontItalic,
        bool? fontUnderline,
        string? fillColor,
        string? numberFormatCode,
        BorderSnapshot? border,
        IReadOnlyList<string> appliedStyleNames)
    {
        FontName = fontName;
        FontSize = fontSize;
        FontBold = fontBold;
        FontItalic = fontItalic;
        FontUnderline = fontUnderline;
        FillColor = fillColor;
        NumberFormatCode = numberFormatCode;
        Border = border;
        AppliedStyleNames = appliedStyleNames;
    }
}
```

---

### 3.4 結合セル定義

```csharp
public sealed class MergedRange
{
    public int Top { get; }
    public int Left { get; }
    public int RowSpan { get; }
    public int ColSpan { get; }

    public int Bottom => Top + RowSpan - 1;
    public int Right  => Left + ColSpan - 1;

    public MergedRange(int top, int left, int rowSpan, int colSpan)
    {
        Top = top;
        Left = left;
        RowSpan = rowSpan;
        ColSpan = colSpan;
    }
}
```

---

### 3.5 名前付き領域 Area

```csharp
public sealed class Area
{
    public string Name { get; }
    public int Top { get; }
    public int Bottom { get; }
    public int Left { get; }
    public int Right { get; }

    public Area(string name, int top, int bottom, int left, int right)
    {
        Name = name;
        Top = top;
        Bottom = bottom;
        Left = left;
        Right = right;
    }
}
```

---

### 3.6 formulaRef 系列

```csharp
public enum FormulaSeriesOrientation
{
    Row,
    Column
}

public sealed class FormulaSeries
{
    public string Name { get; }
    public FormulaSeriesOrientation Orientation { get; }
    public IReadOnlyList<(int Row, int Col)> Cells { get; }

    public FormulaSeries(string name, FormulaSeriesOrientation orientation,
                         IReadOnlyList<(int Row, int Col)> cells)
    {
        Name = name;
        Orientation = orientation;
        Cells = cells;
    }
}
```

---

## 4. 処理フロー

### 4.1 Build フロー

1. 全シートをループ
2. セル占有管理
3. 結合セル検証・生成
4. Area の構築
5. FormulaSeries の構築
6. WorksheetState を構築
7. WorkbookState にまとめて返す

---

### 4.2 占有検証（擬似コード）

```csharp
foreach (var lc in sheetLayout.Cells)
{
    var key = (lc.Row, lc.Col);

    if (cellMap.ContainsKey(key))
    {
        issues.Add(Issue.CellOverlap(sheetLayout.Name, lc.Row, lc.Col));
        continue;
    }

    cellMap[key] = ConvertToCellState(lc);

    if (lc.RowSpan > 1 || lc.ColSpan > 1)
        mergedCandidates.Add(new MergedRange(lc.Row, lc.Col, lc.RowSpan, lc.ColSpan));
}
```

---

### 4.3 結合セル検証ルール

- シート範囲を超える結合 → Fatal
- 対象セルが存在しない → Error
- 結合領域が他の結合と重複 → Error
- 通過したものだけ MergedRanges に追加

---

### 4.4 Area の構築

- LayoutEngine が AreaLayout を提供
- 例えば```<repeat name="DetailRows" …>```の名前が同じとき
  - 後勝ちで辞書に登録
  - 同名が複数ある場合は Warning

---

### 4.5 FormulaSeries の構築

- LayoutEngine の提供値をそのまま移す
- 空系列は Warning
- 1D連続は LayoutEngine 側で保証済み

---

## 5. エラーモデル

### 5.1 IssueKind

- CellOverlap
- MergeRangeOutOfSheet
- MergeRangeConflict
- MergeRangeIncomplete
- AreaDuplicateName
- FormulaSeriesEmpty

Severity:

- Fatal: 配置不能
- Error: 修正されるが結果に欠損
- Warning: 実行可能だが注意必要

---

## 6. 性能

- セル数 N → O(N)
- 結合セル M → O(M log M)
- WorksheetState は単純データ構造で高速

---

## 7. テスト観点

### 正常系

- 結合セルありなし
- Area 正常
- formulaRef 正常

### 異常系

- セル重複
- 結合範囲外
- 結合欠損
- 結合重複
- Area 名重複
- 空 formulaRef 系列

---

## 8. 最小実装例

```csharp
public sealed class WorksheetStateBuilder : IWorksheetStateBuilder
{
    public WorksheetWorkbookState Build(LayoutPlan layoutPlan)
    {
        var allSheetStates = new List<WorksheetState>();
        var allIssues = new List<Issue>();

        foreach (var sheetLayout in layoutPlan.Sheets)
        {
            var sheetIssues = new List<Issue>();
            var cellMap = new Dictionary<(int,int),CellState>();
            var mergedCandidates = new List<MergedRange>();

            foreach (var lc in sheetLayout.Cells)
            {
                var key = (lc.Row, lc.Col);
                if (cellMap.ContainsKey(key))
                {
                    sheetIssues.Add(Issue.CellOverlap(sheetLayout.Name, lc.Row, lc.Col));
                    continue;
                }

                cellMap[key] = ConvertToCellState(lc);

                if (lc.RowSpan > 1 || lc.ColSpan > 1)
                    mergedCandidates.Add(new MergedRange(lc.Row, lc.Col, lc.RowSpan, lc.ColSpan));
            }

            var mergedRanges = ValidateMergedRanges(sheetLayout, mergedCandidates, cellMap, sheetIssues);

            var namedAreas = BuildAreas(sheetLayout, sheetIssues);
            var seriesMap = BuildFormulaSeries(sheetLayout, sheetIssues);

            var sheetState = new WorksheetState(
                sheetLayout.Name,
                sheetLayout.Rows,
                sheetLayout.Cols,
                cellMap.Values.OrderBy(x => x.Row).ThenBy(x => x.Col).ToList(),
                mergedRanges,
                namedAreas,
                seriesMap,
                sheetIssues);

            allSheetStates.Add(sheetState);
            allIssues.AddRange(sheetIssues);
        }

        return new WorksheetWorkbookState(allSheetStates, allIssues);
    }
}
```

---

以上、WorksheetState の詳細設計の **完全版** です。
