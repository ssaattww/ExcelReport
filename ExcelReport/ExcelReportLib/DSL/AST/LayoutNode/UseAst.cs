using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    /// <summary>
    /// 別定義のコンポーネントを使用することを表すASTノード
    /// </summary>
    public sealed class UseAst : LayoutNodeAst, INamedAreaTarget
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "use";
        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string ComponentName { get; init; } = string.Empty;
        /// <summary>
        /// Gets the target area name.
        /// </summary>
        public string? AreaName { get; init; }
        /// <summary>
        /// Gets or sets the with expr raw.
        /// </summary>
        public string? WithExprRaw { get; init; }

        /// <summary>
        /// コンポーネント参照先（解析フェーズで設定される）s
        /// </summary>
        public LayoutNodeAst ComponentRef { get; set; } = default!; 


        /// <summary>
        /// Initializes a new instance of the use ast type.
        /// </summary>
        /// <param name="elem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public UseAst(XElement elem, List<Issue> issues)
        {
            var nameAttr = elem.Attribute("component");
            if (nameAttr == null)
            {
                issues.Add(new Issue 
                {
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Severity = IssueSeverity.Error,
                    Message = "Use 要素に component 属性がありません。",
                });
                return;
            }

            if (elem.Attribute("instance") is not null)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = "<use> 要素の instance 属性は廃止されました。area 属性を使用してください。",
                    Span = SourceSpan.CreateSpanAttributes(elem),
                });
            }

            var areaAttr = elem.Attribute("area");
            var withAttr = elem.Attribute("with");

            ComponentName = nameAttr.Value;
            AreaName = string.IsNullOrWhiteSpace(areaAttr?.Value) ? null : areaAttr.Value.Trim();
            WithExprRaw = withAttr?.Value;
        }
    }
}
