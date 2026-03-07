using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.Styles;

/// <summary>
/// Represents style plan.
/// </summary>
public sealed class StylePlan
{
    /// <summary>
    /// Initializes a new instance of the style plan type.
    /// </summary>
    /// <param name="effectiveStyle">The effective style.</param>
    /// <param name="appliedStyles">The applied styles.</param>
    /// <param name="workbookDefault">The workbook default.</param>
    /// <param name="sheetDefault">The sheet default.</param>
    /// <param name="referenceStyles">The reference styles.</param>
    /// <param name="inlineStyles">The inline styles.</param>
    /// <param name="fontNameTrace">The font name trace.</param>
    /// <param name="fontSizeTrace">The font size trace.</param>
    /// <param name="fontBoldTrace">The font bold trace.</param>
    /// <param name="fontItalicTrace">The font italic trace.</param>
    /// <param name="fontUnderlineTrace">The font underline trace.</param>
    /// <param name="fillColorTrace">The fill color trace.</param>
    /// <param name="numberFormatCodeTrace">The number format code trace.</param>
    /// <param name="borderTraces">The border traces.</param>
    public StylePlan(
        ResolvedStyle effectiveStyle,
        IReadOnlyList<ResolvedStyle> appliedStyles,
        ResolvedStyle? workbookDefault,
        ResolvedStyle? sheetDefault,
        IReadOnlyList<ResolvedStyle> referenceStyles,
        IReadOnlyList<ResolvedStyle> inlineStyles,
        StyleValueTrace<string?>? fontNameTrace,
        StyleValueTrace<double?>? fontSizeTrace,
        StyleValueTrace<bool?>? fontBoldTrace,
        StyleValueTrace<bool?>? fontItalicTrace,
        StyleValueTrace<bool?>? fontUnderlineTrace,
        StyleValueTrace<string?>? fillColorTrace,
        StyleValueTrace<string?>? numberFormatCodeTrace,
        IReadOnlyList<StyleValueTrace<BorderInfo>> borderTraces)
    {
        EffectiveStyle = effectiveStyle;
        AppliedStyles = appliedStyles;
        WorkbookDefault = workbookDefault;
        SheetDefault = sheetDefault;
        ReferenceStyles = referenceStyles;
        InlineStyles = inlineStyles;
        FontNameTrace = fontNameTrace;
        FontSizeTrace = fontSizeTrace;
        FontBoldTrace = fontBoldTrace;
        FontItalicTrace = fontItalicTrace;
        FontUnderlineTrace = fontUnderlineTrace;
        FillColorTrace = fillColorTrace;
        NumberFormatCodeTrace = numberFormatCodeTrace;
        BorderTraces = borderTraces;
    }

    /// <summary>
    /// Gets the effective style.
    /// </summary>
    public ResolvedStyle EffectiveStyle { get; }

    /// <summary>
    /// Gets the applied styles.
    /// </summary>
    public IReadOnlyList<ResolvedStyle> AppliedStyles { get; }

    /// <summary>
    /// Gets the workbook default.
    /// </summary>
    public ResolvedStyle? WorkbookDefault { get; }

    /// <summary>
    /// Gets the sheet default.
    /// </summary>
    public ResolvedStyle? SheetDefault { get; }

    /// <summary>
    /// Gets the reference styles.
    /// </summary>
    public IReadOnlyList<ResolvedStyle> ReferenceStyles { get; }

    /// <summary>
    /// Gets the inline styles.
    /// </summary>
    public IReadOnlyList<ResolvedStyle> InlineStyles { get; }

    /// <summary>
    /// Gets the font name.
    /// </summary>
    public string? FontName => EffectiveStyle.FontName;

    /// <summary>
    /// Gets the font size.
    /// </summary>
    public double? FontSize => EffectiveStyle.FontSize;

    /// <summary>
    /// Gets a value indicating whether font bold.
    /// </summary>
    public bool? FontBold => EffectiveStyle.FontBold;

    /// <summary>
    /// Gets a value indicating whether font italic.
    /// </summary>
    public bool? FontItalic => EffectiveStyle.FontItalic;

    /// <summary>
    /// Gets a value indicating whether font underline.
    /// </summary>
    public bool? FontUnderline => EffectiveStyle.FontUnderline;

    /// <summary>
    /// Gets the fill color.
    /// </summary>
    public string? FillColor => EffectiveStyle.FillColor;

    /// <summary>
    /// Gets the number format code.
    /// </summary>
    public string? NumberFormatCode => EffectiveStyle.NumberFormatCode;

    /// <summary>
    /// Gets the borders.
    /// </summary>
    public IReadOnlyList<BorderInfo> Borders => EffectiveStyle.Borders;

    /// <summary>
    /// Gets the font name trace.
    /// </summary>
    public StyleValueTrace<string?>? FontNameTrace { get; }

    /// <summary>
    /// Gets the font size trace.
    /// </summary>
    public StyleValueTrace<double?>? FontSizeTrace { get; }

    /// <summary>
    /// Gets a value indicating whether font bold trace.
    /// </summary>
    public StyleValueTrace<bool?>? FontBoldTrace { get; }

    /// <summary>
    /// Gets a value indicating whether font italic trace.
    /// </summary>
    public StyleValueTrace<bool?>? FontItalicTrace { get; }

    /// <summary>
    /// Gets a value indicating whether font underline trace.
    /// </summary>
    public StyleValueTrace<bool?>? FontUnderlineTrace { get; }

    /// <summary>
    /// Gets the fill color trace.
    /// </summary>
    public StyleValueTrace<string?>? FillColorTrace { get; }

    /// <summary>
    /// Gets the number format code trace.
    /// </summary>
    public StyleValueTrace<string?>? NumberFormatCodeTrace { get; }

    /// <summary>
    /// Gets the border traces.
    /// </summary>
    public IReadOnlyList<StyleValueTrace<BorderInfo>> BorderTraces { get; }
}

/// <summary>
/// Represents style value trace.
/// </summary>
public sealed class StyleValueTrace<T>
{
    /// <summary>
    /// Initializes a new style value trace.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="source">The source.</param>
    public StyleValueTrace(T value, ResolvedStyle source)
    {
        Value = value;
        SourceName = source.SourceName;
        SourceKind = source.SourceKind;
        DeclaredScope = source.DeclaredScope;
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    public T Value { get; }

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
}
