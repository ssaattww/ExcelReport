using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    public abstract class LayoutNodeAst : IAst<LayoutNodeAst>
    {
        /// <summary>
        /// attributes
        /// </summary>
        public Placement Placement { get; private set; }

        /// <summary>
        /// 定義位置
        /// </summary>
        public SourceSpan? Span { get; private set; }

        public IReadOnlyList<StyleRefAst> StyleRefs { get; private set; } = new List<StyleRefAst>();

        private static readonly IReadOnlyDictionary<string, Func<XElement, List<Issue>, LayoutNodeAst>>LayoutNodeFactories =
        new Dictionary<string, Func<XElement, List<Issue>, LayoutNodeAst>>(StringComparer.Ordinal)
        {
            [GridAst.TagName] = (elem, issues) => new GridAst(elem, issues),
            [UseAst.TagName] = (elem, issues) => new UseAst(elem, issues),
            [RepeatAst.TagName] = (elem, issues) => new RepeatAst(elem, issues),
            [CellAst.TagName] = (elem, issues) => new CellAst(elem, issues),
        };
        public static readonly ISet<string> AllowedLayoutNodeNames = new HashSet<string>(LayoutNodeFactories.Keys, StringComparer.Ordinal);
        public LayoutNodeAst(XElement elem, List<Issue> issues)
        {
            if (!LayoutNodeFactories.TryGetValue(elem.Name.LocalName, out var factory))
            {
                ThrowUnknownLayoutNode(elem, issues);
                return;
            }

            var layoutNodeAst = factory(elem, issues);

            var styleElems = elem.Elements(elem.Name.Namespace + "styleRef");
            var styles = styleElems.Select(e => new StyleRefAst(e, issues)).ToList();

            Placement = Placement.ParsePlacementAttributes(elem, issues);
            Span = SourceSpan.CreateSpanAttributes(elem);
            StyleRefs = styles;
        }

        private static LayoutNodeAst ThrowUnknownLayoutNode(
            XElement elem,
            IList<Issue> issues)
        {
            issues.Add(new Issue
            {
                Message = $"Unknown layout node: {elem.Name.LocalName}",
                Span = SourceSpan.CreateSpanAttributes(elem),
            });

            throw new InvalidOperationException(
                $"Unknown layout node: {elem.Name.LocalName}");
        }
    }
}
