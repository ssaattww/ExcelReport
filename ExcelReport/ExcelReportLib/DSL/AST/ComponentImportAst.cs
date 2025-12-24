using ExcelReportLib.DSL.AST.LayoutNode;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public sealed class ComponentImportAst : IAst<ComponentImportAst>
    {
        public static string TagName => "componentImport";
        public string HrefRaw { get; private set; }
        public string PathStr { get; private set; }

        public ComponentsAst Components { get; init;}

        public StylesAst? Styles { get; init; }         // <styles>（任意）

        public SourceSpan? Span { get; init; }

        public ComponentImportAst(XElement elem, List<Issue> issues, string dslDir = "")
        {
            Span = SourceSpan.CreateSpanAttributes(elem);

            var hrefElem = elem.Attribute("href");
            if (hrefElem == null)
            {
                issues.Add(
                    new Issue
                    {
                        Kind = IssueKind.UndefinedRequiredAttribute,
                        Severity = IssueSeverity.Error,
                        Message = "componentImport 要素に href 属性がありません。",
                        Span = Span,
                    });
                HrefRaw = string.Empty;
                return;
            }
            HrefRaw = hrefElem.Value;

            bool isPathRooted = Path.IsPathRooted(HrefRaw);
            if (false == isPathRooted && dslDir == String.Empty)
            {
                dslDir = System.IO.Directory.GetCurrentDirectory();
                issues.Add(
                    new Issue
                    {
                        Kind = IssueKind.LoadFile,
                        Severity = IssueSeverity.Info,
                        Message = "DSL ファイルのディレクトリが指定されていないため、カレントディレクトリを使用します。",
                        Span = Span,
                    });
            }

            PathStr = isPathRooted ? HrefRaw : Path.Combine(dslDir,HrefRaw);
            if(false == File.Exists(PathStr))
            {
                issues.Add(
                    new Issue
                    {
                        Kind = IssueKind.LoadFile,
                        Severity = IssueSeverity.Error,
                        Message = $"componentImport で指定されたファイルが見つかりません: {PathStr} filePathでDSL Parserを呼び出すことを検討してください",
                        Span = Span,
                    });
                return;
            }

            using var stream = System.IO.File.OpenRead(PathStr);
            XDocument? doc;
            try
            {
                doc = XDocument.Load(stream, LoadOptions.SetLineInfo);
                Components = new ComponentsAst(doc.Root!, issues);
            }
            catch (XmlException ex)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Fatal,
                    Kind = IssueKind.XmlMalformed,
                    Message = ex.Message,
                });
                return;
            }
        }
    }
}
