using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    /// <summary>
    /// レイアウトノードを繰り返すことを表すASTノード
    /// </summary>
    public sealed class RepeatAst : LayoutNodeAst, INamedAreaTarget
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "repeat";
        /// <summary>
        /// Gets the target area name.
        /// </summary>
        public string? AreaName { get; init; }
        
        /// <summary>
        /// fromの生値
        /// (Attribute / Child Element)
        /// </summary>
        public string FromExprRaw { get; init; } = string.Empty;
        
        /// <summary>
        /// 繰り返す変数指定
        /// (Attribute / Child Element)
        /// </summary>
        public string VarName { get; init; } = string.Empty;
        
        /// <summary>
        /// 拡張方向
        /// (Attribute / Child Element)
        /// </summary>
        public RepeatDirection Direction { get; init; } = default!;

        /// <summary>
        /// 繰り返すLayout要素
        /// (Child Element)
        /// </summary>
        public LayoutNodeAst? Body { get; init; }
        /// <summary>
        /// Gets conditional formatting rules defined under repeat.
        /// </summary>
        public IReadOnlyList<ConditionalFormattingAst> ConditionalFormattings { get; init; } = Array.Empty<ConditionalFormattingAst>();

        /// <summary>
        /// XElementからAST構築
        /// </summary>
        /// <param name="repeatElem">repeat要素</param>
        /// <param name="issues">エラー集約</param>
        /// <returns>RepeatAST</returns>
        public RepeatAst(XElement repeatElem, List<Issue> issues)
        {
            if (repeatElem.Attribute("name") is not null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = "<repeat> 要素の name 属性は廃止されました。area 属性を使用してください。",
                    Span = SourceSpan.CreateSpanAttributes(repeatElem),
                });
            }

            var areaRaw = repeatElem.Attribute("area")?.Value;
            var areaName = string.IsNullOrWhiteSpace(areaRaw) ? null : areaRaw.Trim();
            var fromExprRaw = ResolvePreferredText(
                repeatElem,
                repeatElem.Attribute("from"),
                repeatElem.GetFirstOrDefaultChildElement("from"),
                "from",
                issues);

            var varRaw = ResolvePreferredText(
                repeatElem,
                repeatElem.Attribute("var"),
                repeatElem.GetFirstOrDefaultChildElement("var"),
                "var",
                issues);
            // var は省略可能。未指定時は既定値 "item"
            var varNameStr = string.IsNullOrWhiteSpace(varRaw) ? "item" : varRaw;

            var direction = repeatElem.Attribute("direction");
            var directionStr = direction?.Value ?? string.Empty;
            RepeatDirection repeatDirection = directionStr switch
            {
                "down" => RepeatDirection.Down,
                "right" => RepeatDirection.Right,
                _ => RepeatDirection.Err
            };
            if (repeatDirection == RepeatDirection.Err)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<repeat> 要素に正しいdirection 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(repeatElem)
                });
            }
            
            // 子要素
            var layoutElems = repeatElem.Elements().Where(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
            var conditionalFormattings = repeatElem.Elements(repeatElem.Name.Namespace + ConditionalFormattingAst.TagName)
                .Select(element => new ConditionalFormattingAst(element, issues))
                .ToArray();
            if(layoutElems is null || 1 != layoutElems.Count())
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.RepeatChildCountInvalid,
                    Message = "<repeat> 要素は一つのLayout要素(component等)が必要です",
                    Span = SourceSpan.CreateSpanAttributes(repeatElem)
                });
            }
            LayoutNodeAst? body = layoutElems?.Select(e => LayoutNodeAst.LayoutNodeAstFactory(e, issues)).FirstOrDefault() ?? null;
            
            AreaName = areaName;
            FromExprRaw = fromExprRaw;
            VarName = varNameStr;
            Direction = repeatDirection;
            Body = body;
            ConditionalFormattings = conditionalFormattings;
        }

        private static string ResolvePreferredText(
            XElement owner,
            XAttribute? attribute,
            XElement? element,
            string targetName,
            List<Issue> issues)
        {
            if (attribute is not null && element is not null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Warning,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = $"<repeat> 要素の {targetName} は属性と子要素の両方に指定されています。属性値を優先します。",
                    Span = SourceSpan.CreateSpanAttributes(owner)
                });
            }

            if (attribute is not null)
            {
                return attribute.Value;
            }

            return element?.Value.Trim() ?? string.Empty;
        }
    }
    /// <summary>
    /// Specifies repeat direction values.
    /// </summary>
    public enum RepeatDirection
    {
        /// <summary>
        /// Represents the down option.
        /// </summary>
        Down,
        /// <summary>
        /// Represents the right option.
        /// </summary>
        Right,
        /// <summary>
        /// Represents err.
        /// </summary>
        Err
    }
}

