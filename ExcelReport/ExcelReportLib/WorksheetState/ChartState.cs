namespace ExcelReportLib.WorksheetState;

/// <summary>
/// Represents final chart state for renderer.
/// </summary>
public sealed class ChartState
{
    /// <summary>
    /// Initializes a new instance of the chart state type.
    /// </summary>
    /// <param name="chartType">The chart type.</param>
    /// <param name="title">The chart title.</param>
    /// <param name="name">The chart name.</param>
    /// <param name="topRow">The top row (1-based).</param>
    /// <param name="leftColumn">The left column (1-based).</param>
    /// <param name="widthColumns">The chart width in columns.</param>
    /// <param name="heightRows">The chart height in rows.</param>
    /// <param name="categoryFormula">The category formula reference.</param>
    /// <param name="legendPosition">The legend position.</param>
    /// <param name="showDataLabels">Whether data labels are visible.</param>
    /// <param name="series">The series states.</param>
    public ChartState(
        string chartType,
        string? title,
        string? name,
        int topRow,
        int leftColumn,
        int widthColumns,
        int heightRows,
        string categoryFormula,
        string? legendPosition,
        bool showDataLabels,
        IReadOnlyList<ChartSeriesState> series)
    {
        ChartType = chartType ?? string.Empty;
        Title = title;
        Name = name;
        TopRow = topRow;
        LeftColumn = leftColumn;
        WidthColumns = widthColumns;
        HeightRows = heightRows;
        CategoryFormula = categoryFormula ?? string.Empty;
        LegendPosition = legendPosition;
        ShowDataLabels = showDataLabels;
        Series = series ?? Array.Empty<ChartSeriesState>();
    }

    /// <summary>
    /// Gets the chart type.
    /// </summary>
    public string ChartType { get; }

    /// <summary>
    /// Gets the chart title.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Gets the chart name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the top row (1-based).
    /// </summary>
    public int TopRow { get; }

    /// <summary>
    /// Gets the left column (1-based).
    /// </summary>
    public int LeftColumn { get; }

    /// <summary>
    /// Gets the width in columns.
    /// </summary>
    public int WidthColumns { get; }

    /// <summary>
    /// Gets the height in rows.
    /// </summary>
    public int HeightRows { get; }

    /// <summary>
    /// Gets the category formula reference.
    /// </summary>
    public string CategoryFormula { get; }

    /// <summary>
    /// Gets the legend position.
    /// </summary>
    public string? LegendPosition { get; }

    /// <summary>
    /// Gets a value indicating whether data labels are visible.
    /// </summary>
    public bool ShowDataLabels { get; }

    /// <summary>
    /// Gets the series states.
    /// </summary>
    public IReadOnlyList<ChartSeriesState> Series { get; }
}

/// <summary>
/// Represents final chart series state for renderer.
/// </summary>
public sealed class ChartSeriesState
{
    /// <summary>
    /// Initializes a new instance of the chart series state type.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="valueFormula">The series value formula reference.</param>
    /// <param name="pointColors">The resolved per-point colors.</param>
    public ChartSeriesState(
        string? name,
        string valueFormula,
        IReadOnlyList<string>? pointColors)
    {
        Name = name;
        ValueFormula = valueFormula ?? string.Empty;
        PointColors = pointColors?.ToArray();
    }

    /// <summary>
    /// Gets the series name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the value formula reference.
    /// </summary>
    public string ValueFormula { get; }

    /// <summary>
    /// Gets the resolved per-point colors.
    /// </summary>
    public IReadOnlyList<string>? PointColors { get; }
}
