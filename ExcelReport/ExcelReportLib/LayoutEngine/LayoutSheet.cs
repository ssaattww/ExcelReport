using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.LayoutEngine;

/// <summary>
/// Represents layout sheet.
/// </summary>
public sealed class LayoutSheet
{
    /// <summary>
    /// Initializes a new instance of the layout sheet type.
    /// </summary>
    /// <param name="name">The target name.</param>
    /// <param name="cells">The cells.</param>
    /// <param name="rows">The rows.</param>
    /// <param name="cols">The cols.</param>
    public LayoutSheet(string name, IEnumerable<LayoutCell> cells, int rows, int cols)
        : this(name, cells, rows, cols, namedAreas: null, options: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the layout sheet type.
    /// </summary>
    /// <param name="name">The target name.</param>
    /// <param name="cells">The cells.</param>
    /// <param name="rows">The rows.</param>
    /// <param name="cols">The cols.</param>
    /// <param name="namedAreas">The named areas.</param>
    /// <param name="options">Options that control the operation.</param>
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

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the rows.
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// Gets the cols.
    /// </summary>
    public int Cols { get; }

    /// <summary>
    /// Gets the cells.
    /// </summary>
    public IReadOnlyList<LayoutCell> Cells { get; }

    /// <summary>
    /// Gets the named areas.
    /// </summary>
    public IReadOnlyList<LayoutNamedArea> NamedAreas { get; }

    /// <summary>
    /// Gets the options.
    /// </summary>
    public SheetOptionsAst? Options { get; }
}

/// <summary>
/// Represents layout named area.
/// </summary>
public sealed class LayoutNamedArea
{
    /// <summary>
    /// Initializes a new instance of the layout named area type.
    /// </summary>
    /// <param name="name">The target name.</param>
    /// <param name="topRow">The top row.</param>
    /// <param name="leftColumn">The left column.</param>
    /// <param name="bottomRow">The bottom row.</param>
    /// <param name="rightColumn">The right column.</param>
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
