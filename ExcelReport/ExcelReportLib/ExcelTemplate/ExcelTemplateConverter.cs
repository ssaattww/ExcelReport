using ExcelReportLib.DSL;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Converts ExcelTemplate xlsx workbooks into DSL-compatible XML text outputs.
/// </summary>
public sealed class ExcelTemplateConverter
{
    private readonly ExcelTemplateExtractor extractor = new();
    private readonly ExcelTemplateOutputContractBuilder contractBuilder = new();
    private readonly XmlTemplateSerializer xmlTemplateSerializer = new();
    private readonly DslEmitter dslEmitter = new();

    /// <summary>
    /// Converts an xlsx workbook to DSL text.
    /// </summary>
    /// <param name="xlsxPath">The source xlsx file path.</param>
    /// <param name="options">Optional conversion settings.</param>
    /// <returns>The conversion result.</returns>
    public ExcelTemplateConversionResult ConvertToDsl(string xlsxPath, ExcelTemplateConvertOptions? options = null)
    {
        return ConvertInternal(
            xlsxPath,
            contract => dslEmitter.Emit(contract),
            options);
    }

    /// <summary>
    /// Converts an xlsx workbook to DSL-compatible XML debug text.
    /// </summary>
    /// <param name="xlsxPath">The source xlsx file path.</param>
    /// <param name="options">Optional conversion settings.</param>
    /// <returns>The conversion result.</returns>
    public ExcelTemplateConversionResult ConvertToXmlTemplate(string xlsxPath, ExcelTemplateConvertOptions? options = null)
    {
        return ConvertInternal(
            xlsxPath,
            contract => xmlTemplateSerializer.Serialize(contract).ToString(),
            options);
    }

    private ExcelTemplateConversionResult ConvertInternal(
        string xlsxPath,
        Func<Model.ExcelTemplateOutputContract, string> emitter,
        ExcelTemplateConvertOptions? options)
    {
        var effectiveOptions = options ?? new ExcelTemplateConvertOptions();
        var issues = new List<Issue>();

        if (string.IsNullOrWhiteSpace(xlsxPath))
        {
            issues.Add(CreateLoadIssue("ExcelTemplate xlsx path is required."));
            return new ExcelTemplateConversionResult(string.Empty, issues);
        }

        if (!File.Exists(xlsxPath))
        {
            issues.Add(CreateLoadIssue($"ExcelTemplate file not found: {xlsxPath}"));
            return new ExcelTemplateConversionResult(string.Empty, issues);
        }

        try
        {
            var workbook = extractor.Extract(xlsxPath);
            var contract = contractBuilder.Build(workbook);
            issues.AddRange(contract.Issues);

            var text = emitter(contract);

            if (effectiveOptions.EnableSchemaValidation)
            {
                var parseResult = DslParser.ParseFromText(
                    text,
                    new DslParserOptions { EnableSchemaValidation = true });
                issues.AddRange(parseResult.Issues);
            }

            return new ExcelTemplateConversionResult(text, issues);
        }
        catch (IOException ex)
        {
            issues.Add(CreateLoadIssue(ex.Message));
            return new ExcelTemplateConversionResult(string.Empty, issues);
        }
        catch (InvalidDataException ex)
        {
            issues.Add(CreateLoadIssue(ex.Message));
            return new ExcelTemplateConversionResult(string.Empty, issues);
        }
        catch (FileFormatException ex)
        {
            issues.Add(CreateLoadIssue(ex.Message));
            return new ExcelTemplateConversionResult(string.Empty, issues);
        }
        catch (UnauthorizedAccessException ex)
        {
            issues.Add(CreateLoadIssue(ex.Message));
            return new ExcelTemplateConversionResult(string.Empty, issues);
        }
    }

    private static Issue CreateLoadIssue(string message) =>
        new()
        {
            Severity = IssueSeverity.Fatal,
            Kind = IssueKind.LoadFile,
            Message = message,
        };
}
