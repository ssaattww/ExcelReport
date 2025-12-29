using ExcelReportLib.DSL.AST.LayoutNode;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public sealed class SheetAst : IAst<SheetAst>
    {
        public static string TagName => "sheet";
        public string Name { get; init; } = string.Empty;

        public IReadOnlyList<StyleRefAst> StyleRefs { get; init; } = Array.Empty<StyleRefAst>();
        public IReadOnlyDictionary<Placement,LayoutNodeAst> Children { get; init; }
        public SheetOptionsAst? Options { get; init; }

        public SourceSpan? Span { get; init; }

        public SheetAst(XElement sheetElem, List<Issue> issues)
        {
            // <sheet> 要素から SheetAst を構築する。
            var nameAttr = sheetElem.Attribute("name");
            string name = nameAttr?.Value ?? "Sheet1";

            // スタイル参照の解析
            var styleElems = sheetElem.Elements(sheetElem.Name.Namespace + StyleRefAst.TagName);
            var styles = styleElems.Select(e => new StyleRefAst(e, issues)).ToList();

            // オプションの解析
            var optionsElem = sheetElem.Element(sheetElem.Name.Namespace + SheetOptionsAst.TagName);
            SheetOptionsAst? options = optionsElem != null ? new SheetOptionsAst(optionsElem, issues) : null;

            // レイアウトノードの解析
            var layoutElems = sheetElem.Elements().Where(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
            var children = layoutElems.Select(e => LayoutNodeAst.LayoutNodeAstFactory(e, issues)).Select(e => new { Child = e, Place = e.Placement }).ToList();

            Name = name;
            StyleRefs = styles;
            Children = children.ToDictionary(child => child.Place, child => child.Child);
            Options = options;
            Span = SourceSpan.CreateSpanAttributes(sheetElem);
        }
    }
}
