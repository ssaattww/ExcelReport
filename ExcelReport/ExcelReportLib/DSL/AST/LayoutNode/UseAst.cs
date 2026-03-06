using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    /// <summary>
    /// 別定義のコンポーネントを使用することを表すASTノード
    /// </summary>
    public sealed class UseAst : LayoutNodeAst
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
        /// Gets or sets the instance name.
        /// </summary>
        public string? InstanceName { get; init; }
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
            var instanceAttr = elem.Attribute("instance");
            var withAttr = elem.Attribute("with");

            ComponentName = nameAttr.Value;
            InstanceName = instanceAttr?.Value;
            WithExprRaw = withAttr?.Value;
        }
    }
}
