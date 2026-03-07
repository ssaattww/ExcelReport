using ExcelReportLib.DSL.AST.LayoutNode;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Represents component import ast.
    /// </summary>
    public sealed class ComponentImportAst : IAst<ComponentImportAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "componentImport";
        /// <summary>
        /// Gets or sets the href raw.
        /// </summary>
        public string HrefRaw { get; private set; }
        /// <summary>
        /// Gets or sets the path str.
        /// </summary>
        public string PathStr { get; private set; }

        /// <summary>
        /// Gets or sets the components.
        /// </summary>
        public ComponentsAst Components { get; init;}

        /// <summary>
        /// Gets or sets the styles.
        /// </summary>
        public StylesAst? Styles { get; init; }         // <styles>（任意）

        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        /// <summary>
        /// Initializes a new instance of the component import ast type.
        /// </summary>
        /// <param name="elem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        /// <param name="dslDir">The base directory used to resolve relative imports.</param>
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
                var ns = doc.Root!.Name.Namespace;
                var importDir = Path.GetDirectoryName(PathStr) ?? string.Empty;
                var stylesElem = doc.Root.Element(ns + StylesAst.TagName);

                Styles = stylesElem is null
                    ? null
                    : new StylesAst(stylesElem, issues, importDir);
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
