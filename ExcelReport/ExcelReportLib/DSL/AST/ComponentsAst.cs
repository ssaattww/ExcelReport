using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public class ComponentsAst : IAst<ComponentsAst>
    {
        public static string TagName => "components";

        public SourceSpan? Span { get; init; }
        public IReadOnlyList<ComponentAst> ComponentList { get; init; }

        public ComponentsAst(XElement componentsElem, List<Issue> issues)
        {
            Span = SourceSpan.CreateSpanAttributes(componentsElem);
            ComponentList = componentsElem.Elements(componentsElem.Name.Namespace + ComponentAst.TagName)
                .Select(e => new ComponentAst(e, issues)).ToList();
        }
    }
}
