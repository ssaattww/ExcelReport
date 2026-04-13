using System.Text;
using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Builds the normalized conversion output contract used by ExcelTemplate serializers and emitters.
/// </summary>
public sealed class ExcelTemplateOutputContractBuilder
{
    private const string ComponentSheetPrefix = "__component_";
    private readonly ExcelTemplateComponentRangeResolver componentRangeResolver = new();
    private readonly ExcelTemplateValidator validator = new();
    private readonly UseTriggerParser useTriggerParser = new();

    /// <summary>
    /// Builds the conversion output contract from an extracted workbook.
    /// </summary>
    /// <param name="workbook">The extracted workbook.</param>
    /// <returns>The normalized contract.</returns>
    public ExcelTemplateOutputContract Build(ExcelTemplateWorkbook workbook)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        var rangeResolution = componentRangeResolver.Resolve(workbook);
        var validation = validator.Validate(workbook, rangeResolution.Ranges);
        var aggregatedIssues = rangeResolution.Issues.Concat(validation.Issues).ToArray();
        var rangesBySheet = rangeResolution.Ranges.ToDictionary(range => range.SheetName, StringComparer.Ordinal);

        var components = workbook.Sheets
            .Where(IsComponentSheet)
            .Select(sheet =>
            {
                var hasRange = rangesBySheet.TryGetValue(sheet.Name, out var range);
                return new ExcelTemplateOutputComponent(
                    sheet.Name[ComponentSheetPrefix.Length..],
                    sheet.Name,
                    hasRange ? range!.Reference : null,
                    hasRange,
                    BuildItems(sheet, hasRange ? range!.Reference : null));
            })
            .ToArray();

        var sheets = workbook.Sheets
            .Where(sheet => !IsComponentSheet(sheet))
            .Select(sheet => new ExcelTemplateOutputSheet(sheet.Name, BuildItems(sheet, rangeReference: null)))
            .ToArray();

        return new ExcelTemplateOutputContract(components, sheets, aggregatedIssues);
    }

    private IReadOnlyList<ExcelTemplateOutputItem> BuildItems(ExcelTemplateSheet sheet, string? rangeReference)
    {
        var filter = TryParseRange(rangeReference, out var topRow, out var leftColumn, out var bottomRow, out var rightColumn);
        var items = new List<ExcelTemplateOutputItem>();

        foreach (var cell in sheet.Cells
                     .OrderBy(candidate => candidate.Row)
                     .ThenBy(candidate => candidate.Column))
        {
            if (filter && !IsWithinRange(cell, topRow, leftColumn, bottomRow, rightColumn))
            {
                continue;
            }

            items.Add(NormalizeItem(cell));
        }

        return items;
    }

    private ExcelTemplateOutputItem NormalizeItem(ExcelTemplateCell cell)
    {
        var styleIndex = cell.Style?.Index;
        var triggerResult = useTriggerParser.Parse(cell.Value);

        if (!triggerResult.IsTrigger || triggerResult.Trigger is null)
        {
            return new ExcelTemplateOutputCell(cell.Reference, cell.Row, cell.Column, styleIndex, cell.Value, cell.Formula);
        }

        if (string.IsNullOrWhiteSpace(triggerResult.Trigger.FromExpression))
        {
            return new ExcelTemplateOutputUse(
                cell.Reference,
                cell.Row,
                cell.Column,
                styleIndex,
                triggerResult.Trigger.ComponentName,
                styleOverflow: null);
        }

        return new ExcelTemplateOutputRepeatUse(
            cell.Reference,
            cell.Row,
            cell.Column,
            styleIndex,
            triggerResult.Trigger.ComponentName,
            triggerResult.Trigger.FromExpression!,
            triggerResult.Trigger.VariableName!,
            triggerResult.Trigger.RepeatDirection ?? "down",
            styleOverflow: null);
    }

    private static bool IsComponentSheet(ExcelTemplateSheet sheet) =>
        sheet.Name.StartsWith(ComponentSheetPrefix, StringComparison.Ordinal) &&
        sheet.Name.Length > ComponentSheetPrefix.Length;

    private static bool IsWithinRange(
        ExcelTemplateCell cell,
        int topRow,
        int leftColumn,
        int bottomRow,
        int rightColumn) =>
        cell.Row >= topRow &&
        cell.Row <= bottomRow &&
        cell.Column >= leftColumn &&
        cell.Column <= rightColumn;

    private static bool TryParseRange(
        string? reference,
        out int topRow,
        out int leftColumn,
        out int bottomRow,
        out int rightColumn)
    {
        topRow = 0;
        leftColumn = 0;
        bottomRow = 0;
        rightColumn = 0;

        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        var tokens = reference.Split(':', StringSplitOptions.TrimEntries);
        if (tokens.Length is < 1 or > 2 || !TryParseCellReference(tokens[0], out topRow, out leftColumn))
        {
            return false;
        }

        if (tokens.Length == 1)
        {
            bottomRow = topRow;
            rightColumn = leftColumn;
            return true;
        }

        if (!TryParseCellReference(tokens[1], out bottomRow, out rightColumn))
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
