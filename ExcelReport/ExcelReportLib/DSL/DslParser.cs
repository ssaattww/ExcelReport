using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;
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
            ResolveStyleRefs(root, issues);
            ResolveComponentRefs(root, issues);

            return new DslParseResult
            {
                Root = issues.Any(i => i.Severity == IssueSeverity.Fatal) ? null : root,
                Issues = issues,
            };
        }

        /// <summary>
        /// use 要素の componentRef を解決する。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="issues"></param>
        private static void ResolveComponentRefs(WorkbookAst root, List<Issue> issues)
        {
            var componentMap = new Dictionary<string, LayoutNodeAst>(StringComparer.Ordinal);

            void AddComponent(ComponentAst comp, SourceSpan? span)
            {
                if (componentMap.ContainsKey(comp.Name))
                {
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.DuplicateComponentName,
                        Message = $"Component 名が重複しています: {comp.Name}",
                        Span = span
                    });
                    return;
                }
                componentMap[comp.Name] = comp.Body;
            }

            if (root.Components != null)
                foreach (var c in root.Components)
                    AddComponent(c, c.Span);

            if (root.ComponentInports != null)
                foreach (var imp in root.ComponentInports)
                    foreach (var c in imp.Components.ComponentList)
                        AddComponent(c, c.Span);

            foreach (var use in EnumerateLayoutNodes(root).OfType<UseAst>())
            {
                if (componentMap.TryGetValue(use.ComponentName, out var body))
                    use.ComponentRef = body;
                else
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.UndefinedComponent,
                        Message = $"未定義の component 参照: {use.ComponentName}",
                        Span = use.Span
                    });
            }
        }

        /// <summary>
        /// StyleRef を解決する。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="issues"></param>
        private static void ResolveStyleRefs(WorkbookAst root, List<Issue> issues)
        {
            var styleIndex = new Dictionary<string, StyleAst>(StringComparer.Ordinal);

            /// <summary>
            /// スタイル定義をインデックス化する。
            /// </summary>  
            void IndexStyles(StylesAst? styles)
            {
                if (styles?.Styles == null) return;
                foreach (var s in styles.Styles)
                {
                    if (styleIndex.ContainsKey(s.Name))
                    {
                        issues.Add(new Issue
                        {
                            Severity = IssueSeverity.Error,
                            Kind = IssueKind.DuplicateStyleName,
                            Message = $"Style 名が重複しています: {s.Name}",
                            Span = s.Span
                        });
                        continue;
                    }
                    styleIndex[s.Name] = s;
                }

                if (styles.StyleImportAsts != null)
                {
                    foreach (var imp in styles.StyleImportAsts)
                    {
                        if (imp?.StylesAst != null)
                            IndexStyles(imp.StylesAst);
                    }
                }
            }

            // ルート styles
            IndexStyles(root.Styles);

            // componentImport 側の styles を使いたい場合は
            // ComponentImportAst に Styles を積む or ここで取り込む
            if (root.ComponentInports != null)
            {
                foreach (var imp in root.ComponentInports)
                {
                    if (imp.Styles != null)
                        IndexStyles(imp.Styles);
                }
            }

            /// <summary>
            /// 入れ子になっている styleRef をすべて列挙する。
            /// yeild returnで列挙後、もう一度呼び出したときにstackに子要素を追加していくことで遅延実行している。
            /// </summary>
            IEnumerable<StyleRefAst> EnumerateStyleRefs(IEnumerable<StyleRefAst> roots)
            {
                var stack = new Stack<StyleRefAst>(roots);
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    yield return cur;
                    foreach (var child in cur.StyleRefs)
                        stack.Push(child);
                }
            }

            var allStyleRefs = new List<StyleRefAst>();

            // sheet 直下
            foreach (var sheet in root.Sheets)
                allStyleRefs.AddRange(sheet.StyleRefs);

            // layout nodes 直下
            foreach (var node in EnumerateLayoutNodes(root))
                allStyleRefs.AddRange(node.StyleRefs);

            foreach (var styleRef in EnumerateStyleRefs(allStyleRefs))
            {
                if (styleIndex.TryGetValue(styleRef.Name, out var stylesAst))
                    styleRef.StyleRef = stylesAst;
                else
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.UndefinedStyle,
                        Message = $"未定義の style 参照: {styleRef.Name}",
                        Span = styleRef.Span
                    });
            }
        }

        /// <summary>
        /// 全ての LayoutNodeAst を列挙する。
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private static IEnumerable<LayoutNodeAst> EnumerateLayoutNodes(WorkbookAst root)
        {
            foreach (var sheet in root.Sheets)
            {
                foreach (var node in sheet.Children.Values)
                {
                    foreach (var n in EnumerateLayoutNodes(node))
                        yield return n;
                }
            }

            if (root.Components != null)
            {
                foreach (var comp in root.Components)
                {
                    foreach (var n in EnumerateLayoutNodes(comp.Body))
                        yield return n;
                }
            }

            if (root.ComponentInports != null)
            {
                foreach (var import in root.ComponentInports)
                {
                    foreach (var comp in import.Components.ComponentList)
                    {
                        foreach (var n in EnumerateLayoutNodes(comp.Body))
                            yield return n;
                    }
                }
            }
        }

        private static IEnumerable<LayoutNodeAst> EnumerateLayoutNodes(LayoutNodeAst node)
        {
            yield return node;

            switch (node)
            {
                case GridAst grid:
                    foreach (var child in grid.Children.Values)
                        foreach (var n in EnumerateLayoutNodes(child))
                            yield return n;
                    break;

                case RepeatAst repeat:
                    if (repeat.Body != null)
                    {
                        foreach (var n in EnumerateLayoutNodes(repeat.Body))
                            yield return n;
                    }
                    break;

                // UseAst / CellAst は子を持たない
            }
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
