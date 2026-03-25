namespace ExcelReportLib.WorksheetState;

/// <summary>
/// Represents worksheet state.
/// </summary>
public sealed class WorksheetState
{
    /// <summary>
    /// Initializes a new instance of the worksheet state type.
    /// </summary>
    /// <param name="name">The worksheet name.</param>
    /// <param name="rowCount">The number of rows tracked for the worksheet.</param>
    /// <param name="columnCount">The number of columns tracked for the worksheet.</param>
    /// <param name="cells">The cell states keyed by row and column coordinates.</param>
    /// <param name="mergedRanges">The merged-cell ranges defined on the worksheet.</param>
    /// <param name="namedAreas">The named areas available in the worksheet.</param>
    /// <param name="options">The worksheet-level options.</param>
    public WorksheetState(
        string name,
        int rowCount,
        int columnCount,
        IReadOnlyDictionary<(int Row, int Column), CellState> cells,
        IReadOnlyList<MergedCellRange> mergedRanges,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas,
        WorksheetOptionsState options)
    {
        Name = name;
        RowCount = rowCount;
        ColumnCount = columnCount;
        Cells = new Dictionary<(int Row, int Column), CellState>(cells ?? throw new ArgumentNullException(nameof(cells)));
        MergedRanges = (mergedRanges ?? throw new ArgumentNullException(nameof(mergedRanges))).ToArray();
        NamedAreas = new Dictionary<string, NamedAreaState>(
            namedAreas ?? throw new ArgumentNullException(nameof(namedAreas)),
            StringComparer.Ordinal);
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the row count.
    /// </summary>
    public int RowCount { get; }

    /// <summary>
    /// Gets the column count.
    /// </summary>
    public int ColumnCount { get; }

    /// <summary>
    /// Gets the cells.
    /// </summary>
    public IReadOnlyDictionary<(int Row, int Column), CellState> Cells { get; }

    /// <summary>
    /// Gets the merged ranges.
    /// </summary>
    public IReadOnlyList<MergedCellRange> MergedRanges { get; }

    /// <summary>
    /// Gets the named areas.
    /// </summary>
    public IReadOnlyDictionary<string, NamedAreaState> NamedAreas { get; }

    /// <summary>
    /// Gets the options.
    /// </summary>
    public WorksheetOptionsState Options { get; }
}

/// <summary>
/// Represents merged cell range.
/// </summary>
public sealed class MergedCellRange
{
    /// <summary>
    /// Initializes a new instance of the merged cell range type.
    /// </summary>
    /// <param name="topRow">The top row.</param>
    /// <param name="leftColumn">The left column.</param>
    /// <param name="bottomRow">The bottom row.</param>
    /// <param name="rightColumn">The right column.</param>
    public MergedCellRange(int topRow, int leftColumn, int bottomRow, int rightColumn)
    {
        TopRow = topRow;
        LeftColumn = leftColumn;
        BottomRow = bottomRow;
        RightColumn = rightColumn;
    }

    /// <summary>
    /// Gets the top row.
    /// </summary>
    public int TopRow { get; }

    /// <summary>
    /// Gets the left column.
    /// </summary>
    public int LeftColumn { get; }

    /// <summary>
    /// Gets the bottom row.
    /// </summary>
    public int BottomRow { get; }

    /// <summary>
    /// Gets the right column.
    /// </summary>
    public int RightColumn { get; }
}

/// <summary>
/// Represents named area state.
/// </summary>
public sealed class NamedAreaState
{
    /// <summary>
    /// Initializes a new instance of the named area state type.
    /// </summary>
    /// <param name="name">The target name.</param>
    /// <param name="topRow">The top row.</param>
    /// <param name="leftColumn">The left column.</param>
    /// <param name="bottomRow">The bottom row.</param>
    /// <param name="rightColumn">The right column.</param>
    public NamedAreaState(
        string name,
        int topRow,
        int leftColumn,
        int bottomRow,
        int rightColumn)
    {
        Name = name;
        TopRow = topRow;
        LeftColumn = leftColumn;
        BottomRow = bottomRow;
        RightColumn = rightColumn;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the top row.
    /// </summary>
    public int TopRow { get; }

    /// <summary>
    /// Gets the left column.
    /// </summary>
    public int LeftColumn { get; }

    /// <summary>
    /// Gets the bottom row.
    /// </summary>
    public int BottomRow { get; }

    /// <summary>
    /// Gets the right column.
    /// </summary>
    public int RightColumn { get; }
}

/// <summary>
/// Represents worksheet options state.
/// </summary>
public sealed class WorksheetOptionsState
{
    /// <summary>
    /// Gets the empty.
    /// </summary>
    public static WorksheetOptionsState Empty { get; } = new(
        freezePanes: null,
        rowGroups: [],
        columnGroups: [],
        autoFilter: null,
        conditionalFormattings: []);

