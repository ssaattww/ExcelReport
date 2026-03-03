using ExcelReportLib.Styles;

namespace ExcelReportLib.WorksheetState;

public sealed class CellState
{
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

    public int Row { get; }

    public int Column { get; }

    public object? Value { get; }

    public string? Formula { get; }

    public string? FormulaReference { get; }

    public ResolvedStyle Style { get; }

    public bool IsMergedHead { get; }

    public bool IsFormula => string.IsNullOrWhiteSpace(Formula) == false;
}
