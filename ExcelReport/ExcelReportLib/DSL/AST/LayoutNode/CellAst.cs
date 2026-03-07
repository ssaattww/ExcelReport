using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    /// <summary>
    /// Represents cell ast.
    /// </summary>
    public sealed class CellAst : LayoutNodeAst, IAst<CellAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "cell";
        /// <summary>
        /// Gets or sets the value raw.
        /// </summary>
        public string? ValueRaw { get; init; }
        /// <summary>
        /// Gets or sets the style ref shortcut.
        /// </summary>
        public string? StyleRefShortcut { get; init; }
        /// <summary>
        /// Gets or sets the formula ref.
        /// </summary>
        public string? FormulaRef { get; init; }

        /// <summary>
        /// Initializes a new instance of the cell ast type.
        /// </summary>
        /// <param name="elem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public CellAst(XElement elem, List<Issue> issues)
        {
            var valueAttr = elem.Attribute("value");
            var styleRefAttr = elem.Attribute("styleRef");
            var formulaRefAttr = elem.Attribute("formulaRef");

            ValueRaw = valueAttr?.Value;
            StyleRefShortcut = styleRefAttr?.Value;
            FormulaRef = formulaRefAttr?.Value;
            // Todo: ValueRaw から式のパース
        }
    }
}
