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
        public static string TagName => "sheetOptions";
        public FreezeAst? Freeze { get; init; }
        public IReadOnlyList<GroupRowsAst> GroupRows { get; init; } = Array.Empty<GroupRowsAst>();
        public IReadOnlyList<GroupColsAst> GroupCols { get; init; } = Array.Empty<GroupColsAst>();
        public AutoFilterAst? AutoFilter { get; init; }

        public SourceSpan? Span { get; init; }

        public SheetOptionsAst(XElement elem, List<Issue> issues)
        {
            var ns = elem.Name.Namespace;
            var freezeElem = elem.Element(ns + "freeze");
            if (freezeElem is not null)
            {
                Freeze = new FreezeAst(freezeElem, issues);
            }
            var groupsElems = elem.Elements(ns + "groups");
            if(groupsElems is not null)
            {
                var groupRowsElems = groupsElems.Elements(ns + "groupRows");
                if (groupRowsElems is not null)
                {
                    GroupRows = groupRowsElems.Select(e => new GroupRowsAst(e, issues)).ToList();
                }
            }
            else
            {
                GroupRows = Array.Empty<GroupRowsAst>();
            }

            var groupColsElems = elem.Elements(ns + "groupCols");
            if (groupColsElems is not null)
            {
                GroupCols = groupColsElems.Select(e => new GroupColsAst(e, issues)).ToList();
            }
            else
            {
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
        public static string TagName => "freeze";
        public string At { get; init; } = string.Empty;
        public SourceSpan? Span { get; init; }

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
        public static string TagName => "groupRows";
        public string At { get; init; } = string.Empty;
        public bool Collapsed { get; init; }
        public SourceSpan? Span { get; init; }

        internal GroupRowsAst(XElement elem, List<Issue> issues)
        {
            Span = SourceSpan.CreateSpanAttributes(elem);
            At = elem.Attribute("at")?.Value ?? string.Empty;

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
        public static string TagName => "groupCols";
        public string At { get; init; } = string.Empty;
        public bool Collapsed { get; init; }
        public SourceSpan? Span { get; init; }

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
        public static string TagName => "autoFilter";
        public string At { get; init; } = string.Empty;
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
