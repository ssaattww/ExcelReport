using ExcelReportLib.DSL.AST.LayoutNode;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public sealed class ComponentAst : ICellAst
    {
        public static string TagName => "component";
        public SourceSpan? Span { get; init; }
        public string Name { get; init; } = string.Empty;  // @name
        public LayoutNodeAst Body { get; init; } = default!; // <grid>|<repeat>を想定<cell>や<use>が来た場合componentの定義として意味ないけれど破綻はしないので許容する
        public IReadOnlyList<StyleRefAst> StyleRefs { get; init; } = default!;

        public Placement Placement { get; init; } = default!;

        public ComponentAst(XElement componentElem, List<Issue> issues)
        {
            // <component> 要素から ComponentAst を構築する。
            var nameAttr = componentElem.Attribute("name");
            if (nameAttr == null)
            {
                issues.Add(new Issue
                {
                    Kind = IssueKind.UndefinedRequiredAttribute,
                    Severity = IssueSeverity.Error,
                    Message = "Component 要素に name 属性がありません。",
                });
                return;
            }
            // ボディの解析
            var bodyElem = componentElem.Elements().FirstOrDefault(e => LayoutNodeAst.AllowedLayoutNodeNames.Contains(e.Name.LocalName));
            if (bodyElem == null)
            {
                issues.Add(new Issue
                { 
                    Kind = IssueKind.UndefinedRequiredElement, 
                    Severity = IssueSeverity.Warning,
                    Message = "Component 要素に有効なレイアウトノードが含まれていません。",
                });
                return;
            }
            var bodyAst = LayoutNodeAst.LayoutNodeAstFactory(bodyElem, issues);

            var ns = componentElem.Name.Namespace;
            var stylesElem = componentElem.Elements(ns + StyleAst.TagName);

            Name = nameAttr.Value;
            Body = bodyAst;
            Span = SourceSpan.CreateSpanAttributes(componentElem);
            Placement = Placement.ParsePlacementAttributes(componentElem, issues);
        }
    }
}
