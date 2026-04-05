using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL;
using ExcelReportLib.LayoutEngine;
using System.Text.RegularExpressions;

namespace ExcelReportLib.WorksheetState;

/// <summary>
/// Represents worksheet state builder.
/// </summary>
public sealed class WorksheetStateBuilder : IWorksheetStateBuilder
{
    private static readonly Regex FormulaPlaceholderRegex = new(
        "#\\{(?<start>[^}:]+)(:(?<end>[^}]+))?\\}",
        RegexOptions.Compiled);

    private static readonly Regex ChartCellReferenceRegex = new(
        "^\\$?(?<col>[A-Za-z]+)\\$?(?<row>[0-9]+)$",
        RegexOptions.Compiled);

    private static readonly string[] DefaultChartPalette =
    [
        "#4472C4",
        "#ED7D31",
        "#A5A5A5",
        "#FFC000",
        "#5B9BD5",
        "#70AD47",
        "#264478",
        "#9E480E",
        "#636363",
        "#997300",
    ];

    /// <summary>
    /// Builds worksheet state models from an expanded layout plan.
    /// </summary>
    /// <param name="layoutPlan">The layout plan.</param>
    /// <param name="issues">Optional issue sink for non-fatal worksheet-state warnings.</param>
    /// <returns>A collection containing the result.</returns>
    public IReadOnlyList<WorksheetState> Build(LayoutPlan layoutPlan, IList<Issue>? issues = null)
    {
        ArgumentNullException.ThrowIfNull(layoutPlan);

        return layoutPlan.Sheets
            .Select(layoutSheet => BuildSheet(layoutSheet, layoutPlan.ChartPalette, issues))
            .ToArray();
    }

    private static WorksheetState BuildSheet(
        LayoutSheet layoutSheet,
        IReadOnlyDictionary<string, string> chartPalette,
        IList<Issue>? issues)
    {
        ValidateSheetBounds(layoutSheet.Rows, layoutSheet.Cols, layoutSheet.Name);

        var cells = new Dictionary<(int Row, int Column), CellState>();
        var mergedRanges = new List<MergedCellRange>();
        var mergedCoverage = new HashSet<(int Row, int Column)>();

        foreach (var layoutCell in layoutSheet.Cells)
        {
            ValidateCellBounds(layoutSheet, layoutCell);

            var cellKey = (layoutCell.Row, layoutCell.Col);
            if (cells.ContainsKey(cellKey) || mergedCoverage.Contains(cellKey))
            {
                throw new InvalidOperationException(
                    $"Cell '{layoutSheet.Name}!R{layoutCell.Row}C{layoutCell.Col}' overlaps an existing cell or merged range.");
            }

            if (layoutCell.Merge)
            {
                foreach (var coveredCoordinate in EnumerateCoveredCoordinates(layoutCell))
                {
                    if (coveredCoordinate != cellKey && cells.ContainsKey(coveredCoordinate))
                    {
                        throw new InvalidOperationException(
                            $"Merged range in '{layoutSheet.Name}!R{layoutCell.Row}C{layoutCell.Col}' overlaps an existing cell.");
                    }

                    if (mergedCoverage.Contains(coveredCoordinate))
                    {
                        throw new InvalidOperationException(
                            $"Merged range in '{layoutSheet.Name}!R{layoutCell.Row}C{layoutCell.Col}' overlaps an existing merged range.");
                    }
                }
            }

            cells[cellKey] = new CellState(
                layoutCell.Row,
                layoutCell.Col,
                layoutCell.Value,
                layoutCell.Formula,
                layoutCell.FormulaRef,
                layoutCell.StylePlan.EffectiveStyle,
                layoutCell.Merge);

            if (!layoutCell.Merge)
            {
                continue;
            }

            mergedRanges.Add(
                new MergedCellRange(
                    layoutCell.Row,
                    layoutCell.Col,
                    layoutCell.Row + layoutCell.RowSpan - 1,
                    layoutCell.Col + layoutCell.ColSpan - 1));

            foreach (var coveredCoordinate in EnumerateCoveredCoordinates(layoutCell))
            {
                mergedCoverage.Add(coveredCoordinate);
            }
        }

        var namedAreas = BuildNamedAreas(layoutSheet);
        var formulaPlaceholderAreas = BuildFormulaPlaceholderAreas(layoutSheet, namedAreas);
        var resolvedCells = ResolveFormulaPlaceholders(cells, layoutSheet.Cells, formulaPlaceholderAreas, issues);
        var charts = BuildCharts(layoutSheet, resolvedCells, namedAreas, formulaPlaceholderAreas, chartPalette, issues);
        var options = BuildOptions(layoutSheet.Options, layoutSheet.ConditionalFormattings, namedAreas, formulaPlaceholderAreas, issues);

        return new WorksheetState(
            layoutSheet.Name,
            layoutSheet.Rows,
            layoutSheet.Cols,
            resolvedCells,
            mergedRanges,
            namedAreas,
            charts,
            options);
    }

    private static IReadOnlyDictionary<string, NamedAreaState> BuildNamedAreas(LayoutSheet layoutSheet)
    {
        var namedAreas = new Dictionary<string, NamedAreaState>(StringComparer.Ordinal);

        foreach (var area in layoutSheet.NamedAreas)
        {
            ValidateAreaBounds(layoutSheet, area);

            namedAreas[area.Name] = new NamedAreaState(
                area.Name,
                area.TopRow,
                area.LeftColumn,
                area.BottomRow,
                area.RightColumn);
        }

        return namedAreas;
    }

    private static FormulaPlaceholderContext BuildFormulaPlaceholderAreas(
        LayoutSheet layoutSheet,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas)
    {
        var globalAreas = new Dictionary<string, NamedAreaState>(namedAreas, StringComparer.Ordinal);
        var localAreasByScope = new Dictionary<string, Dictionary<string, NamedAreaState>>(StringComparer.Ordinal);
        AddFormulaReferenceNamedAreas(layoutSheet, globalAreas, localAreasByScope);
        return new FormulaPlaceholderContext(globalAreas, localAreasByScope);
    }

