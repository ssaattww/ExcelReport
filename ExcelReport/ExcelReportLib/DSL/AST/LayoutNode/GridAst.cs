using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    public sealed class GridAst : LayoutNodeAst, ICellAst
    {
        public static string TagName => "grid";

        /// <summary>
        /// グリッド行数。省略時は 0。
        /// </summary>
        public int Rows { get; init; }

        /// <summary>
        /// グリッド列数。省略時は 0。
        /// </summary>
        public int Cols { get; init; }

        public IReadOnlyDictionary<Placement, LayoutNodeAst> Children { get; init; } = default!;

        public GridAst(XElement elem, List<Issue> issues)
        {
            var layoutElems = elem.Elements().Where(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
            var children = layoutElems.Select(e => LayoutNodeAst.LayoutNodeAstFactory(e, issues)).ToList();

            Rows = ParseOptionalNonNegativeIntAttribute(elem, "rows", issues);
            Cols = ParseOptionalNonNegativeIntAttribute(elem, "cols", issues);
            Children = AstDictionaryBuilder.BuildLayoutNodeMap(children, issues, TagName);
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
