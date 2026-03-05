using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.Styles;

public sealed class StyleResolver : IStyleResolver
{
    private static readonly IReadOnlyDictionary<string, StyleAst> EmptyGlobalStyles =
        new Dictionary<string, StyleAst>(StringComparer.Ordinal);

    public StyleResolver(StylesAst? styles)
        : this(BuildGlobalStyles(styles))
    {
    }

    public StyleResolver(IReadOnlyDictionary<string, StyleAst>? globalStyles)
    {
        GlobalStyles = globalStyles is null
            ? EmptyGlobalStyles
            : new Dictionary<string, StyleAst>(globalStyles, StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, StyleAst> GlobalStyles { get; }

    public static IReadOnlyDictionary<string, StyleAst> BuildGlobalStyles(StylesAst? styles)
    {
        var globalStyles = new Dictionary<string, StyleAst>(StringComparer.Ordinal);
        IndexStyles(styles, globalStyles);
        return globalStyles;
    }

    public StyleAst? ResolveByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return GlobalStyles.TryGetValue(name, out var style)
            ? style
            : null;
    }

    public ResolvedStyle? Resolve(
        string styleName,
        StyleTarget target,
        IList<Issue> issues,
        SourceSpan? span = null)
    {
        if (!GlobalStyles.TryGetValue(styleName, out var style))
        {
            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.UndefinedStyle,
                Message = $"未定義の style 参照: {styleName}",
                Span = span,
            });
            return null;
        }

