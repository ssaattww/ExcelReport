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
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "styleRef";
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// 入れ子になっているスタイル参照
        /// </summary>
        public IReadOnlyList<StyleRefAst> StyleRefs { get; init; } = Array.Empty<StyleRefAst>();

        /// <summary>
        /// スタイル参照 解析フェーズで設定される
        /// </summary>
        public StyleAst? StyleRef { get; set; }

        /// <summary>
        /// Initializes a new instance of the style ref ast type.
        /// </summary>
        /// <param name="styleRefElem">The style ref elem.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
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
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "スタイル参照の名前が指定されていません。",
                    Span = SourceSpan.CreateSpanAttributes(styleRefElem),
                });
                return;
            }

            var styleRefsElems = styleRefElem.Elements(styleRefElem.Name.Namespace + TagName);
            var styleRefs = styleRefsElems.Select(e => new StyleRefAst(e, issues)).ToList();

            Name = name;
            Span = SourceSpan.CreateSpanAttributes(styleRefElem);
            StyleRefs = styleRefs;
        }
    }
}
