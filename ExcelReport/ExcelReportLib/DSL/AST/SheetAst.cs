using ExcelReportLib.DSL.AST.LayoutNode;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Represents sheet ast.
    /// </summary>
    public sealed class SheetAst : IAst<SheetAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "sheet";

        /// <summary>
        /// シート名。
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// シート行数。省略時は 0。
        /// </summary>
        public int Rows { get; init; }

        /// <summary>
        /// シート列数。省略時は 0。
        /// </summary>
        public int Cols { get; init; }

        /// <summary>
        /// Gets or sets the style refs.
        /// </summary>
        public IReadOnlyList<StyleRefAst> StyleRefs { get; init; } = Array.Empty<StyleRefAst>();
        /// <summary>
        /// Gets or sets the children.
        /// </summary>
        public IReadOnlyDictionary<Placement,LayoutNodeAst> Children { get; init; }
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        public SheetOptionsAst? Options { get; init; }

        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the sheet ast type.
        /// </summary>
        /// <param name="sheetElem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
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
            Rows = ParseOptionalNonNegativeIntAttribute(sheetElem, "rows", issues);
            Cols = ParseOptionalNonNegativeIntAttribute(sheetElem, "cols", issues);
            StyleRefs = styles;
            Children = AstDictionaryBuilder.BuildLayoutNodeMap(children, issues, TagName);
            Options = options;
            Span = SourceSpan.CreateSpanAttributes(sheetElem);
        }

        private static int ParseOptionalNonNegativeIntAttribute(XElement elem, string attrName, List<Issue> issues)
        {
            var attr = elem.Attribute(attrName);
            if (attr is null)
            {
                return 0;
            }

            if (int.TryParse(attr.Value, out var parsedValue) && parsedValue >= 0)
            {
                return parsedValue;
            }

            issues.Add(
                new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"<sheet> 要素の {attrName} 属性が不正です。0 以上の整数を指定してください。",
                Span = SourceSpan.CreateSpanAttributes(elem),
            });

            return 0;
        }
    }
}
