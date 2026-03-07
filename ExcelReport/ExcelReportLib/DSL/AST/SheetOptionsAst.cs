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
}
