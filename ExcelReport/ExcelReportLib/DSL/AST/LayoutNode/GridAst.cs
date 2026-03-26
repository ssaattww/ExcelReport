using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    /// <summary>
    /// Represents grid ast.
    /// </summary>
    public sealed class GridAst : LayoutNodeAst, ICellAst, INamedAreaTarget
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "grid";

        /// <summary>
        /// グリッド行数。省略時は 0。
        /// </summary>
        public int Rows { get; init; }

        /// <summary>
        /// グリッド列数。省略時は 0。
        /// </summary>
        public int Cols { get; init; }

        /// <summary>
        /// Gets or sets the children.
        /// </summary>
        public IReadOnlyDictionary<Placement, LayoutNodeAst> Children { get; init; } = default!;
        /// <summary>
        /// Gets the target area name.
        /// </summary>
        public string? AreaName { get; init; }
        /// <summary>
        /// Gets conditional formatting rules defined under grid.
        /// </summary>
        public IReadOnlyList<ConditionalFormattingAst> ConditionalFormattings { get; init; } = Array.Empty<ConditionalFormattingAst>();

        /// <summary>
        /// Initializes a new instance of the grid ast type.
        /// </summary>
        /// <param name="elem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public GridAst(XElement elem, List<Issue> issues)
        {
            var layoutElems = elem.Elements().Where(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
            var children = layoutElems.Select(e => LayoutNodeAst.LayoutNodeAstFactory(e, issues)).ToList();
            var conditionalFormattings = elem.Elements(elem.Name.Namespace + ConditionalFormattingAst.TagName)
                .Select(element => new ConditionalFormattingAst(element, issues))
                .ToArray();
            var areaAttr = elem.Attribute("area");

            if (elem.Attribute("name") is not null)
            {
                issues.Add(
                    new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.InvalidAttributeValue,
                        Message = "<grid> 要素の name 属性は廃止されました。area 属性を使用してください。",
                        Span = SourceSpan.CreateSpanAttributes(elem),
                    });
            }

            Rows = ParseOptionalNonNegativeIntAttribute(elem, "rows", issues);
            Cols = ParseOptionalNonNegativeIntAttribute(elem, "cols", issues);
            AreaName = string.IsNullOrWhiteSpace(areaAttr?.Value)
                ? null
                : areaAttr.Value.Trim();
            Children = AstDictionaryBuilder.BuildLayoutNodeMap(children, issues, TagName);
            ConditionalFormattings = conditionalFormattings;
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
                    Message = $"<grid> 要素の {attrName} 属性が不正です。0 以上の整数を指定してください。",
                    Span = SourceSpan.CreateSpanAttributes(elem),
                });

            return 0;
        }
    }
}
