using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public sealed class StylesAst : IAst<StylesAst>
    {
        public static string TagName => "styles";

        public IReadOnlyList<StyleRefAst>? StyleRefs { get; init; } = Array.Empty<StyleRefAst>();
        public IReadOnlyList<StyleAst>? Styles { get; init; }
        public IReadOnlyList<StyleImportAst>? styleImportAsts  { get; init; } 


        public SourceSpan? Span { get; init; }

        public StylesAst(XElement stylesElem, List<Issue> issues)
        {
            // <styles> 要素から StylesAst を構築する。
            var styleElems = stylesElem.Elements(stylesElem.Name.Namespace + "style");
            var styles = styleElems.Select(e => new StyleAst(e, issues)).ToList();
            
            Styles = styles;
            Span = SourceSpan.CreateSpanAttributes(stylesElem);
        }
    }
}
