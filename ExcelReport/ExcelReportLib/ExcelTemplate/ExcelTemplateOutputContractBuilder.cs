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
    private static readonly IReadOnlySet<string> EmptyVariableNames = new HashSet<string>(StringComparer.Ordinal);
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
        var variableScope = ResolveComponentVariableScopes(workbook);
        var aggregatedIssues = rangeResolution.Issues
            .Concat(validation.Issues)
            .Concat(variableScope.Issues)
            .ToArray();
        var rangesBySheet = rangeResolution.Ranges.ToDictionary(range => range.SheetName, StringComparer.Ordinal);

        var components = workbook.Sheets
            .Where(IsComponentSheet)
            .Select(sheet =>
            {
                var hasRange = rangesBySheet.TryGetValue(sheet.Name, out var range);
                var componentName = sheet.Name[ComponentSheetPrefix.Length..];
                var valueVariableNames = variableScope.ComponentVariableNames.TryGetValue(componentName, out var variables)
                    ? variables
                    : EmptyVariableNames;
                return new ExcelTemplateOutputComponent(
                    componentName,
                    sheet.Name,
                    hasRange ? range!.Reference : null,
                    hasRange,
                    BuildItems(sheet, hasRange ? range!.Reference : null, valueVariableNames));
            })
            .ToArray();

        var sheets = workbook.Sheets
            .Where(sheet => !IsComponentSheet(sheet))
            .Select(sheet => new ExcelTemplateOutputSheet(sheet.Name, BuildItems(sheet, rangeReference: null, EmptyVariableNames)))
            .ToArray();

        return new ExcelTemplateOutputContract(components, sheets, aggregatedIssues);
    }

    private IReadOnlyList<ExcelTemplateOutputItem> BuildItems(
        ExcelTemplateSheet sheet,
        string? rangeReference,
        IReadOnlySet<string> localVariableNames)
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

            items.Add(NormalizeItem(cell, localVariableNames));
        }

        return items;
    }

    private ExcelTemplateOutputItem NormalizeItem(ExcelTemplateCell cell, IReadOnlySet<string> localVariableNames)
    {
        var styleIndex = cell.Style?.Index;
        var triggerResult = useTriggerParser.Parse(cell.Value);

        if (!triggerResult.IsTrigger || triggerResult.Trigger is null)
        {
            return new ExcelTemplateOutputCell(
                cell.Reference,
                cell.Row,
                cell.Column,
                styleIndex,
                ExcelTemplateExpressionNormalizer.Normalize(cell.Value, localVariableNames),
                cell.Formula);
        }

        if (string.IsNullOrWhiteSpace(triggerResult.Trigger.FromExpression))
        {
            return new ExcelTemplateOutputUse(
                cell.Reference,
                cell.Row,
                cell.Column,
                styleIndex,
                triggerResult.Trigger.ComponentName,
                triggerResult.Trigger.StyleOverflow);
        }

        return new ExcelTemplateOutputRepeatUse(
            cell.Reference,
            cell.Row,
            cell.Column,
            styleIndex,
            triggerResult.Trigger.ComponentName,
            ExcelTemplateExpressionNormalizer.Normalize(triggerResult.Trigger.FromExpression)!,
            triggerResult.Trigger.VariableName!,
            triggerResult.Trigger.RepeatDirection ?? "down",
            triggerResult.Trigger.StyleOverflow);
    }

    private ComponentVariableScopeResolution ResolveComponentVariableScopes(ExcelTemplateWorkbook workbook)
    {
        var discoveredVariables = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var componentsWithSimpleShorthand = CollectComponentsWithSimpleShorthand(workbook);
        var issues = new List<Issue>();
        var componentVariableNames = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

        foreach (var cell in workbook.Sheets.SelectMany(sheet => sheet.Cells))
        {
            var triggerResult = useTriggerParser.Parse(cell.Value);
            if (!triggerResult.IsTrigger ||
                triggerResult.Trigger is null ||
                string.IsNullOrWhiteSpace(triggerResult.Trigger.ComponentName) ||
                string.IsNullOrWhiteSpace(triggerResult.Trigger.VariableName))
            {
                continue;
            }

            if (!discoveredVariables.TryGetValue(triggerResult.Trigger.ComponentName, out var variableNames))
            {
                variableNames = new HashSet<string>(StringComparer.Ordinal);
                discoveredVariables[triggerResult.Trigger.ComponentName] = variableNames;
            }

            variableNames.Add(triggerResult.Trigger.VariableName);
        }

        foreach (var discovered in discoveredVariables)
        {
            if (discovered.Value.Count == 1)
            {
                componentVariableNames[discovered.Key] = discovered.Value;
                continue;
            }

            componentVariableNames[discovered.Key] = EmptyVariableNames;
            if (componentsWithSimpleShorthand.Contains(discovered.Key))
            {
                issues.Add(
                    new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.InvalidAttributeValue,
                        Message = $"component '{discovered.Key}' is referenced by multiple repeat variables ({string.Join(", ", discovered.Value.OrderBy(value => value, StringComparer.Ordinal))}); simple shorthand local variable normalization is ambiguous.",
                    });
            }
        }

        return new ComponentVariableScopeResolution(componentVariableNames, issues);
    }

    private HashSet<string> CollectComponentsWithSimpleShorthand(ExcelTemplateWorkbook workbook)
    {
        var components = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sheet in workbook.Sheets.Where(IsComponentSheet))
        {
            var componentName = sheet.Name[ComponentSheetPrefix.Length..];
            var hasSimpleShorthand = sheet.Cells.Any(
                cell =>
                {
                    var triggerResult = useTriggerParser.Parse(cell.Value);
                    return (!triggerResult.IsTrigger || triggerResult.Trigger is null) && IsSimpleShorthandExpression(cell.Value);
                });
            if (hasSimpleShorthand)
            {
                components.Add(componentName);
            }
        }

        return components;
    }

    private static bool IsSimpleShorthandExpression(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('@') || trimmed.StartsWith("@(", StringComparison.Ordinal))
        {
            return false;
        }

        var body = trimmed[1..].Trim();
        return body.Length > 0 && body.All(character => char.IsLetterOrDigit(character) || character == '_');
    }

    private sealed class ComponentVariableScopeResolution
    {
        public ComponentVariableScopeResolution(
            IReadOnlyDictionary<string, IReadOnlySet<string>> componentVariableNames,
            IReadOnlyList<Issue> issues)
        {
            ComponentVariableNames = componentVariableNames;
            Issues = issues;
        }

        public IReadOnlyDictionary<string, IReadOnlySet<string>> ComponentVariableNames { get; }

        public IReadOnlyList<Issue> Issues { get; }
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
