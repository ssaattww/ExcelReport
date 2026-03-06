using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Represents styles ast.
    /// </summary>
    public sealed class StylesAst : IAst<StylesAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "styles";
        /// <summary>
        /// Gets or sets the styles.
        /// </summary>
        public IReadOnlyList<StyleAst>? Styles { get; init; }
        /// <summary>
        /// Gets or sets the style import asts.
        /// </summary>
        public IReadOnlyList<StyleImportAst>? StyleImportAsts  { get; init; } 


        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the styles ast type.
        /// </summary>
        /// <param name="stylesElem">The styles elem.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        /// <param name="dslDir">The base directory used to resolve relative imports.</param>
        public StylesAst(XElement stylesElem, List<Issue> issues, string dslDir="")
        {
            // <styles> 要素から StylesAst を構築する。
            var styleElems = stylesElem.Elements(stylesElem.Name.Namespace + StyleAst.TagName);
            var styles = styleElems.Select(e => new StyleAst(e, issues)).ToList();

            var styleImportsElems = stylesElem.Elements(stylesElem.Name.Namespace + StyleImportAst.TagName);
            var styleImports = styleImportsElems.Select(e => new StyleImportAst(e, issues, dslDir)).ToList();

            Styles = styles;
            StyleImportAsts = styleImports;
            Span = SourceSpan.CreateSpanAttributes(stylesElem);
        }
    }
}
