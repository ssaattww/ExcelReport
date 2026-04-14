namespace ExcelReportLib.ExcelTemplate.Model;

/// <summary>
/// Represents an extracted Excel template cell.
/// </summary>
public sealed class ExcelTemplateCell
{
    /// <summary>
    /// Initializes a new instance of the cell model.
    /// </summary>
    /// <param name="reference">The A1 reference.</param>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="column">The 1-based column index.</param>
    /// <param name="value">The cell value.</param>
    /// <param name="formula">The cell formula text without leading equals.</param>
    /// <param name="style">The cell style metadata.</param>
    public ExcelTemplateCell(
        string reference,
        int row,
        int column,
        string? value,
        string? formula,
        ExcelTemplateStyle? style)
    {
        Reference = reference;
        Row = row;
        Column = column;
        Value = value;
        Formula = formula;
        Style = style;
    }

    /// <summary>
    /// Gets the A1 reference.
    /// </summary>
    public string Reference { get; }

    /// <summary>
    /// Gets the 1-based row index.
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// Gets the 1-based column index.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the cell value.
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// Gets the formula text.
    /// </summary>
    public string? Formula { get; }

    /// <summary>
    /// Gets the extracted style metadata.
    /// </summary>
    public ExcelTemplateStyle? Style { get; }
}
