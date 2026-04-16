using System.Text;
using System.Xml;
using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Builds the normalized conversion output contract used by ExcelTemplate serializers and emitters.
/// </summary>
public sealed class ExcelTemplateOutputContractBuilder
{
    private const string ComponentSheetPrefix = "__component_";
    private const string SheetMetaSheetName = "__sheet_meta";
    private const string WorkbookMetaShapeName = "__workbook_meta";
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
        var workbookMetaResolution = ResolveWorkbookMeta(workbook);
        var aggregatedIssues = rangeResolution.Issues
            .Concat(validation.Issues)
            .Concat(variableScope.Issues)
            .Concat(workbookMetaResolution.Issues)
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

        var sheets = BuildSheets(workbook, workbookMetaResolution);

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

    private IReadOnlyList<ExcelTemplateOutputSheet> BuildSheets(
        ExcelTemplateWorkbook workbook,
        WorkbookMetaResolution workbookMetaResolution)
    {
        var definitionsByTemplateSheet = workbookMetaResolution.SheetDefinitions
            .ToDictionary(definition => definition.TemplateSheetName, StringComparer.Ordinal);
        var outputSheets = new List<ExcelTemplateOutputSheet>();

        foreach (var sheet in workbook.Sheets)
        {
            if (IsComponentSheet(sheet) || IsMetaSheet(sheet))
            {
                continue;
            }

            if (!definitionsByTemplateSheet.TryGetValue(sheet.Name, out var definition))
            {
                outputSheets.Add(new ExcelTemplateOutputSheet(sheet.Name, BuildItems(sheet, rangeReference: null, EmptyVariableNames)));
                continue;
            }

            var localVariableNames = ResolveSheetLocalVariableNames(definition);
            outputSheets.Add(
                new ExcelTemplateOutputSheet(
                    definition.SheetName,
                    BuildItems(sheet, rangeReference: null, localVariableNames),
                    definition.FromExpression,
                    definition.VariableName));
        }

        return outputSheets;
    }

