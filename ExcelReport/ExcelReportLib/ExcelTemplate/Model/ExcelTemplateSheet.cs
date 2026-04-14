namespace ExcelReportLib.ExcelTemplate.Model;

/// <summary>
/// Represents an extracted Excel template sheet.
/// </summary>
public sealed class ExcelTemplateSheet
{
    /// <summary>
    /// Initializes a new instance of the sheet model.
    /// </summary>
    /// <param name="name">The sheet name.</param>
    /// <param name="cells">The extracted cells.</param>
    /// <param name="mergedRanges">The extracted merged ranges.</param>
    /// <param name="hasConditionalFormatting">Whether the sheet contains conditional formatting.</param>
    public ExcelTemplateSheet(
        string name,
        IReadOnlyList<ExcelTemplateCell>? cells = null,
        IReadOnlyList<string>? mergedRanges = null,
        bool hasConditionalFormatting = false)
    {
        Name = name;
        Cells = cells?.ToArray() ?? [];
        MergedRanges = mergedRanges?.ToArray() ?? [];
        HasConditionalFormatting = hasConditionalFormatting;
    }

    /// <summary>
    /// Gets the sheet name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the extracted cells.
    /// </summary>
    public IReadOnlyList<ExcelTemplateCell> Cells { get; }

    /// <summary>
    /// Gets the merged range references.
    /// </summary>
    public IReadOnlyList<string> MergedRanges { get; }

    /// <summary>
    /// Gets a value indicating whether the sheet contains conditional formatting.
    /// </summary>
    public bool HasConditionalFormatting { get; }
}
