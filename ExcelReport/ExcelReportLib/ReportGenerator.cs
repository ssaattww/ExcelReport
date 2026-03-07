using ExcelReportLib.DSL;
using ExcelReportLib.ExpressionEngine;
using ExcelReportLib.LayoutEngine;
using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;
using ExcelReportLib.Styles;
using ExcelReportLib.WorksheetState;
using DefaultExpressionEngine = ExcelReportLib.ExpressionEngine.ExpressionEngine;
using DefaultLayoutEngine = ExcelReportLib.LayoutEngine.LayoutEngine;
using DefaultRenderer = ExcelReportLib.Renderer.XlsxRenderer;
using DefaultStyleResolver = ExcelReportLib.Styles.StyleResolver;
using DefaultWorksheetStateBuilder = ExcelReportLib.WorksheetState.WorksheetStateBuilder;
using WorksheetStateModel = ExcelReportLib.WorksheetState.WorksheetState;

namespace ExcelReportLib;

/// <summary>
/// Represents report generator.
/// </summary>
public sealed class ReportGenerator
{
    private readonly IExpressionEngine _expressionEngine;
    private readonly ILayoutEngine _layoutEngine;
    private readonly IWorksheetStateBuilder _worksheetStateBuilder;
    private readonly IRenderer _renderer;

    /// <summary>
    /// <see cref="ReportGenerator"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="expressionEngine">式評価エンジン。省略時は既定実装を使用します。</param>
    /// <param name="layoutEngine">レイアウト展開エンジン。省略時は既定実装を使用します。</param>
    /// <param name="worksheetStateBuilder">ワークシート状態ビルダー。省略時は既定実装を使用します。</param>
    /// <param name="renderer">レンダラー。省略時は既定実装を使用します。</param>
    public ReportGenerator(
        IExpressionEngine? expressionEngine = null,
        ILayoutEngine? layoutEngine = null,
        IWorksheetStateBuilder? worksheetStateBuilder = null,
        IRenderer? renderer = null)
    {
        _expressionEngine = expressionEngine ?? new DefaultExpressionEngine();
        _layoutEngine = layoutEngine ?? new DefaultLayoutEngine(_expressionEngine);
        _worksheetStateBuilder = worksheetStateBuilder ?? new DefaultWorksheetStateBuilder();
        _renderer = renderer ?? new DefaultRenderer();
    }

    /// <summary>
    /// DSL テキストからレポートを生成します。
    /// </summary>
    /// <param name="dsl">レポート定義 DSL。</param>
    /// <param name="data">式評価で参照するデータ。</param>
    /// <param name="options">生成オプション。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>生成結果。</returns>
    public ReportGeneratorResult Generate(
        string dsl,
        object? data,
        ReportGeneratorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveOptions = options ?? new ReportGeneratorOptions();
        var logger = effectiveOptions.Logger ?? new ReportLogger();
        var issues = new List<Issue>();

        try
        {
            if (string.IsNullOrWhiteSpace(dsl))
            {
                var issue = CreateFatalIssue("DSL text is required.");
                issues.Add(issue);
                logger.LogIssue(issue, ReportPhase.Parsing);
                return CreateResult(renderResult: null, issues, logger, abortedByFatal: true);
            }

            logger.Info("Parsing DSL.", ReportPhase.Parsing);
            var parseResult = DslParser.ParseFromText(
                dsl,
                new DslParserOptions
                {
                    EnableSchemaValidation = effectiveOptions.EnableSchemaValidation,
                    TreatExpressionSyntaxErrorAsFatal = effectiveOptions.TreatExpressionSyntaxErrorAsFatal,
                });

            return GenerateFromParsedResult(parseResult, data, effectiveOptions, logger, issues, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message, ReportPhase.Rendering);
            return CreateResult(renderResult: null, issues, logger, unhandledException: ex);
        }
    }

