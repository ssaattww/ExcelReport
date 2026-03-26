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
        /// シート反復元の式。未指定時は空。
        /// </summary>
        public string FromExprRaw { get; init; } = string.Empty;

        /// <summary>
        /// シート反復時の変数名。省略時は item。
        /// </summary>
        public string VarName { get; init; } = "item";

        /// <summary>
        /// var 指定が明示されたかを取得します。
        /// </summary>
        public bool HasVarAttribute { get; init; }

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
        /// Gets or sets conditional formatting rules defined under sheet.
        /// </summary>
        public IReadOnlyList<ConditionalFormattingAst> ConditionalFormattings { get; init; } = Array.Empty<ConditionalFormattingAst>();

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

            // 条件付き書式の解析（sheet 直下で定義）
            var conditionalFormattings = sheetElem.Elements(sheetElem.Name.Namespace + ConditionalFormattingAst.TagName)
                .Select(element => new ConditionalFormattingAst(element, issues))
                .ToArray();

            // レイアウトノードの解析
            var layoutElems = sheetElem.Elements().Where(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
            var children = layoutElems.Select(e => LayoutNodeAst.LayoutNodeAstFactory(e, issues)).ToList();
            var fromExprRaw = ResolvePreferredText(
                sheetElem,
                sheetElem.Attribute("from"),
                sheetElem.GetFirstOrDefaultChildElement("from"),
                "from",
                issues);

            var varRaw = ResolvePreferredText(
                sheetElem,
                sheetElem.Attribute("var"),
                sheetElem.GetFirstOrDefaultChildElement("var"),
                "var",
                issues);
            var varName = string.IsNullOrWhiteSpace(varRaw) ? "item" : varRaw;
            var hasVarSpecified =
                sheetElem.Attribute("var") is not null
                || sheetElem.GetFirstOrDefaultChildElement("var") is not null;

            Name = name;
            Rows = ParseOptionalNonNegativeIntAttribute(sheetElem, "rows", issues);
            FromExprRaw = fromExprRaw;
            VarName = varName;
            HasVarAttribute = hasVarSpecified;
            Cols = ParseOptionalNonNegativeIntAttribute(sheetElem, "cols", issues);
            StyleRefs = styles;
            Children = AstDictionaryBuilder.BuildLayoutNodeMap(children, issues, TagName);
            Options = options;
            ConditionalFormattings = conditionalFormattings;
            Span = SourceSpan.CreateSpanAttributes(sheetElem);
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
                    Message = $"<sheet> 要素の {targetName} は属性と子要素の両方に指定されています。属性値を優先します。",
                    Span = SourceSpan.CreateSpanAttributes(owner),
                });
            }

            if (attribute is not null)
            {
                return attribute.Value;
            }

            return element?.Value.Trim() ?? string.Empty;
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