    private static IReadOnlyDictionary<(int Row, int Column), CellState> ResolveFormulaPlaceholders(
        IReadOnlyDictionary<(int Row, int Column), CellState> cells,
        IReadOnlyList<LayoutCell> layoutCells,
        FormulaPlaceholderContext context,
        IList<Issue>? issues)
    {
        var layoutCellByCoordinate = layoutCells.ToDictionary(cell => (cell.Row, cell.Col));
        var resolvedCells = new Dictionary<(int Row, int Column), CellState>(cells.Count);

        foreach (var (coordinate, cell) in cells)
        {
            if (!cell.IsFormula || cell.Formula is null)
            {
                resolvedCells[coordinate] = cell;
                continue;
            }

            if (!layoutCellByCoordinate.TryGetValue(coordinate, out var layoutCell))
            {
                resolvedCells[coordinate] = cell;
                continue;
            }

            var resolvedFormula = ReplaceFormulaPlaceholders(cell.Formula, layoutCell.ScopePath, context, issues);
            if (string.Equals(resolvedFormula, cell.Formula, StringComparison.Ordinal))
            {
                resolvedCells[coordinate] = cell;
                continue;
            }

            resolvedCells[coordinate] = new CellState(
                cell.Row,
                cell.Column,
                cell.Value,
                resolvedFormula,
                cell.FormulaReference,
                cell.Style,
                cell.IsMergedHead);
        }

        return resolvedCells;
    }

    private static string ReplaceFormulaPlaceholders(
        string formula,
        string scopePath,
        FormulaPlaceholderContext context,
        IList<Issue>? issues) =>
        FormulaPlaceholderRegex.Replace(
            formula,
            match =>
            {
                var startName = match.Groups["start"].Value.Trim();
                if (startName.Length == 0)
                {
                    return match.Value;
                }

                var startArea = FindNamedArea(startName, scopePath, context, issues);
                if (startArea is null)
                {
                    return match.Value;
                }

                var startReference = ToCellReference(startArea.TopRow, startArea.LeftColumn);
                var endGroup = match.Groups["end"];
                if (!endGroup.Success)
                {
                    return startReference;
                }

                var endName = endGroup.Value.Trim();
                if (endName.Length == 0)
                {
                    return match.Value;
                }

                var endArea = FindNamedArea(endName, scopePath, context, issues);
                if (endArea is null)
                {
                    return match.Value;
                }

                var endReference = ToCellReference(endArea.BottomRow, endArea.RightColumn);
                return $"{startReference}:{endReference}";
            });

    private static void AddFormulaReferenceNamedAreas(
        LayoutSheet layoutSheet,
        IDictionary<string, NamedAreaState> globalNamedAreas,
        IDictionary<string, Dictionary<string, NamedAreaState>> localNamedAreasByScope)
    {
        var globalSeriesByName = new Dictionary<string, List<LayoutCell>>(StringComparer.Ordinal);
        var localSeriesByScopeAndName = new Dictionary<string, Dictionary<string, List<LayoutCell>>>(StringComparer.Ordinal);

        foreach (var cell in layoutSheet.Cells)
        {
            var formulaRefName = cell.FormulaRef?.Trim();
            if (string.IsNullOrWhiteSpace(formulaRefName))
            {
                continue;
            }

            var formulaRefScope = ResolveFormulaRefScope(cell.FormulaRefScope);
            if (string.Equals(formulaRefScope, "local", StringComparison.Ordinal))
            {
                if (!localSeriesByScopeAndName.TryGetValue(cell.ScopePath, out var byName))
                {
                    byName = new Dictionary<string, List<LayoutCell>>(StringComparer.Ordinal);
                    localSeriesByScopeAndName[cell.ScopePath] = byName;
                }

                if (!byName.TryGetValue(formulaRefName, out var localSeriesCells))
                {
                    localSeriesCells = [];
                    byName[formulaRefName] = localSeriesCells;
                }

                localSeriesCells.Add(cell);
                continue;
            }

            if (!globalSeriesByName.TryGetValue(formulaRefName, out var globalSeriesCells))
            {
                globalSeriesCells = [];
                globalSeriesByName[formulaRefName] = globalSeriesCells;
            }

            globalSeriesCells.Add(cell);
        }

        foreach (var (name, seriesCells) in globalSeriesByName)
        {
            var orderedCells = seriesCells
                .OrderBy(cell => cell.Row)
                .ThenBy(cell => cell.Col)
                .ToArray();
            var firstCell = orderedCells[0];
            var lastCell = orderedCells[^1];

            TryAddFormulaReferenceNamedArea(globalNamedAreas, name, firstCell);
            TryAddFormulaReferenceNamedArea(globalNamedAreas, $"{name}End", lastCell);
        }

        foreach (var (scopePath, scopedSeriesByName) in localSeriesByScopeAndName)
        {
            var scopedNamedAreas = new Dictionary<string, NamedAreaState>(StringComparer.Ordinal);

            foreach (var (name, seriesCells) in scopedSeriesByName)
            {
                var orderedCells = seriesCells
                    .OrderBy(cell => cell.Row)
                    .ThenBy(cell => cell.Col)
                    .ToArray();
                var firstCell = orderedCells[0];
                var lastCell = orderedCells[^1];

                TryAddFormulaReferenceNamedArea(scopedNamedAreas, name, firstCell);
                TryAddFormulaReferenceNamedArea(scopedNamedAreas, $"{name}End", lastCell);
            }

            localNamedAreasByScope[scopePath] = scopedNamedAreas;
        }
    }