    /// <summary>
    /// DSL ファイルからレポートを生成します。
    /// </summary>
    /// <param name="dslFilePath">レポート定義 DSL ファイルパス。</param>
    /// <param name="data">式評価で参照するデータ。</param>
    /// <param name="options">生成オプション。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>生成結果。</returns>
    public ReportGeneratorResult GenerateFromFile(
        string dslFilePath,
        object? data,
        ReportGeneratorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveOptions = options ?? new ReportGeneratorOptions();
        var logger = effectiveOptions.Logger ?? new ReportLogger();
        var issues = new List<Issue>();

        try
        {
            if (string.IsNullOrWhiteSpace(dslFilePath))
            {
                var issue = CreateFatalIssue("DSL file path is required.", IssueKind.LoadFile);
                return CreateFatalResult(issues, logger, issue, ReportPhase.Parsing);
            }

            if (!File.Exists(dslFilePath))
            {
                var issue = CreateFatalIssue($"DSL file not found: {dslFilePath}", IssueKind.LoadFile);
                return CreateFatalResult(issues, logger, issue, ReportPhase.Parsing);
            }

            logger.Info("Parsing DSL.", ReportPhase.Parsing);
            var parseResult = DslParser.ParseFromFile(
                dslFilePath,
                new DslParserOptions
                {
                    EnableSchemaValidation = effectiveOptions.EnableSchemaValidation,
                    TreatExpressionSyntaxErrorAsFatal = effectiveOptions.TreatExpressionSyntaxErrorAsFatal,
                });

            return GenerateFromParsedResult(parseResult, data, effectiveOptions, logger, issues, cancellationToken);
        }
        catch (IOException ex)
        {
            var issue = CreateFatalIssue(ex.Message, IssueKind.LoadFile);
            return CreateFatalResult(issues, logger, issue, ReportPhase.Parsing);
        }
        catch (UnauthorizedAccessException ex)
        {
            var issue = CreateFatalIssue(ex.Message, IssueKind.LoadFile);
            return CreateFatalResult(issues, logger, issue, ReportPhase.Parsing);
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message, ReportPhase.Rendering);
            return CreateResult(renderResult: null, issues, logger, unhandledException: ex);
        }
    }

    private ReportGeneratorResult GenerateFromParsedResult(
        DslParseResult parseResult,
        object? data,
        ReportGeneratorOptions effectiveOptions,
        IReportLogger logger,
        List<Issue> issues,
        CancellationToken cancellationToken)
    {
        issues.AddRange(parseResult.Issues);
        LogIssues(logger, parseResult.Issues, ReportPhase.Parsing);

        if (parseResult.HasFatal || parseResult.Root is null)
        {
            logger.Warning("Parsing aborted due to fatal issues.", ReportPhase.Parsing);
            return CreateResult(renderResult: null, issues, logger, abortedByFatal: true);
        }

        logger.Info("Resolving styles.", ReportPhase.StyleResolving);
        IStyleResolver styleResolver = new DefaultStyleResolver(parseResult.Root.Styles);
        logger.Debug(
            $"Resolved {styleResolver.GlobalStyles.Count} global style(s).",
            ReportPhase.StyleResolving);

        logger.Info("Expanding layout.", ReportPhase.LayoutExpanding);
        var layoutPlan = _layoutEngine.Expand(parseResult.Root, data);
        issues.AddRange(layoutPlan.Issues);
        LogIssues(logger, layoutPlan.Issues, ReportPhase.LayoutExpanding);

        if (HasFatal(layoutPlan.Issues))
        {
            logger.Warning("Layout expansion aborted due to fatal issues.", ReportPhase.LayoutExpanding);
            return CreateResult(renderResult: null, issues, logger, abortedByFatal: true);
        }

        logger.Info("Building worksheet state.", ReportPhase.LayoutExpanding);
        IReadOnlyList<WorksheetStateModel> worksheets;
        try
        {
            worksheets = _worksheetStateBuilder.Build(layoutPlan);
        }
        catch (InvalidOperationException ex)
        {
            var issue = CreateFatalIssue(ex.Message, IssueKind.InvalidAttributeValue);
            return CreateFatalResult(issues, logger, issue, ReportPhase.LayoutExpanding);
        }

        logger.Info("Rendering workbook.", ReportPhase.Rendering);
        var renderResult = _renderer.Render(
            worksheets,
            effectiveOptions.RenderOptions,
            issues,
            cancellationToken);
        logger.Info("Rendering complete.", ReportPhase.Rendering);

        return CreateResult(renderResult, issues, logger);
    }

    private static bool HasFatal(IEnumerable<Issue> issues) =>
        issues.Any(issue => issue.Severity == IssueSeverity.Fatal);

    private static void LogIssues(
        IReportLogger logger,
        IEnumerable<Issue> issues,
        ReportPhase phase)
    {
        foreach (var issue in issues)
        {
            logger.LogIssue(issue, phase);
        }
    }

    private static Issue CreateFatalIssue(
        string message,
        IssueKind kind = IssueKind.InvalidAttributeValue) =>
        new()
        {
            Severity = IssueSeverity.Fatal,
            Kind = kind,
            Message = message,
        };

    private static ReportGeneratorResult CreateFatalResult(
        List<Issue> issues,
        IReportLogger logger,
        Issue issue,
        ReportPhase phase)
    {
        issues.Add(issue);
        logger.LogIssue(issue, phase);
        return CreateResult(renderResult: null, issues, logger, abortedByFatal: true);
    }

    private static ReportGeneratorResult CreateResult(
        RenderResult? renderResult,
        IReadOnlyList<Issue> issues,
        IReportLogger logger,
        bool abortedByFatal = false,
        Exception? unhandledException = null) =>
        new(
            renderResult,
            issues,
            logger.GetEntries(),
            abortedByFatal,
            unhandledException);
}
