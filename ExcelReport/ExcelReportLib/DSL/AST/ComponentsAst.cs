using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Represents components ast.
    /// </summary>
    public class ComponentsAst : IAst<ComponentsAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "components";

        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }
        /// <summary>
        /// Gets or sets the component list.
        /// </summary>
        public IReadOnlyList<ComponentAst> ComponentList { get; init; }

        /// <summary>
        /// Initializes a new instance of the components ast type.
        /// </summary>
        /// <param name="componentsElem">The components elem.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public ComponentsAst(XElement componentsElem, List<Issue> issues)
        {
            Span = SourceSpan.CreateSpanAttributes(componentsElem);
            ComponentList = componentsElem.Elements(componentsElem.Name.Namespace + ComponentAst.TagName)
                .Select(e => new ComponentAst(e, issues)).ToList();
        }
    }
}
