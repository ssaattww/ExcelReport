using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public sealed class WorkbookAst
    {
        public StylesAst? Styles { get; init; }         // <styles>（任意）
        public IReadOnlyList<ComponentAst> Components { get; init; } = Array.Empty<ComponentAst>(); // <component>*
        public IReadOnlyList<SheetAst> Sheets { get; init; } = Array.Empty<SheetAst>(); // <sheet>+
        public SourceSpan? Span { get; init; }


        public WorkbookAst(XElement workbookElem, List<Issue> issues)
        {
            // ルート <workbook> 要素から各子要素を AST に変換する。
            var stylesElem = workbookElem.Element(workbookElem.Name.Namespace + "styles");
            StylesAst? stylesAst = stylesElem != null ? new StylesAst(stylesElem, issues) : null;

            var componentElems = workbookElem.Elements(workbookElem.Name.Namespace + "component");
            var components = componentElems.Select(e => new ComponentAst(e, issues)).ToList();

            var sheetElems = workbookElem.Elements(workbookElem.Name.Namespace + "sheet");
            var sheets = sheetElems.Select(e => new SheetAst(e, issues)).ToList();

            Styles = stylesAst;
            Components = components;
            Sheets = sheets;
            Span = SourceSpan.CreateSpanAttributes(workbookElem);
        }
    }
}
