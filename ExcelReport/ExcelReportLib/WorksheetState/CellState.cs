using ExcelReportLib.Styles;

namespace ExcelReportLib.WorksheetState;

/// <summary>
/// Represents cell state.
/// </summary>
public sealed class CellState
{
    /// <summary>
    /// Initializes a new instance of the cell state type.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="value">The value.</param>
    /// <param name="formula">The formula.</param>
    /// <param name="formulaReference">The formula reference.</param>
    /// <param name="style">The style.</param>
    /// <param name="isMergedHead">The is merged head.</param>
    public CellState(
        int row,
        int column,
        object? value,
        string? formula,
        string? formulaReference,
        ResolvedStyle style,
        bool isMergedHead)
    {
        Row = row;
        Column = column;
        Value = value;
        Formula = formula;
        FormulaReference = formulaReference;
        Style = style ?? throw new ArgumentNullException(nameof(style));
        IsMergedHead = isMergedHead;
    }

    /// <summary>
    /// Gets the row.
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// Gets the column.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the formula.
    /// </summary>
    public string? Formula { get; }

    /// <summary>
    /// Gets the formula reference.
    /// </summary>
    public string? FormulaReference { get; }

    /// <summary>
    /// Gets the style.
    /// </summary>
    public ResolvedStyle Style { get; }

    /// <summary>
    /// Gets a value indicating whether merged head.
    /// </summary>
    public bool IsMergedHead { get; }

    /// <summary>
    /// Gets a value indicating whether formula.
    /// </summary>
    public bool IsFormula => string.IsNullOrWhiteSpace(Formula) == false;
}