    /// <summary>
    /// Initializes a new instance of the worksheet options state type.
    /// </summary>
    /// <param name="freezePanes">The freeze panes.</param>
    /// <param name="rowGroups">The row groups.</param>
    /// <param name="columnGroups">The column groups.</param>
    /// <param name="autoFilter">The auto filter.</param>
    /// <param name="conditionalFormattings">The conditional formatting definitions.</param>
    public WorksheetOptionsState(
        FreezePaneState? freezePanes,
        IReadOnlyList<WorksheetGroupState> rowGroups,
        IReadOnlyList<WorksheetGroupState> columnGroups,
        AutoFilterState? autoFilter,
        IReadOnlyList<ConditionalFormattingState> conditionalFormattings)
    {
        FreezePanes = freezePanes;
        RowGroups = (rowGroups ?? throw new ArgumentNullException(nameof(rowGroups))).ToArray();
        ColumnGroups = (columnGroups ?? throw new ArgumentNullException(nameof(columnGroups))).ToArray();
        AutoFilter = autoFilter;
        ConditionalFormattings = (conditionalFormattings ?? throw new ArgumentNullException(nameof(conditionalFormattings))).ToArray();
    }

    /// <summary>
    /// Gets the freeze panes.
    /// </summary>
    public FreezePaneState? FreezePanes { get; }

    /// <summary>
    /// Gets the row groups.
    /// </summary>
    public IReadOnlyList<WorksheetGroupState> RowGroups { get; }

    /// <summary>
    /// Gets the column groups.
    /// </summary>
    public IReadOnlyList<WorksheetGroupState> ColumnGroups { get; }

    /// <summary>
    /// Gets the auto filter.
    /// </summary>
    public AutoFilterState? AutoFilter { get; }

