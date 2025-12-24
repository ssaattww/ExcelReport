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

        public CellAst(XElement elem, List<Issue> issues)
        {
            var valueAttr = elem.Attribute("value");
            ValueRaw = valueAttr?.Value;
            // Todo: ValueRaw から式のパース
        }
    }
}
