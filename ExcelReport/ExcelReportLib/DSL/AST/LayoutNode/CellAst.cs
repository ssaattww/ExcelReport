using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    public sealed class CellAst : LayoutNodeAst, IAst<CellAst>
    {
        public static string TagName => "cell";
        public string? ValueRaw { get; init; }
        public string? StyleRefShortcut { get; init; }
        public string? FormulaRef { get; init; }

        //public new static CellAst BuildAst(XElement elem, List<Issue> issues)
        //{
        //    // レイアウトノードの解析
        //    var layoutElems = elem.Elements().Where(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
        //    var children = layoutElems.Select(e => LayoutNodeAst.BuildAst(e, issues)).Select(e => new { Child = e, Place = e.Placement }).ToList();

        //    var styleElems = elem.Elements(elem.Name.Namespace + "styleRef");
        //    var styles = styleElems.Select(e => StyleRefAst.BuildAst(e, issues)).ToList();

        //    return new CellAst
        //    {
        //        Span = SourceSpan.CreateSpanAttributes(elem),
        //        Placement = Placement.ParsePlacementAttributes(elem, issues),
        //        StyleRefs = styles,
        //    };
        //}
        public CellAst(XElement elem, List<Issue> issues) : base(elem, issues)
        {
            var valueAttr = elem.Attribute("value");
            ValueRaw = valueAttr?.Value;
            // Todo: ValueRaw から式のパース
        }
    }
}
