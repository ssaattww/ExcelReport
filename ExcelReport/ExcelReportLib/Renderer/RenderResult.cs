namespace ExcelReportLib.Renderer;

public sealed class RenderResult
{
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

    public MemoryStream Output { get; }

    public int SheetCount { get; }

    public int CellCount { get; }

    public int IssueCount { get; }
}