    private static NamedAreaState? FindNamedArea(
        string name,
        string scopePath,
        FormulaPlaceholderContext context,
        IList<Issue>? issues)
    {
        var currentScope = scopePath;
        while (currentScope.Length > 0)
        {
            if (context.LocalAreasByScope.TryGetValue(currentScope, out var scopedAreas)
                && scopedAreas.TryGetValue(name, out var scopedArea))
            {
                return scopedArea;
            }

            var separatorIndex = currentScope.LastIndexOf('/');
            if (separatorIndex <= 0)
            {
                break;
            }

            currentScope = currentScope[..separatorIndex];
        }

        var hasGlobalArea = context.GlobalAreas.TryGetValue(name, out var globalArea);

        // Allow sibling visibility by resolving a unique descendant local area under the current scope.
        if (TryResolveUniqueDescendantLocalArea(name, scopePath, context.LocalAreasByScope, out var descendantArea, out var descendantMatchCount))
        {
            if (hasGlobalArea)
            {
                AddFormulaRefResolutionWarning(
                    issues,
                    $"Local formulaRef resolution for target '{name}' in scope '{scopePath}' found both global and unique descendant local candidates; preferring global lookup.");
                return globalArea;
            }

            return descendantArea;
        }

        if (descendantMatchCount > 1)
        {
            AddFormulaRefResolutionWarning(
                issues,
                $"Ambiguous local formulaRef resolution for target '{name}' in scope '{scopePath}': found {descendantMatchCount} descendant candidates, falling back to global/named lookup.");
        }

        return hasGlobalArea ? globalArea : null;
    }

    private static bool TryResolveUniqueDescendantLocalArea(
        string name,
        string scopePath,
        IReadOnlyDictionary<string, Dictionary<string, NamedAreaState>> localAreasByScope,
        out NamedAreaState area,
        out int matchCount)
    {
        var descendantMatches = localAreasByScope
            .Where(pair =>
                !string.Equals(pair.Key, scopePath, StringComparison.Ordinal) &&
                IsScopeWithinDefinitionScope(pair.Key, scopePath))
            .Select(pair => pair.Value.TryGetValue(name, out var localArea) ? localArea : null)
            .Where(localArea => localArea is not null)
            .Cast<NamedAreaState>()
            .DistinctBy(match => (match.TopRow, match.LeftColumn, match.BottomRow, match.RightColumn))
            .ToArray();

        matchCount = descendantMatches.Length;
        if (descendantMatches.Length == 1)
        {
            area = descendantMatches[0];
            return true;
        }

        area = default!;
        return false;
    }

    private static string ResolveFormulaRefScope(string? formulaRefScope)
    {
        if (string.Equals(formulaRefScope?.Trim(), "local", StringComparison.OrdinalIgnoreCase))
        {
            return "local";
        }

        return "global";
    }

    private sealed record FormulaPlaceholderContext(
        IReadOnlyDictionary<string, NamedAreaState> GlobalAreas,
        IReadOnlyDictionary<string, Dictionary<string, NamedAreaState>> LocalAreasByScope);

    private static void TryAddFormulaReferenceNamedArea(
        IDictionary<string, NamedAreaState> namedAreas,
        string name,
        LayoutCell cell)
    {
        if (namedAreas.ContainsKey(name))
        {
            return;
        }

        namedAreas[name] = CreateCellNamedArea(name, cell);
    }

    private static NamedAreaState CreateCellNamedArea(string name, LayoutCell cell) =>
        new(
            name,
            cell.Row,
            cell.Col,
            cell.Row + cell.RowSpan - 1,
            cell.Col + cell.ColSpan - 1);

    private static WorksheetOptionsState BuildOptions(
        SheetOptionsAst? options,
        IReadOnlyList<LayoutConditionalFormatting> conditionalFormattings,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas,
        FormulaPlaceholderContext formulaPlaceholderContext,
        IList<Issue>? issues)
    {
        if (options is null && conditionalFormattings.Count == 0)
        {
            return WorksheetOptionsState.Empty;
        }

        var freeze = options?.Freeze;
        var groupRows = options?.GroupRows ?? Array.Empty<GroupRowsAst>();
        var groupCols = options?.GroupCols ?? Array.Empty<GroupColsAst>();
        var autoFilter = options?.AutoFilter;

        return new WorksheetOptionsState(
            freeze is null
                ? null
                : new FreezePaneState(ResolveFreezeTarget(freeze.At, namedAreas)),
            groupRows
                .Select(group => new WorksheetGroupState(ResolveRowGroupTarget(group.At, namedAreas), group.Collapsed))
                .ToArray(),
            groupCols
                .Select(group => new WorksheetGroupState(ResolveColumnGroupTarget(group.At, namedAreas), group.Collapsed))
                .ToArray(),
            autoFilter is null
                ? null
                : new AutoFilterState(ResolveAutoFilterTarget(autoFilter.At, namedAreas)),
            conditionalFormattings
                .SelectMany(
                    scopedRule => ResolveConditionalFormattingTargets(scopedRule.Rule.At, namedAreas, formulaPlaceholderContext, scopedRule.ScopePath)
                        .Select(
                            resolvedTarget => new ConditionalFormattingState(
                                resolvedTarget,
                                scopedRule.Rule.MinColor,
                                scopedRule.Rule.MaxColor,
                                scopedRule.Rule.MidColor,
                                scopedRule.Rule.Formula,
                                ResolveConditionalFormulaRefTarget(scopedRule.Rule.FormulaRef, resolvedTarget, namedAreas, formulaPlaceholderContext, scopedRule.ScopePath, issues),
                                scopedRule.Rule.FillColor,
                                scopedRule.Rule.FontName,
                                scopedRule.Rule.FontSize,
                                scopedRule.Rule.FontBold,
                                scopedRule.Rule.FontItalic,
                                scopedRule.Rule.FontUnderline,
                                scopedRule.Rule.NumberFormatCode,
                                scopedRule.Rule.BorderTop,
                                scopedRule.Rule.BorderBottom,
                                scopedRule.Rule.BorderLeft,
                                scopedRule.Rule.BorderRight,
                                scopedRule.Rule.BorderColor)))
                .ToArray());
    }

