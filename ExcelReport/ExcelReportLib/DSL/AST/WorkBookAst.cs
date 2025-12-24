using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public sealed class WorkbookAst
    {
        public StylesAst? Styles { get; init; }         // <styles>（任意）
        
        public IReadOnlyList<ComponentAst>? Components { get; init; } // <component>*
        public IReadOnlyList<ComponentImportAst>? ComponentInports { get; init; } // <componentImport>*
        public IReadOnlyList<SheetAst> Sheets { get; init; } =default!; // <sheet>+


        public SourceSpan? Span { get; init; }

        public string FilePath { get; init; } = string.Empty; // この AST が生成されたファイルのパス


        public WorkbookAst(XElement workbookElem, List<Issue> issues, string? filePath = null)
        {
            // ルート <workbook> 要素から各子要素を AST に変換する。
            var dslDir = filePath is null ? "" : Path.GetDirectoryName(filePath) ?? "";
            
            var stylesElem = workbookElem.Element(workbookElem.Name.Namespace + StylesAst.TagName);
            StylesAst? stylesAst = stylesElem != null ? new StylesAst(stylesElem, issues, dslDir) : null;

            var componentElems = workbookElem.Elements(workbookElem.Name.Namespace + ComponentAst.TagName);
            var components = componentElems.Select(e => new ComponentAst(e, issues)).ToList();

            var sheetElems = workbookElem.Elements(workbookElem.Name.Namespace + SheetAst.TagName);
            var sheets = sheetElems.Select(e => new SheetAst(e, issues)).ToList();

            var componentImportsElems = workbookElem.Elements(workbookElem.Name.Namespace + ComponentImportAst.TagName);
            var componentImports = componentImportsElems.Select(e => new ComponentImportAst(e, issues, dslDir)).ToList();

            Styles = stylesAst;
            Components = components;
            ComponentInports = (componentImports.Count == 0) ? null : componentImports;
            Sheets = sheets;
            Span = SourceSpan.CreateSpanAttributes(workbookElem);
        }
    }
}
