using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.Styles;

public sealed class ResolvedStyle
{
    private static readonly IReadOnlyList<BorderInfo> EmptyBorders = Array.Empty<BorderInfo>();

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

    public string SourceName { get; }

    public StyleSourceKind SourceKind { get; }

    public StyleScope DeclaredScope { get; }

    public string? FontName { get; }

    public double? FontSize { get; }

    public bool? FontBold { get; }

    public bool? FontItalic { get; }

    public bool? FontUnderline { get; }

    public string? FillColor { get; }

    public string? NumberFormatCode { get; }

    public IReadOnlyList<BorderInfo> Borders { get; }

    public bool HasContent =>
        FontName is not null ||
        FontSize is not null ||
        FontBold is not null ||
        FontItalic is not null ||
        FontUnderline is not null ||
        FillColor is not null ||
        NumberFormatCode is not null ||
        Borders.Count > 0;

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
