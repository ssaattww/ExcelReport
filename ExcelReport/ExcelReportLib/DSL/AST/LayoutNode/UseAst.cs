using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST.LayoutNode
{
    /// <summary>
    /// 別定義のコンポーネントを使用することを表すASTノード
    /// </summary>
    public sealed class UseAst : LayoutNodeAst, IAst<UseAst>
    {
        public static string TagName => "use";
        public string ComponentName { get; init; } = string.Empty;
        public string? InstanceName { get; init; }
        public string? WithExprRaw { get; init; }
        public UseAst(XElement elem, List<Issue> issues) :base(elem, issues)
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
            // スタイル参照の解析
            var styleElems = elem.Elements(elem.Name.Namespace + "styleRef");
            var styles = styleElems.Select(e => new StyleRefAst(e, issues)).ToList();

            ComponentName = nameAttr.Value;
            InstanceName = instanceAttr?.Value;
            WithExprRaw = withAttr?.Value;
        }
    }
}
