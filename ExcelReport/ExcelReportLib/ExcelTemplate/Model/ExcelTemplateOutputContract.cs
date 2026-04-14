using ExcelReportLib.DSL;

namespace ExcelReportLib.ExcelTemplate.Model;

/// <summary>
/// Represents the normalized conversion output contract used by ExcelTemplate emitters.
/// </summary>
public sealed class ExcelTemplateOutputContract
{
    /// <summary>
    /// Initializes a new instance of the output contract.
    /// </summary>
    /// <param name="components">The normalized component scopes.</param>
    /// <param name="sheets">The normalized sheet scopes.</param>
    /// <param name="issues">The aggregated conversion issues.</param>
    public ExcelTemplateOutputContract(
        IReadOnlyList<ExcelTemplateOutputComponent>? components = null,
        IReadOnlyList<ExcelTemplateOutputSheet>? sheets = null,
        IReadOnlyList<Issue>? issues = null)
    {
        Components = components?.ToArray() ?? [];
        Sheets = sheets?.ToArray() ?? [];
        Issues = issues?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the normalized component scopes.
    /// </summary>
    public IReadOnlyList<ExcelTemplateOutputComponent> Components { get; }

    /// <summary>
    /// Gets the normalized sheet scopes.
    /// </summary>
    public IReadOnlyList<ExcelTemplateOutputSheet> Sheets { get; }

    /// <summary>
    /// Gets the aggregated conversion issues.
    /// </summary>
    public IReadOnlyList<Issue> Issues { get; }
}

/// <summary>
/// Represents a normalized component scope for conversion output.
/// </summary>
public sealed class ExcelTemplateOutputComponent
{
    /// <summary>
    /// Initializes a new instance of the component scope.
    /// </summary>
    /// <param name="name">The logical component name.</param>
    /// <param name="sourceSheetName">The source component sheet name.</param>
    /// <param name="rangeReference">The resolved source range when available.</param>
    /// <param name="isRangeResolved">Whether the component range was successfully resolved.</param>
    /// <param name="items">The normalized output items.</param>
    public ExcelTemplateOutputComponent(
        string name,
        string sourceSheetName,
        string? rangeReference,
        bool isRangeResolved,
        IReadOnlyList<ExcelTemplateOutputItem>? items = null)
    {
        Name = name;
        SourceSheetName = sourceSheetName;
        RangeReference = rangeReference;
        IsRangeResolved = isRangeResolved;
        Items = items?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the logical component name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the source component sheet name.
    /// </summary>
    public string SourceSheetName { get; }

    /// <summary>
    /// Gets the resolved source range when available.
    /// </summary>
    public string? RangeReference { get; }

    /// <summary>
    /// Gets a value indicating whether the component range was successfully resolved.
    /// </summary>
    public bool IsRangeResolved { get; }

    /// <summary>
    /// Gets the normalized output items.
    /// </summary>
    public IReadOnlyList<ExcelTemplateOutputItem> Items { get; }
}

/// <summary>
/// Represents a normalized sheet scope for conversion output.
/// </summary>
public sealed class ExcelTemplateOutputSheet
{
    /// <summary>
    /// Initializes a new instance of the sheet scope.
    /// </summary>
    /// <param name="name">The sheet name.</param>
    /// <param name="items">The normalized output items.</param>
    public ExcelTemplateOutputSheet(
        string name,
        IReadOnlyList<ExcelTemplateOutputItem>? items = null)
    {
        Name = name;
        Items = items?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the sheet name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the normalized output items.
    /// </summary>
    public IReadOnlyList<ExcelTemplateOutputItem> Items { get; }
}

/// <summary>
/// Represents a normalized output item within a component or sheet scope.
/// </summary>
public abstract class ExcelTemplateOutputItem
{
    /// <summary>
    /// Initializes a new instance of the output item.
    /// </summary>
    /// <param name="reference">The A1 reference.</param>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="column">The 1-based column index.</param>
    /// <param name="styleIndex">The workbook style index when present.</param>
    protected ExcelTemplateOutputItem(string reference, int row, int column, uint? styleIndex)
    {
        Reference = reference;
        Row = row;
        Column = column;
        StyleIndex = styleIndex;
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
    /// Gets the workbook style index when present.
    /// </summary>
    public uint? StyleIndex { get; }
}

/// <summary>
/// Represents a normalized value or formula cell.
/// </summary>
public sealed class ExcelTemplateOutputCell : ExcelTemplateOutputItem
{
    /// <summary>
    /// Initializes a new instance of the cell output item.
    /// </summary>
    /// <param name="reference">The A1 reference.</param>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="column">The 1-based column index.</param>
    /// <param name="styleIndex">The workbook style index when present.</param>
    /// <param name="value">The raw cell value.</param>
    /// <param name="formula">The raw cell formula.</param>
    public ExcelTemplateOutputCell(
        string reference,
        int row,
        int column,
        uint? styleIndex,
        string? value,
        string? formula)
        : base(reference, row, column, styleIndex)
    {
        Value = value;
        Formula = formula;
    }