        return ResolveStyleCore(style, styleName, StyleSourceKind.Reference, target, issues, span);
    }

    public StylePlan BuildPlan(
        IEnumerable<StyleRefAst>? styleRefs,
        IEnumerable<StyleAst>? inlineStyles,
        StyleAst? sheetDefault,
        StyleAst? workbookDefault,
        StyleTarget target,
        IList<Issue> issues)
    {
        var referenceStyles = ResolveStyleRefs(styleRefs, target, issues);
        var resolvedInlineStyles = ResolveInlineStyles(inlineStyles, target, issues);
        var resolvedSheetDefault = ResolveLocalStyle(sheetDefault, "sheet-default", StyleSourceKind.SheetDefault, target, issues);
        var resolvedWorkbookDefault = ResolveLocalStyle(workbookDefault, "workbook-default", StyleSourceKind.WorkbookDefault, target, issues);

        var appliedStyles = new List<ResolvedStyle>();
        if (resolvedWorkbookDefault is not null)
        {
            appliedStyles.Add(resolvedWorkbookDefault);
        }

        if (resolvedSheetDefault is not null)
        {
            appliedStyles.Add(resolvedSheetDefault);
        }

        appliedStyles.AddRange(referenceStyles);
        appliedStyles.AddRange(resolvedInlineStyles);

        var fontName = default(string);
        var fontSize = default(double?);
        var fontBold = default(bool?);
        var fontItalic = default(bool?);
        var fontUnderline = default(bool?);
        var fillColor = default(string);
        var numberFormatCode = default(string);
        StyleValueTrace<string?>? fontNameTrace = null;
        StyleValueTrace<double?>? fontSizeTrace = null;
        StyleValueTrace<bool?>? fontBoldTrace = null;
        StyleValueTrace<bool?>? fontItalicTrace = null;
        StyleValueTrace<bool?>? fontUnderlineTrace = null;
        StyleValueTrace<string?>? fillColorTrace = null;
        StyleValueTrace<string?>? numberFormatCodeTrace = null;
        var borders = new List<BorderInfo>();
        var borderTraces = new List<StyleValueTrace<BorderInfo>>();

        foreach (var style in appliedStyles)
        {
            if (style.FontName is not null)
            {
                fontName = style.FontName;
                fontNameTrace = new StyleValueTrace<string?>(style.FontName, style);
            }

            if (style.FontSize is not null)
            {
                fontSize = style.FontSize;
                fontSizeTrace = new StyleValueTrace<double?>(style.FontSize, style);
            }

            if (style.FontBold is not null)
            {
                fontBold = style.FontBold;
                fontBoldTrace = new StyleValueTrace<bool?>(style.FontBold, style);
            }

            if (style.FontItalic is not null)
            {
                fontItalic = style.FontItalic;
                fontItalicTrace = new StyleValueTrace<bool?>(style.FontItalic, style);
            }

            if (style.FontUnderline is not null)
            {
                fontUnderline = style.FontUnderline;
                fontUnderlineTrace = new StyleValueTrace<bool?>(style.FontUnderline, style);
            }

            if (style.FillColor is not null)
            {
                fillColor = style.FillColor;
                fillColorTrace = new StyleValueTrace<string?>(style.FillColor, style);
            }

            if (style.NumberFormatCode is not null)
            {
                numberFormatCode = style.NumberFormatCode;
                numberFormatCodeTrace = new StyleValueTrace<string?>(style.NumberFormatCode, style);
            }

            foreach (var border in style.Borders)
            {
                var clonedBorder = CloneBorder(border);
                borders.Add(clonedBorder);
                borderTraces.Add(new StyleValueTrace<BorderInfo>(clonedBorder, style));
            }
        }

        var effectiveStyle = new ResolvedStyle(
            "effective",
            StyleSourceKind.Computed,
            StyleScope.Both,
            fontName,
            fontSize,
            fontBold,
            fontItalic,
            fontUnderline,
            fillColor,
            numberFormatCode,
            borders);

        return new StylePlan(
            effectiveStyle,
            appliedStyles,
            resolvedWorkbookDefault,
            resolvedSheetDefault,
            referenceStyles,
            resolvedInlineStyles,
            fontNameTrace,
            fontSizeTrace,
            fontBoldTrace,
            fontItalicTrace,
            fontUnderlineTrace,
            fillColorTrace,
            numberFormatCodeTrace,
            borderTraces);
    }

    private static void IndexStyles(StylesAst? styles, IDictionary<string, StyleAst> styleIndex)
    {
        if (styles?.Styles is not null)
        {
            foreach (var style in styles.Styles)
            {
                if (!string.IsNullOrWhiteSpace(style.Name))
                {
                    styleIndex[style.Name] = style;
                }
            }
        }

        if (styles?.StyleImportAsts is null)
        {
            return;
        }

        foreach (var styleImport in styles.StyleImportAsts)
        {
            if (styleImport?.StylesAst is not null)
            {
                IndexStyles(styleImport.StylesAst, styleIndex);
            }
        }
    }

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

    private static bool IsScopeViolation(StyleScope scope, StyleTarget target) =>
        (target == StyleTarget.Cell && scope == StyleScope.Grid) ||
        (target == StyleTarget.Grid && scope == StyleScope.Cell);

    private static bool IsCellIncompatibleBorderMode(string? mode) =>
        string.Equals(mode, "outer", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase);

    private static string DescribeTarget(StyleTarget target) =>
        target == StyleTarget.Cell ? "cell" : "grid";

    private static string GetSourceName(StyleAst style, string fallbackName) =>
        string.IsNullOrWhiteSpace(style.Name) ? fallbackName : style.Name;

    private List<ResolvedStyle> ResolveStyleRefs(
        IEnumerable<StyleRefAst>? styleRefs,
        StyleTarget target,
        IList<Issue> issues)
    {
        var resolved = new List<ResolvedStyle>();
        foreach (var styleRef in EnumerateStyleRefs(styleRefs))
        {
            var style = Resolve(styleRef.Name, target, issues, styleRef.Span);
            if (style is not null)
            {
                resolved.Add(style);
            }
        }

        return resolved;
    }

    private List<ResolvedStyle> ResolveInlineStyles(
        IEnumerable<StyleAst>? inlineStyles,
        StyleTarget target,
        IList<Issue> issues)
    {
        var resolved = new List<ResolvedStyle>();
        if (inlineStyles is null)
        {
            return resolved;
        }

        var index = 0;
        foreach (var inlineStyle in inlineStyles)
        {
            index++;
            var style = ResolveLocalStyle(
                inlineStyle,
                $"inline-{index}",
                StyleSourceKind.Inline,
                target,
                issues);

            if (style is not null)
            {
                resolved.Add(style);
            }
        }

        return resolved;
    }

    private ResolvedStyle? ResolveLocalStyle(
        StyleAst? style,
        string fallbackName,
        StyleSourceKind sourceKind,
        StyleTarget target,
        IList<Issue> issues)
    {
        if (style is null)
        {
            return null;
        }

        return ResolveStyleCore(style, GetSourceName(style, fallbackName), sourceKind, target, issues, style.Span);
    }

    private static IEnumerable<StyleRefAst> EnumerateStyleRefs(IEnumerable<StyleRefAst>? roots)
    {
        if (roots is null)
        {
            yield break;
        }

        var stack = new Stack<StyleRefAst>(roots.Reverse());
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            for (var i = current.StyleRefs.Count - 1; i >= 0; i--)
            {
                stack.Push(current.StyleRefs[i]);
            }
        }
    }

    private static ResolvedStyle ResolveStyleCore(
        StyleAst style,
        string sourceName,
        StyleSourceKind sourceKind,
        StyleTarget target,
        IList<Issue> issues,
        SourceSpan? span)
    {
        var borders = style.Borders
            .Select(CloneBorder)
            .ToList();
        var isGridStyleResolvedForCell = target == StyleTarget.Cell && style.Scope == StyleScope.Grid;

        if (IsScopeViolation(style.Scope, target))
        {
            if (!isGridStyleResolvedForCell)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Warning,
                    Kind = IssueKind.StyleScopeViolation,
                    Message =
                        $"style '{sourceName}' は {DescribeTarget(target)} コンテキストと scope が一致しません。border は無視され、その他のプロパティは維持されます。",
                    Span = span,
                });
            }

            borders.Clear();
        }

        if (target == StyleTarget.Cell)
        {
            var filteredBorders = borders
                .Where(border => !IsCellIncompatibleBorderMode(border.Mode))
                .ToArray();

            if (filteredBorders.Length != borders.Count && !isGridStyleResolvedForCell)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Warning,
                    Kind = IssueKind.StyleScopeViolation,
                    Message =
                        $"style '{sourceName}' の outer/all border は cell コンテキストでは無視されます。",
                    Span = span,
                });
            }

            borders = filteredBorders.ToList();
        }

        return ResolvedStyle.FromStyle(style, sourceName, sourceKind, borders);
    }
}
