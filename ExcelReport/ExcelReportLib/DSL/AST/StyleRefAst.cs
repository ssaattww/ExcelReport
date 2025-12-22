using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// コンポーネントが参照するスタイルを表すASTノード
    /// </summary>
    public sealed class StyleRefAst : IAst<StyleRefAst>
    {
        public static string TagName => "styleRef";
        public string Name { get; init; } = string.Empty;
        public SourceSpan? Span { get; init; }
        public IReadOnlyList<StyleRefAst> StyleRefs => Array.Empty<StyleRefAst>();
        public StyleRefAst(XElement styleRefElem, List<Issue> issues)
        {
            // <styleRef> 要素から StyleRefAst を構築する。
            var nameAttr = styleRefElem.Attribute("name");
            string name = nameAttr?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.StyleScopeViolation,
                    Message = "スタイル参照の名前が指定されていません。",
                    Span = SourceSpan.CreateSpanAttributes(styleRefElem),
                });
                return;
            }

            Name = name;
            Span = SourceSpan.CreateSpanAttributes(styleRefElem);
        }
    }
}
