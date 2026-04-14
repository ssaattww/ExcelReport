using System.Collections;
using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;
using ExcelReportLib.ExpressionEngine;
using ExcelReportLib.Styles;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExcelReportLib.LayoutEngine;

/// <summary>
/// Represents layout engine.
/// </summary>
public sealed class LayoutEngine : ILayoutEngine
{
    private const int MaxExcelRows = 1_048_576;
    private const int MaxExcelColumns = 16_384;

    private readonly IExpressionEngine _expressionEngine;

    /// <summary>
    /// Initializes a new instance of the layout engine type.
    /// </summary>
    public LayoutEngine()
        : this(new ExpressionEngine.ExpressionEngine())
    {
    }

    /// <summary>
    /// Initializes a new instance of the layout engine type.
    /// </summary>
    /// <param name="expressionEngine">The expression engine.</param>
    public LayoutEngine(IExpressionEngine expressionEngine)
    {
        _expressionEngine = expressionEngine ?? throw new ArgumentNullException(nameof(expressionEngine));
    }

    /// <summary>
    /// Expands workbook layout nodes into concrete sheet plans.
    /// </summary>
    /// <param name="workbook">The workbook.</param>
    /// <param name="rootData">The root data.</param>
    /// <returns>The resulting layout plan.</returns>
    public LayoutPlan Expand(WorkbookAst workbook, object? rootData)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        var issues = new List<Issue>();
        var chartPalette = BuildChartPalette(workbook.ChartPalette);
        var styleResolver = new StyleResolver(workbook.Styles);
        var componentIndex = BuildComponentIndex(workbook);
        var sheets = new List<LayoutSheet>(workbook.Sheets.Count);
        var rootVars = new Dictionary<string, object?>(StringComparer.Ordinal);
        var expandedSheetNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sheet in workbook.Sheets)
        {
            if (string.IsNullOrWhiteSpace(sheet.FromExprRaw))
            {
                var resolvedName = ResolveSheetName(sheet, rootData, rootData, rootVars, issues);
                ExpandSheet(
                    sheet,
                    resolvedName,
                    rootData,
                    rootData,
                    rootVars,
                    componentIndex,
                    styleResolver,
                    sheets,
                    expandedSheetNames,
                    issues);
                continue;
            }

            var sequence = EvaluateSequence(sheet.FromExprRaw, rootData, rootData, rootVars, issues, owner: "sheet");
            if (sequence is null)
            {
                continue;
            }

            foreach (var item in sequence)
            {
                var sheetVars = CloneVars(rootVars);
                sheetVars[sheet.VarName] = item;

                var resolvedName = ResolveSheetName(sheet, rootData, item, sheetVars, issues);
                ExpandSheet(
                    sheet,
                    resolvedName,
                    rootData,
                    item,
                    sheetVars,
                    componentIndex,
                    styleResolver,
                    sheets,
                    expandedSheetNames,
                    issues);
            }
        }

        return new LayoutPlan(sheets, issues, chartPalette);
    }

    private void ExpandSheet(
        SheetAst sheet,
        string sheetName,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IReadOnlyDictionary<string, ComponentAst> componentIndex,
        IStyleResolver styleResolver,
        ICollection<LayoutSheet> sheets,
        ISet<string> expandedSheetNames,
        IList<Issue> issues)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.InvalidAttributeValue,
                Message = "sheet 名が空です。",
                Span = sheet.Span,
            });
            return;
        }

        if (!expandedSheetNames.Add(sheetName))
        {
            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.DuplicateSheetName,
                Message = $"Sheet 名が重複しています: {sheetName}",
                Span = sheet.Span,
            });
        }

        var cells = new List<LayoutCell>();
        var namedAreas = new List<LayoutNamedArea>();
        var conditionalFormattings = new List<LayoutConditionalFormatting>();
        var charts = CreateLayoutCharts(sheet, rootData, dataContext, vars, issues);
        var inheritedStyles = StyleScope.From(sheet.StyleRefs, null);
        conditionalFormattings.AddRange(CreateScopedConditionalFormattings(sheet.ConditionalFormattings, "/sheet"));

        var sheetChildren = sheet.Children.Values.ToArray();
        for (var childIndex = 0; childIndex < sheetChildren.Length; childIndex++)
        {
            var child = sheetChildren[childIndex];
            var childScopePath = child is CellAst
                ? "/sheet"
                : $"/sheet/node-{childIndex}";
            var result = ExpandNode(
                child,
                baseRow: 1,
                baseCol: 1,
                rootData,
                dataContext,
                vars,
                scopePath: childScopePath,
                inheritedStyles,
                componentIndex,
                styleResolver,
                issues);

            if (result.Cells.Count > 0)
            {
                cells.AddRange(result.Cells);
            }

            if (result.NamedAreas.Count > 0)
            {
                namedAreas.AddRange(result.NamedAreas);
            }

            if (result.ConditionalFormattings.Count > 0)
            {
                conditionalFormattings.AddRange(result.ConditionalFormattings);
            }
        }

        var maxUsedRow = GetMaxUsedRow(cells);
        var maxUsedCol = GetMaxUsedCol(cells);
        if (charts.Count > 0)
        {
            maxUsedRow = Math.Max(maxUsedRow, charts.Max(chart => chart.TopRow + chart.HeightRows - 1));
            maxUsedCol = Math.Max(maxUsedCol, charts.Max(chart => chart.LeftColumn + chart.WidthColumns - 1));
        }

        var resolvedSheetRows = ResolveContainerSize(sheet.Rows, maxUsedRow, minimumSize: 1);
        var resolvedSheetCols = ResolveContainerSize(sheet.Cols, maxUsedCol, minimumSize: 1);

        ValidateCoordinates(sheetName, resolvedSheetRows, resolvedSheetCols, cells, issues);
        var validCharts = ValidateChartCoordinates(sheetName, resolvedSheetRows, resolvedSheetCols, charts, issues);
        sheets.Add(
            LayoutSheet.CreateWithScopedConditionalFormattings(
                sheetName,
                cells,
                resolvedSheetRows,
                resolvedSheetCols,
                namedAreas,
                sheet.Options,
                conditionalFormattings,
                validCharts));
    }

    private string ResolveSheetName(
        SheetAst sheet,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        if (!LooksLikeExpression(sheet.Name))
        {
            return sheet.Name;
        }

        var value = EvaluateExpressionValue(sheet.Name, rootData, dataContext, vars, issues);
        return value?.ToString() ?? string.Empty;
    }

    private ExpandResult ExpandNode(
        LayoutNodeAst node,
        int baseRow,
        int baseCol,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        string scopePath,
        StyleScope inheritedStyles,
        IReadOnlyDictionary<string, ComponentAst> componentIndex,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        if (!ShouldRender(node.Placement.WhenExprRaw, rootData, dataContext, vars, issues))
        {
            return ExpandResult.Empty;
        }

        var rowOffset = ResolveOffset(node.Placement.Row);
        var colOffset = ResolveOffset(node.Placement.Col);
        var startRow = baseRow + rowOffset;
        var startCol = baseCol + colOffset;

        return node switch
        {
            CellAst cell => ExpandCell(
                cell,
                startRow,
                startCol,
                rootData,
                dataContext,
                vars,
                scopePath,
                inheritedStyles,
                styleResolver,
                issues),
            GridAst grid => ExpandGrid(
                grid,
                startRow,
                startCol,
                rootData,
                dataContext,
                vars,
                scopePath,
                inheritedStyles,
                componentIndex,
                styleResolver,
                issues),
            RepeatAst repeat => ExpandRepeat(
                repeat,
                startRow,
                startCol,
                rootData,
                dataContext,
                vars,
                scopePath,
                inheritedStyles,
                componentIndex,
                styleResolver,
                issues),
            UseAst use => ExpandUse(
                use,
                startRow,
                startCol,
                rootData,
                dataContext,
                vars,
                scopePath,
                inheritedStyles,
                componentIndex,
                styleResolver,
                issues),
            _ => ExpandResult.Empty,
        };
    }

    private ExpandResult ExpandCell(
        CellAst cell,
        int row,
        int col,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        string scopePath,
        StyleScope inheritedStyles,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        var styleScope = inheritedStyles.Append(cell.StyleRefs, cell.Style);
        if (!string.IsNullOrWhiteSpace(cell.StyleRefShortcut))
        {
            styleScope = styleScope.Append([CreateStyleRef(cell.StyleRefShortcut, issues)], null);
        }

        var rendered = EvaluateCellValue(cell.ValueRaw, cell.FormulaRaw, rootData, dataContext, vars, issues);
        var stylePlan = styleResolver.BuildPlan(
            styleScope.StyleRefs,
            styleScope.InlineStyles,
            sheetDefault: null,
            workbookDefault: null,
            StyleTarget.Cell,
            issues);

        var layoutCell = new LayoutCell(
            row,
            col,
            cell.Placement.RowSpan,
            cell.Placement.ColSpan,
            rendered.Value,
            rendered.Formula,
            cell.FormulaRef,
            cell.FormulaRefScope,
            scopePath,
            stylePlan);

        return new ExpandResult(
            [layoutCell],
            cell.Placement.RowSpan,
            cell.Placement.ColSpan,
            Array.Empty<LayoutNamedArea>(),
            Array.Empty<LayoutConditionalFormatting>());
    }

    private ExpandResult ExpandGrid(
        GridAst grid,
        int baseRow,
        int baseCol,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        string scopePath,
        StyleScope inheritedStyles,
        IReadOnlyDictionary<string, ComponentAst> componentIndex,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        var styleScope = inheritedStyles.Append(grid.StyleRefs, grid.Style);
        var cells = new List<LayoutCell>();
        var namedAreas = new List<LayoutNamedArea>();
        var conditionalFormattings = new List<LayoutConditionalFormatting>();
        var maxHeight = 0;
        var maxWidth = 0;
        conditionalFormattings.AddRange(CreateScopedConditionalFormattings(grid.ConditionalFormattings, scopePath));

        var gridChildren = grid.Children.Values.ToArray();
        for (var childIndex = 0; childIndex < gridChildren.Length; childIndex++)
        {
            var child = gridChildren[childIndex];
            // Keep direct cell siblings in the same local scope, while isolating nested containers.
            var childScopePath = child is CellAst
                ? scopePath
                : $"{scopePath}/node-{childIndex}";
            var childResult = ExpandNode(
                child,
                baseRow,
                baseCol,
                rootData,
                dataContext,
                vars,
                childScopePath,
                styleScope,
                componentIndex,
                styleResolver,
                issues);

            if (childResult.NamedAreas.Count > 0)
            {
                namedAreas.AddRange(childResult.NamedAreas);
            }

            if (childResult.ConditionalFormattings.Count > 0)
            {
                conditionalFormattings.AddRange(childResult.ConditionalFormattings);
            }

            if (childResult.Cells.Count == 0)
            {
                continue;
            }

            cells.AddRange(childResult.Cells);
            var childHeightWithOffset = ResolveOffset(child.Placement.Row) + childResult.Height;
            var childWidthWithOffset = ResolveOffset(child.Placement.Col) + childResult.Width;
            maxHeight = Math.Max(maxHeight, childHeightWithOffset);
            maxWidth = Math.Max(maxWidth, childWidthWithOffset);
        }

        TryAddNamedAreaFromTarget(namedAreas, grid, cells);
        var result = new ExpandResult(
            cells,
            ResolveContainerSize(grid.Rows, maxHeight, grid.Placement.RowSpan),
            ResolveContainerSize(grid.Cols, maxWidth, grid.Placement.ColSpan),
            namedAreas,
            conditionalFormattings);

        return ApplyGridBorders(result, styleScope, styleResolver);
    }

    private ExpandResult ExpandRepeat(
        RepeatAst repeat,
        int baseRow,
        int baseCol,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        string scopePath,
        StyleScope inheritedStyles,
        IReadOnlyDictionary<string, ComponentAst> componentIndex,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        if (repeat.Body is null || repeat.Direction == RepeatDirection.Err)
        {
            return ExpandResult.Empty;
        }

        var sequence = EvaluateSequence(repeat.FromExprRaw, rootData, dataContext, vars, issues);
        if (sequence is null)
        {
            return ExpandResult.Empty;
        }

        var styleScope = inheritedStyles.Append(repeat.StyleRefs, repeat.Style);
        var cells = new List<LayoutCell>();
        var namedAreas = new List<LayoutNamedArea>();
        var conditionalFormattings = new List<LayoutConditionalFormatting>();
        var nextRow = baseRow;
        var nextCol = baseCol;
        var totalHeight = 0;
        var totalWidth = 0;
        var maxHeight = 0;
        var maxWidth = 0;

        var iterationIndex = 0;
        foreach (var item in sequence)
        {
            var itemVars = CloneVars(vars);
            itemVars[repeat.VarName] = item;

            var result = ExpandNode(
                repeat.Body,
                nextRow,
                nextCol,
                rootData,
                dataContext,
                itemVars,
                $"{scopePath}/repeat-{iterationIndex}",
                styleScope,
                componentIndex,
                styleResolver,
                issues);
            conditionalFormattings.AddRange(
                CreateScopedConditionalFormattings(
                    repeat.ConditionalFormattings,
                    $"{scopePath}/repeat-{iterationIndex}"));
            iterationIndex++;

            if (result.Cells.Count > 0)
            {
                cells.AddRange(result.Cells);
            }

            if (result.NamedAreas.Count > 0)
            {
                namedAreas.AddRange(result.NamedAreas);
            }

            if (result.ConditionalFormattings.Count > 0)
            {
                conditionalFormattings.AddRange(result.ConditionalFormattings);
            }

            if (repeat.Direction == RepeatDirection.Down)
            {
                nextRow += result.Height;
                totalHeight += result.Height;
                maxWidth = Math.Max(maxWidth, result.Width);
            }
            else
            {
                nextCol += result.Width;
                totalWidth += result.Width;
                maxHeight = Math.Max(maxHeight, result.Height);
            }
        }

        if (repeat.Direction == RepeatDirection.Down)
        {
            totalHeight = Math.Max(totalHeight, repeat.Placement.RowSpan);
            maxWidth = Math.Max(maxWidth, repeat.Placement.ColSpan);
            TryAddNamedAreaFromTarget(namedAreas, repeat, cells);
            return new ExpandResult(cells, totalHeight, maxWidth, namedAreas, conditionalFormattings);
        }

        totalWidth = Math.Max(totalWidth, repeat.Placement.ColSpan);
        maxHeight = Math.Max(maxHeight, repeat.Placement.RowSpan);
        TryAddNamedAreaFromTarget(namedAreas, repeat, cells);
        return new ExpandResult(cells, maxHeight, totalWidth, namedAreas, conditionalFormattings);
    }

    private ExpandResult ExpandUse(
        UseAst use,
        int baseRow,
        int baseCol,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        string scopePath,
        StyleScope inheritedStyles,
        IReadOnlyDictionary<string, ComponentAst> componentIndex,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        if (!componentIndex.TryGetValue(use.ComponentName, out var component))
        {
            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.UndefinedComponent,
                Message = $"未定義の component 参照: {use.ComponentName}",
                Span = use.Span,
            });
            return ExpandResult.Empty;
        }

        var boundData = dataContext;
        if (!string.IsNullOrWhiteSpace(use.WithExprRaw))
        {
            boundData = EvaluateExpressionValue(use.WithExprRaw, rootData, dataContext, vars, issues);
        }

        if (!ShouldRender(component.Placement.WhenExprRaw, rootData, boundData, vars, issues))
        {
            return ExpandResult.Empty;
        }

        var componentBaseRow = baseRow + ResolveOffset(component.Placement.Row);
        var componentBaseCol = baseCol + ResolveOffset(component.Placement.Col);
        var componentStyleScope = inheritedStyles.Append(component.StyleRefs, component.Style);

        var result = ExpandNode(
            component.Body,
            componentBaseRow,
            componentBaseCol,
            rootData,
            boundData,
            vars,
            $"{scopePath}/use",
            componentStyleScope,
            componentIndex,
            styleResolver,
            issues);
        var useSeedScope = StyleScope.From(use.StyleRefs, use.Style);
        var seedResult = CreateUseSeedResult(use, baseRow, baseCol, $"{scopePath}/use-seed", useSeedScope, styleResolver, issues);
        result = MergeSeedCells(result, seedResult.Cells);
        result = ApplyUseStyleOverflow(result, seedResult.Cells, use, baseRow, baseCol, $"{scopePath}/use-overflow", issues);
        var conditionalFormattings = new List<LayoutConditionalFormatting>(result.ConditionalFormattings);
        conditionalFormattings.AddRange(
            CreateScopedConditionalFormattings(
                component.ConditionalFormattings,
                $"{scopePath}/use"));

        var heightOffset = ResolveOffset(component.Placement.Row);
        var widthOffset = ResolveOffset(component.Placement.Col);
        var namedAreas = new List<LayoutNamedArea>(result.NamedAreas);
        TryAddNamedAreaFromTarget(namedAreas, use, result.Cells);

        return new ExpandResult(
            result.Cells,
            Math.Max(result.Height + heightOffset, use.Placement.RowSpan),
            Math.Max(result.Width + widthOffset, use.Placement.ColSpan),
            namedAreas,
            conditionalFormattings);
    }

    private object? EvaluateExpressionValue(
        string? expression,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        if (TryEvaluateLiteralExpression(expression, out var literalValue))
        {
            return literalValue;
        }

        if (TryEvaluateVariableShortcut(expression, vars, out var shortcutResult))
        {
            return shortcutResult;
        }

        if (TryRewriteVariableScopedExpression(expression, vars, out var rewrittenExpression, out var scopedData))
        {
            var scopedResult = _expressionEngine.Evaluate(
                rewrittenExpression,
                new ExpressionContext(rootData, scopedData, CloneVars(vars)));

            AppendIssues(issues, scopedResult.Issues);
            return scopedResult.Value;
        }

        var result = _expressionEngine.Evaluate(
            expression,
            new ExpressionContext(rootData, dataContext, CloneVars(vars)));

        AppendIssues(issues, result.Issues);
        return result.Value;
    }

    private RenderedValue EvaluateCellValue(
        string? valueRaw,
        string? formulaRaw,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        if (!string.IsNullOrWhiteSpace(formulaRaw))
        {
            return new RenderedValue(
                Value: null,
                Formula: EvaluateFormulaText(formulaRaw, rootData, dataContext, vars, issues));
        }

        if (string.IsNullOrWhiteSpace(valueRaw))
        {
            return RenderedValue.Empty;
        }

        if (LooksLikeExpression(valueRaw))
        {
            var evaluated = EvaluateExpressionValue(valueRaw, rootData, dataContext, vars, issues);
            if (evaluated is string evaluatedText &&
                evaluatedText.StartsWith("=", StringComparison.Ordinal))
            {
                return new RenderedValue(evaluatedText, evaluatedText);
            }

            return new RenderedValue(
                evaluated,
                Formula: null);
        }

        if (valueRaw.StartsWith("=", StringComparison.Ordinal))
        {
            return new RenderedValue(valueRaw, valueRaw);
        }

        return new RenderedValue(valueRaw, Formula: null);
    }

    private string? EvaluateFormulaText(
        string formulaRaw,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        if (LooksLikeExpression(formulaRaw))
        {
            var evaluated = EvaluateExpressionValue(formulaRaw, rootData, dataContext, vars, issues);
            return NormalizeFormulaText(evaluated?.ToString());
        }

        return NormalizeFormulaText(formulaRaw);
    }

    private static string? NormalizeFormulaText(string? formula)
    {
        var normalized = formula?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.StartsWith("=", StringComparison.Ordinal)
            ? normalized
            : "=" + normalized;
    }

    private bool ShouldRender(
        string? whenExprRaw,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        if (string.IsNullOrWhiteSpace(whenExprRaw))
        {
            return true;
        }

        var value = EvaluateExpressionValue(whenExprRaw, rootData, dataContext, vars, issues);
        return ToBoolean(value);
    }

    private IEnumerable<object?>? EvaluateSequence(
        string expression,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues,
        string owner = "repeat")
    {
        var value = EvaluateExpressionValue(expression, rootData, dataContext, vars, issues);
        if (value is null)
        {
            return Array.Empty<object?>();
        }

        if (value is string)
        {
            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"{owner} from はコレクションである必要があります: {expression}",
            });
            return null;
        }

        if (value is IEnumerable sequence)
        {
            return sequence.Cast<object?>();
        }

        issues.Add(new Issue
        {
            Severity = IssueSeverity.Error,
            Kind = IssueKind.InvalidAttributeValue,
            Message = $"{owner} from はコレクションである必要があります: {expression}",
        });
        return null;
    }

    private static IReadOnlyDictionary<string, ComponentAst> BuildComponentIndex(WorkbookAst workbook)
    {
        var index = new Dictionary<string, ComponentAst>(StringComparer.Ordinal);
        IndexComponents(workbook.Components, index);

        if (workbook.ComponentInports != null)
        {
            foreach (var componentImport in workbook.ComponentInports)
            {
                IndexComponents(componentImport.Components?.ComponentList, index);
            }
        }

        return index;
    }

    private static IReadOnlyDictionary<string, string> BuildChartPalette(ChartPaletteAst? palette)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (palette is null)
        {
            return result;
        }

        foreach (var color in palette.Colors)
        {
            if (string.IsNullOrWhiteSpace(color.Key) || string.IsNullOrWhiteSpace(color.Value))
            {
                continue;
            }

            result[color.Key] = color.Value;
        }

        return result;
    }

    private IReadOnlyList<LayoutChart> CreateLayoutCharts(
        SheetAst sheet,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        var result = new List<LayoutChart>();
        foreach (var chart in sheet.Charts)
        {
            if (!ShouldRender(chart.WhenExprRaw, rootData, dataContext, vars, issues))
            {
                continue;
            }

            var series = chart.Series
                .Select(
                    seriesAst =>
                        new LayoutChartSeries(
                            seriesAst.Name,
                            seriesAst.ValueRef,
                            seriesAst.Color,
                            seriesAst.ColorKey,
                            seriesAst.ColorByRef))
                .ToArray();

            result.Add(
                new LayoutChart(
                    chart.ChartType,
                    chart.Title,
                    chart.Name,
                    chart.Row,
                    chart.Column,
                    chart.Width,
                    chart.Height,
                    chart.CategoryRef,
                    chart.Legend,
                    chart.ShowDataLabels,
                    series));
        }

        return result;
    }

    private static void IndexComponents(
        IReadOnlyList<ComponentAst>? components,
        IDictionary<string, ComponentAst> index)
    {
        if (components is null)
        {
            return;
        }

        foreach (var component in components)
        {
            if (!string.IsNullOrWhiteSpace(component.Name))
            {
                index.TryAdd(component.Name, component);
            }
        }
    }

    private static void ValidateCoordinates(
        string sheetName,
        int sheetRows,
        int sheetCols,
        IEnumerable<LayoutCell> cells,
        IList<Issue> issues)
    {
        foreach (var cell in cells)
        {
            var endRow = cell.Row + cell.RowSpan - 1;
            var endCol = cell.Col + cell.ColSpan - 1;
            if (cell.Row < 1 || cell.Col < 1 || endRow > sheetRows || endCol > sheetCols)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.CoordinateOutOfRange,
                    Message = $"セル配置がシート範囲外です: sheet={sheetName}, r={cell.Row}, c={cell.Col}, rowSpan={cell.RowSpan}, colSpan={cell.ColSpan}",
                    Span = null,
                });
            }
        }
    }

    private static IReadOnlyList<LayoutChart> ValidateChartCoordinates(
        string sheetName,
        int sheetRows,
        int sheetCols,
        IEnumerable<LayoutChart> charts,
        IList<Issue> issues)
    {
        var validCharts = new List<LayoutChart>();
        foreach (var chart in charts)
        {
            var endRow = chart.TopRow + chart.HeightRows - 1;
            var endCol = chart.LeftColumn + chart.WidthColumns - 1;

            if (chart.TopRow < 1 ||
                chart.LeftColumn < 1 ||
                chart.WidthColumns <= 0 ||
                chart.HeightRows <= 0 ||
                endRow > sheetRows ||
                endCol > sheetCols ||
                endRow > MaxExcelRows ||
                endCol > MaxExcelColumns)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.CoordinateOutOfRange,
                    Message = $"グラフ配置がシートまたは Excel の範囲外です: sheet={sheetName}, r={chart.TopRow}, c={chart.LeftColumn}, width={chart.WidthColumns}, height={chart.HeightRows}",
                    Span = null,
                });

                continue;
            }

            validCharts.Add(chart);
        }

        return validCharts;
    }

    private static int GetMaxUsedRow(IReadOnlyCollection<LayoutCell> cells) =>
        cells.Count == 0
            ? 0
            : cells.Max(cell => cell.Row + cell.RowSpan - 1);

    private static int GetMaxUsedCol(IReadOnlyCollection<LayoutCell> cells) =>
        cells.Count == 0
            ? 0
            : cells.Max(cell => cell.Col + cell.ColSpan - 1);

    private static int ResolveContainerSize(int declaredSize, int contentSize, int minimumSize) =>
        Math.Max(declaredSize > 0 ? declaredSize : contentSize, minimumSize);

    private static void TryAddNamedAreaFromTarget(
        ICollection<LayoutNamedArea> areas,
        INamedAreaTarget target,
        IReadOnlyList<LayoutCell> cells) =>
        TryAddNamedArea(areas, target.AreaName, cells);

    private static void TryAddNamedArea(
        ICollection<LayoutNamedArea> areas,
        string? name,
        IReadOnlyList<LayoutCell> cells)
    {
        if (string.IsNullOrWhiteSpace(name) || cells.Count == 0)
        {
            return;
        }

        areas.Add(CreateBoundingNamedArea(name, cells));
    }

    private static LayoutNamedArea CreateBoundingNamedArea(string name, IReadOnlyList<LayoutCell> cells)
    {
        var topRow = cells.Min(cell => cell.Row);
        var leftColumn = cells.Min(cell => cell.Col);
        var bottomRow = cells.Max(cell => cell.Row + cell.RowSpan - 1);
        var rightColumn = cells.Max(cell => cell.Col + cell.ColSpan - 1);

        return new LayoutNamedArea(name, topRow, leftColumn, bottomRow, rightColumn);
    }

    private static StyleRefAst CreateStyleRef(string name, IList<Issue> issues)
    {
        var styleRefIssues = new List<Issue>();
        var styleRef = new StyleRefAst(
            new XElement("styleRef", new XAttribute("name", name)),
            styleRefIssues);
        AppendIssues(issues, styleRefIssues);
        return styleRef;
    }

    private static Dictionary<string, object?> CloneVars(IReadOnlyDictionary<string, object?> vars) =>
        vars.Count == 0
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(vars, StringComparer.Ordinal);

    private static IReadOnlyList<LayoutConditionalFormatting> CreateScopedConditionalFormattings(
        IEnumerable<ConditionalFormattingAst>? rules,
        string scopePath)
    {
        if (rules is null)
        {
            return Array.Empty<LayoutConditionalFormatting>();
        }

        return rules
            .Select(rule => new LayoutConditionalFormatting(rule, scopePath))
            .ToArray();
    }

    private static bool LooksLikeExpression(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("@(", StringComparison.Ordinal)
            && trimmed.EndsWith(')');
    }

    private static int ResolveOffset(int? coordinate) => coordinate.GetValueOrDefault(1) - 1;

    private static bool TryEvaluateVariableShortcut(
        string expression,
        IReadOnlyDictionary<string, object?> vars,
        out object? value)
    {
        var body = NormalizeExpressionBody(expression);
        if (body.Length > 0 && body.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            return vars.TryGetValue(body, out value);
        }

        value = null;
        return false;
    }

    private static bool TryEvaluateLiteralExpression(string expression, out object? value)
    {
        var body = NormalizeExpressionBody(expression);
        if (string.Equals(body, "true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (string.Equals(body, "false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        if (string.Equals(body, "null", StringComparison.OrdinalIgnoreCase))
        {
            value = null;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryRewriteVariableScopedExpression(
        string expression,
        IReadOnlyDictionary<string, object?> vars,
        out string rewrittenExpression,
        out object? scopedData)
    {
        var body = NormalizeExpressionBody(expression);
        foreach (var pair in vars)
        {
            var variableName = pair.Key;
            if (TryRewriteScopedVariableExpressionBody(
                body,
                variableName,
                out var rewrittenBody,
                out var wasRewritten))
            {
                if (!wasRewritten)
                {
                    continue;
                }

                rewrittenExpression = "@(" + rewrittenBody + ")";
                scopedData = pair.Value;
                return true;
            }

            if (body.StartsWith(variableName + ".", StringComparison.Ordinal))
            {
                rewrittenExpression = "@(data." + body[(variableName.Length + 1)..] + ")";
                scopedData = pair.Value;
                return true;
            }

            if (body.StartsWith(variableName + "[", StringComparison.Ordinal))
            {
                rewrittenExpression = "@(data" + body[variableName.Length..] + ")";
                scopedData = pair.Value;
                return true;
            }
        }

        rewrittenExpression = string.Empty;
        scopedData = null;
        return false;
    }

    private static bool TryRewriteScopedVariableExpressionBody(
        string expressionBody,
        string variableName,
        out string rewrittenBody,
        out bool wasRewritten)
    {
        var syntax = SyntaxFactory.ParseExpression(expressionBody);
        if (syntax.ContainsDiagnostics)
        {
            rewrittenBody = string.Empty;
            wasRewritten = false;
            return false;
        }

        var rewriter = new ScopedVariableExpressionRewriter(variableName);
        if (rewriter.Visit(syntax) is not ExpressionSyntax rewrittenSyntax)
        {
            rewrittenBody = string.Empty;
            wasRewritten = false;
            return false;
        }

        wasRewritten = rewriter.Rewritten;
        rewrittenBody = wasRewritten ? rewrittenSyntax.ToFullString() : expressionBody;
        return true;
    }

    private sealed class ScopedVariableExpressionRewriter : CSharpSyntaxRewriter
    {
        private readonly string _variableName;

        public ScopedVariableExpressionRewriter(string variableName)
        {
            _variableName = variableName;
        }

        public bool Rewritten { get; private set; }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (string.Equals(node.Identifier.ValueText, _variableName, StringComparison.Ordinal)
                && !IsIdentifierInMemberNamePosition(node))
            {
                Rewritten = true;
                return SyntaxFactory.IdentifierName("data").WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var visited = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node)!;
            if (visited.Expression is IdentifierNameSyntax identifier
                && string.Equals(identifier.Identifier.ValueText, _variableName, StringComparison.Ordinal))
            {
                Rewritten = true;
                return visited.WithExpression(SyntaxFactory.IdentifierName("data"));
            }

            return visited;
        }

        public override SyntaxNode? VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            var visited = (ElementAccessExpressionSyntax)base.VisitElementAccessExpression(node)!;
            if (visited.Expression is IdentifierNameSyntax identifier
                && string.Equals(identifier.Identifier.ValueText, _variableName, StringComparison.Ordinal))
            {
                Rewritten = true;
                return visited.WithExpression(SyntaxFactory.IdentifierName("data"));
            }

            return visited;
        }

        private static bool IsIdentifierInMemberNamePosition(IdentifierNameSyntax node)
        {
            if (node.Parent is MemberAccessExpressionSyntax memberAccess
                && ReferenceEquals(memberAccess.Name, node))
            {
                return true;
            }

            if (node.Parent is QualifiedNameSyntax qualifiedName
                && ReferenceEquals(qualifiedName.Right, node))
            {
                return true;
            }

            return false;
        }
    }

    private static string NormalizeExpressionBody(string expression)
    {
        var trimmed = expression.Trim();
        if (trimmed.StartsWith("@(", StringComparison.Ordinal) && trimmed.EndsWith(')'))
        {
            return trimmed[2..^1].Trim();
        }

        return trimmed;
    }

    private static bool ToBoolean(object? value) =>
        value switch
        {
            null => false,
            bool boolean => boolean,
            string text when bool.TryParse(text, out var parsed) => parsed,
            sbyte number => number != 0,
            byte number => number != 0,
            short number => number != 0,
            ushort number => number != 0,
            int number => number != 0,
            uint number => number != 0,
            long number => number != 0,
            ulong number => number != 0,
            float number => Math.Abs(number) > float.Epsilon,
            double number => Math.Abs(number) > double.Epsilon,
            decimal number => number != 0,
            _ => true,
        };

    private static void AppendIssues(IList<Issue> target, IEnumerable<Issue>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var issue in source)
        {
            target.Add(issue);
        }
    }

    private static ExpandResult CreateUseSeedResult(
        UseAst use,
        int baseRow,
        int baseCol,
        string scopePath,
        StyleScope useSeedScope,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        if (!HasUseSeedStyles(useSeedScope))
        {
            return ExpandResult.Empty;
        }

        var stylePlan = styleResolver.BuildPlan(
            useSeedScope.StyleRefs,
            useSeedScope.InlineStyles,
            sheetDefault: null,
            workbookDefault: null,
            StyleTarget.Cell,
            issues);
        var seedCells = new List<LayoutCell>(use.Placement.RowSpan * use.Placement.ColSpan);
        var anchorBottom = baseRow + use.Placement.RowSpan - 1;
        var anchorRight = baseCol + use.Placement.ColSpan - 1;

        for (var row = baseRow; row <= anchorBottom; row++)
        {
            for (var col = baseCol; col <= anchorRight; col++)
            {
                seedCells.Add(
                    new LayoutCell(
                        row,
                        col,
                        rowSpan: 1,
                        colSpan: 1,
                        value: null,
                        formula: null,
                        formulaRef: null,
                        formulaRefScope: null,
                        scopePath,
                        stylePlan));
            }
        }

        var seedResult = new ExpandResult(
            seedCells,
            use.Placement.RowSpan,
            use.Placement.ColSpan,
            Array.Empty<LayoutNamedArea>(),
            Array.Empty<LayoutConditionalFormatting>());

        return ApplyGridBorders(seedResult, useSeedScope, styleResolver);
    }

    private static bool HasUseSeedStyles(StyleScope useSeedScope) =>
        useSeedScope.StyleRefs.Count > 0 || useSeedScope.InlineStyles.Count > 0;

    private static ExpandResult MergeSeedCells(ExpandResult result, IReadOnlyList<LayoutCell> seedCells)
    {
        if (seedCells.Count == 0)
        {
            return result;
        }

        var mergedCells = result.Cells.ToList();
        var updatedHeads = new HashSet<(int Row, int Col)>();

        foreach (var seedCell in seedCells)
        {
            if (!HasStyleContent(seedCell.StylePlan))
            {
                continue;
            }

            if (TryFindCoveringCellIndex(mergedCells, seedCell.Row, seedCell.Col, out var existingIndex))
            {
                var existingCell = mergedCells[existingIndex];
                var existingHead = (existingCell.Row, existingCell.Col);
                if (updatedHeads.Add(existingHead))
                {
                    mergedCells[existingIndex] = AppendStyleToCell(existingCell, seedCell.StylePlan);
                }

                continue;
            }

            mergedCells.Add(seedCell);
        }

        return result with
        {
            Cells = mergedCells,
        };
    }

    private static ExpandResult ApplyUseStyleOverflow(
        ExpandResult result,
        IReadOnlyList<LayoutCell> seedCells,
        UseAst use,
        int baseRow,
        int baseCol,
        string scopePath,
        IList<Issue> issues)
    {
        var anchorBottom = baseRow + use.Placement.RowSpan - 1;
        var anchorRight = baseCol + use.Placement.ColSpan - 1;
        var expandedBottom = baseRow + result.Height - 1;
        var expandedRight = baseCol + result.Width - 1;
        var deltaRows = Math.Max(0, expandedBottom - anchorBottom);
        var deltaCols = Math.Max(0, expandedRight - anchorRight);

        if (deltaRows == 0 && deltaCols == 0)
        {
            return result;
        }

        var shouldTrackOverflow =
            use.HasStyleOverflowAttribute ||
            use.Placement.RowSpan > 1 ||
            use.Placement.ColSpan > 1 ||
            seedCells.Count > 0;
        if (!shouldTrackOverflow)
        {
            return result;
        }

        issues.Add(new Issue
        {
            Severity = IssueSeverity.Warning,
            Kind = IssueKind.TemplateRangeOverflow,
            Message =
                $"component '{use.ComponentName}' expanded beyond anchor range at r={baseRow}, c={baseCol} (deltaRows={deltaRows}, deltaCols={deltaCols}).",
            Span = use.Span,
        });

        if (!string.Equals(use.StyleOverflow, "edge", StringComparison.Ordinal) || seedCells.Count == 0)
        {
            return result;
        }

        var mergedCells = result.Cells.ToList();
        var seedCellLookup = seedCells.ToDictionary(cell => (cell.Row, cell.Col));

        if (deltaCols > 0)
        {
            for (var row = baseRow; row <= anchorBottom; row++)
            {
                if (!seedCellLookup.TryGetValue((row, anchorRight), out var seedCell) || !HasStyleContent(seedCell.StylePlan))
                {
                    continue;
                }

                for (var col = anchorRight + 1; col <= expandedRight; col++)
                {
                    AddOrMergeStyleCell(mergedCells, row, col, seedCell.StylePlan, scopePath);
                }
            }
        }

        if (deltaRows > 0)
        {
            for (var col = baseCol; col <= anchorRight; col++)
            {
                if (!seedCellLookup.TryGetValue((anchorBottom, col), out var seedCell) || !HasStyleContent(seedCell.StylePlan))
                {
                    continue;
                }

                for (var row = anchorBottom + 1; row <= expandedBottom; row++)
                {
                    AddOrMergeStyleCell(mergedCells, row, col, seedCell.StylePlan, scopePath);
                }
            }
        }

        if (deltaRows > 0 &&
            deltaCols > 0 &&
            seedCellLookup.TryGetValue((anchorBottom, anchorRight), out var cornerSeedCell) &&
            HasStyleContent(cornerSeedCell.StylePlan))
        {
            for (var row = anchorBottom + 1; row <= expandedBottom; row++)
            {
                for (var col = anchorRight + 1; col <= expandedRight; col++)
                {
                    AddOrMergeStyleCell(mergedCells, row, col, cornerSeedCell.StylePlan, scopePath);
                }
            }
        }

        return result with
        {
            Cells = mergedCells,
        };
    }

    private static void AddOrMergeStyleCell(
        IList<LayoutCell> cells,
        int row,
        int col,
        StylePlan stylePlan,
        string scopePath)
    {
        if (!HasStyleContent(stylePlan))
        {
            return;
        }

        if (TryFindCoveringCellIndex(cells, row, col, out var existingIndex))
        {
            cells[existingIndex] = AppendStyleToCell(cells[existingIndex], stylePlan);
            return;
        }

        cells.Add(
            new LayoutCell(
                row,
                col,
                rowSpan: 1,
                colSpan: 1,
                value: null,
                formula: null,
                formulaRef: null,
                formulaRefScope: null,
                scopePath,
                stylePlan));
    }

    private static bool TryFindCoveringCellIndex(
        IList<LayoutCell> cells,
        int row,
        int col,
        out int index)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (row < cell.Row || col < cell.Col)
            {
                continue;
            }

            if (row <= cell.Row + cell.RowSpan - 1 && col <= cell.Col + cell.ColSpan - 1)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private static bool HasStyleContent(StylePlan stylePlan) =>
        stylePlan.EffectiveStyle.HasContent;

    /// <summary>
    /// グリッド解決後のセル集合へ、mode=outer/all の border を mode=cell として展開する。
    /// </summary>
    /// <param name="result">グリッド展開結果。</param>
    /// <param name="styleScope">グリッドに適用されるスタイルスコープ。</param>
    /// <param name="styleResolver">スタイル解決器。</param>
    /// <returns>border 展開後の展開結果。</returns>
    private static ExpandResult ApplyGridBorders(
        ExpandResult result,
        StyleScope styleScope,
        IStyleResolver styleResolver)
    {
        if (result.Cells.Count == 0)
        {
            return result;
        }

        var gridBorders = ResolveGridBorders(styleScope, styleResolver);
        if (gridBorders.Count == 0)
        {
            return result;
        }

        var topRow = result.Cells.Min(cell => cell.Row);
        var bottomRow = result.Cells.Max(cell => cell.Row + cell.RowSpan - 1);
        var leftCol = result.Cells.Min(cell => cell.Col);
        var rightCol = result.Cells.Max(cell => cell.Col + cell.ColSpan - 1);

        var expandedCells = new List<LayoutCell>(result.Cells.Count);
        foreach (var cell in result.Cells)
        {
            var expandedBorders = ExpandGridBordersForCell(gridBorders, cell, topRow, bottomRow, leftCol, rightCol);
            expandedCells.Add(expandedBorders.Count == 0 ? cell : AppendBordersToCell(cell, expandedBorders));
        }

        return new ExpandResult(expandedCells, result.Height, result.Width, result.NamedAreas, result.ConditionalFormattings);
    }

    /// <summary>
    /// スタイルスコープから mode=outer/all の border を抽出する。
    /// </summary>
    /// <param name="styleScope">スタイルスコープ。</param>
    /// <param name="styleResolver">スタイル解決器。</param>
    /// <returns>抽出された grid border 一覧。</returns>
    private static IReadOnlyList<BorderInfo> ResolveGridBorders(
        StyleScope styleScope,
        IStyleResolver styleResolver)
    {
        var borders = new List<BorderInfo>();

        foreach (var styleRef in EnumerateStyleRefs(styleScope.StyleRefs))
        {
            var style = styleResolver.ResolveByName(styleRef.Name);
            if (style is null || style.Scope == ExcelReportLib.DSL.AST.StyleScope.Cell)
            {
                continue;
            }

            borders.AddRange(style.Borders.Where(border => IsGridBorderMode(border.Mode)).Select(CloneBorder));
        }

        foreach (var inlineStyle in styleScope.InlineStyles)
        {
            if (inlineStyle.Scope == ExcelReportLib.DSL.AST.StyleScope.Cell)
            {
                continue;
            }

            borders.AddRange(inlineStyle.Borders.Where(border => IsGridBorderMode(border.Mode)).Select(CloneBorder));
        }

        return borders;
    }

    /// <summary>
    /// styleRef の入れ子を含めて深さ優先で列挙する。
    /// </summary>
    /// <param name="roots">列挙開始する styleRef 群。</param>
    /// <returns>順序を維持した styleRef 列挙。</returns>
    private static IEnumerable<StyleRefAst> EnumerateStyleRefs(IEnumerable<StyleRefAst>? roots)
    {
        if (roots is null)
        {
            yield break;
        }

        var stack = new Stack<StyleRefAst>(roots.Reverse());
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            for (var i = current.StyleRefs.Count - 1; i >= 0; i--)
            {
                stack.Push(current.StyleRefs[i]);
            }
        }
    }

    /// <summary>
    /// セル位置に応じて grid border を cell border に展開する。
    /// </summary>
    /// <param name="gridBorders">mode=outer/all の border 一覧。</param>
    /// <param name="cell">対象セル。</param>
    /// <param name="topRow">グリッド上端行。</param>
    /// <param name="bottomRow">グリッド下端行。</param>
    /// <param name="leftCol">グリッド左端列。</param>
    /// <param name="rightCol">グリッド右端列。</param>
    /// <returns>展開された mode=cell border 一覧。</returns>
    private static IReadOnlyList<BorderInfo> ExpandGridBordersForCell(
        IReadOnlyList<BorderInfo> gridBorders,
        LayoutCell cell,
        int topRow,
        int bottomRow,
        int leftCol,
        int rightCol)
    {
        var expanded = new List<BorderInfo>();
        foreach (var gridBorder in gridBorders)
        {
            var border = ExpandBorderForCell(gridBorder, cell, topRow, bottomRow, leftCol, rightCol);
            if (border is not null)
            {
                expanded.Add(border);
            }
        }

        return expanded;
    }

    /// <summary>
    /// 単一 border をセル境界に合わせて展開する。
    /// </summary>
    /// <param name="gridBorder">展開対象 border。</param>
    /// <param name="cell">対象セル。</param>
    /// <param name="topRow">グリッド上端行。</param>
    /// <param name="bottomRow">グリッド下端行。</param>
    /// <param name="leftCol">グリッド左端列。</param>
    /// <param name="rightCol">グリッド右端列。</param>
    /// <returns>展開後 border。適用不要なら <see langword="null"/>。</returns>
    private static BorderInfo? ExpandBorderForCell(
        BorderInfo gridBorder,
        LayoutCell cell,
        int topRow,
        int bottomRow,
        int leftCol,
        int rightCol)
    {
        if (string.Equals(gridBorder.Mode, "all", StringComparison.OrdinalIgnoreCase))
        {
            var allBorder = new BorderInfo
            {
                Mode = "cell",
                Top = gridBorder.Top,
                Bottom = gridBorder.Bottom,
                Left = gridBorder.Left,
                Right = gridBorder.Right,
                Color = gridBorder.Color,
            };

            return HasAnyBorderSide(allBorder) ? allBorder : null;
        }

        if (!string.Equals(gridBorder.Mode, "outer", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var cellBottom = cell.Row + cell.RowSpan - 1;
        var cellRight = cell.Col + cell.ColSpan - 1;
        var outerBorder = new BorderInfo
        {
            Mode = "cell",
            Top = cell.Row == topRow ? gridBorder.Top : null,
            Bottom = cellBottom == bottomRow ? gridBorder.Bottom : null,
            Left = cell.Col == leftCol ? gridBorder.Left : null,
            Right = cellRight == rightCol ? gridBorder.Right : null,
            Color = gridBorder.Color,
        };

        return HasAnyBorderSide(outerBorder) ? outerBorder : null;
    }

    /// <summary>
    /// 既存セルへ追加 border を適用した新しいセルを生成する。
    /// </summary>
    /// <param name="cell">元セル。</param>
    /// <param name="additionalBorders">追加する border 一覧。</param>
    /// <returns>border 追加後セル。</returns>
    private static LayoutCell AppendBordersToCell(LayoutCell cell, IReadOnlyList<BorderInfo> additionalBorders)
    {
        var updatedStylePlan = CreateStylePlanWithAdditionalBorders(cell.StylePlan, additionalBorders);
        return new LayoutCell(
            cell.Row,
            cell.Col,
            cell.RowSpan,
            cell.ColSpan,
            cell.Value,
            cell.Formula,
            cell.FormulaRef,
            cell.FormulaRefScope,
            cell.ScopePath,
            updatedStylePlan);
    }

    private static LayoutCell AppendStyleToCell(LayoutCell cell, StylePlan overlayStylePlan)
    {
        var updatedStylePlan = MergeStylePlans(cell.StylePlan, overlayStylePlan);
        return new LayoutCell(
            cell.Row,
            cell.Col,
            cell.RowSpan,
            cell.ColSpan,
            cell.Value,
            cell.Formula,
            cell.FormulaRef,
            cell.FormulaRefScope,
            cell.ScopePath,
            updatedStylePlan);
    }

    /// <summary>
    /// 既存 StylePlan に border を追記した新しい StylePlan を生成する。
    /// </summary>
    /// <param name="stylePlan">元のスタイルプラン。</param>
    /// <param name="additionalBorders">追記する border 一覧。</param>
    /// <returns>border 追記後のスタイルプラン。</returns>
    private static StylePlan CreateStylePlanWithAdditionalBorders(
        StylePlan stylePlan,
        IReadOnlyList<BorderInfo> additionalBorders)
    {
        if (additionalBorders.Count == 0)
        {
            return stylePlan;
        }

        var mergedBorders = additionalBorders
            .Concat(stylePlan.Borders)
            .Select(CloneBorder)
            .ToArray();

        var effectiveStyle = stylePlan.EffectiveStyle;
        var updatedEffectiveStyle = new ResolvedStyle(
            effectiveStyle.SourceName,
            effectiveStyle.SourceKind,
            effectiveStyle.DeclaredScope,
            effectiveStyle.FontName,
            effectiveStyle.FontSize,
            effectiveStyle.FontBold,
            effectiveStyle.FontItalic,
            effectiveStyle.FontUnderline,
            effectiveStyle.FillColor,
            effectiveStyle.NumberFormatCode,
            mergedBorders);

        return new StylePlan(
            updatedEffectiveStyle,
            stylePlan.AppliedStyles,
            stylePlan.WorkbookDefault,
            stylePlan.SheetDefault,
            stylePlan.ReferenceStyles,
            stylePlan.InlineStyles,
            stylePlan.FontNameTrace,
            stylePlan.FontSizeTrace,
            stylePlan.FontBoldTrace,
            stylePlan.FontItalicTrace,
            stylePlan.FontUnderlineTrace,
            stylePlan.FillColorTrace,
            stylePlan.NumberFormatCodeTrace,
            stylePlan.BorderTraces);
    }

    private static StylePlan MergeStylePlans(
        StylePlan baseStylePlan,
        StylePlan overlayStylePlan)
    {
        if (!HasStyleContent(overlayStylePlan))
        {
            return baseStylePlan;
        }

        var mergedBorders = baseStylePlan.Borders
            .Concat(overlayStylePlan.Borders)
            .Select(CloneBorder)
            .ToArray();
        var mergedEffectiveStyle = new ResolvedStyle(
            "effective",
            StyleSourceKind.Computed,
            DSL.AST.StyleScope.Both,
            overlayStylePlan.FontName ?? baseStylePlan.FontName,
            overlayStylePlan.FontSize ?? baseStylePlan.FontSize,
            overlayStylePlan.FontBold ?? baseStylePlan.FontBold,
            overlayStylePlan.FontItalic ?? baseStylePlan.FontItalic,
            overlayStylePlan.FontUnderline ?? baseStylePlan.FontUnderline,
            overlayStylePlan.FillColor ?? baseStylePlan.FillColor,
            overlayStylePlan.NumberFormatCode ?? baseStylePlan.NumberFormatCode,
            mergedBorders);

        return new StylePlan(
            mergedEffectiveStyle,
            baseStylePlan.AppliedStyles.Concat(overlayStylePlan.AppliedStyles).ToArray(),
            baseStylePlan.WorkbookDefault,
            baseStylePlan.SheetDefault,
            baseStylePlan.ReferenceStyles.Concat(overlayStylePlan.ReferenceStyles).ToArray(),
            baseStylePlan.InlineStyles.Concat(overlayStylePlan.InlineStyles).ToArray(),
            overlayStylePlan.FontNameTrace ?? baseStylePlan.FontNameTrace,
            overlayStylePlan.FontSizeTrace ?? baseStylePlan.FontSizeTrace,
            overlayStylePlan.FontBoldTrace ?? baseStylePlan.FontBoldTrace,
            overlayStylePlan.FontItalicTrace ?? baseStylePlan.FontItalicTrace,
            overlayStylePlan.FontUnderlineTrace ?? baseStylePlan.FontUnderlineTrace,
            overlayStylePlan.FillColorTrace ?? baseStylePlan.FillColorTrace,
            overlayStylePlan.NumberFormatCodeTrace ?? baseStylePlan.NumberFormatCodeTrace,
            baseStylePlan.BorderTraces.Concat(overlayStylePlan.BorderTraces).ToArray());
    }

    /// <summary>
    /// border mode が grid 展開対象かを判定する。
    /// </summary>
    /// <param name="mode">判定対象 mode。</param>
    /// <returns>outer または all の場合 <see langword="true"/>。</returns>
    private static bool IsGridBorderMode(string? mode) =>
        string.Equals(mode, "outer", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// border に少なくとも1辺の定義があるか判定する。
    /// </summary>
    /// <param name="border">判定対象 border。</param>
    /// <returns>いずれかの辺が定義されている場合 <see langword="true"/>。</returns>
    private static bool HasAnyBorderSide(BorderInfo border) =>
        border.Top is not null ||
        border.Bottom is not null ||
        border.Left is not null ||
        border.Right is not null;

    /// <summary>
    /// border 情報を複製する。
    /// </summary>
    /// <param name="border">複製対象 border。</param>
    /// <returns>複製された border。</returns>
    private static BorderInfo CloneBorder(BorderInfo border) =>
        new()
        {
            Mode = border.Mode,
            Top = border.Top,
            Bottom = border.Bottom,
            Left = border.Left,
            Right = border.Right,
            Color = border.Color,
        };

    private sealed record ExpandResult(
        IReadOnlyList<LayoutCell> Cells,
        int Height,
        int Width,
        IReadOnlyList<LayoutNamedArea> NamedAreas,
        IReadOnlyList<LayoutConditionalFormatting> ConditionalFormattings)
    {
        /// <summary>
        /// Represents an empty expansion result with no cells, size, or named areas.
        /// </summary>
        public static readonly ExpandResult Empty = new(
            Array.Empty<LayoutCell>(),
            0,
            0,
            Array.Empty<LayoutNamedArea>(),
            Array.Empty<LayoutConditionalFormatting>());
    }

    private sealed record RenderedValue(object? Value, string? Formula)
    {
        public static readonly RenderedValue Empty = new(null, null);
    }

    private sealed record StyleScope(
        IReadOnlyList<StyleRefAst> StyleRefs,
        IReadOnlyList<StyleAst> InlineStyles)
    {
        public static readonly StyleScope Empty = new(
            Array.Empty<StyleRefAst>(),
            Array.Empty<StyleAst>());

        public static StyleScope From(
            IEnumerable<StyleRefAst>? styleRefs,
            IEnumerable<StyleAst>? inlineStyles) =>
            Empty.Append(styleRefs, inlineStyles);

        public StyleScope Append(
            IEnumerable<StyleRefAst>? styleRefs,
            IEnumerable<StyleAst>? inlineStyles)
        {
            var mergedRefs = Merge(StyleRefs, styleRefs);
            var mergedInlineStyles = Merge(InlineStyles, inlineStyles);
            return new StyleScope(mergedRefs, mergedInlineStyles);
        }

        private static IReadOnlyList<T> Merge<T>(IReadOnlyList<T> baseItems, IEnumerable<T>? additionalItems)
        {
            if (additionalItems is null)
            {
                return baseItems;
            }

            var merged = new List<T>(baseItems.Count);
            merged.AddRange(baseItems);
            merged.AddRange(additionalItems);
            return merged;
        }
    }
}
