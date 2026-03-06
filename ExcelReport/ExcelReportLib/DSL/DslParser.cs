using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ExcelReportLib.DSL
{
    /// <summary>
    /// Represents dsl parser.
    /// </summary>
    public static class DslParser
    {
        private const string SchemaResourceName = "ExcelReportLib.DSL.DslDefinition_v1.xsd";
        private const string SchemaRelativePath = "Design/DslDefinition/DslDefinition_v1.xsd";
        private const int MaxExcelRows = 1_048_576;
        private const int MaxExcelColumns = 16_384;

        /// <summary>
        /// Parses from file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="parseOptions">The parse options.</param>
        /// <returns>The resulting dsl parse result.</returns>
        public static DslParseResult ParseFromFile(string filePath, DslParserOptions? parseOptions = null)
        {
            parseOptions = parseOptions is null
                ? new DslParserOptions { RootFilePath = filePath }
                : new DslParserOptions
                {
                    EnableSchemaValidation = parseOptions.EnableSchemaValidation,
                    TreatExpressionSyntaxErrorAsFatal = parseOptions.TreatExpressionSyntaxErrorAsFatal,
                    RootFilePath = parseOptions.RootFilePath ?? filePath,
                };

            using var stream = File.OpenRead(filePath);
            return ParseFromStream(stream, parseOptions);
        }

        /// <summary>
        /// Parses from text.
        /// </summary>
        /// <param name="xmlText">The xml text.</param>
        /// <param name="parseOptions">The parse options.</param>
        /// <returns>The resulting dsl parse result.</returns>
        public static DslParseResult ParseFromText(string xmlText, DslParserOptions? parseOptions = null)
        {
            parseOptions ??= new DslParserOptions();
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlText));
            return ParseFromStream(stream, parseOptions);
        }

        /// <summary>
        /// Parses from stream.
        /// </summary>
        /// <param name="xmlStream">The xml stream.</param>
        /// <param name="options">Options that control the operation.</param>
        /// <returns>The resulting dsl parse result.</returns>
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

            if (options.EnableSchemaValidation)
            {
                ValidateWithSchema(doc, issues);
                if (issues.Any(i => i.Severity == IssueSeverity.Fatal))
                {
                    return new DslParseResult { Root = null, Issues = issues };
                }
            }

            var root = options.RootFilePath is not null
                ? new WorkbookAst(doc.Root!, issues, options.RootFilePath)
                : new WorkbookAst(doc.Root!, issues);

            ValidateDsl(root, issues, options);
            ResolveStyleRefs(root);
            ResolveComponentRefs(root);

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
        private static void ResolveComponentRefs(WorkbookAst root)
        {
            var componentMap = BuildComponentIndex(root);

            foreach (var use in EnumerateLayoutNodes(root).OfType<UseAst>())
            {
                if (componentMap.TryGetValue(use.ComponentName, out var body))
                {
                    use.ComponentRef = body;
                }
            }
        }

        /// <summary>
        /// StyleRef を解決する。
        /// </summary>
        /// <param name="root"></param>
        private static void ResolveStyleRefs(WorkbookAst root)
        {
            var styleIndex = BuildStyleIndex(root);

            foreach (var styleRef in EnumerateAllStyleRefs(root))
            {
                if (styleIndex.TryGetValue(styleRef.Name, out var style))
                {
                    styleRef.StyleRef = style;
                }
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
                foreach (var node in EnumerateLayoutNodes(sheet.Children.Values))
                {
                    yield return node;
                }
            }

            foreach (var component in EnumerateComponents(root))
            {
                foreach (var node in EnumerateLayoutNodes(component.Body))
                {
                    yield return node;
                }
            }
        }

        private static IEnumerable<LayoutNodeAst> EnumerateLayoutNodes(IEnumerable<LayoutNodeAst> nodes)
        {
            foreach (var node in nodes)
            {
                foreach (var child in EnumerateLayoutNodes(node))
                {
                    yield return child;
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
                    {
                        foreach (var nested in EnumerateLayoutNodes(child))
                        {
                            yield return nested;
                        }
                    }
                    break;

                case RepeatAst repeat when repeat.Body != null:
                    foreach (var nested in EnumerateLayoutNodes(repeat.Body))
                    {
                        yield return nested;
                    }
                    break;

                // UseAst / CellAst は子を持たない
            }
        }

        private static void ValidateWithSchema(XDocument doc, List<Issue> issues)
        {
            if (!TryCreateSchemaSet(out var schemaSet, out var errorMessage))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Fatal,
                    Kind = IssueKind.SchemaViolation,
                    Message = errorMessage,
                });
                return;
            }

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet,
            };

            settings.ValidationEventHandler += (_, e) =>
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Fatal,
                    Kind = IssueKind.SchemaViolation,
                    Message = e.Message,
                    Span = CreateSchemaViolationSpan(e.Exception),
                });
            };

            using var reader = doc.CreateReader();
            using var validatingReader = XmlReader.Create(reader, settings);
            while (validatingReader.Read())
            {
                // すべてのノードを読み進めることで検証を完了させる。
            }
        }

        private static void ValidateDsl(WorkbookAst root, List<Issue> issues, DslParserOptions options)
        {
            _ = options;

            var styleIndex = BuildStyleIndex(root, issues);
            var componentIndex = BuildComponentIndex(root, issues);

            ValidateDuplicateSheetNames(root, issues);
            ValidateStyleReferences(root, styleIndex, issues);
            ValidateComponentReferences(root, componentIndex, issues);
            ValidateRepeatConstraints(root, issues);
            ValidateSheetOptions(root, issues);
            ValidateStaticLayout(root, issues);
        }

        private static Dictionary<string, StyleAst> BuildStyleIndex(WorkbookAst root, List<Issue>? issues = null)
        {
            var styleIndex = new Dictionary<string, StyleAst>(StringComparer.Ordinal);
            var scannedImportSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            static string? GetImportSourceKey(StyleImportAst importAst)
            {
                var source = importAst.PathStr;
                if (string.IsNullOrWhiteSpace(source))
                {
                    source = importAst.HrefRaw;
                }

                if (string.IsNullOrWhiteSpace(source))
                {
                    return null;
                }

                try
                {
                    return Path.GetFullPath(source);
                }
                catch (Exception)
                {
                    return source;
                }
            }

            void IndexStyles(StylesAst? styles)
            {
                if (styles?.Styles != null)
                {
                    foreach (var style in styles.Styles)
                    {
                        if (string.IsNullOrWhiteSpace(style.Name))
                        {
                            continue;
                        }

                        if (!styleIndex.TryAdd(style.Name, style))
                        {
                            issues?.Add(new Issue
                            {
                                Severity = IssueSeverity.Error,
                                Kind = IssueKind.DuplicateStyleName,
                                Message = $"Style 名が重複しています: {style.Name}",
                                Span = style.Span,
                            });
                        }
                    }
                }

                if (styles?.StyleImportAsts == null)
                {
                    return;
                }

                foreach (var import in styles.StyleImportAsts)
                {
                    if (import?.StylesAst == null)
                    {
                        continue;
                    }

                    var importSourceKey = GetImportSourceKey(import);
                    if (importSourceKey != null && !scannedImportSources.Add(importSourceKey))
                    {
                        continue;
                    }

                    IndexStyles(import.StylesAst);
                }
            }

            IndexStyles(root.Styles);

            if (root.ComponentInports == null)
            {
                return styleIndex;
            }

            foreach (var import in root.ComponentInports)
            {
                IndexStyles(import.Styles);
            }

            return styleIndex;
        }

        private static Dictionary<string, LayoutNodeAst> BuildComponentIndex(WorkbookAst root, List<Issue>? issues = null)
        {
            var componentMap = new Dictionary<string, LayoutNodeAst>(StringComparer.Ordinal);

            foreach (var component in EnumerateComponents(root))
            {
                if (string.IsNullOrWhiteSpace(component.Name))
                {
                    continue;
                }

                if (!componentMap.TryAdd(component.Name, component.Body))
                {
                    issues?.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.DuplicateComponentName,
                        Message = $"Component 名が重複しています: {component.Name}",
                        Span = component.Span,
                    });
                }
            }

            return componentMap;
        }

        private static IEnumerable<ComponentAst> EnumerateComponents(WorkbookAst root)
        {
            if (root.Components != null)
            {
                foreach (var component in root.Components)
                {
                    if (component.Body != null)
                    {
                        yield return component;
                    }
                }
            }

            if (root.ComponentInports == null)
            {
                yield break;
            }

            foreach (var import in root.ComponentInports)
            {
                if (import.Components == null)
                {
                    continue;
                }

                foreach (var component in import.Components.ComponentList)
                {
                    if (component.Body != null)
                    {
                        yield return component;
                    }
                }
            }
        }

        private static IEnumerable<StyleRefAst> EnumerateAllStyleRefs(WorkbookAst root)
        {
            foreach (var sheet in root.Sheets)
            {
                foreach (var styleRef in EnumerateNestedStyleRefs(sheet.StyleRefs))
                {
                    yield return styleRef;
                }
            }

            foreach (var component in EnumerateComponents(root))
            {
                foreach (var styleRef in EnumerateNestedStyleRefs(component.StyleRefs))
                {
                    yield return styleRef;
                }
            }

            foreach (var node in EnumerateLayoutNodes(root))
            {
                foreach (var styleRef in EnumerateNestedStyleRefs(node.StyleRefs))
                {
                    yield return styleRef;
                }
            }
        }

        private static IEnumerable<StyleRefAst> EnumerateNestedStyleRefs(IEnumerable<StyleRefAst> roots)
        {
            var stack = new Stack<StyleRefAst>(roots.Reverse());
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                for (var i = current.StyleRefs.Count - 1; i >= 0; i--)
                {
                    stack.Push(current.StyleRefs[i]);
                }
            }
        }

        private static void ValidateDuplicateSheetNames(WorkbookAst root, List<Issue> issues)
        {
            var sheetNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var sheet in root.Sheets)
            {
                if (string.IsNullOrWhiteSpace(sheet.Name))
                {
                    continue;
                }

                if (!sheetNames.Add(sheet.Name))
                {
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.DuplicateSheetName,
                        Message = $"Sheet 名が重複しています: {sheet.Name}",
                        Span = sheet.Span,
                    });
                }
            }
        }

        private static void ValidateStyleReferences(
            WorkbookAst root,
            IReadOnlyDictionary<string, StyleAst> styleIndex,
            List<Issue> issues)
        {
            foreach (var sheet in root.Sheets)
            {
                ValidateStyleReferenceCollection(sheet.StyleRefs, requiresCellScope: false, styleIndex, issues);
            }

            foreach (var component in EnumerateComponents(root))
            {
                ValidateStyleReferenceCollection(component.StyleRefs, requiresCellScope: false, styleIndex, issues);
            }

            foreach (var node in EnumerateLayoutNodes(root))
            {
                var requiresCellScope = node is CellAst;
                ValidateStyleReferenceCollection(node.StyleRefs, requiresCellScope, styleIndex, issues);

                if (node is CellAst cell && !string.IsNullOrWhiteSpace(cell.StyleRefShortcut))
                {
                    ValidateStyleReferenceName(cell.StyleRefShortcut!, cell.Span, requiresCellScope, styleIndex, issues);
                }
            }
        }

        private static void ValidateStyleReferenceCollection(
            IEnumerable<StyleRefAst> roots,
            bool requiresCellScope,
            IReadOnlyDictionary<string, StyleAst> styleIndex,
            List<Issue> issues)
        {
            foreach (var styleRef in EnumerateNestedStyleRefs(roots))
            {
                ValidateStyleReferenceName(styleRef.Name, styleRef.Span, requiresCellScope, styleIndex, issues);
            }
        }

        private static void ValidateStyleReferenceName(
            string styleName,
            SourceSpan? span,
            bool requiresCellScope,
            IReadOnlyDictionary<string, StyleAst> styleIndex,
            List<Issue> issues)
        {
            if (!styleIndex.TryGetValue(styleName, out var style))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedStyle,
                    Message = $"未定義の style 参照: {styleName}",
                    Span = span,
                });
                return;
            }

            var isScopeViolation =
                (requiresCellScope && style.Scope == StyleScope.Grid) ||
                (!requiresCellScope && style.Scope == StyleScope.Cell);

            if (isScopeViolation)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Warning,
                    Kind = IssueKind.StyleScopeViolation,
                    Message = $"style '{styleName}' は {(requiresCellScope ? "cell" : "grid")} コンテキストでは使用できません。",
                    Span = span,
                });
            }
        }

        private static void ValidateComponentReferences(
            WorkbookAst root,
            IReadOnlyDictionary<string, LayoutNodeAst> componentIndex,
            List<Issue> issues)
        {
            foreach (var use in EnumerateLayoutNodes(root).OfType<UseAst>())
            {
                if (string.IsNullOrWhiteSpace(use.ComponentName))
                {
                    continue;
                }

                if (!componentIndex.ContainsKey(use.ComponentName))
                {
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.UndefinedComponent,
                        Message = $"未定義の component 参照: {use.ComponentName}",
                        Span = use.Span,
                    });
                }
            }
        }

        private static void ValidateRepeatConstraints(WorkbookAst root, List<Issue> issues)
        {
            foreach (var repeat in EnumerateLayoutNodes(root).OfType<RepeatAst>())
            {
                if (string.IsNullOrWhiteSpace(repeat.FromExprRaw))
                {
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.UndefinedRequiredAttribute,
                        Message = "<repeat> 要素に from 属性がありません。",
                        Span = repeat.Span,
                    });
                }
            }
        }

        private static void ValidateSheetOptions(WorkbookAst root, List<Issue> issues)
        {
            foreach (var sheet in root.Sheets)
            {
                if (sheet.Options == null)
                {
                    continue;
                }

                var targets = CollectSheetOptionTargets(sheet);

                ValidateSheetOptionTarget(sheet.Options.Freeze?.At, "<freeze>", sheet.Options.Freeze?.Span, targets, issues);
                ValidateSheetOptionTarget(sheet.Options.AutoFilter?.At, "<autoFilter>", sheet.Options.AutoFilter?.Span, targets, issues);

                foreach (var groupRows in sheet.Options.GroupRows)
                {
                    ValidateSheetOptionTarget(groupRows.At, "<groupRows>", groupRows.Span, targets, issues);
                }

                foreach (var groupCols in sheet.Options.GroupCols)
                {
                    ValidateSheetOptionTarget(groupCols.At, "<groupCols>", groupCols.Span, targets, issues);
                }
            }
        }

        private static HashSet<string> CollectSheetOptionTargets(SheetAst sheet)
        {
            var targets = new HashSet<string>(StringComparer.Ordinal);

            foreach (var node in EnumerateLayoutNodes(sheet.Children.Values))
            {
                if (node is UseAst use && !string.IsNullOrWhiteSpace(use.InstanceName))
                {
                    targets.Add(use.InstanceName);
                }

                if (node is RepeatAst repeat && !string.IsNullOrWhiteSpace(repeat.Name))
                {
                    targets.Add(repeat.Name);
                }
            }

            return targets;
        }

        private static void ValidateSheetOptionTarget(
            string? targetName,
            string ownerElement,
            SourceSpan? span,
            IReadOnlySet<string> availableTargets,
            List<Issue> issues)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return;
            }

            if (!availableTargets.Contains(targetName))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.SheetOptionsTargetNotFound,
                    Message = $"{ownerElement} の at 属性が参照する対象が見つかりません: {targetName}",
                    Span = span,
                });
            }
        }

        private static void ValidateStaticLayout(WorkbookAst root, List<Issue> issues)
        {
            foreach (var sheet in root.Sheets)
            {
                if (sheet.Rows <= 0 || sheet.Cols <= 0)
                {
                    continue;
                }

                foreach (var node in sheet.Children.Values)
                {
                    ValidateStaticLayoutNode(node, issues, sheet.Rows, sheet.Cols, parentRow: 1, parentCol: 1, recurseIntoChildren: true);
                }
            }
        }

        private static void ValidateStaticLayoutNode(
            LayoutNodeAst node,
            List<Issue> issues,
            int sheetRows,
            int sheetCols,
            int? parentRow,
            int? parentCol,
            bool recurseIntoChildren)
        {
            int? absoluteRow = null;
            int? absoluteCol = null;

            if (node.Placement.Row.HasValue && parentRow.HasValue)
            {
                absoluteRow = parentRow.Value + node.Placement.Row.Value - 1;
            }

            if (node.Placement.Col.HasValue && parentCol.HasValue)
            {
                absoluteCol = parentCol.Value + node.Placement.Col.Value - 1;
            }

            if (absoluteRow.HasValue && absoluteCol.HasValue)
            {
                var endRow = absoluteRow.Value + node.Placement.RowSpan - 1;
                var endCol = absoluteCol.Value + node.Placement.ColSpan - 1;

                if (absoluteRow.Value < 1 || absoluteCol.Value < 1 ||
                    endRow > sheetRows || endCol > sheetCols ||
                    endRow > MaxExcelRows || endCol > MaxExcelColumns)
                {
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.CoordinateOutOfRange,
                        Message = $"レイアウト要素の配置がシートまたは Excel の上限を超えています: r={absoluteRow.Value}, c={absoluteCol.Value}",
                        Span = node.Span,
                    });
                }
            }

            if (!recurseIntoChildren)
            {
                return;
            }

            switch (node)
            {
                case GridAst grid:
                    foreach (var child in grid.Children.Values)
                    {
                        ValidateStaticLayoutNode(child, issues, sheetRows, sheetCols, absoluteRow, absoluteCol, recurseIntoChildren: true);
                    }
                    break;

                case RepeatAst repeat when repeat.Body != null:
                    // repeat の展開回数は静的に確定しないため、子の詳細な境界計算は行わない。
                    ValidateStaticLayoutNode(repeat.Body, issues, sheetRows, sheetCols, absoluteRow, absoluteCol, recurseIntoChildren: false);
                    break;
            }
        }

        private static bool TryCreateSchemaSet(out XmlSchemaSet schemaSet, out string errorMessage)
        {
            schemaSet = new XmlSchemaSet();
            errorMessage = string.Empty;

            try
            {
                using var schemaStream = OpenSchemaStream();
                if (schemaStream == null)
                {
                    errorMessage = $"XSD が見つかりません: {SchemaRelativePath}";
                    return false;
                }

                using var schemaReader = XmlReader.Create(schemaStream);
                schemaSet.Add("urn:excelreport:v1", schemaReader);
                schemaSet.Compile();
                return true;
            }
            catch (Exception ex) when (ex is XmlException or XmlSchemaException or InvalidOperationException)
            {
                errorMessage = $"XSD の読み込みに失敗しました: {ex.Message}";
                return false;
            }
        }

        private static Stream? OpenSchemaStream()
        {
            var resourceStream = typeof(DslParser).Assembly.GetManifestResourceStream(SchemaResourceName);
            if (resourceStream != null)
            {
                return resourceStream;
            }

            foreach (var startDirectory in GetSchemaSearchRoots())
            {
                var directory = startDirectory;
                while (!string.IsNullOrWhiteSpace(directory))
                {
                    var candidate = Path.Combine(directory, SchemaRelativePath);
                    if (File.Exists(candidate))
                    {
                        return File.OpenRead(candidate);
                    }

                    directory = Path.GetDirectoryName(directory);
                }
            }

            return null;
        }

        private static IEnumerable<string> GetSchemaSearchRoots()
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(Directory.GetCurrentDirectory()))
            {
                roots.Add(Path.GetFullPath(Directory.GetCurrentDirectory()));
            }

            if (!string.IsNullOrWhiteSpace(AppContext.BaseDirectory))
            {
                roots.Add(Path.GetFullPath(AppContext.BaseDirectory));
            }

            var assemblyDirectory = Path.GetDirectoryName(typeof(DslParser).Assembly.Location);
            if (!string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                roots.Add(Path.GetFullPath(assemblyDirectory));
            }

            return roots;
        }

        private static SourceSpan? CreateSchemaViolationSpan(Exception? exception)
        {
            if (exception is not XmlSchemaValidationException validationException)
            {
                return null;
            }

            if (validationException.LineNumber <= 0 || validationException.LinePosition <= 0)
            {
                return null;
            }

            return new SourceSpan
            {
                Line = validationException.LineNumber,
                Column = validationException.LinePosition,
            };
        }
    }

    /// <summary>
    /// Represents dsl parser options.
    /// </summary>
    public sealed class DslParserOptions
    {
        /// <summary>XML スキーマ検証を有効化するか。</summary>
        public bool EnableSchemaValidation { get; init; } = true;

        /// <summary>C# 式の構文エラーを Fatal として扱うか。</summary>
        public bool TreatExpressionSyntaxErrorAsFatal { get; init; } = true;

        /// <summary>
        /// Gets or sets the root file path.
        /// </summary>
        public string? RootFilePath { get; init; }
    }

    /// <summary>
    /// Specifies issue severity values.
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// Informational note that does not indicate an invalid DSL definition.
        /// </summary>
        Info,
        /// <summary>
        /// Represents the warning option.
        /// </summary>
        Warning,
        /// <summary>
        /// Represents the error option.
        /// </summary>
        Error,
        /// <summary>
        /// Represents fatal.
        /// </summary>
        Fatal
    }

    /// <summary>
    /// Specifies issue kind values.
    /// </summary>
    public enum IssueKind
    {
        // XML レベル
        /// <summary>
        /// The input is not well-formed XML and cannot be parsed.
        /// </summary>
        XmlMalformed,
        /// <summary>
        /// Represents the schema violation option.
        /// </summary>
        SchemaViolation,

        // 定義・参照
        /// <summary>
        /// Represents the undefined required attribute option.
        /// </summary>
        UndefinedRequiredAttribute,
        /// <summary>
        /// Represents the undefined required element option.
        /// </summary>
        UndefinedRequiredElement,
        /// <summary>
        /// Represents the undefined component option.
        /// </summary>
        UndefinedComponent,
        /// <summary>
        /// Represents the undefined style option.
        /// </summary>
        UndefinedStyle,

        // ファイル以外から読み込んだ
        /// <summary>
        /// Represents the load file option.
        /// </summary>
        LoadFile,

        /// <summary>
        /// Represents the invalid attribute value option.
        /// </summary>
        InvalidAttributeValue,

        /// <summary>
        /// Represents the duplicate component name option.
        /// </summary>
        DuplicateComponentName,
        /// <summary>
        /// Represents the duplicate style name option.
        /// </summary>
        DuplicateStyleName,
        /// <summary>
        /// Represents the duplicate sheet name option.
        /// </summary>
        DuplicateSheetName,

        // スタイル
        /// <summary>
        /// Represents the style scope violation option.
        /// </summary>
        StyleScopeViolation,

        // repeat / layout
        /// <summary>
        /// Represents the repeat child count invalid option.
        /// </summary>
        RepeatChildCountInvalid,
        /// <summary>
        /// Represents the coordinate out of range option.
        /// </summary>
        CoordinateOutOfRange,
        /// <summary>
        /// A formula reference series must be a single continuous one-dimensional range.
        /// </summary>
        FormulaRefSeriesNot1DContinuous,

        // sheetOptions
        /// <summary>
        /// Represents the sheet options target not found option.
        /// </summary>
        SheetOptionsTargetNotFound,

        // 式
        /// <summary>
        /// Represents the expression syntax error option.
        /// </summary>
        ExpressionSyntaxError,
    }

    /// <summary>
    /// Represents issue.
    /// </summary>
    public sealed class Issue
    {
        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        public IssueSeverity Severity { get; init; }
        /// <summary>
        /// Gets or sets the kind.
        /// </summary>
        public IssueKind Kind { get; init; }
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }
    }

    /// <summary>
    /// Represents dsl parse result.
    /// </summary>
    public sealed class DslParseResult
    {
        /// <summary>
        /// Gets or sets the root.
        /// </summary>
        public WorkbookAst? Root { get; init; }
        /// <summary>
        /// Gets or sets the issues.
        /// </summary>
        public IReadOnlyList<Issue> Issues { get; init; } = Array.Empty<Issue>();

        /// <summary>
        /// Gets a value indicating whether fatal.
        /// </summary>
        public bool HasFatal => Issues.Any(i => i.Severity == IssueSeverity.Fatal);
    }
}
