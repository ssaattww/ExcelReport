using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    /// <summary>
    /// Represents cell ast.
    /// </summary>
    public sealed class CellAst : LayoutNodeAst, IAst<CellAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "cell";
        /// <summary>
        /// Gets or sets the value raw.
        /// </summary>
        public string? ValueRaw { get; init; }
        /// <summary>
        /// Gets or sets the formula raw.
        /// </summary>
        public string? FormulaRaw { get; init; }
        /// <summary>
        /// Gets or sets the style ref shortcut.
        /// </summary>
        public string? StyleRefShortcut { get; init; }
        /// <summary>
        /// Gets or sets the formula ref.
        /// </summary>
        public string? FormulaRef { get; init; }
        /// <summary>
        /// Gets or sets the formula ref scope.
        /// </summary>
        public string? FormulaRefScope { get; init; }

        /// <summary>
        /// Initializes a new instance of the cell ast type.
        /// </summary>
        /// <param name="elem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public CellAst(XElement elem, List<Issue> issues)
        {
            var valueAttr = elem.Attribute("value");
            var formulaAttr = elem.Attribute("formula");
            var styleRefAttr = elem.Attribute("styleRef");
            var formulaRefAttr = elem.Attribute("formulaRef");
            var formulaRefScopeAttr = elem.Attribute("formulaRefScope");

            ValueRaw = ResolvePreferredText(
                elem,
                valueAttr,
                elem.GetFirstOrDefaultChildElement("value"),
                "value",
                issues);
            FormulaRaw = formulaAttr?.Value;
            StyleRefShortcut = styleRefAttr?.Value;
            FormulaRef = formulaRefAttr?.Value;
            FormulaRefScope = NormalizeFormulaRefScope(formulaRefScopeAttr?.Value, elem, issues);
        }

        private static string? NormalizeFormulaRefScope(string? rawScope, XElement owner, List<Issue> issues)
        {
            if (string.IsNullOrWhiteSpace(rawScope))
            {
                return null;
            }

            if (string.Equals(rawScope, "local", StringComparison.OrdinalIgnoreCase))
            {
                return "local";
            }

            if (string.Equals(rawScope, "global", StringComparison.OrdinalIgnoreCase))
            {
                return "global";
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Warning,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"<cell> 要素の formulaRefScope には 'local' または 'global' を指定してください。'{rawScope}' は無効のため 'global' として扱います。",
                Span = SourceSpan.CreateSpanAttributes(owner),
            });

            return "global";
        }

        private static string ResolvePreferredText(
            XElement owner,
            XAttribute? attribute,
            XElement? element,
            string targetName,
            List<Issue> issues)
        {
            if (attribute is not null && element is not null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Warning,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = $"<cell> 要素の {targetName} は属性と子要素の両方に指定されています。属性値を優先します。",
                    Span = SourceSpan.CreateSpanAttributes(owner),
                });
            }

            if (attribute is not null)
            {
                return attribute.Value;
            }

            return element?.Value.Trim() ?? string.Empty;
        }
    }
}
