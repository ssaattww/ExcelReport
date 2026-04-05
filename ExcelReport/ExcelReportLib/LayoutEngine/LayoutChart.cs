namespace ExcelReportLib.LayoutEngine;

/// <summary>
/// Represents chart layout information resolved from DSL.
/// </summary>
public sealed class LayoutChart
{
    /// <summary>
    /// Initializes a new instance of the layout chart type.
    /// </summary>
    /// <param name="chartType">The chart type.</param>
    /// <param name="title">The title.</param>
    /// <param name="name">The chart name.</param>
    /// <param name="topRow">The top row (1-based).</param>
    /// <param name="leftColumn">The left column (1-based).</param>
    /// <param name="widthColumns">The chart width in columns.</param>
    /// <param name="heightRows">The chart height in rows.</param>
    /// <param name="categoryReference">The raw category reference.</param>
    /// <param name="legendPosition">The legend position.</param>
    /// <param name="showDataLabels">Whether data labels are visible.</param>
    /// <param name="series">The chart series.</param>
    public LayoutChart(
        string chartType,
        string? title,
        string? name,
        int topRow,
        int leftColumn,
        int widthColumns,
        int heightRows,
        string categoryReference,
        string? legendPosition,
        bool showDataLabels,
        IReadOnlyList<LayoutChartSeries>? series = null)
    {
        ChartType = chartType ?? string.Empty;
        Title = title;
        Name = name;
        TopRow = topRow;
        LeftColumn = leftColumn;
        WidthColumns = widthColumns;
        HeightRows = heightRows;
        CategoryReference = categoryReference ?? string.Empty;
        LegendPosition = legendPosition;
        ShowDataLabels = showDataLabels;
        Series = series ?? Array.Empty<LayoutChartSeries>();
    }

    /// <summary>
    /// Gets the chart type.
    /// </summary>
    public string ChartType { get; }

    /// <summary>
    /// Gets the title.
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
    /// Gets the chart width in columns.
    /// </summary>
    public int WidthColumns { get; }

    /// <summary>
    /// Gets the chart height in rows.
    /// </summary>
    public int HeightRows { get; }

    /// <summary>
    /// Gets the raw category reference.
    /// </summary>
    public string CategoryReference { get; }

    /// <summary>
    /// Gets the legend position.
    /// </summary>
    public string? LegendPosition { get; }

    /// <summary>
    /// Gets a value indicating whether data labels are visible.
    /// </summary>
    public bool ShowDataLabels { get; }

    /// <summary>
    /// Gets the chart series collection.
    /// </summary>
    public IReadOnlyList<LayoutChartSeries> Series { get; }
}

/// <summary>
/// Represents chart series layout information resolved from DSL.
/// </summary>
public sealed class LayoutChartSeries
{
    /// <summary>
    /// Initializes a new instance of the layout chart series type.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="valueReference">The raw value reference.</param>
    /// <param name="color">The fixed color.</param>
    /// <param name="colorKey">The color key.</param>
    /// <param name="colorByReference">The per-point color key reference.</param>
    public LayoutChartSeries(
        string? name,
        string valueReference,
        string? color,
        string? colorKey,
        string? colorByReference)
    {
        Name = name;
        ValueReference = valueReference ?? string.Empty;
        Color = color;
        ColorKey = colorKey;
        ColorByReference = colorByReference;
    }

    /// <summary>
    /// Gets the series name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the raw value reference.
    /// </summary>
    public string ValueReference { get; }

    /// <summary>
    /// Gets the fixed color.
    /// </summary>
    public string? Color { get; }

    /// <summary>
    /// Gets the series color key.
    /// </summary>
    public string? ColorKey { get; }

    /// <summary>
    /// Gets the per-point color key reference.
    /// </summary>
    public string? ColorByReference { get; }
}
