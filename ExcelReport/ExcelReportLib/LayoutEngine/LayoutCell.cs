using ExcelReportLib.Styles;

namespace ExcelReportLib.LayoutEngine;

/// <summary>
/// Represents layout cell.
/// </summary>
public sealed class LayoutCell
{
    /// <summary>
    /// Initializes a new instance of the layout cell type.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="col">The col.</param>
    /// <param name="rowSpan">The row span.</param>
    /// <param name="colSpan">The col span.</param>
    /// <param name="value">The value.</param>
    /// <param name="formula">The formula.</param>
    /// <param name="formulaRef">The formula ref.</param>
    /// <param name="stylePlan">The style plan.</param>
    public LayoutCell(
        int row,
        int col,
        int rowSpan,
        int colSpan,
        object? value,
        string? formula,
        string? formulaRef,
        StylePlan stylePlan)
    {
        Row = row;
        Col = col;
        RowSpan = rowSpan <= 0 ? 1 : rowSpan;
        ColSpan = colSpan <= 0 ? 1 : colSpan;
        Value = value;
        Formula = formula;
        FormulaRef = formulaRef;
        StylePlan = stylePlan;
    }

    /// <summary>
    /// Gets the row.
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// Gets the col.
    /// </summary>
    public int Col { get; }

    /// <summary>
    /// Gets the row span.
    /// </summary>
    public int RowSpan { get; }

    /// <summary>
    /// Gets the col span.
    /// </summary>
    public int ColSpan { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the formula.
    /// </summary>
    public string? Formula { get; }

    /// <summary>
    /// Gets the formula ref.
    /// </summary>
    public string? FormulaRef { get; }

    /// <summary>
    /// Gets the style plan.
    /// </summary>
    public StylePlan StylePlan { get; }

    /// <summary>
    /// Gets a value indicating whether merge.
    /// </summary>
    public bool Merge => RowSpan > 1 || ColSpan > 1;
}
