using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public sealed class StylesAst : IAst<StylesAst>
    {
        public static string TagName => "styles";

        public IReadOnlyList<StyleRefAst>? StyleRefs { get; init; } = Array.Empty<StyleRefAst>();
        public IReadOnlyList<StyleAst>? Styles { get; init; }
        public IReadOnlyList<StyleImportAst>? StyleImportAsts  { get; init; } 


        public SourceSpan? Span { get; init; }

        public StylesAst(XElement stylesElem, List<Issue> issues, string dslDir="")
        {
            // <styles> 要素から StylesAst を構築する。
            var styleElems = stylesElem.Elements(stylesElem.Name.Namespace + StyleAst.TagName);
            var styles = styleElems.Select(e => new StyleAst(e, issues)).ToList();

            var styleImportsElems = styleElems.Elements(stylesElem.Name.Namespace + StyleImportAst.TagName);
            var styleImports = styleImportsElems.Select(e => new StyleImportAst(e, issues, dslDir)).ToList();

            Styles = styles;
            StyleImportAsts = styleImports;
            Span = SourceSpan.CreateSpanAttributes(stylesElem);
        }
    }
}
