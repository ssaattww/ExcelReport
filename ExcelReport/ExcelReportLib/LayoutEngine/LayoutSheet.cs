using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.LayoutEngine;

public sealed class LayoutSheet
{
    public LayoutSheet(string name, IEnumerable<LayoutCell> cells, int rows, int cols)
        : this(name, cells, rows, cols, namedAreas: null, options: null)
    {
    }

    public LayoutSheet(
        string name,
        IEnumerable<LayoutCell> cells,
        int rows,
        int cols,
        IEnumerable<LayoutNamedArea>? namedAreas = null,
        SheetOptionsAst? options = null)
    {
        Name = name;
        Rows = rows;
        Cols = cols;
        Cells = (cells ?? Array.Empty<LayoutCell>())
            .OrderBy(cell => cell.Row)
            .ThenBy(cell => cell.Col)
            .ToArray();
        NamedAreas = (namedAreas ?? Array.Empty<LayoutNamedArea>()).ToArray();
        Options = options;
    }

    public string Name { get; }

    public int Rows { get; }

    public int Cols { get; }

    public IReadOnlyList<LayoutCell> Cells { get; }

    public IReadOnlyList<LayoutNamedArea> NamedAreas { get; }

    public SheetOptionsAst? Options { get; }
}

public sealed class LayoutNamedArea
{
    public LayoutNamedArea(
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
