using System.Text;
using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Resolves component ranges from extracted Excel template workbooks.
/// </summary>
public sealed class ExcelTemplateComponentRangeResolver
{
    private const string ComponentSheetPrefix = "__component_";
    private const string ComponentRangeNamePrefix = "__component_range_";

    /// <summary>
    /// Resolves component ranges for all component sheets in the workbook.
    /// </summary>
    /// <param name="workbook">The extracted workbook.</param>
    /// <returns>The resolved ranges and issues.</returns>
    public ExcelTemplateComponentRangeResolutionResult Resolve(ExcelTemplateWorkbook workbook)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        var ranges = new List<ExcelTemplateComponentRange>();
        var issues = new List<Issue>();

        foreach (var sheet in workbook.Sheets.Where(IsComponentSheet))
        {
            var componentName = sheet.Name[ComponentSheetPrefix.Length..];
            var definedName = ComponentRangeNamePrefix + componentName;

            if (workbook.DefinedNames.TryGetValue(definedName, out var explicitReference))
            {
                if (TryResolveExplicitRange(
                        sheet,
                        componentName,
                        explicitReference,
                        out var rangeReference,
                        out var explicitIssueKind,
                        out var explicitMessage))
                {
                    ranges.Add(new ExcelTemplateComponentRange(componentName, sheet.Name, rangeReference));
                }
                else
                {
                    issues.Add(new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = explicitIssueKind,
                        Message = explicitMessage,
                    });
                }

                continue;
            }

