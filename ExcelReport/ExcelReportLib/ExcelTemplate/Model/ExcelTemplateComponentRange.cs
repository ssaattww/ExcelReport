namespace ExcelReportLib.ExcelTemplate.Model;

/// <summary>
/// Represents a resolved component range in an Excel template workbook.
/// </summary>
public sealed class ExcelTemplateComponentRange
{
    /// <summary>
    /// Initializes a new instance of the component range model.
    /// </summary>
    /// <param name="componentName">The logical component name.</param>
    /// <param name="sheetName">The source sheet name.</param>
    /// <param name="reference">The A1 range reference.</param>
    public ExcelTemplateComponentRange(string componentName, string sheetName, string reference)
    {
        ComponentName = componentName;
        SheetName = sheetName;
        Reference = reference;
    }

    /// <summary>
    /// Gets the logical component name.
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// Gets the source sheet name.
    /// </summary>
    public string SheetName { get; }

    /// <summary>
    /// Gets the A1 range reference.
    /// </summary>
    public string Reference { get; }
}
