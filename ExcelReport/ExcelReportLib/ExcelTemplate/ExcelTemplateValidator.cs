using System.Text;
using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Validates extracted ExcelTemplate workbooks against initial-release constraints.
/// </summary>
public sealed class ExcelTemplateValidator
{
    private readonly UseTriggerParser useTriggerParser = new();

    /// <summary>
    /// Validates the workbook and resolved component ranges.
    /// </summary>
    /// <param name="workbook">The extracted workbook.</param>
    /// <param name="componentRanges">The resolved component ranges.</param>
    /// <returns>The validation result.</returns>
    public ExcelTemplateValidationResult Validate(
        ExcelTemplateWorkbook workbook,
        IReadOnlyList<ExcelTemplateComponentRange> componentRanges)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(componentRanges);

        var issues = new List<Issue>();
        var rangesBySheet = componentRanges.ToDictionary(range => range.SheetName, StringComparer.Ordinal);
        var componentNames = new HashSet<string>(componentRanges.Select(range => range.ComponentName), StringComparer.Ordinal);

        foreach (var sheet in workbook.Sheets)
        {
            ValidateUnsupportedFeatures(sheet, issues);
            ValidateUseTriggers(sheet, componentNames, issues);

            if (rangesBySheet.TryGetValue(sheet.Name, out var componentRange))
            {
                ValidateMergedRanges(sheet, componentRange, issues);
            }
        }

        return new ExcelTemplateValidationResult(issues);
    }

    private void ValidateUnsupportedFeatures(ExcelTemplateSheet sheet, IList<Issue> issues)
    {
        if (!sheet.HasConditionalFormatting)
        {
            return;
        }

        issues.Add(new Issue
        {
            Severity = IssueSeverity.Error,
            Kind = IssueKind.UnsupportedExcelTemplateFeature,
            Message = $"sheet '{sheet.Name}' uses conditional formatting, which is unsupported in the initial ExcelTemplate release.",
        });
    }

    private void ValidateUseTriggers(
        ExcelTemplateSheet sheet,
        ISet<string> componentNames,
        IList<Issue> issues)
    {
        foreach (var cell in sheet.Cells)
        {
            var result = useTriggerParser.Parse(cell.Value);
            if (!result.IsTrigger)
            {
                continue;
            }

            if (result.Issues.Count > 0)
            {
                foreach (var issue in result.Issues)
                {
                    issues.Add(new Issue
                    {
                        Severity = issue.Severity,
                        Kind = issue.Kind,
                        Message = $"sheet '{sheet.Name}' cell {cell.Reference}: {issue.Message}",
                    });
                }

                continue;
            }

            if (result.Trigger is not null && !componentNames.Contains(result.Trigger.ComponentName))
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedComponent,
                    Message = $"sheet '{sheet.Name}' cell {cell.Reference} references undefined component '{result.Trigger.ComponentName}'.",
                });
            }
        }
    }

    private static void ValidateMergedRanges(
        ExcelTemplateSheet sheet,
        ExcelTemplateComponentRange componentRange,
        IList<Issue> issues)
    {
        if (!TryParseRangeReference(componentRange.Reference, out var topRow, out var leftColumn, out var bottomRow, out var rightColumn))
        {
            return;
        }

        foreach (var mergedRange in sheet.MergedRanges)
        {
            if (!TryParseRangeReference(mergedRange, out var mergedTop, out var mergedLeft, out var mergedBottom, out var mergedRight))
            {
                continue;
            }

            var intersects =
                mergedBottom >= topRow &&
                mergedTop <= bottomRow &&
                mergedRight >= leftColumn &&
                mergedLeft <= rightColumn;
            var contained =
                mergedTop >= topRow &&
                mergedBottom <= bottomRow &&
                mergedLeft >= leftColumn &&
                mergedRight <= rightColumn;

            if (intersects && !contained)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.MergedCellBoundaryViolation,
                    Message = $"component sheet '{sheet.Name}' has merged range '{mergedRange}' crossing resolved component range '{componentRange.Reference}'.",
                });
            }
        }
    }

    private static bool TryParseRangeReference(
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
        if (parts.Length is < 1 or > 2 || !TryParseCellReference(parts[0], out topRow, out leftColumn))
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

        if (letters.Length == 0 || digits.Length == 0 || !int.TryParse(digits.ToString(), out row))
        {
            return false;
        }

        foreach (var character in letters.ToString())
        {
            column = checked((column * 26) + (character - 'A' + 1));
        }

        return column > 0;
    }
}

/// <summary>
/// Represents ExcelTemplate validation output.
/// </summary>
public sealed class ExcelTemplateValidationResult
{
    /// <summary>
    /// Initializes a new instance of the validation result.
    /// </summary>
    /// <param name="issues">The collected issues.</param>
    public ExcelTemplateValidationResult(IReadOnlyList<Issue>? issues = null)
    {
        Issues = issues?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the collected issues.
    /// </summary>
    public IReadOnlyList<Issue> Issues { get; }
}