    private static IReadOnlyList<ChartState> BuildCharts(
        LayoutSheet layoutSheet,
        IReadOnlyDictionary<(int Row, int Column), CellState> cells,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas,
        FormulaPlaceholderContext formulaPlaceholderContext,
        IReadOnlyDictionary<string, string> chartPalette,
        IList<Issue>? issues)
    {
        if (layoutSheet.Charts.Count == 0)
        {
            return Array.Empty<ChartState>();
        }

        var normalizedPalette = chartPalette.ToDictionary(
            pair => pair.Key,
            pair => NormalizeRgbColor(pair.Value),
            StringComparer.Ordinal);
        var keyColorAssignments = new Dictionary<string, string>(normalizedPalette, StringComparer.Ordinal);
        var nextPaletteIndex = 0;
        var results = new List<ChartState>();

        foreach (var chart in layoutSheet.Charts)
        {
            if (!TryResolveChartReference(
                    chart.CategoryReference,
                    layoutSheet.Name,
                    cells,
                    namedAreas,
                    formulaPlaceholderContext,
                    requireValues: false,
                    issues,
                    out var categoryReference))
            {
                AddChartIssue(
                    issues,
                    $"chart '{chart.Name ?? chart.ChartType}' の category 参照解決に失敗しました: {chart.CategoryReference}");
                continue;
            }

            var resolvedSeries = new List<ChartSeriesState>();
            var seriesIndex = 0;
            foreach (var series in chart.Series)
            {
                if (!TryResolveChartReference(
                        series.ValueReference,
                        layoutSheet.Name,
                        cells,
                        namedAreas,
                        formulaPlaceholderContext,
                        requireValues: false,
                        issues,
                        out var valueReference))
                {
                    AddChartIssue(
                        issues,
                        $"chart '{chart.Name ?? chart.ChartType}' の series 参照解決に失敗しました: {series.ValueReference}");
                    seriesIndex++;
                    continue;
                }

                if (valueReference.Length != categoryReference.Length)
                {
                    AddChartIssue(
                        issues,
                        $"chart '{chart.Name ?? chart.ChartType}' の系列長が不一致です: category={categoryReference.Length}, value={valueReference.Length}");
                    seriesIndex++;
                    continue;
                }

                if (!TryResolveSeriesPointColors(
                        series,
                        categoryReference.Length,
                        layoutSheet.Name,
                        cells,
                        namedAreas,
                        formulaPlaceholderContext,
                        normalizedPalette,
                        keyColorAssignments,
                        ref nextPaletteIndex,
                        seriesIndex,
                        issues,
                        out var pointColors))
                {
                    seriesIndex++;
                    continue;
                }

                resolvedSeries.Add(
                    new ChartSeriesState(
                        series.Name,
                        valueReference.Formula,
                        pointColors));
                seriesIndex++;
            }

            if (resolvedSeries.Count == 0)
            {
                AddChartIssue(
                    issues,
                    $"chart '{chart.Name ?? chart.ChartType}' に有効な series が存在しないためスキップしました。");
                continue;
            }

            results.Add(
                new ChartState(
                    chart.ChartType,
                    chart.Title,
                    chart.Name,
                    chart.TopRow,
                    chart.LeftColumn,
                    chart.WidthColumns,
                    chart.HeightRows,
                    categoryReference.Formula,
                    chart.LegendPosition,
                    chart.ShowDataLabels,
                    resolvedSeries));
        }

        return results;
    }

    private static bool TryResolveSeriesPointColors(
        LayoutChartSeries series,
        int pointCount,
        string currentSheetName,
        IReadOnlyDictionary<(int Row, int Column), CellState> cells,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas,
        FormulaPlaceholderContext formulaPlaceholderContext,
        IReadOnlyDictionary<string, string> chartPalette,
        IDictionary<string, string> keyColorAssignments,
        ref int nextPaletteIndex,
        int seriesIndex,
        IList<Issue>? issues,
        out IReadOnlyList<string>? pointColors)
    {
        if (!string.IsNullOrWhiteSpace(series.Color))
        {
            pointColors = Enumerable.Repeat(NormalizeRgbColor(series.Color), pointCount).ToArray();
            return true;
        }

        if (!string.IsNullOrWhiteSpace(series.ColorByReference))
        {
            if (!TryResolveChartReference(
                    series.ColorByReference,
                    currentSheetName,
                    cells,
                    namedAreas,
                    formulaPlaceholderContext,
                    requireValues: true,
                    issues,
                    out var colorByReference))
            {
                AddChartIssue(
                    issues,
                    $"series '{series.Name ?? series.ValueReference}' の colorBy 参照解決に失敗しました: {series.ColorByReference}");
                pointColors = null;
                return false;
            }

            if (colorByReference.Length != pointCount || colorByReference.Values is null)
            {
                AddChartIssue(
                    issues,
                    $"series '{series.Name ?? series.ValueReference}' の colorBy 系列長が不一致です: expected={pointCount}, actual={colorByReference.Length}");
                pointColors = null;
                return false;
            }

            var resolved = new string[colorByReference.Values.Count];
            for (var index = 0; index < colorByReference.Values.Count; index++)
            {
                var key = colorByReference.Values[index]?.ToString()?.Trim() ?? string.Empty;
                resolved[index] = ResolveColorByKey(
                    key,
                    chartPalette,
                    keyColorAssignments,
                    ref nextPaletteIndex);
            }

            pointColors = resolved;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(series.ColorKey))
        {
            var resolvedColor = ResolveColorByKey(
                series.ColorKey,
                chartPalette,
                keyColorAssignments,
                ref nextPaletteIndex);
            pointColors = Enumerable.Repeat(resolvedColor, pointCount).ToArray();
            return true;
        }

        var defaultColor = DefaultChartPalette[seriesIndex % DefaultChartPalette.Length];
        pointColors = Enumerable.Repeat(defaultColor, pointCount).ToArray();
        return true;
    }

