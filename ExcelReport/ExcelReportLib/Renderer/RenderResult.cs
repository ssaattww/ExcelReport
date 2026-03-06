namespace ExcelReportLib.Renderer;

/// <summary>
/// Represents render result.
/// </summary>
public sealed class RenderResult
{
    /// <summary>
    /// Initializes a new instance of the render result type.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <param name="sheetCount">The sheet count.</param>
    /// <param name="cellCount">The cell count.</param>
    /// <param name="issueCount">The issue count.</param>
    public RenderResult(
        MemoryStream output,
        int sheetCount,
        int cellCount,
        int issueCount)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        SheetCount = sheetCount;
        CellCount = cellCount;
        IssueCount = issueCount;
    }

    /// <summary>
    /// Gets the output.
    /// </summary>
    public MemoryStream Output { get; }

    /// <summary>
    /// Gets the sheet count.
    /// </summary>
    public int SheetCount { get; }

    /// <summary>
    /// Gets the cell count.
    /// </summary>
    public int CellCount { get; }

    /// <summary>
    /// Gets the issue count.
    /// </summary>
    public int IssueCount { get; }
}
