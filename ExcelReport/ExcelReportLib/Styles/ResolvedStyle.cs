using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.Styles;

/// <summary>
/// Represents resolved style.
/// </summary>
public sealed class ResolvedStyle
{
    private static readonly IReadOnlyList<BorderInfo> EmptyBorders = Array.Empty<BorderInfo>();

    /// <summary>
    /// Initializes a new instance of the resolved style type.
    /// </summary>
    /// <param name="sourceName">The source name.</param>
    /// <param name="sourceKind">The source kind.</param>
    /// <param name="declaredScope">The declared scope.</param>
    /// <param name="fontName">The font name.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="fontBold">The font bold.</param>
    /// <param name="fontItalic">The font italic.</param>
    /// <param name="fontUnderline">The font underline.</param>
    /// <param name="fillColor">The fill color.</param>
    /// <param name="numberFormatCode">The number format code.</param>
    /// <param name="borders">The borders.</param>
    public ResolvedStyle(
        string sourceName,
        StyleSourceKind sourceKind,
        StyleScope declaredScope,
        string? fontName = null,
        double? fontSize = null,
        bool? fontBold = null,
        bool? fontItalic = null,
        bool? fontUnderline = null,
        string? fillColor = null,
        string? numberFormatCode = null,
        IReadOnlyList<BorderInfo>? borders = null)
    {
        SourceName = sourceName;
        SourceKind = sourceKind;
        DeclaredScope = declaredScope;
        FontName = fontName;
        FontSize = fontSize;
        FontBold = fontBold;
        FontItalic = fontItalic;
        FontUnderline = fontUnderline;
        FillColor = fillColor;
        NumberFormatCode = numberFormatCode;
        Borders = borders is null
            ? EmptyBorders
            : borders.Select(CloneBorder).ToArray();
    }

    /// <summary>
    /// Gets the source name.
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// Gets the source kind.
    /// </summary>
    public StyleSourceKind SourceKind { get; }

    /// <summary>
    /// Gets the declared scope.
    /// </summary>
    public StyleScope DeclaredScope { get; }

    /// <summary>
    /// Gets the font name.
    /// </summary>
    public string? FontName { get; }

    /// <summary>
    /// Gets the font size.
    /// </summary>
    public double? FontSize { get; }

    /// <summary>
    /// Gets a value indicating whether font bold.
    /// </summary>
    public bool? FontBold { get; }

    /// <summary>
    /// Gets a value indicating whether font italic.
    /// </summary>
    public bool? FontItalic { get; }

    /// <summary>
    /// Gets a value indicating whether font underline.
    /// </summary>
    public bool? FontUnderline { get; }

    /// <summary>
    /// Gets the fill color.
    /// </summary>
    public string? FillColor { get; }

    /// <summary>
    /// Gets the number format code.
    /// </summary>
    public string? NumberFormatCode { get; }

    /// <summary>
    /// Gets the borders.
    /// </summary>
    public IReadOnlyList<BorderInfo> Borders { get; }

    /// <summary>
    /// Gets a value indicating whether content.
    /// </summary>
    public bool HasContent =>
        FontName is not null ||
        FontSize is not null ||
        FontBold is not null ||
        FontItalic is not null ||
        FontUnderline is not null ||
        FillColor is not null ||
        NumberFormatCode is not null ||
        Borders.Count > 0;

    /// <summary>
    /// Creates a resolved style from a style AST definition.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="sourceName">The source name.</param>
    /// <param name="sourceKind">The source kind.</param>
    /// <param name="borders">The borders.</param>
    /// <returns>The resulting resolved style.</returns>
    public static ResolvedStyle FromStyle(
        StyleAst style,
        string sourceName,
        StyleSourceKind sourceKind,
        IReadOnlyList<BorderInfo>? borders = null) =>
        new(
            sourceName,
            sourceKind,
            style.Scope,
            GetString(style, "font.name"),
            GetDouble(style, "font.size"),
            GetBool(style, "font.bold"),
            GetBool(style, "font.italic"),
            GetBool(style, "font.underline"),
            GetString(style, "fill.color"),
            GetString(style, "numberFormat.code"),
            borders ?? style.Borders);

    private static string? GetString(StyleAst style, string key) =>
        style.RawProperties.TryGetValue(key, out var value)
            ? value as string
            : null;

    private static double? GetDouble(StyleAst style, string key) =>
        style.RawProperties.TryGetValue(key, out var value) && value is double number
            ? number
            : null;

    private static bool? GetBool(StyleAst style, string key) =>
        style.RawProperties.TryGetValue(key, out var value) && value is bool flag
            ? flag
            : null;

    private static BorderInfo CloneBorder(BorderInfo border) =>
        new()
        {
            Mode = border.Mode,
            Top = border.Top,
            Bottom = border.Bottom,
            Left = border.Left,
            Right = border.Right,
            Color = border.Color,
        };
}