    /// <summary>
    /// Gets the raw cell value.
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// Gets the raw cell formula.
    /// </summary>
    public string? Formula { get; }
}

/// <summary>
/// Represents a normalized use trigger.
/// </summary>
public sealed class ExcelTemplateOutputUse : ExcelTemplateOutputItem
{
    /// <summary>
    /// Initializes a new instance of the use output item.
    /// </summary>
    /// <param name="reference">The A1 reference.</param>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="column">The 1-based column index.</param>
    /// <param name="styleIndex">The workbook style index when present.</param>
    /// <param name="componentName">The referenced component name.</param>
    /// <param name="styleOverflow">The normalized style overflow mode when explicitly known.</param>
    public ExcelTemplateOutputUse(
        string reference,
        int row,
        int column,
        uint? styleIndex,
        string componentName,
        string? styleOverflow)
        : base(reference, row, column, styleIndex)
    {
        ComponentName = componentName;
        StyleOverflow = styleOverflow;
    }

    /// <summary>
    /// Gets the referenced component name.
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// Gets the normalized style overflow mode when explicitly known.
    /// </summary>
    public string? StyleOverflow { get; }
}

/// <summary>
/// Represents a normalized repeat-use trigger.
/// </summary>
public sealed class ExcelTemplateOutputRepeatUse : ExcelTemplateOutputItem
{
    /// <summary>
    /// Initializes a new instance of the repeat-use output item.
    /// </summary>
    /// <param name="reference">The A1 reference.</param>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="column">The 1-based column index.</param>
    /// <param name="styleIndex">The workbook style index when present.</param>
    /// <param name="componentName">The referenced component name.</param>
    /// <param name="fromExpression">The repeat source expression.</param>
    /// <param name="variableName">The repeat variable name.</param>
    /// <param name="direction">The normalized repeat direction.</param>
    /// <param name="styleOverflow">The normalized style overflow mode when explicitly known.</param>
    public ExcelTemplateOutputRepeatUse(
        string reference,
        int row,
        int column,
        uint? styleIndex,
        string componentName,
        string fromExpression,
        string variableName,
        string direction,
        string? styleOverflow)
        : base(reference, row, column, styleIndex)
    {
        ComponentName = componentName;
        FromExpression = fromExpression;
        VariableName = variableName;
        Direction = direction;
        StyleOverflow = styleOverflow;
    }

    /// <summary>
    /// Gets the referenced component name.
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// Gets the repeat source expression.
    /// </summary>
    public string FromExpression { get; }

    /// <summary>
    /// Gets the repeat variable name.
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// Gets the normalized repeat direction.
    /// </summary>
    public string Direction { get; }

    /// <summary>
    /// Gets the normalized style overflow mode when explicitly known.
    /// </summary>
    public string? StyleOverflow { get; }
}
