using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.Styles;

public sealed class StylePlan
{
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

    public ResolvedStyle EffectiveStyle { get; }

    public IReadOnlyList<ResolvedStyle> AppliedStyles { get; }

    public ResolvedStyle? WorkbookDefault { get; }

    public ResolvedStyle? SheetDefault { get; }

    public IReadOnlyList<ResolvedStyle> ReferenceStyles { get; }

    public IReadOnlyList<ResolvedStyle> InlineStyles { get; }

    public string? FontName => EffectiveStyle.FontName;

    public double? FontSize => EffectiveStyle.FontSize;

    public bool? FontBold => EffectiveStyle.FontBold;

    public bool? FontItalic => EffectiveStyle.FontItalic;

    public bool? FontUnderline => EffectiveStyle.FontUnderline;

    public string? FillColor => EffectiveStyle.FillColor;

    public string? NumberFormatCode => EffectiveStyle.NumberFormatCode;

    public IReadOnlyList<BorderInfo> Borders => EffectiveStyle.Borders;

    public StyleValueTrace<string?>? FontNameTrace { get; }

    public StyleValueTrace<double?>? FontSizeTrace { get; }

    public StyleValueTrace<bool?>? FontBoldTrace { get; }

    public StyleValueTrace<bool?>? FontItalicTrace { get; }

    public StyleValueTrace<bool?>? FontUnderlineTrace { get; }

    public StyleValueTrace<string?>? FillColorTrace { get; }

    public StyleValueTrace<string?>? NumberFormatCodeTrace { get; }

    public IReadOnlyList<StyleValueTrace<BorderInfo>> BorderTraces { get; }
}

public sealed class StyleValueTrace<T>
{
    public StyleValueTrace(T value, ResolvedStyle source)
    {
        Value = value;
        SourceName = source.SourceName;
        SourceKind = source.SourceKind;
        DeclaredScope = source.DeclaredScope;
    }

    public T Value { get; }

    public string SourceName { get; }

    public StyleSourceKind SourceKind { get; }

    public StyleScope DeclaredScope { get; }
}
