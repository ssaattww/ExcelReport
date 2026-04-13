using ExcelReportLib.DSL;
using ExcelReportLib.Logger;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Generates final XLSX output directly from an ExcelTemplate workbook.
/// </summary>
public sealed class ExcelTemplateReportGenerator
{
    private readonly ExcelTemplateConverter converter;
    private readonly ReportGenerator reportGenerator;

    /// <summary>
    /// Initializes a new instance of the report generator facade.
    /// </summary>
    /// <param name="converter">The ExcelTemplate converter.</param>
    /// <param name="reportGenerator">The downstream DSL report generator.</param>
    public ExcelTemplateReportGenerator(
        ExcelTemplateConverter? converter = null,
        ReportGenerator? reportGenerator = null)
    {
        this.converter = converter ?? new ExcelTemplateConverter();
        this.reportGenerator = reportGenerator ?? new ReportGenerator();
    }

    /// <summary>
    /// Converts an ExcelTemplate workbook to DSL and renders the final XLSX through the existing report pipeline.
    /// </summary>
    /// <param name="xlsxPath">The source xlsx path.</param>
    /// <param name="data">The data object used by the DSL runtime.</param>
    /// <param name="options">Optional generation settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The aggregated report generation result.</returns>
    public ReportGeneratorResult GenerateFromExcelTemplate(
        string xlsxPath,
        object? data,
        ExcelTemplateGenerateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveOptions = options ?? new ExcelTemplateGenerateOptions();
        var reportOptions = effectiveOptions.ReportGeneratorOptions ?? new ReportGeneratorOptions();
        var logger = reportOptions.Logger ?? new ReportLogger();
        var reportOptionsWithLogger = EnsureLogger(reportOptions, logger);

        var conversionResult = converter.ConvertToDsl(xlsxPath, effectiveOptions.ConvertOptions);
        LogIssues(logger, conversionResult.Issues);

        if (HasFatal(conversionResult.Issues) || string.IsNullOrWhiteSpace(conversionResult.Text))
        {
            return new ReportGeneratorResult(
                renderResult: null,
                issues: conversionResult.Issues,
                logEntries: logger.GetEntries(),
                abortedByFatal: true);
        }

        var reportResult = reportGenerator.Generate(
            conversionResult.Text,
            data,
            reportOptionsWithLogger,
            cancellationToken);

        return new ReportGeneratorResult(
            reportResult.RenderResult,
            conversionResult.Issues.Concat(reportResult.Issues).ToArray(),
            reportResult.LogEntries,
            reportResult.AbortedByFatal,
            reportResult.UnhandledException);
    }

    private static ReportGeneratorOptions EnsureLogger(ReportGeneratorOptions options, IReportLogger logger)
    {
        if (ReferenceEquals(options.Logger, logger))
        {
            return options;
        }

        return new ReportGeneratorOptions
        {
            EnableSchemaValidation = options.EnableSchemaValidation,
            TreatExpressionSyntaxErrorAsFatal = options.TreatExpressionSyntaxErrorAsFatal,
            Logger = logger,
            RenderOptions = options.RenderOptions,
        };
    }

    private static bool HasFatal(IEnumerable<Issue> issues) =>
        issues.Any(issue => issue.Severity == IssueSeverity.Fatal);

    private static void LogIssues(IReportLogger logger, IEnumerable<Issue> issues)
    {
        foreach (var issue in issues)
        {
            logger.LogIssue(issue, ReportPhase.Parsing);
        }
    }
}
