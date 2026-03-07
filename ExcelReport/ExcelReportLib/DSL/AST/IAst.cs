using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Defines the common members shared by all AST nodes.
    /// </summary>
    internal interface IAst<TSelf> where TSelf : IAst<TSelf>
    {
        /// <summary>
        /// Gets the XML tag name represented by the AST node type.
        /// </summary>
        public static string TagName { get; }

        /// <summary>
        /// Gets the source span where this node originated, when available.
        /// </summary>
        public SourceSpan? Span { get; }
    }
}
