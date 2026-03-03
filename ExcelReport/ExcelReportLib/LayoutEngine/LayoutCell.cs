using ExcelReportLib.Styles;

namespace ExcelReportLib.LayoutEngine;

public sealed class LayoutCell
{
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

    public int Row { get; }

    public int Col { get; }

    public int RowSpan { get; }

    public int ColSpan { get; }

    public object? Value { get; }

    public string? Formula { get; }

    public string? FormulaRef { get; }

    public StylePlan StylePlan { get; }

    public bool Merge => RowSpan > 1 || ColSpan > 1;
}
