using ExcelReportLib.Logger;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Represents options for end-to-end ExcelTemplate report generation.
/// </summary>
public sealed class ExcelTemplateGenerateOptions
{
    /// <summary>
    /// Gets or sets conversion options for the ExcelTemplate-to-DSL step.
    /// </summary>
    public ExcelTemplateConvertOptions? ConvertOptions { get; init; }

    /// <summary>
    /// Gets or sets report generation options for the DSL-to-XLSX step.
    /// </summary>
    public ReportGeneratorOptions? ReportGeneratorOptions { get; init; }
}
