namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Represents options for ExcelTemplate conversion-only APIs.
/// </summary>
public sealed class ExcelTemplateConvertOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether emitted text should be validated against the DSL schema.
    /// </summary>
    public bool EnableSchemaValidation { get; init; } = true;
}
