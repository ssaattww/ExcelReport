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
        : this(name, cells, rows, cols, namedAreas: null, options: null, scopedConditionalFormattings: null)
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
    /// <param name="conditionalFormattings">Conditional formatting rules defined on the sheet scope.</param>
    public LayoutSheet(
        string name,
        IEnumerable<LayoutCell> cells,
        int rows,
        int cols,
        IEnumerable<LayoutNamedArea>? namedAreas = null,
        SheetOptionsAst? options = null,
        IEnumerable<ConditionalFormattingAst>? conditionalFormattings = null)
        : this(
            name,
            cells,
            rows,
            cols,
            namedAreas,
            options,
            conditionalFormattings?.Select(rule => new LayoutConditionalFormatting(rule, "/sheet")))
    {
    }

    private LayoutSheet(
        string name,
        IEnumerable<LayoutCell> cells,
        int rows,
        int cols,
        IEnumerable<LayoutNamedArea>? namedAreas,
        SheetOptionsAst? options,
        IEnumerable<LayoutConditionalFormatting>? scopedConditionalFormattings)
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
        ConditionalFormattings = (scopedConditionalFormattings ?? Array.Empty<LayoutConditionalFormatting>()).ToArray();
    }

    /// <summary>
    /// Creates a layout sheet with explicit scoped conditional formatting definitions.
    /// </summary>
    /// <param name="name">The target name.</param>
    /// <param name="cells">The cells.</param>
    /// <param name="rows">The rows.</param>
    /// <param name="cols">The cols.</param>
    /// <param name="namedAreas">The named areas.</param>
    /// <param name="options">Options that control the operation.</param>
    /// <param name="scopedConditionalFormattings">Scoped conditional formatting rules.</param>
    /// <returns>The created sheet.</returns>
    internal static LayoutSheet CreateWithScopedConditionalFormattings(
        string name,
        IEnumerable<LayoutCell> cells,
        int rows,
        int cols,
        IEnumerable<LayoutNamedArea>? namedAreas,
        SheetOptionsAst? options,
        IEnumerable<LayoutConditionalFormatting>? scopedConditionalFormattings) =>
        new(
            name,
            cells,
            rows,
            cols,
            namedAreas,
            options,
            scopedConditionalFormattings);

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

    /// <summary>
    /// Gets conditional formatting rules defined on the sheet.
    /// </summary>
    public IReadOnlyList<LayoutConditionalFormatting> ConditionalFormattings { get; }
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

/// <summary>
/// Represents a scoped conditional formatting definition.
/// </summary>
public sealed class LayoutConditionalFormatting
{
    /// <summary>
    /// Initializes a new instance of the scoped conditional formatting type.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <param name="scopePath">The definition scope path.</param>
    public LayoutConditionalFormatting(ConditionalFormattingAst rule, string scopePath)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        ScopePath = scopePath ?? throw new ArgumentNullException(nameof(scopePath));
    }

    /// <summary>
    /// Gets the rule.
    /// </summary>
    public ConditionalFormattingAst Rule { get; }

    /// <summary>
    /// Gets the definition scope path.
    /// </summary>
    public string ScopePath { get; }
}
