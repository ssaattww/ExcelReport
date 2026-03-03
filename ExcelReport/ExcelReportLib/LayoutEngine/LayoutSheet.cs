namespace ExcelReportLib.LayoutEngine;

public sealed class LayoutSheet
{
    public LayoutSheet(string name, IEnumerable<LayoutCell> cells, int rows, int cols)
    {
        Name = name;
        Rows = rows;
        Cols = cols;
        Cells = (cells ?? Array.Empty<LayoutCell>())
            .OrderBy(cell => cell.Row)
            .ThenBy(cell => cell.Col)
            .ToArray();
    }

    public string Name { get; }

    public int Rows { get; }

    public int Cols { get; }

    public IReadOnlyList<LayoutCell> Cells { get; }
}
