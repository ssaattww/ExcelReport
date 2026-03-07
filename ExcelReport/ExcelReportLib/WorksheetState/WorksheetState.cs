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
        autoFilter: null);

    /// <summary>
    /// Initializes a new instance of the worksheet options state type.
    /// </summary>
    /// <param name="freezePanes">The freeze panes.</param>
    /// <param name="rowGroups">The row groups.</param>
    /// <param name="columnGroups">The column groups.</param>
    /// <param name="autoFilter">The auto filter.</param>
    public WorksheetOptionsState(
        FreezePaneState? freezePanes,
        IReadOnlyList<WorksheetGroupState> rowGroups,
        IReadOnlyList<WorksheetGroupState> columnGroups,
        AutoFilterState? autoFilter)
    {
        FreezePanes = freezePanes;
        RowGroups = (rowGroups ?? throw new ArgumentNullException(nameof(rowGroups))).ToArray();
        ColumnGroups = (columnGroups ?? throw new ArgumentNullException(nameof(columnGroups))).ToArray();
        AutoFilter = autoFilter;
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