    /// <summary>
    /// Gets the conditional formatting definitions.
    /// </summary>
    public IReadOnlyList<ConditionalFormattingState> ConditionalFormattings { get; }
}

/// <summary>
/// Represents freeze pane state.
/// </summary>
public sealed class FreezePaneState
{
    /// <summary>
    /// Initializes a new instance of the freeze pane state type.
    /// </summary>
    /// <param name="target">The target.</param>
    public FreezePaneState(string target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    /// <summary>
    /// Gets the target.
    /// </summary>
    public string Target { get; }
}

/// <summary>
/// Represents auto filter state.
/// </summary>
public sealed class AutoFilterState
{
    /// <summary>
    /// Initializes a new instance of the auto filter state type.
    /// </summary>
    /// <param name="target">The target.</param>
    public AutoFilterState(string target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    /// <summary>
    /// Gets the target.
    /// </summary>
    public string Target { get; }
}

/// <summary>
/// Represents conditional formatting state.
/// </summary>
public sealed class ConditionalFormattingState
{
    /// <summary>
    /// Initializes a new instance of the conditional formatting state type.
    /// </summary>
    /// <param name="target">The target range.</param>
    /// <param name="minColor">The minimum scale color.</param>
    /// <param name="maxColor">The maximum scale color.</param>
    /// <param name="midColor">The middle scale color for 3-color scale.</param>
    /// <param name="formula">The formula rule expression.</param>
    /// <param name="formulaRef">The formula reference target.</param>
    /// <param name="fillColor">The fill color used when formula condition matches.</param>
    /// <param name="fontName">The font name.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="fontBold">The bold flag.</param>
    /// <param name="fontItalic">The italic flag.</param>
    /// <param name="fontUnderline">The underline flag.</param>
    /// <param name="numberFormatCode">The number format code.</param>
    /// <param name="borderTop">The top border style.</param>
    /// <param name="borderBottom">The bottom border style.</param>
    /// <param name="borderLeft">The left border style.</param>
    /// <param name="borderRight">The right border style.</param>
    /// <param name="borderColor">The border color.</param>
    public ConditionalFormattingState(
        string target,
        string minColor,
        string maxColor,
        string? midColor,
        string? formula,
        string? formulaRef,
        string fillColor,
        string? fontName,
        double? fontSize,
        bool? fontBold,
        bool? fontItalic,
        bool? fontUnderline,
        string? numberFormatCode,
        string? borderTop,
        string? borderBottom,
        string? borderLeft,
        string? borderRight,
        string? borderColor)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        MinColor = minColor ?? throw new ArgumentNullException(nameof(minColor));
        MaxColor = maxColor ?? throw new ArgumentNullException(nameof(maxColor));
        MidColor = midColor;
        Formula = formula;
        FormulaRef = formulaRef;
        FillColor = fillColor ?? throw new ArgumentNullException(nameof(fillColor));
        FontName = fontName;
        FontSize = fontSize;
        FontBold = fontBold;
        FontItalic = fontItalic;
        FontUnderline = fontUnderline;
        NumberFormatCode = numberFormatCode;
        BorderTop = borderTop;
        BorderBottom = borderBottom;
        BorderLeft = borderLeft;
        BorderRight = borderRight;
        BorderColor = borderColor;
    }

    /// <summary>
    /// Gets the target range.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the minimum scale color.
    /// </summary>
    public string MinColor { get; }

    /// <summary>
    /// Gets the maximum scale color.
    /// </summary>
    public string MaxColor { get; }

    /// <summary>
    /// Gets the middle scale color for 3-color scale.
    /// </summary>
    public string? MidColor { get; }

    /// <summary>
    /// Gets the formula condition.
    /// </summary>
    public string? Formula { get; }

    /// <summary>
    /// Gets the formula reference target.
    /// </summary>
    public string? FormulaRef { get; }

    /// <summary>
    /// Gets the fill color for formula-based formatting.
    /// </summary>
    public string FillColor { get; }

    /// <summary>
    /// Gets the font name.
    /// </summary>
    public string? FontName { get; }

    /// <summary>
    /// Gets the font size.
    /// </summary>
    public double? FontSize { get; }

    /// <summary>
    /// Gets a value indicating whether bold font.
    /// </summary>
    public bool? FontBold { get; }

    /// <summary>
    /// Gets a value indicating whether italic font.
    /// </summary>
    public bool? FontItalic { get; }

    /// <summary>
    /// Gets a value indicating whether underline font.
    /// </summary>
    public bool? FontUnderline { get; }

    /// <summary>
    /// Gets the number format code.
    /// </summary>
    public string? NumberFormatCode { get; }

    /// <summary>
    /// Gets the top border style.
    /// </summary>
    public string? BorderTop { get; }

    /// <summary>
    /// Gets the bottom border style.
    /// </summary>
    public string? BorderBottom { get; }

    /// <summary>
    /// Gets the left border style.
    /// </summary>
    public string? BorderLeft { get; }

    /// <summary>
    /// Gets the right border style.
    /// </summary>
    public string? BorderRight { get; }

    /// <summary>
    /// Gets the border color.
    /// </summary>
    public string? BorderColor { get; }
}

/// <summary>
/// Represents worksheet group state.
/// </summary>
public sealed class WorksheetGroupState
{
    /// <summary>
    /// Initializes a new instance of the worksheet group state type.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="collapsed">The collapsed.</param>
    public WorksheetGroupState(string target, bool collapsed)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Collapsed = collapsed;
    }

    /// <summary>
    /// Gets the target.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets a value indicating whether collapsed.
    /// </summary>
    public bool Collapsed { get; }
}
