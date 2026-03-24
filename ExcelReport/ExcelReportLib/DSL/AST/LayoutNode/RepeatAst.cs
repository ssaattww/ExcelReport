using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    /// <summary>
    /// レイアウトノードを繰り返すことを表すASTノード
    /// </summary>
    public sealed class RepeatAst : LayoutNodeAst
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "repeat";
        /// <summary>
        /// ノードの定義名
        /// (Attribute / Child Element)
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
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
        /// XElementからAST構築
        /// </summary>
        /// <param name="repeatElem">repeat要素</param>
        /// <param name="issues">エラー集約</param>
        /// <returns>RepeatAST</returns>
        public RepeatAst(XElement repeatElem, List<Issue> issues)
        {
            var nameStr = repeatElem.Attribute("name")?.Value ?? string.Empty;
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
            
            Name = nameStr;
            FromExprRaw = fromExprRaw;
            VarName = varNameStr;
            Direction = repeatDirection;
            Body = body;
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

