namespace ExcelReportLib.WorksheetState;

public sealed class WorksheetState
{
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

    public string Name { get; }

    public int RowCount { get; }

    public int ColumnCount { get; }

    public IReadOnlyDictionary<(int Row, int Column), CellState> Cells { get; }

    public IReadOnlyList<MergedCellRange> MergedRanges { get; }

    public IReadOnlyDictionary<string, NamedAreaState> NamedAreas { get; }

    public WorksheetOptionsState Options { get; }
}

public sealed class MergedCellRange
{
    public MergedCellRange(int topRow, int leftColumn, int bottomRow, int rightColumn)
    {
        TopRow = topRow;
        LeftColumn = leftColumn;
        BottomRow = bottomRow;
        RightColumn = rightColumn;
    }

    public int TopRow { get; }

    public int LeftColumn { get; }

    public int BottomRow { get; }

    public int RightColumn { get; }
}

public sealed class NamedAreaState
{
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

    public string Name { get; }

    public int TopRow { get; }

    public int LeftColumn { get; }

    public int BottomRow { get; }

    public int RightColumn { get; }
}

public sealed class WorksheetOptionsState
{
    public static WorksheetOptionsState Empty { get; } = new(
        freezePanes: null,
        rowGroups: [],
        columnGroups: [],
        autoFilter: null);

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

    public FreezePaneState? FreezePanes { get; }

    public IReadOnlyList<WorksheetGroupState> RowGroups { get; }

    public IReadOnlyList<WorksheetGroupState> ColumnGroups { get; }

    public AutoFilterState? AutoFilter { get; }
}

public sealed class FreezePaneState
{
    public FreezePaneState(string target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public string Target { get; }
}

public sealed class AutoFilterState
{
    public AutoFilterState(string target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public string Target { get; }
}

public sealed class WorksheetGroupState
{
    public WorksheetGroupState(string target, bool collapsed)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Collapsed = collapsed;
    }

    public string Target { get; }

    public bool Collapsed { get; }
}