    private static bool TryResolveChartReference(
        string rawReference,
        string currentSheetName,
        IReadOnlyDictionary<(int Row, int Column), CellState> cells,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas,
        FormulaPlaceholderContext formulaPlaceholderContext,
        bool requireValues,
        IList<Issue>? issues,
        out ChartReferenceResolution resolution)
    {
        if (string.IsNullOrWhiteSpace(rawReference))
        {
            resolution = default!;
            return false;
        }

        if (namedAreas.TryGetValue(rawReference, out var namedArea))
        {
            return TryResolveChartReferenceFromArea(
                namedArea,
                currentSheetName,
                cells,
                requireValues,
                issues,
                out resolution);
        }

        var localFormulaRefAreas = ResolveLocalFormulaRefSeriesAreas(
            rawReference,
            formulaPlaceholderContext.LocalAreasByScope,
            "/sheet");
        if (localFormulaRefAreas.Count == 1)
        {
            return TryResolveChartReferenceFromArea(
                localFormulaRefAreas[0],
                currentSheetName,
                cells,
                requireValues,
                issues,
                out resolution);
        }

        if (localFormulaRefAreas.Count > 1)
        {
            AddChartIssue(
                issues,
                $"chart 参照 '{rawReference}' は local formulaRef の候補が複数あるため解決できません。");
            resolution = default!;
            return false;
        }

        if (TryResolveFormulaRefSeriesArea(rawReference, formulaPlaceholderContext.GlobalAreas, out var formulaRefArea))
        {
            return TryResolveChartReferenceFromArea(
                formulaRefArea,
                currentSheetName,
                cells,
                requireValues,
                issues,
                out resolution);
        }

        if (!TryParseChartRangeReference(rawReference, out var targetSheetName, out var topRow, out var leftColumn, out var bottomRow, out var rightColumn))
        {
            resolution = default!;
            return false;
        }

        if (!IsOneDimensionalRange(topRow, leftColumn, bottomRow, rightColumn))
        {
            issues?.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.FormulaRefSeriesNot1DContinuous,
                Message = $"chart 参照は 1 次元連続範囲である必要があります: {rawReference}",
            });
            resolution = default!;
            return false;
        }

        var sheetName = string.IsNullOrWhiteSpace(targetSheetName) ? currentSheetName : targetSheetName;
        var values = default(IReadOnlyList<object?>);
        if (requireValues)
        {
            if (!string.Equals(sheetName, currentSheetName, StringComparison.Ordinal))
            {
                AddChartIssue(
                    issues,
                    $"chart の colorBy で cross-sheet 直接範囲はサポートしていません: {rawReference}");
                resolution = default!;
                return false;
            }

            values = EnumerateRangeValues(cells, topRow, leftColumn, bottomRow, rightColumn);
        }

        var length = CalculateRangeLength(topRow, leftColumn, bottomRow, rightColumn);
        resolution = new ChartReferenceResolution(
            ToAbsoluteRangeFormula(sheetName, topRow, leftColumn, bottomRow, rightColumn),
            length,
            values);
        return true;
    }

    private static bool TryResolveChartReferenceFromArea(
        NamedAreaState area,
        string currentSheetName,
        IReadOnlyDictionary<(int Row, int Column), CellState> cells,
        bool requireValues,
        IList<Issue>? issues,
        out ChartReferenceResolution resolution)
    {
        if (!IsOneDimensionalRange(area.TopRow, area.LeftColumn, area.BottomRow, area.RightColumn))
        {
            issues?.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.FormulaRefSeriesNot1DContinuous,
                Message = $"chart 参照は 1 次元連続範囲である必要があります: {area.Name}",
            });
            resolution = default!;
            return false;
        }

        var values = requireValues
            ? EnumerateRangeValues(cells, area.TopRow, area.LeftColumn, area.BottomRow, area.RightColumn)
            : null;
        var length = CalculateRangeLength(area.TopRow, area.LeftColumn, area.BottomRow, area.RightColumn);
        resolution = new ChartReferenceResolution(
            ToAbsoluteRangeFormula(currentSheetName, area.TopRow, area.LeftColumn, area.BottomRow, area.RightColumn),
            length,
            values);
        return true;
    }

    private static IReadOnlyList<object?> EnumerateRangeValues(
        IReadOnlyDictionary<(int Row, int Column), CellState> cells,
        int topRow,
        int leftColumn,
        int bottomRow,
        int rightColumn)
    {
        var values = new List<object?>();
        if (leftColumn == rightColumn)
        {
            for (var row = topRow; row <= bottomRow; row++)
            {
                values.Add(cells.TryGetValue((row, leftColumn), out var cell) ? cell.Value : null);
            }

            return values;
        }

        for (var column = leftColumn; column <= rightColumn; column++)
        {
            values.Add(cells.TryGetValue((topRow, column), out var cell) ? cell.Value : null);
        }

        return values;
    }

    private static bool TryParseChartRangeReference(
        string reference,
        out string? sheetName,
        out int topRow,
        out int leftColumn,
        out int bottomRow,
        out int rightColumn)
    {
        sheetName = null;
        topRow = leftColumn = bottomRow = rightColumn = 0;

        var trimmed = reference.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        var separatorIndex = trimmed.LastIndexOf('!');
        var rangeToken = trimmed;
        if (separatorIndex >= 0)
        {
            sheetName = UnescapeSheetName(trimmed[..separatorIndex]);
            rangeToken = trimmed[(separatorIndex + 1)..];
        }

        var parts = rangeToken.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 2)
        {
            return false;
        }

        if (!TryParseChartCellReference(parts[0], out var firstRow, out var firstColumn))
        {
            return false;
        }

        var lastRow = firstRow;
        var lastColumn = firstColumn;
        if (parts.Length == 2 && !TryParseChartCellReference(parts[1], out lastRow, out lastColumn))
        {
            return false;
        }

        topRow = Math.Min(firstRow, lastRow);
        bottomRow = Math.Max(firstRow, lastRow);
        leftColumn = Math.Min(firstColumn, lastColumn);
        rightColumn = Math.Max(firstColumn, lastColumn);
        return true;
    }

    private static bool TryParseChartCellReference(string token, out int row, out int column)
    {
        row = 0;
        column = 0;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var match = ChartCellReferenceRegex.Match(token.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["row"].Value, out row) || row <= 0)
        {
            return false;
        }

        var letters = match.Groups["col"].Value.ToUpperInvariant();
        foreach (var letter in letters)
        {
            if (letter is < 'A' or > 'Z')
            {
                return false;
            }

            column = (column * 26) + (letter - 'A' + 1);
        }

        return column > 0;
    }

    private static string? UnescapeSheetName(string token)
    {
        var trimmed = token.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.StartsWith('\'') && trimmed.EndsWith('\'') && trimmed.Length >= 2)
        {
            return trimmed[1..^1].Replace("''", "'", StringComparison.Ordinal);
        }

        return trimmed;
    }

    private static bool IsOneDimensionalRange(int topRow, int leftColumn, int bottomRow, int rightColumn) =>
        topRow == bottomRow || leftColumn == rightColumn;

    private static int CalculateRangeLength(int topRow, int leftColumn, int bottomRow, int rightColumn) =>
        leftColumn == rightColumn
            ? (bottomRow - topRow + 1)
            : (rightColumn - leftColumn + 1);

    private static string ResolveColorByKey(
        string key,
        IReadOnlyDictionary<string, string> chartPalette,
        IDictionary<string, string> keyColorAssignments,
        ref int nextPaletteIndex)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return DefaultChartPalette[nextPaletteIndex++ % DefaultChartPalette.Length];
        }

        if (chartPalette.TryGetValue(key, out var paletteColor))
        {
            return paletteColor;
        }

        if (keyColorAssignments.TryGetValue(key, out var assignedColor))
        {
            return assignedColor;
        }

        var nextColor = DefaultChartPalette[nextPaletteIndex++ % DefaultChartPalette.Length];
        keyColorAssignments[key] = nextColor;
        return nextColor;
    }

    private static string NormalizeRgbColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "#000000";
        }

        var trimmed = color.Trim();
        if (!trimmed.StartsWith('#'))
        {
            trimmed = $"#{trimmed}";
        }

        return trimmed.ToUpperInvariant();
    }

    private static string ToAbsoluteRangeFormula(
        string sheetName,
        int topRow,
        int leftColumn,
        int bottomRow,
        int rightColumn)
    {
        var escapedSheetName = EscapeSheetName(sheetName);
        var start = ToAbsoluteCellReference(topRow, leftColumn);
        var end = ToAbsoluteCellReference(bottomRow, rightColumn);
        return start == end
            ? $"{escapedSheetName}!{start}"
            : $"{escapedSheetName}!{start}:{end}";
    }

    private static string EscapeSheetName(string sheetName) =>
        $"'{sheetName.Replace("'", "''", StringComparison.Ordinal)}'";

    private static string ToAbsoluteCellReference(int row, int column) =>
        $"${ColumnIndexToName(column)}${row}";

    private static void AddChartIssue(IList<Issue>? issues, string message)
    {
        if (issues is null)
        {
            return;
        }

        issues.Add(new Issue
        {
            Severity = IssueSeverity.Error,
            Kind = IssueKind.InvalidAttributeValue,
            Message = message,
        });
    }

    private sealed record ChartReferenceResolution(
        string Formula,
        int Length,
        IReadOnlyList<object?>? Values);

    private static string ResolveFreezeTarget(
        string target,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas)
    {
        if (!namedAreas.TryGetValue(target, out var area))
        {
            return target;
        }

        return ToCellReference(area.TopRow, area.LeftColumn);
    }

    private static string ResolveRowGroupTarget(
        string target,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas)
    {
        if (!namedAreas.TryGetValue(target, out var area))
        {
            return target;
        }

        return $"{area.TopRow}:{area.BottomRow}";
    }

    private static string ResolveColumnGroupTarget(
        string target,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas)
    {
        if (!namedAreas.TryGetValue(target, out var area))
        {
            return target;
        }

        return $"{ColumnIndexToName(area.LeftColumn)}:{ColumnIndexToName(area.RightColumn)}";
    }

    private static string ResolveAutoFilterTarget(
        string target,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas)
    {
        if (!namedAreas.TryGetValue(target, out var area))
        {
            return target;
        }

        return $"{ToCellReference(area.TopRow, area.LeftColumn)}:{ToCellReference(area.BottomRow, area.RightColumn)}";
    }

    private static IReadOnlyList<string> ResolveConditionalFormattingTargets(
        string target,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas,
        FormulaPlaceholderContext formulaPlaceholderContext,
        string definitionScopePath)
    {
        if (namedAreas.TryGetValue(target, out var namedArea))
        {
            return [ToRangeReference(namedArea)];
        }

        var localSeriesAreas = ResolveLocalFormulaRefSeriesAreas(target, formulaPlaceholderContext.LocalAreasByScope, definitionScopePath);
        if (localSeriesAreas.Count > 0)
        {
            return localSeriesAreas
                .Select(ToRangeReference)
                .ToArray();
        }

        if (TryResolveFormulaRefSeriesArea(target, formulaPlaceholderContext.GlobalAreas, out var globalSeriesArea))
        {
            return [ToRangeReference(globalSeriesArea)];
        }

        return [target];
    }

    private static string? ResolveConditionalFormulaRefTarget(
        string? formulaRef,
        string conditionalFormattingTarget,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas,
        FormulaPlaceholderContext formulaPlaceholderContext,
        string definitionScopePath,
        IList<Issue>? issues)
    {
        if (string.IsNullOrWhiteSpace(formulaRef))
        {
            return null;
        }

        if (TryResolveTargetArea(conditionalFormattingTarget, out var targetArea))
        {
            var scopedCandidates = formulaPlaceholderContext.LocalAreasByScope
                .Where(pair => IsScopeWithinDefinitionScope(pair.Key, definitionScopePath))
                .Select(pair => new
                {
                    Scope = pair.Key,
                    Area = pair.Value.TryGetValue(formulaRef, out var localArea) ? localArea : null,
                })
                .Where(candidate => candidate.Area is not null && AreasIntersect(candidate.Area, targetArea))
                .Select(candidate => new { candidate.Scope, Area = candidate.Area! })
                .ToArray();

            if (scopedCandidates.Length == 1)
            {
                return ToCellReference(scopedCandidates[0].Area.TopRow, scopedCandidates[0].Area.LeftColumn);
            }

            if (scopedCandidates.Length > 1)
            {
                var selected = scopedCandidates
                    .OrderByDescending(candidate => CalculateIntersectionArea(candidate.Area, targetArea))
                    .ThenByDescending(candidate => candidate.Scope.Count(c => c == '/'))
                    .ThenBy(candidate => candidate.Scope, StringComparer.Ordinal)
                    .First();

                AddFormulaRefResolutionWarning(
                    issues,
                    $"Ambiguous local conditional formulaRef resolution for target '{formulaRef}' at '{conditionalFormattingTarget}': {scopedCandidates.Length} scoped candidates matched, selected scope '{selected.Scope}' by deterministic tie-break.");

                return ToCellReference(selected.Area.TopRow, selected.Area.LeftColumn);
            }
        }

        var uniqueLocalArea = formulaPlaceholderContext.LocalAreasByScope
            .Where(pair => ShouldResolveLocalScopeCandidate(pair.Key, definitionScopePath))
            .Select(pair => pair.Value)
            .Select(areasByName => areasByName.TryGetValue(formulaRef, out var localArea) ? localArea : null)
            .Where(localArea => localArea is not null)
            .Cast<NamedAreaState>()
            .Distinct()
            .ToArray();
        if (uniqueLocalArea.Length == 1)
        {
            return ToCellReference(uniqueLocalArea[0].TopRow, uniqueLocalArea[0].LeftColumn);
        }

        if (uniqueLocalArea.Length > 1)
        {
            if (formulaPlaceholderContext.GlobalAreas.TryGetValue(formulaRef, out var globalArea))
            {
                AddFormulaRefResolutionWarning(
                    issues,
                    $"Ambiguous local conditional formulaRef resolution for target '{formulaRef}' in scope '{definitionScopePath}': multiple local candidates found, falling back to global lookup.");
                return ToCellReference(globalArea.TopRow, globalArea.LeftColumn);
            }

            if (namedAreas.TryGetValue(formulaRef, out var namedArea))
            {
                AddFormulaRefResolutionWarning(
                    issues,
                    $"Ambiguous local conditional formulaRef resolution for target '{formulaRef}' in scope '{definitionScopePath}': multiple local candidates found, falling back to named-area lookup.");
                return ToCellReference(namedArea.TopRow, namedArea.LeftColumn);
            }

            AddFormulaRefResolutionWarning(
                issues,
                $"Ambiguous local conditional formulaRef resolution for target '{formulaRef}' in scope '{definitionScopePath}': multiple local candidates found, falling back to raw target.");
            return formulaRef;
        }

        if (formulaPlaceholderContext.GlobalAreas.TryGetValue(formulaRef, out var area) ||
            namedAreas.TryGetValue(formulaRef, out area))
        {
            return ToCellReference(area.TopRow, area.LeftColumn);
        }

        return formulaRef;
    }

    private static void AddFormulaRefResolutionWarning(IList<Issue>? issues, string message)
    {
        if (issues is null)
        {
            return;
        }

        issues.Add(
            new Issue
            {
                Severity = IssueSeverity.Warning,
                Kind = IssueKind.FormulaRefResolutionFallback,
                Message = message,
            });
    }

    private static bool TryResolveTargetArea(string target, out NamedAreaState area)
    {
        if (TryParseCellReference(target, out var row, out var col))
        {
            area = new NamedAreaState("_target", row, col, row, col);
            return true;
        }

        if (TryParseRangeReference(target, out var topRow, out var leftCol, out var bottomRow, out var rightCol))
        {
            area = new NamedAreaState("_target", topRow, leftCol, bottomRow, rightCol);
            return true;
        }

        area = default!;
        return false;
    }

    private static bool AreasIntersect(NamedAreaState left, NamedAreaState right) =>
        left.TopRow <= right.BottomRow &&
        left.BottomRow >= right.TopRow &&
        left.LeftColumn <= right.RightColumn &&
        left.RightColumn >= right.LeftColumn;

    private static IReadOnlyList<NamedAreaState> ResolveLocalFormulaRefSeriesAreas(
        string formulaRefName,
        IReadOnlyDictionary<string, Dictionary<string, NamedAreaState>> localAreasByScope,
        string definitionScopePath) =>
        localAreasByScope
            // local は sheet 直下定義では漏らさず、それ以外は定義配下スコープを解決する。
            .Where(pair => ShouldResolveLocalScopeCandidate(pair.Key, definitionScopePath))
            .Select(pair => pair.Value)
            .Select(
                localAreas =>
                    TryResolveFormulaRefSeriesArea(formulaRefName, localAreas, out var seriesArea)
                        ? seriesArea
                        : null)
            .Where(seriesArea => seriesArea is not null)
            .Select(seriesArea => seriesArea!)
            .DistinctBy(
                area => (area.TopRow, area.LeftColumn, area.BottomRow, area.RightColumn))
            .OrderBy(area => area.TopRow)
            .ThenBy(area => area.LeftColumn)
            .ThenBy(area => area.BottomRow)
            .ThenBy(area => area.RightColumn)
            .ToArray();

    private static bool ShouldResolveLocalScopeCandidate(string candidateScopePath, string definitionScopePath)
    {
        // sheet 直下定義は local 非リーク維持のため exact match のみ許可する。
        if (string.Equals(definitionScopePath, "/sheet", StringComparison.Ordinal))
        {
            return string.Equals(candidateScopePath, definitionScopePath, StringComparison.Ordinal);
        }

        return IsScopeWithinDefinitionScope(candidateScopePath, definitionScopePath);
    }

    private static bool IsScopeWithinDefinitionScope(string candidateScopePath, string definitionScopePath)
    {
        if (string.IsNullOrWhiteSpace(definitionScopePath))
        {
            return true;
        }

        if (string.Equals(candidateScopePath, definitionScopePath, StringComparison.Ordinal))
        {
            return true;
        }

        return candidateScopePath.StartsWith(definitionScopePath + "/", StringComparison.Ordinal);
    }

    private static bool TryResolveFormulaRefSeriesArea(
        string formulaRefName,
        IReadOnlyDictionary<string, NamedAreaState> areasByName,
        out NamedAreaState seriesArea)
    {
        if (!areasByName.TryGetValue(formulaRefName, out var startArea))
        {
            seriesArea = default!;
            return false;
        }

        if (!areasByName.TryGetValue($"{formulaRefName}End", out var endArea))
        {
            seriesArea = startArea;
            return true;
        }

        seriesArea = new NamedAreaState(
            formulaRefName,
            Math.Min(startArea.TopRow, endArea.TopRow),
            Math.Min(startArea.LeftColumn, endArea.LeftColumn),
            Math.Max(startArea.BottomRow, endArea.BottomRow),
            Math.Max(startArea.RightColumn, endArea.RightColumn));
        return true;
    }

    private static int CalculateIntersectionArea(NamedAreaState left, NamedAreaState right)
    {
        var top = Math.Max(left.TopRow, right.TopRow);
        var bottom = Math.Min(left.BottomRow, right.BottomRow);
        var leftCol = Math.Max(left.LeftColumn, right.LeftColumn);
        var rightCol = Math.Min(left.RightColumn, right.RightColumn);
        if (top > bottom || leftCol > rightCol)
        {
            return 0;
        }

        return (bottom - top + 1) * (rightCol - leftCol + 1);
    }

    private static bool TryParseRangeReference(string reference, out int topRow, out int leftColumn, out int bottomRow, out int rightColumn)
    {
        topRow = bottomRow = leftColumn = rightColumn = 0;
        var parts = reference.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!TryParseCellReference(parts[0], out var firstRow, out var firstColumn) ||
            !TryParseCellReference(parts[1], out var lastRow, out var lastColumn))
        {
            return false;
        }

        topRow = Math.Min(firstRow, lastRow);
        bottomRow = Math.Max(firstRow, lastRow);
        leftColumn = Math.Min(firstColumn, lastColumn);
        rightColumn = Math.Max(firstColumn, lastColumn);
        return true;
    }

    private static string ToRangeReference(NamedAreaState area) =>
        $"{ToCellReference(area.TopRow, area.LeftColumn)}:{ToCellReference(area.BottomRow, area.RightColumn)}";

    private static bool TryParseCellReference(string reference, out int row, out int column)
    {
        row = 0;
        column = 0;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        var trimmed = reference.Trim();
        var letters = new string(trimmed.TakeWhile(char.IsLetter).ToArray());
        var digits = new string(trimmed.SkipWhile(char.IsLetter).ToArray());
        if (letters.Length == 0 || digits.Length == 0 || !int.TryParse(digits, out row) || row <= 0)
        {
            return false;
        }

        foreach (var letter in letters.ToUpperInvariant())
        {
            if (letter < 'A' || letter > 'Z')
            {
                return false;
            }

            column = (column * 26) + (letter - 'A' + 1);
        }

        return column > 0;
    }

    private static string ToCellReference(int row, int column) =>
        $"{ColumnIndexToName(column)}{row}";

    private static string ColumnIndexToName(int columnIndex)
    {
        if (columnIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        var value = columnIndex;
        var buffer = new Stack<char>();
        while (value > 0)
        {
            value--;
            buffer.Push((char)('A' + (value % 26)));
            value /= 26;
        }

        return new string(buffer.ToArray());
    }

    private static void ValidateSheetBounds(int rowCount, int columnCount, string sheetName)
    {
        if (rowCount <= 0 || columnCount <= 0)
        {
            throw new InvalidOperationException(
                $"Sheet '{sheetName}' must declare positive bounds.");
        }
    }

    private static void ValidateCellBounds(LayoutSheet layoutSheet, LayoutCell layoutCell)
    {
        if (layoutCell.Row <= 0 ||
            layoutCell.Col <= 0 ||
            layoutCell.Row > layoutSheet.Rows ||
            layoutCell.Col > layoutSheet.Cols ||
            layoutCell.Row + layoutCell.RowSpan - 1 > layoutSheet.Rows ||
            layoutCell.Col + layoutCell.ColSpan - 1 > layoutSheet.Cols)
        {
            throw new InvalidOperationException(
                $"Cell '{layoutSheet.Name}!R{layoutCell.Row}C{layoutCell.Col}' exceeds the sheet bounds.");
        }
    }

    private static void ValidateAreaBounds(LayoutSheet layoutSheet, LayoutNamedArea area)
    {
        if (string.IsNullOrWhiteSpace(area.Name))
        {
            throw new InvalidOperationException(
                $"Sheet '{layoutSheet.Name}' contains a named area with an empty name.");
        }

        if (area.TopRow <= 0 ||
            area.LeftColumn <= 0 ||
            area.BottomRow < area.TopRow ||
            area.RightColumn < area.LeftColumn ||
            area.BottomRow > layoutSheet.Rows ||
            area.RightColumn > layoutSheet.Cols)
        {
            throw new InvalidOperationException(
                $"Named area '{area.Name}' exceeds the sheet bounds.");
        }
    }

    private static IEnumerable<(int Row, int Column)> EnumerateCoveredCoordinates(LayoutCell layoutCell)
    {
        for (var row = layoutCell.Row; row < layoutCell.Row + layoutCell.RowSpan; row++)
        {
            for (var column = layoutCell.Col; column < layoutCell.Col + layoutCell.ColSpan; column++)
            {
                yield return (row, column);
            }
        }
    }
}
