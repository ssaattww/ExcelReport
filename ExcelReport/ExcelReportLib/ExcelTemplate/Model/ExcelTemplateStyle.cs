namespace ExcelReportLib.ExcelTemplate.Model;

/// <summary>
/// Represents extracted style metadata for a cell.
/// </summary>
public sealed class ExcelTemplateStyle
{
    /// <summary>
    /// Initializes a new instance of the style model.
    /// </summary>
    /// <param name="index">The workbook style index.</param>
    public ExcelTemplateStyle(uint index)
    {
        Index = index;
    }

    /// <summary>
    /// Gets the workbook style index.
    /// </summary>
    public uint Index { get; }
}
