using ExcelReportLib.DSL.AST.LayoutNode;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Represents component ast.
    /// </summary>
    public sealed class ComponentAst : ICellAst
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "component";
        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; init; } = string.Empty;  // @name
        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        public LayoutNodeAst Body { get; init; } = default!; // <grid>|<repeat>を想定<cell>や<use>が来た場合componentの定義として意味ないけれど破綻はしないので許容する
        /// <summary>
        /// Gets or sets the style refs.
        /// </summary>
        public IReadOnlyList<StyleRefAst> StyleRefs { get; init; }
        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        public IReadOnlyList<StyleAst> Style { get; init; }

        /// <summary>
        /// Gets or sets the placement.
        /// </summary>
        public Placement Placement { get; init; } = default!;

        /// <summary>
        /// Initializes a new instance of the component ast type.
        /// </summary>
        /// <param name="componentElem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public ComponentAst(XElement componentElem, List<Issue> issues)
        {
            // <component> 要素から ComponentAst を構築する。
            var nameAttr = componentElem.Attribute("name");
            if (nameAttr == null)
            {
                issues.Add(new Issue
                {
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Severity = IssueSeverity.Error,
                    Message = "Component 要素に name 属性がありません。",
                });
                return;
            }
            // ボディの解析
            var bodyElem = componentElem.Elements().FirstOrDefault(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
            if (bodyElem == null)
            {
                issues.Add(new Issue
                { 
                    Kind = IssueKind.UndefinedRequiredElement, 
                    Severity = IssueSeverity.Warning,
                    Message = "Component 要素に有効なレイアウトノードが含まれていません。",
                });
                return;
            }
            var bodyAst = LayoutNodeAst.LayoutNodeAstFactory(bodyElem, issues);

            var ns = componentElem.Name.Namespace;
            var stylesElem = componentElem.Elements(ns + StyleAst.TagName);
            var styles = stylesElem.Select(e => new StyleAst(e, issues)).ToList();

            var styleRefsElem = componentElem.Elements(ns + StyleRefAst.TagName);
            var styleRefs = styleRefsElem.Select(e => new StyleRefAst(e, issues)).ToList();

            StyleRefs = styleRefs;
            Style = styles;
            Name = nameAttr.Value;
            Body = bodyAst;
            Span = SourceSpan.CreateSpanAttributes(componentElem);
            Placement = Placement.ParsePlacementAttributes(componentElem, issues);
        }
    }
}
