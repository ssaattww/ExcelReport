using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public class StyleImportAst : IAst<StyleImportAst>
    {
        public static string TagName => "styleImport";
        public string PathStr { get; init; }

        public string HrefRaw { get; init; }
        public SourceSpan? Span { get; init; }

        public StylesAst StylesAst { get; init; }
        public StyleImportAst(XElement elem, List<Issue> issues, string dslDir = "")
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
                        Message = "StylesImport 要素に href 属性がありません。",
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

            PathStr = isPathRooted ? HrefRaw : Path.Combine(dslDir, HrefRaw);
            if (false == File.Exists(PathStr))
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
                StylesAst = new StylesAst(doc.Root!, issues);
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
