using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    /// <summary>
    /// レイアウトノードを繰り返すことを表すASTノード
    /// </summary>
    public sealed class RepeatAst : LayoutNodeAst, IAst<RepeatAst>
    {
        public static string TagName => "repeat";
        /// <summary>
        /// ノードの定義名
        /// (Attribute)
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// fromの生値
        /// (Attribute)
        /// </summary>
        public string FromExprRaw { get; init; } = string.Empty;
        
        /// <summary>
        /// 繰り返す変数指定
        /// (Attribute)
        /// </summary>
        public string VarName { get; init; } = string.Empty;
        
        /// <summary>
        /// 拡張方向
        /// (Attribute)
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
        public RepeatAst(XElement repeatElem, List<Issue> issues): base(repeatElem, issues)
        {
            var nameStr = repeatElem.Attribute("name")?.Value ?? string.Empty;

            var varName = repeatElem.Attribute("var");
            if(varName is null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<repeat> 要素に var 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(repeatElem)
                });
            }
            var varNameStr = varName?.Value ?? string.Empty;

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
            LayoutNodeAst? body = layoutElems?.Select(e => new LayoutNodeAst(e, issues)).FirstOrDefault() ?? null;
            
            Name = nameStr;
            VarName = varNameStr;
            Direction = repeatDirection;
            Body = body;
        }
    }
    public enum RepeatDirection
    {
        Down,
        Right,
        Err
    }
}
