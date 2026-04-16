namespace ExcelReportLib.ExcelTemplate.Model;

/// <summary>
/// Represents an extracted Excel template workbook.
/// </summary>
public sealed class ExcelTemplateWorkbook
{
    /// <summary>
    /// Initializes a new instance of the workbook model.
    /// </summary>
    /// <param name="sheets">The extracted sheets.</param>
    /// <param name="definedNames">The extracted defined names.</param>
    /// <param name="workbookMetaXml">The workbook-level meta XML extracted from __sheet_meta/__workbook_meta shape.</param>
    public ExcelTemplateWorkbook(
        IReadOnlyList<ExcelTemplateSheet>? sheets = null,
        IReadOnlyDictionary<string, string>? definedNames = null,
        string? workbookMetaXml = null)
    {
        Sheets = sheets?.ToArray() ?? [];
        DefinedNames = definedNames is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(definedNames, StringComparer.Ordinal);
        WorkbookMetaXml = workbookMetaXml;
    }

    /// <summary>
    /// Gets the extracted sheets.
    /// </summary>
    public IReadOnlyList<ExcelTemplateSheet> Sheets { get; }

    /// <summary>
    /// Gets the extracted workbook defined names.
    /// </summary>
    public IReadOnlyDictionary<string, string> DefinedNames { get; }

    /// <summary>
    /// Gets the workbook-level meta XML extracted from the fixed meta shape when available.
    /// </summary>
    public string? WorkbookMetaXml { get; }
}
