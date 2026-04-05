using System.Globalization;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Represents workbook chart palette ast.
    /// </summary>
    public sealed class ChartPaletteAst : IAst<ChartPaletteAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "chartPalette";

        /// <summary>
        /// Gets the palette colors.
        /// </summary>
        public IReadOnlyList<ChartColorAst> Colors { get; init; } = Array.Empty<ChartColorAst>();

        /// <summary>
        /// Gets the source span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the chart palette ast type.
        /// </summary>
        /// <param name="element">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public ChartPaletteAst(XElement element, List<Issue> issues)
        {
            var colors = element
                .Elements(element.Name.Namespace + ChartColorAst.TagName)
                .Select(colorElement => new ChartColorAst(colorElement, issues))
                .ToArray();

            var duplicatedKeys = colors
                .Where(color => string.IsNullOrWhiteSpace(color.Key) == false)
                .GroupBy(color => color.Key, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            foreach (var duplicatedKey in duplicatedKeys)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Warning,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = $"<chartPalette> で color key が重複しています。後勝ちで扱います: {duplicatedKey}",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }

            Colors = colors;
            Span = SourceSpan.CreateSpanAttributes(element);
        }
    }

    /// <summary>
    /// Represents chart palette color ast.
    /// </summary>
    public sealed class ChartColorAst : IAst<ChartColorAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "color";

        /// <summary>
        /// Gets the color key.
        /// </summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>
        /// Gets the color value.
        /// </summary>
        public string Value { get; init; } = string.Empty;

        /// <summary>
        /// Gets the source span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the chart color ast type.
        /// </summary>
        /// <param name="element">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public ChartColorAst(XElement element, List<Issue> issues)
        {
            var key = element.Attribute("key")?.Value.Trim() ?? string.Empty;
            var value = element.Attribute("value")?.Value.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<color> 要素に key 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }

            if (!ChartAst.IsValidRgbColor(value))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = $"<color> 要素の value 属性が不正です。'#RRGGBB' を指定してください: {value}",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }

            Key = key;
            Value = value;
            Span = SourceSpan.CreateSpanAttributes(element);
        }
    }

    /// <summary>
    /// Represents chart ast.
    /// </summary>
    public sealed class ChartAst : IAst<ChartAst>
    {
        private static readonly HashSet<string> SupportedChartTypes =
        [
            "barStacked",
            "line",
        ];

        private static readonly HashSet<string> SupportedLegendPositions =
        [
            "none",
            "right",
            "left",
            "top",
            "bottom",
        ];

        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "chart";

        /// <summary>
        /// Gets chart type.
        /// </summary>
        public string ChartType { get; init; } = string.Empty;

        /// <summary>
        /// Gets chart title.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets chart logical name.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Gets top row anchor (1-based).
        /// </summary>
        public int Row { get; init; }

        /// <summary>
        /// Gets left column anchor (1-based).
        /// </summary>
        public int Column { get; init; }

        /// <summary>
        /// Gets chart width in columns.
        /// </summary>
        public int Width { get; init; } = 8;

        /// <summary>
        /// Gets chart height in rows.
        /// </summary>
        public int Height { get; init; } = 15;

        /// <summary>
        /// Gets category reference.
        /// </summary>
        public string CategoryRef { get; init; } = string.Empty;

        /// <summary>
        /// Gets legend position.
        /// </summary>
        public string? Legend { get; init; }

        /// <summary>
        /// Gets a value indicating whether data labels are shown.
        /// </summary>
        public bool ShowDataLabels { get; init; }

        /// <summary>
        /// Gets when expression.
        /// </summary>
        public string? WhenExprRaw { get; init; }

        /// <summary>
        /// Gets chart series collection.
        /// </summary>
        public IReadOnlyList<ChartSeriesAst> Series { get; init; } = Array.Empty<ChartSeriesAst>();

        /// <summary>
        /// Gets source span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the chart ast type.
        /// </summary>
        /// <param name="element">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public ChartAst(XElement element, List<Issue> issues)
        {
            var chartType = element.Attribute("type")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(chartType))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<chart> 要素に type 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }
            else if (!SupportedChartTypes.Contains(chartType))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = $"<chart> 要素の type 属性が不正です: {chartType}",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }

            var categoryRef = element.Attribute("category")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(categoryRef))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<chart> 要素に category 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }

            var row = ParseRequiredPositiveIntAttribute(element, "r", issues);
            var column = ParseRequiredPositiveIntAttribute(element, "c", issues);
            var width = ParseOptionalPositiveIntAttribute(element, "width", defaultValue: 8, issues);
            var height = ParseOptionalPositiveIntAttribute(element, "height", defaultValue: 15, issues);

            var legend = element.Attribute("legend")?.Value.Trim();
            if (!string.IsNullOrWhiteSpace(legend) && !SupportedLegendPositions.Contains(legend!))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = $"<chart> 要素の legend 属性が不正です: {legend}",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }

            var showDataLabels = ParseBooleanAttribute(element, "showDataLabels", defaultValue: false, issues);

            var series = element
                .Elements(element.Name.Namespace + ChartSeriesAst.TagName)
                .Select(seriesElement => new ChartSeriesAst(seriesElement, issues))
                .ToArray();

            if (series.Length == 0)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredElement,
                    Message = "<chart> 要素には 1 つ以上の <series> 要素が必要です。",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }

            ChartType = chartType;
            Title = element.Attribute("title")?.Value;
            Name = element.Attribute("name")?.Value;
            Row = row;
            Column = column;
            Width = width;
            Height = height;
            CategoryRef = categoryRef;
            Legend = legend;
            ShowDataLabels = showDataLabels;
            WhenExprRaw = element.Attribute("when")?.Value;
            Series = series;
            Span = SourceSpan.CreateSpanAttributes(element);
        }

        /// <summary>
        /// Checks whether the input is #RRGGBB format.
        /// </summary>
        /// <param name="value">The target string.</param>
        /// <returns>True if the color is valid.</returns>
        public static bool IsValidRgbColor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value!.Length != 7 || value[0] != '#')
            {
                return false;
            }

            return value[1..].All(IsHexChar);
        }

        private static bool IsHexChar(char c) =>
            (c >= '0' && c <= '9') ||
            (c >= 'A' && c <= 'F') ||
            (c >= 'a' && c <= 'f');

        private static int ParseRequiredPositiveIntAttribute(
            XElement element,
            string attributeName,
            List<Issue> issues)
        {
            var attribute = element.Attribute(attributeName);
            if (attribute is null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = $"<chart> 要素に {attributeName} 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
                return 1;
            }

            if (int.TryParse(attribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"<chart> 要素の {attributeName} 属性が不正です。1 以上の整数を指定してください。",
                Span = SourceSpan.CreateSpanAttributes(element),
            });

            return 1;
        }

        private static int ParseOptionalPositiveIntAttribute(
            XElement element,
            string attributeName,
            int defaultValue,
            List<Issue> issues)
        {
            var attribute = element.Attribute(attributeName);
            if (attribute is null)
            {
                return defaultValue;
            }

            if (int.TryParse(attribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"<chart> 要素の {attributeName} 属性が不正です。1 以上の整数を指定してください。",
                Span = SourceSpan.CreateSpanAttributes(element),
            });

            return defaultValue;
        }

        private static bool ParseBooleanAttribute(
            XElement element,
            string attributeName,
            bool defaultValue,
            List<Issue> issues)
        {
            var attribute = element.Attribute(attributeName);
            if (attribute is null)
            {
                return defaultValue;
            }

            var raw = attribute.Value.Trim();
            if (bool.TryParse(raw, out var parsed))
            {
                return parsed;
            }

            if (raw == "1")
            {
                return true;
            }

            if (raw == "0")
            {
                return false;
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Warning,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"<chart> 要素の {attributeName} 属性が不正です。'{attribute.Value}' は false として扱います。",
                Span = SourceSpan.CreateSpanAttributes(element),
            });

            return defaultValue;
        }
    }

    /// <summary>
    /// Represents chart series ast.
    /// </summary>
    public sealed class ChartSeriesAst : IAst<ChartSeriesAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "series";

        /// <summary>
        /// Gets series name.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Gets value reference.
        /// </summary>
        public string ValueRef { get; init; } = string.Empty;

        /// <summary>
        /// Gets fixed color.
        /// </summary>
        public string? Color { get; init; }

        /// <summary>
        /// Gets series color key.
        /// </summary>
        public string? ColorKey { get; init; }

        /// <summary>
        /// Gets per-point color key reference.
        /// </summary>
        public string? ColorByRef { get; init; }

        /// <summary>
        /// Gets source span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the chart series ast type.
        /// </summary>
        /// <param name="element">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public ChartSeriesAst(XElement element, List<Issue> issues)
        {
            var valueRef = element.Attribute("value")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(valueRef))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<series> 要素に value 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }

            var color = element.Attribute("color")?.Value.Trim();
            if (!string.IsNullOrWhiteSpace(color) && !ChartAst.IsValidRgbColor(color))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = $"<series> 要素の color 属性が不正です。'#RRGGBB' を指定してください: {color}",
                    Span = SourceSpan.CreateSpanAttributes(element),
                });
            }

            Name = element.Attribute("name")?.Value;
            ValueRef = valueRef;
            Color = color;
            ColorKey = element.Attribute("colorKey")?.Value.Trim();
            ColorByRef = element.Attribute("colorBy")?.Value.Trim();
            Span = SourceSpan.CreateSpanAttributes(element);
        }
    }
}