    private static IReadOnlySet<string> ResolveSheetLocalVariableNames(WorkbookMetaSheetDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.FromExpression))
        {
            return EmptyVariableNames;
        }

        var effectiveVariableName = string.IsNullOrWhiteSpace(definition.VariableName)
            ? "item"
            : definition.VariableName;
        return new HashSet<string>(StringComparer.Ordinal) { effectiveVariableName };
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

    private WorkbookMetaResolution ResolveWorkbookMeta(ExcelTemplateWorkbook workbook)
    {
        var issues = new List<Issue>();

        if (!workbook.Sheets.Any(IsMetaSheet))
        {
            return new WorkbookMetaResolution([], issues);
        }

        if (string.IsNullOrWhiteSpace(workbook.WorkbookMetaXml))
        {
            issues.Add(
                new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredElement,
                    Message = $"sheet '{SheetMetaSheetName}' is missing fixed shape '{WorkbookMetaShapeName}'.",
                });
            return new WorkbookMetaResolution([], issues);
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(workbook.WorkbookMetaXml);
        }
        catch (Exception ex) when (ex is XmlException or ArgumentException)
        {
            issues.Add(
                new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.XmlMalformed,
                    Message = $"sheet '{SheetMetaSheetName}' fixed shape '{WorkbookMetaShapeName}' contains malformed XML: {ex.Message}",
                });
            return new WorkbookMetaResolution([], issues);
        }

        var root = document.Root;
        if (root is null || !string.Equals(root.Name.LocalName, "workbook", StringComparison.Ordinal))
        {
            issues.Add(
                new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = $"sheet '{SheetMetaSheetName}' fixed shape '{WorkbookMetaShapeName}' root must be <workbook>.",
                });
            return new WorkbookMetaResolution([], issues);
        }

        var sheetsElement = root.Elements().FirstOrDefault(element => element.Name.LocalName == "sheets");
        var sheetElements = sheetsElement?.Elements().Where(element => element.Name.LocalName == "sheet").ToArray() ?? [];
        if (sheetElements.Length == 0)
        {
            issues.Add(
                new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.UndefinedRequiredElement,
                    Message = $"sheet '{SheetMetaSheetName}' fixed shape '{WorkbookMetaShapeName}' must contain workbook/sheets/sheet.",
                });
            return new WorkbookMetaResolution([], issues);
        }

        var availableTemplateSheets = workbook.Sheets
            .Where(sheet => !IsMetaSheet(sheet) && !IsComponentSheet(sheet))
            .Select(sheet => sheet.Name)
            .ToHashSet(StringComparer.Ordinal);
        var templateSheetNames = new HashSet<string>(StringComparer.Ordinal);
        var definitions = new List<WorkbookMetaSheetDefinition>();

        foreach (var sheetElement in sheetElements)
        {
            var templateSheet = sheetElement.Attribute("templateSheet")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(templateSheet))
            {
                issues.Add(
                    new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.UndefinedRequiredAttribute,
                        Message = "workbook/sheets/sheet requires templateSheet attribute.",
                    });
                continue;
            }

            if (!templateSheetNames.Add(templateSheet))
            {
                issues.Add(
                    new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.InvalidAttributeValue,
                        Message = $"workbook/sheets/sheet templateSheet '{templateSheet}' is duplicated.",
                    });
                continue;
            }

            if (!availableTemplateSheets.Contains(templateSheet))
            {
                issues.Add(
                    new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.InvalidAttributeValue,
                        Message = $"workbook/sheets/sheet templateSheet '{templateSheet}' was not found in workbook sheets.",
                    });
                continue;
            }

            var name = sheetElement.Attribute("name")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                issues.Add(
                    new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.UndefinedRequiredAttribute,
                        Message = $"workbook/sheets/sheet templateSheet '{templateSheet}' requires name attribute.",
                    });
                continue;
            }

            var from = sheetElement.Attribute("from")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(from))
            {
                from = null;
            }

            var variableName = sheetElement.Attribute("var")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(variableName))
            {
                variableName = null;
            }

            if (!string.IsNullOrWhiteSpace(variableName) && string.IsNullOrWhiteSpace(from))
            {
                issues.Add(
                    new Issue
                    {
                        Severity = IssueSeverity.Error,
                        Kind = IssueKind.InvalidAttributeValue,
                        Message = $"workbook/sheets/sheet templateSheet '{templateSheet}' cannot specify var without from.",
                    });
                continue;
            }

            var nameVariableScope = string.IsNullOrWhiteSpace(variableName)
                ? EmptyVariableNames
                : new HashSet<string>(StringComparer.Ordinal) { variableName };
            definitions.Add(
                new WorkbookMetaSheetDefinition(
                    templateSheet,
                    ExcelTemplateExpressionNormalizer.Normalize(name, nameVariableScope)!,
                    ExcelTemplateExpressionNormalizer.Normalize(from),
                    variableName));
        }

        return new WorkbookMetaResolution(definitions, issues);
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

    private sealed class WorkbookMetaResolution
    {
        public WorkbookMetaResolution(
            IReadOnlyList<WorkbookMetaSheetDefinition> sheetDefinitions,
            IReadOnlyList<Issue> issues)
        {
            SheetDefinitions = sheetDefinitions;
            Issues = issues;
        }

        public IReadOnlyList<WorkbookMetaSheetDefinition> SheetDefinitions { get; }

        public IReadOnlyList<Issue> Issues { get; }
    }

    private sealed class WorkbookMetaSheetDefinition
    {
        public WorkbookMetaSheetDefinition(
            string templateSheetName,
            string sheetName,
            string? fromExpression,
            string? variableName)
        {
            TemplateSheetName = templateSheetName;
            SheetName = sheetName;
            FromExpression = fromExpression;
            VariableName = variableName;
        }

        public string TemplateSheetName { get; }

        public string SheetName { get; }

        public string? FromExpression { get; }

        public string? VariableName { get; }
    }

    private static bool IsComponentSheet(ExcelTemplateSheet sheet) =>
        sheet.Name.StartsWith(ComponentSheetPrefix, StringComparison.Ordinal) &&
        sheet.Name.Length > ComponentSheetPrefix.Length;

    private static bool IsMetaSheet(ExcelTemplateSheet sheet) =>
        string.Equals(sheet.Name, SheetMetaSheetName, StringComparison.Ordinal);

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
