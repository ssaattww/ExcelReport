using ExcelReportLib.DSL.AST.LayoutNode;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Represents source span.
    /// </summary>
    public sealed class SourceSpan
    {
        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string? FileName { get; init; }
        /// <summary>
        /// Gets or sets the line.
        /// </summary>
        public int Line { get; init; }
        /// <summary>
        /// Gets or sets the column.
        /// </summary>
        public int Column { get; init; }

        /// <summary>
        /// Creates span attributes.
        /// </summary>
        /// <param name="elem">The source XML element.</param>
        /// <returns>The resulting source span.</returns>
        public static SourceSpan? CreateSpanAttributes(XElement elem)
        {
            if (elem is IXmlLineInfo li && li.HasLineInfo())
            {
                return new SourceSpan
                {
                    FileName = null,
                    Line = li.LineNumber,
                    Column = li.LinePosition,
                };
            }
            return null;
        }
    }
    /// <summary>
    /// Represents placement.
    /// </summary>
    public readonly struct Placement
    {
        /// <summary>
        /// Represents none.
        /// </summary>
        public static readonly Placement None = new Placement(null, null, 1, 1, null);

        /// <summary>
        /// Gets the row.
        /// </summary>
        public int? Row { get; }
        /// <summary>
        /// Gets the col.
        /// </summary>
        public int? Col { get; }
        /// <summary>
        /// Gets the row span.
        /// </summary>
        public int RowSpan { get; }
        /// <summary>
        /// Gets the col span.
        /// </summary>
        public int ColSpan { get; }
        /// <summary>
        /// Gets the when expr raw.
        /// </summary>
        public string? WhenExprRaw { get; } // @(...) 式文字列

        /// <summary>
        /// Initializes a new instance of the placement type.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="col">The col.</param>
        /// <param name="rowSpan">The row span.</param>
        /// <param name="colSpan">The col span.</param>
        /// <param name="whenExprRaw">The when expr raw.</param>
        public Placement(int? row, int? col, int rowSpan, int colSpan, string? whenExprRaw)
        {
            Row = row;
            Col = col;
            RowSpan = rowSpan <= 0 ? 1 : rowSpan;
            ColSpan = colSpan <= 0 ? 1 : colSpan;
            WhenExprRaw = whenExprRaw;
        }

        internal static Placement ParsePlacementAttributes(XElement elem, List<Issue> issues)
        {
            int? row = null;
            int? col = null;
            int rowSpan = 1;
            int colSpan = 1;
            string? whenExprRaw = null;
            var rowAttr = elem.Attribute("r");
            if (rowAttr != null && int.TryParse(rowAttr.Value, out var r))
            {
                row = r;
            }
            var colAttr = elem.Attribute("c");
            if (colAttr != null && int.TryParse(colAttr.Value, out var c))
            {
                col = c;
            }
            var rowSpanAttr = elem.Attribute("rowSpan");
            if (rowSpanAttr != null && int.TryParse(rowSpanAttr.Value, out var rs))
            {
                rowSpan = rs;
            }
            var colSpanAttr = elem.Attribute("colSpan");
            if (colSpanAttr != null && int.TryParse(colSpanAttr.Value, out var cs))
            {
                colSpan = cs;
            }
            var whenAttr = elem.Attribute("when");
            if (whenAttr != null)
            {
                whenExprRaw = whenAttr.Value;
            }
            return new Placement(row, col, rowSpan, colSpan, whenExprRaw);
        }
    }

    internal static class AstDictionaryBuilder
    {
        /// <summary>
        /// Builds a placement-to-node map and reports duplicate placements as validation issues.
        /// </summary>
        /// <param name="nodes">The nodes to index by placement.</param>
        /// <param name="issues">The issue list that receives duplicate-placement errors.</param>
        /// <param name="ownerTag">The owner element tag used in issue messages.</param>
        /// <returns>A dictionary keyed by placement containing the first node at each location.</returns>
        public static IReadOnlyDictionary<Placement, LayoutNodeAst> BuildLayoutNodeMap(
            IEnumerable<LayoutNodeAst> nodes,
            List<Issue> issues,
            string ownerTag)
        {
            var result = new Dictionary<Placement, LayoutNodeAst>();
            foreach (var node in nodes)
            {
                if (result.ContainsKey(node.Placement))
                {
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.InvalidAttributeValue,
                        Message = $"<{ownerTag}> 要素内で配置が重複しています: {DescribePlacement(node.Placement)}",
                        Span = node.Span,
                    });
                    continue;
                }

                result[node.Placement] = node;
            }

            return result;
        }

        private static string DescribePlacement(Placement placement)
        {
            var row = placement.Row?.ToString() ?? "auto";
            var col = placement.Col?.ToString() ?? "auto";
            var when = string.IsNullOrWhiteSpace(placement.WhenExprRaw) ? string.Empty : $", when={placement.WhenExprRaw}";

            return $"r={row}, c={col}, rowSpan={placement.RowSpan}, colSpan={placement.ColSpan}{when}";
        }
    }
}

/// <summary>
/// Provides namespace-aware <see cref="XElement"/> extension helpers for child element lookup.
/// </summary>
static public class XElementEx
{
    /// <summary>
    /// 名前空間を解決して、一番目の子要素を返す
    /// </summary>
    /// <param name="parent">検索元の親要素。</param>
    /// <param name="childName">検索する子要素のローカル名。</param>
    /// <returns>一致する最初の子要素。存在しない場合は <see langword="null"/>。</returns>
    public static XElement? GetFirstOrDefaultChildElement(this XElement parent, string childName)
    {
        return parent.Elements(parent.Name.Namespace + childName).FirstOrDefault();
    }

    /// <summary>
    /// Returns all child elements with the specified local name in the parent's namespace.
    /// </summary>
    /// <param name="parent">The parent.</param>
    /// <param name="childName">The child name.</param>
    /// <returns>A collection containing the result.</returns>
    public static IEnumerable<XElement>? GetXElementsOrDefault(this XElement parent, string childName)
    {
        return parent.Elements(parent.Name.Namespace + childName);
    }
}