            if (TryResolveAutoRange(sheet, out var autoReference))
            {
                ranges.Add(new ExcelTemplateComponentRange(componentName, sheet.Name, autoReference));
                continue;
            }

            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.EmptyComponentRange,
                Message = $"component '{componentName}' does not contain any candidate cells for range detection.",
            });
        }

        return new ExcelTemplateComponentRangeResolutionResult(ranges, issues);
    }

    private static bool IsComponentSheet(ExcelTemplateSheet sheet) =>
        sheet.Name.StartsWith(ComponentSheetPrefix, StringComparison.Ordinal) &&
        sheet.Name.Length > ComponentSheetPrefix.Length;

    private static bool TryResolveExplicitRange(
        ExcelTemplateSheet sheet,
        string componentName,
        string explicitReference,
        out string normalizedReference,
        out IssueKind issueKind,
        out string message)
    {
        normalizedReference = string.Empty;
        issueKind = IssueKind.InvalidComponentRange;
        message = string.Empty;

        if (!TryParseSheetQualifiedRange(
                explicitReference,
                out var actualSheetName,
                out var topRow,
                out var leftColumn,
                out var bottomRow,
                out var rightColumn))
        {
            message = $"component '{componentName}' has invalid explicit range: {explicitReference}";
            return false;
        }

        if (!string.Equals(sheet.Name, actualSheetName, StringComparison.Ordinal))
        {
            message = $"component '{componentName}' explicit range points to '{actualSheetName}' instead of '{sheet.Name}'.";
            return false;
        }

        if (topRow <= 0 || leftColumn <= 0 || bottomRow < topRow || rightColumn < leftColumn)
        {
            message = $"component '{componentName}' has invalid explicit range: {explicitReference}";
            return false;
        }

        if (!HasCandidateInRange(sheet, topRow, leftColumn, bottomRow, rightColumn))
        {
            issueKind = IssueKind.EmptyComponentRange;
            message = $"component '{componentName}' explicit range does not contain any candidate cells.";
            return false;
        }

        normalizedReference = ToRangeReference(topRow, leftColumn, bottomRow, rightColumn);
        return true;
    }

    private static bool TryResolveAutoRange(
        ExcelTemplateSheet sheet,
        out string reference)
    {
        reference = string.Empty;

        var minRow = int.MaxValue;
        var minColumn = int.MaxValue;
        var maxRow = 0;
        var maxColumn = 0;

        foreach (var cell in sheet.Cells.Where(IsCandidateCell))
        {
            minRow = Math.Min(minRow, cell.Row);
            minColumn = Math.Min(minColumn, cell.Column);
            maxRow = Math.Max(maxRow, cell.Row);
            maxColumn = Math.Max(maxColumn, cell.Column);
        }

        foreach (var mergedRange in sheet.MergedRanges)
        {
            if (!TryParseLocalRange(mergedRange, out var topRow, out var leftColumn, out var bottomRow, out var rightColumn))
            {
                continue;
            }

            minRow = Math.Min(minRow, topRow);
            minColumn = Math.Min(minColumn, leftColumn);
            maxRow = Math.Max(maxRow, bottomRow);
            maxColumn = Math.Max(maxColumn, rightColumn);
        }

        if (maxRow == 0 || maxColumn == 0)
        {
            return false;
        }

        reference = ToRangeReference(minRow, minColumn, maxRow, maxColumn);
        return true;
    }

    private static bool IsCandidateCell(ExcelTemplateCell cell) =>
        !string.IsNullOrWhiteSpace(cell.Value) ||
        !string.IsNullOrWhiteSpace(cell.Formula) ||
        cell.Style is not null;

    private static bool HasCandidateInRange(
        ExcelTemplateSheet sheet,
        int topRow,
        int leftColumn,
        int bottomRow,
        int rightColumn)
    {
        if (sheet.Cells.Any(cell =>
                IsCandidateCell(cell) &&
                cell.Row >= topRow &&
                cell.Row <= bottomRow &&
                cell.Column >= leftColumn &&
                cell.Column <= rightColumn))
        {
            return true;
        }

        foreach (var mergedRange in sheet.MergedRanges)
        {
            if (!TryParseLocalRange(mergedRange, out var mergedTopRow, out var mergedLeftColumn, out var mergedBottomRow, out var mergedRightColumn))
            {
                continue;
            }

            var intersects =
                mergedBottomRow >= topRow &&
                mergedTopRow <= bottomRow &&
                mergedRightColumn >= leftColumn &&
                mergedLeftColumn <= rightColumn;
            if (intersects)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseSheetQualifiedRange(
        string reference,
        out string sheetName,
        out int topRow,
        out int leftColumn,
        out int bottomRow,
        out int rightColumn)
    {
        sheetName = string.Empty;
        topRow = 0;
        leftColumn = 0;
        bottomRow = 0;
        rightColumn = 0;

        var separatorIndex = reference.LastIndexOf('!');
        if (separatorIndex <= 0 || separatorIndex == reference.Length - 1)
        {
            return false;
        }

        sheetName = NormalizeSheetName(reference[..separatorIndex]);
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            return false;
        }

        return TryParseLocalRange(reference[(separatorIndex + 1)..], out topRow, out leftColumn, out bottomRow, out rightColumn);
    }

    private static string NormalizeSheetName(string token)
    {
        var trimmed = token.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '\'' && trimmed[^1] == '\'')
        {
            return trimmed[1..^1].Replace("''", "'", StringComparison.Ordinal);
        }

        return trimmed;
    }

    private static bool TryParseLocalRange(
        string reference,
        out int topRow,
        out int leftColumn,
        out int bottomRow,
        out int rightColumn)
    {
        topRow = 0;
        leftColumn = 0;
        bottomRow = 0;
        rightColumn = 0;

        if (string.IsNullOrWhiteSpace(reference) || reference.Contains(',', StringComparison.Ordinal))
        {
            return false;
        }

        var parts = reference.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2)
        {
            return false;
        }

        if (!TryParseCellReference(parts[0], out topRow, out leftColumn))
        {
            return false;
        }

        if (parts.Length == 1)
        {
            bottomRow = topRow;
            rightColumn = leftColumn;
            return true;
        }

        if (!TryParseCellReference(parts[1], out bottomRow, out rightColumn))
        {
            return false;
        }

        return bottomRow >= topRow && rightColumn >= leftColumn;
    }

    private static bool TryParseCellReference(string token, out int row, out int column)
    {
        row = 0;
        column = 0;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var trimmed = token.Trim().Replace("$", string.Empty, StringComparison.Ordinal);
        var letters = new StringBuilder();
        var digits = new StringBuilder();

        foreach (var character in trimmed)
        {
            if (char.IsLetter(character))
            {
                if (digits.Length > 0)
                {
                    return false;
                }

                letters.Append(char.ToUpperInvariant(character));
                continue;
            }

            if (!char.IsDigit(character))
            {
                return false;
            }

            digits.Append(character);
        }

        if (letters.Length == 0 ||
            digits.Length == 0 ||
            !int.TryParse(digits.ToString(), out row))
        {
            return false;
        }

        column = 0;
        foreach (var character in letters.ToString())
        {
            column = checked((column * 26) + (character - 'A' + 1));
        }

        return column > 0;
    }

    private static string ToRangeReference(int topRow, int leftColumn, int bottomRow, int rightColumn)
    {
        var start = ToCellReference(topRow, leftColumn);
        var end = ToCellReference(bottomRow, rightColumn);
        return start == end ? start : $"{start}:{end}";
    }

    private static string ToCellReference(int row, int column)
    {
        var current = column;
        var builder = new StringBuilder();
        while (current > 0)
        {
            current--;
            builder.Insert(0, (char)('A' + (current % 26)));
            current /= 26;
        }

        return builder.Append(row).ToString();
    }
}

/// <summary>
/// Represents component range resolution output.
/// </summary>
public sealed class ExcelTemplateComponentRangeResolutionResult
{
    /// <summary>
    /// Initializes a new instance of the resolution result.
    /// </summary>
    /// <param name="ranges">The resolved component ranges.</param>
    /// <param name="issues">The collected issues.</param>
    public ExcelTemplateComponentRangeResolutionResult(
        IReadOnlyList<ExcelTemplateComponentRange>? ranges = null,
        IReadOnlyList<Issue>? issues = null)
    {
        Ranges = ranges?.ToArray() ?? [];
        Issues = issues?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the resolved ranges.
    /// </summary>
    public IReadOnlyList<ExcelTemplateComponentRange> Ranges { get; }

    /// <summary>
    /// Gets the collected issues.
    /// </summary>
    public IReadOnlyList<Issue> Issues { get; }
}
