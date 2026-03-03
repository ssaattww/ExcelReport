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
        public int Rows { get; init; }
        public int Cols { get; init; }

        public IReadOnlyList<StyleRefAst> StyleRefs { get; init; } = Array.Empty<StyleRefAst>();
        public IReadOnlyDictionary<Placement,LayoutNodeAst> Children { get; init; }
        public SheetOptionsAst? Options { get; init; }

        public SourceSpan? Span { get; init; }

        public SheetAst(XElement sheetElem, List<Issue> issues)
        {
            // <sheet> 要素から SheetAst を構築する。
            var nameAttr = sheetElem.Attribute("name");
            string name = nameAttr?.Value ?? "Sheet1";
            if (nameAttr is null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Message = "<sheet> 要素に name 属性がありません。",
                    Span = SourceSpan.CreateSpanAttributes(sheetElem),
                });
            }
            else if (string.IsNullOrWhiteSpace(nameAttr.Value))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = "<sheet> 要素の name 属性が空です。",
                    Span = SourceSpan.CreateSpanAttributes(sheetElem),
                });
            }

            // スタイル参照の解析
            var styleElems = sheetElem.Elements(sheetElem.Name.Namespace + StyleRefAst.TagName);
            var styles = styleElems.Select(e => new StyleRefAst(e, issues)).ToList();

            // オプションの解析
            var optionsElem = sheetElem.Element(sheetElem.Name.Namespace + SheetOptionsAst.TagName);
            SheetOptionsAst? options = optionsElem != null ? new SheetOptionsAst(optionsElem, issues) : null;

            // レイアウトノードの解析
            var layoutElems = sheetElem.Elements().Where(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
            var children = layoutElems.Select(e => LayoutNodeAst.LayoutNodeAstFactory(e, issues)).ToList();

            Name = name;
            Rows = ParseRequiredPositiveIntAttribute(sheetElem, "rows", issues);
            Cols = ParseRequiredPositiveIntAttribute(sheetElem, "cols", issues);
            StyleRefs = styles;
            Children = AstDictionaryBuilder.BuildLayoutNodeMap(children, issues, TagName);
            Options = options;
            Span = SourceSpan.CreateSpanAttributes(sheetElem);
        }

        private static int ParseRequiredPositiveIntAttribute(XElement elem, string attrName, List<Issue> issues)
        {
            var attr = elem.Attribute(attrName);
            if (attr is not null && int.TryParse(attr.Value, out var value) && value > 0)
            {
                return value;
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = attr is null ? IssueKind.UndefinedRequiredAttribute : IssueKind.InvalidAttributeValue,
                Message = attr is null
                    ? $"<sheet> 要素に {attrName} 属性がありません。"
                    : $"<sheet> 要素の {attrName} 属性が不正です。",
                Span = SourceSpan.CreateSpanAttributes(elem),
            });
            return 0;
        }
    }
}
