using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    public sealed class GridAst : LayoutNodeAst, ICellAst
    {
        public static string TagName => "grid";

        public IReadOnlyDictionary<Placement, LayoutNodeAst> Children { get; init; } = default!;

        public GridAst(XElement elem, List<Issue> issues)
        {
            var layoutElems = elem.Elements().Where(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
            var children = layoutElems.Select(e => LayoutNodeAst.LayoutNodeAstFactory(e, issues)).Select(e => new { Child = e, Place = e.Placement }).ToList();

            Children = children.ToDictionary(e => e.Place, e => e.Child);
        }
    }
}
