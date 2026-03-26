using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Sheetのオプション設定を表すASTノード
    /// </summary>
    public sealed class SheetOptionsAst : IAst<SheetOptionsAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "sheetOptions";
        /// <summary>
        /// Gets or sets the freeze.
        /// </summary>
        public FreezeAst? Freeze { get; init; }
        /// <summary>
        /// Gets or sets the group rows.
        /// </summary>
        public IReadOnlyList<GroupRowsAst> GroupRows { get; init; } = Array.Empty<GroupRowsAst>();
        /// <summary>
        /// Gets or sets the group cols.
        /// </summary>
        public IReadOnlyList<GroupColsAst> GroupCols { get; init; } = Array.Empty<GroupColsAst>();
        /// <summary>
        /// Gets or sets the auto filter.
        /// </summary>
        public AutoFilterAst? AutoFilter { get; init; }

        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the sheet options ast type.
        /// </summary>
        /// <param name="elem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public SheetOptionsAst(XElement elem, List<Issue> issues)
        {
            var ns = elem.Name.Namespace;
            var freezeElem = elem.Element(ns + "freeze");
            if (freezeElem is not null)
            {
                Freeze = new FreezeAst(freezeElem, issues);
            }
            var groupsElem = elem.Element(ns + "groups");
            if(groupsElem is not null)
            {
                GroupRows = groupsElem.Elements(ns + "groupRows")
                    .Select(e => new GroupRowsAst(e, issues))
                    .ToList();
                GroupCols = groupsElem.Elements(ns + "groupCols")
                    .Select(e => new GroupColsAst(e, issues))
                    .ToList();
            }
            else
            {
                GroupRows = Array.Empty<GroupRowsAst>();
                GroupCols = Array.Empty<GroupColsAst>();
            }

            var autoFilterElem = elem.Element(ns + "autoFilter");
            if (autoFilterElem is not null)
            {
                AutoFilter = new AutoFilterAst(autoFilterElem, issues);
            }

            foreach (var deprecated in elem.Elements(ns + ConditionalFormattingAst.TagName))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredElement,
                    Message = "<conditionalFormatting> は <sheetOptions> では定義できません。<sheet> 直下へ移動してください。",
                    Span = SourceSpan.CreateSpanAttributes(deprecated),
                });
            }

            Span = SourceSpan.CreateSpanAttributes(elem);
        }
    }

    /// <summary>
    /// セルの表示固定設定を表すASTノード
    /// </summary>
    public sealed class FreezeAst : IAst<FreezeAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "freeze";
        /// <summary>
        /// Gets or sets the at.
        /// </summary>
        public string At { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the freeze ast type.
        /// </summary>
        /// <param name="elem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public FreezeAst(XElement elem, List<Issue> issues)
        {
            Span = SourceSpan.CreateSpanAttributes(elem);

            var atAttr = elem.Attribute("at");
            if (atAttr is null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<freeze> 要素に at 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(elem),
                });
                return;

            }
            else
            {
                At = atAttr.Value;
            }
        }
    }

    /// <summary>
    /// セルの行グループ化設定を表すASTノード
    /// </summary>
    public sealed class GroupRowsAst : IAst<GroupRowsAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "groupRows";
        /// <summary>
        /// Gets or sets the at.
        /// </summary>
        public string At { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether collapsed.
        /// </summary>
        public bool Collapsed { get; init; }
        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        internal GroupRowsAst(XElement elem, List<Issue> issues)
        {
            Span = SourceSpan.CreateSpanAttributes(elem);
            var atAttr = elem.Attribute("at");
            if (atAttr is null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<groupRows> 要素に at 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(elem),
                });
            }

            At = atAttr?.Value ?? string.Empty;

            var collapsedAttr = elem.Attribute("collapsed");
            if(collapsedAttr is null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Warning,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<groupRows> 要素に collapsed 属性がありません。デフォルトで false として扱います。",
                    Span = SourceSpan.CreateSpanAttributes(elem),
                });
                Collapsed = false;
            }
            else
            {
                if(bool.TryParse(collapsedAttr.Value, out var result) == false)
                {
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Warning,
                        Kind = IssueKind.InvalidAttributeValue,
                        Message = "<groupRows> 要素の collapsed 属性の値が不正です。デフォルトで false として扱います。",
                        Span = SourceSpan.CreateSpanAttributes(elem),
                    });
                    Collapsed = false;
                }
                else
                {
                    Collapsed = result;
                }
            }
        }
    }

    /// <summary>
    /// セルの列グループ化設定を表すASTノード
    /// </summary>
    public sealed class GroupColsAst : IAst<GroupColsAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "groupCols";
        /// <summary>
        /// Gets or sets the at.
        /// </summary>
        public string At { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether collapsed.
        /// </summary>
        public bool Collapsed { get; init; }
        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the group cols ast type.
        /// </summary>
        /// <param name="elem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public GroupColsAst(XElement elem, List<Issue> issues)
        {
            Span = SourceSpan.CreateSpanAttributes(elem);

            var atAttr = elem.Attribute("at");
            if(atAttr is null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<groupCols> 要素に at 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(elem),
                });
                return;
            }
            else
            {
                At = atAttr.Value;
            }
                

            var collapsedAttr = elem.Attribute("collapsed");
            if (collapsedAttr is null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Warning,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<groupCols> 要素に collapsed 属性がありません。デフォルトで false として扱います。",
                    Span = SourceSpan.CreateSpanAttributes(elem),
                });
                Collapsed = false;
            }
            else
            {
                if (bool.TryParse(collapsedAttr.Value, out var result) == false)
                {
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Warning,
                        Kind = IssueKind.InvalidAttributeValue,
                        Message = "<groupCols> 要素の collapsed 属性の値が不正です。デフォルトで false として扱います。",
                        Span = SourceSpan.CreateSpanAttributes(elem),
                    });
                    Collapsed = false;
                }
                else
                {
                    Collapsed = result;
                }
            }
        }
    }

    /// <summary>
    /// AutoFilter設定を表すASTノード
    /// </summary>
    public sealed class AutoFilterAst : IAst<AutoFilterAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "autoFilter";
        /// <summary>
        /// Gets or sets the at.
        /// </summary>
        public string At { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        internal AutoFilterAst(XElement elem, List<Issue> issues)
        {
            Span = SourceSpan.CreateSpanAttributes(elem);

            var atAttr = elem.Attribute("at");
            if (atAttr is null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<autoFilter> 要素に at 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(elem),
                });
                return;
            }
            else
            {
                At = atAttr.Value;
            }
        }
    }

    /// <summary>
    /// ConditionalFormatting設定を表すASTノード
    /// </summary>
    public sealed class ConditionalFormattingAst : IAst<ConditionalFormattingAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "conditionalFormatting";
        /// <summary>
        /// Gets or sets the at.
        /// </summary>
        public string At { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the min color.
        /// </summary>
        public string MinColor { get; init; } = "#F8696B";
        /// <summary>
        /// Gets or sets the max color.
        /// </summary>
        public string MaxColor { get; init; } = "#63BE7B";
        /// <summary>
        /// Gets or sets the mid color for 3-color scale.
        /// </summary>
        public string? MidColor { get; init; }
        /// <summary>
        /// Gets or sets the conditional formula.
        /// </summary>
        public string? Formula { get; init; }
        /// <summary>
        /// Gets or sets the formula reference target used by expression rules.
        /// </summary>
        public string? FormulaRef { get; init; }
        /// <summary>
        /// Gets or sets the fill color used when the formula evaluates true.
        /// </summary>
        public string FillColor { get; init; } = "#FFF2CC";
        /// <summary>
        /// Gets or sets the font name for expression-based formatting.
        /// </summary>
        public string? FontName { get; init; }
        /// <summary>
        /// Gets or sets the font size for expression-based formatting.
        /// </summary>
        public double? FontSize { get; init; }
        /// <summary>
        /// Gets or sets a value indicating whether bold font.
        /// </summary>
        public bool? FontBold { get; init; }
        /// <summary>
        /// Gets or sets a value indicating whether italic font.
        /// </summary>
        public bool? FontItalic { get; init; }
        /// <summary>
        /// Gets or sets a value indicating whether underline font.
        /// </summary>
        public bool? FontUnderline { get; init; }
        /// <summary>
        /// Gets or sets the number format code for expression-based formatting.
        /// </summary>
        public string? NumberFormatCode { get; init; }
        /// <summary>
        /// Gets or sets the border top style.
        /// </summary>
        public string? BorderTop { get; init; }
        /// <summary>
        /// Gets or sets the border bottom style.
        /// </summary>
        public string? BorderBottom { get; init; }
        /// <summary>
        /// Gets or sets the border left style.
        /// </summary>
        public string? BorderLeft { get; init; }
        /// <summary>
        /// Gets or sets the border right style.
        /// </summary>
        public string? BorderRight { get; init; }
        /// <summary>
        /// Gets or sets the border color.
        /// </summary>
        public string? BorderColor { get; init; }
        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        internal ConditionalFormattingAst(XElement elem, List<Issue> issues)
        {
            Span = SourceSpan.CreateSpanAttributes(elem);

            var atAttr = elem.Attribute("at");
            if (atAttr is null || string.IsNullOrWhiteSpace(atAttr.Value))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<conditionalFormatting> 要素に at 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(elem),
                });
            }
            else
            {
                At = atAttr.Value;
            }

            MinColor = NormalizeColor(elem.Attribute("minColor")?.Value, "#F8696B", issues, elem, "minColor");
            MaxColor = NormalizeColor(elem.Attribute("maxColor")?.Value, "#63BE7B", issues, elem, "maxColor");
            MidColor = NormalizeOptionalColor(elem.Attribute("midColor")?.Value, issues, elem, "midColor");
            Formula = NormalizeOptionalFormula(elem.Attribute("formula")?.Value);
            FormulaRef = NormalizeOptionalText(elem.Attribute("formulaRef")?.Value);
            FillColor = NormalizeColor(elem.Attribute("fillColor")?.Value, "#FFF2CC", issues, elem, "fillColor");
            FontName = NormalizeOptionalText(elem.Attribute("fontName")?.Value);
            FontSize = NormalizeOptionalDouble(elem.Attribute("fontSize")?.Value, issues, elem, "fontSize");
            FontBold = NormalizeOptionalBool(elem.Attribute("fontBold")?.Value, issues, elem, "fontBold");
            FontItalic = NormalizeOptionalBool(elem.Attribute("fontItalic")?.Value, issues, elem, "fontItalic");
            FontUnderline = NormalizeOptionalBool(elem.Attribute("fontUnderline")?.Value, issues, elem, "fontUnderline");
            NumberFormatCode = NormalizeOptionalText(elem.Attribute("numberFormatCode")?.Value);
            BorderTop = NormalizeOptionalText(elem.Attribute("borderTop")?.Value);
            BorderBottom = NormalizeOptionalText(elem.Attribute("borderBottom")?.Value);
            BorderLeft = NormalizeOptionalText(elem.Attribute("borderLeft")?.Value);
            BorderRight = NormalizeOptionalText(elem.Attribute("borderRight")?.Value);
            BorderColor = NormalizeOptionalColor(elem.Attribute("borderColor")?.Value, issues, elem, "borderColor");
        }

        private static string NormalizeColor(string? raw, string fallback, List<Issue> issues, XElement elem, string attributeName)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            var normalized = raw.Trim();
            if (normalized.StartsWith("#", StringComparison.Ordinal))
            {
                normalized = normalized[1..];
            }

            if (normalized.Length == 6 && normalized.All(Uri.IsHexDigit))
            {
                return $"#{normalized.ToUpperInvariant()}";
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Warning,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"<conditionalFormatting> 要素の {attributeName} 属性値が不正です。既定値を使用します。",
                Span = SourceSpan.CreateSpanAttributes(elem),
            });
            return fallback;
        }

        private static string? NormalizeOptionalColor(string? raw, List<Issue> issues, XElement elem, string attributeName)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var normalized = raw.Trim();
            if (normalized.StartsWith("#", StringComparison.Ordinal))
            {
                normalized = normalized[1..];
            }

            if (normalized.Length == 6 && normalized.All(Uri.IsHexDigit))
            {
                return $"#{normalized.ToUpperInvariant()}";
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Warning,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"<conditionalFormatting> 要素の {attributeName} 属性値が不正です。指定を無視します。",
                Span = SourceSpan.CreateSpanAttributes(elem),
            });
            return null;
        }

        private static string? NormalizeOptionalFormula(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return raw.Trim();
        }

        private static string? NormalizeOptionalText(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? null
                : raw.Trim();

        private static bool? NormalizeOptionalBool(string? raw, List<Issue> issues, XElement elem, string attributeName)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var normalized = raw.Trim();
            if (bool.TryParse(normalized, out var value))
            {
                return value;
            }

            if (string.Equals(normalized, "1", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(normalized, "0", StringComparison.Ordinal))
            {
                return false;
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Warning,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"<conditionalFormatting> 要素の {attributeName} 属性値が不正です。指定を無視します。",
                Span = SourceSpan.CreateSpanAttributes(elem),
            });
            return null;
        }

        private static double? NormalizeOptionalDouble(string? raw, List<Issue> issues, XElement elem, string attributeName)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (double.TryParse(raw.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Warning,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"<conditionalFormatting> 要素の {attributeName} 属性値が不正です。指定を無視します。",
                Span = SourceSpan.CreateSpanAttributes(elem),
            });
            return null;
        }
    }
}
