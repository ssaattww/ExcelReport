using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public sealed class SourceSpan
    {
        public string? FileName { get; init; }
        public int Line { get; init; }
        public int Column { get; init; }

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
    public readonly struct Placement
    {
        public static readonly Placement None = new Placement(null, null, 1, 1, null);

        public int? Row { get; }
        public int? Col { get; }
        public int RowSpan { get; }
        public int ColSpan { get; }
        public string? WhenExprRaw { get; } // @(...) 式文字列

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
            var rowAttr = elem.Attribute("row");
            if (rowAttr != null && int.TryParse(rowAttr.Value, out var r))
            {
                row = r;
            }
            var colAttr = elem.Attribute("col");
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
}

static public class XElementEx
{
    /// <summary>
    /// 名前空間を解決して、一番目の子要素を返す
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="childName"></param>
    /// <returns></returns>
    public static XElement? GetFirstOrDefaultChildElement(this XElement parent, string childName)
    {
        return parent.Elements(parent.Name.Namespace + childName).FirstOrDefault();
    }

    public static IEnumerable<XElement>? GetXElementsOrDefault(this XElement parent, string childName)
    {
        return parent.Elements(parent.Name.Namespace + childName);
    }
}