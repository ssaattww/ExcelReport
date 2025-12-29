using ExcelReportLib.DSL.AST;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ExcelReportLib.DSL
{
    public static class DslParser
    {
        public static DslParseResult ParseFromFile(string filePath, DslParserOptions? parseOptions=null)
        {
            parseOptions ??= new DslParserOptions { RootFilePath = filePath};
            using var stream = System.IO.File.OpenRead(filePath);
            return ParseFromStream(stream, parseOptions);
        }
        public static DslParseResult ParseFromText(string xmlText, DslParserOptions? parseOptions=null)
        {
            parseOptions ??= new DslParserOptions();
            using var reader = new StringReader(xmlText);
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlText));
            return ParseFromStream(stream, parseOptions);
        }
        public static DslParseResult ParseFromStream(Stream xmlStream, DslParserOptions? options = null)
        {
            options ??= new DslParserOptions();
            var issues = new List<Issue>();

            XDocument? doc;
            try
            {
                doc = XDocument.Load(xmlStream, LoadOptions.SetLineInfo);
            }
            catch (XmlException ex)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Fatal,
                    Kind = IssueKind.XmlMalformed,
                    Message = ex.Message,
                });
                return new DslParseResult { Root = null, Issues = issues };
            }

            //if (options.EnableSchemaValidation)
            //{
            //    ValidateWithSchema(doc, issues);
            //    if (issues.Any(i => i.Severity == IssueSeverity.Fatal))
            //    {
            //        return new DslParseResult { Root = null, Issues = issues };
            //    }
            //}
            WorkbookAst root;
            if (options.RootFilePath is not null)
            {
                root = new WorkbookAst(doc.Root!, issues, options.RootFilePath);
            }
            else
            {
                root = new WorkbookAst(doc.Root!, issues);
            }

            ValidateDsl(root, issues, options);

            return new DslParseResult
            {
                Root = issues.Any(i => i.Severity == IssueSeverity.Fatal) ? null : root,
                Issues = issues,
            };
        }

        //private static void ValidateWithSchema(XDocument doc, List<Issue> issues)
        //{
        //    // XmlReaderSettings に _schemaSet を設定し、検証イベントで Issue を追加する。
        //    var settings = new XmlReaderSettings
        //    {
        //        ValidationType = ValidationType.Schema,
        //        Schemas = _schemaSet
        //    };
        //    settings.ValidationEventHandler += (sender, e) =>
        //    {
        //        issues.Add(new Issue
        //        {
        //            Severity = IssueSeverity.Fatal,
        //            Kind = IssueKind.SchemaViolation,
        //            Message = e.Message,
        //        });
        //    };

        //    using var reader = doc.CreateReader();
        //    using var validatingReader = XmlReader.Create(reader, settings);
        //    while (validatingReader.Read())
        //    {
        //        // すべてのノードを読み進めることで検証を完了させる
        //    }
        //}

        private static void ValidateDsl(WorkbookAst root, List<Issue> issues, DslParserOptions options)
        {
            // ここで DSL 固有の検証（未定義参照、repeat 制約、sheetOptions 検証、静的レイアウト検証など）を行う。
            // 具体的な検証内容は 6. エラーモデル と 7. テスト観点を参照して実装する。
        }
    }

    public sealed class DslParserOptions
    {
        /// <summary>XML スキーマ検証を有効化するか。</summary>
        public bool EnableSchemaValidation { get; init; } = true;

        /// <summary>C# 式の構文エラーを Fatal として扱うか。</summary>
        public bool TreatExpressionSyntaxErrorAsFatal { get; init; } = true;

        public string? RootFilePath { get; init; }
    }

    public enum IssueSeverity
    {
        Info,
        Warning,
        Error,
        Fatal
    }

    public enum IssueKind
    {
        // XML レベル
        XmlMalformed,
        SchemaViolation,

        // 定義・参照
        UndefinedRequiredAttribute,// name 属性など必須属性の未定義
        UndefinedRequiredElement,  // component 要素内に body がないなど必須要素の未定義
        UndefinedComponent,
        UndefinedStyle,

        // ファイル以外から読み込んだ
        LoadFile,

        InvalidAttributeValue, // 属性値が不正。パースに失敗した場合など
        
        DuplicateComponentName,
        DuplicateStyleName,
        DuplicateSheetName,

        // スタイル
        StyleScopeViolation,

        // repeat / layout
        RepeatChildCountInvalid,
        CoordinateOutOfRange,
        FormulaRefSeriesNot1DContinuous,

        // sheetOptions
        SheetOptionsTargetNotFound,

        // 式
        ExpressionSyntaxError,
    }

    public sealed class Issue
    {
        public IssueSeverity Severity { get; init; }
        public IssueKind Kind { get; init; }
        public string Message { get; init; } = string.Empty;
        public SourceSpan? Span { get; init; }  // ファイル名、行、列など
    }

    public sealed class DslParseResult
    {
        public WorkbookAst? Root { get; init; }
        public IReadOnlyList<Issue> Issues { get; init; } = Array.Empty<Issue>();

        public bool HasFatal => Issues.Any(i => i.Severity == IssueSeverity.Fatal);
    }
}
