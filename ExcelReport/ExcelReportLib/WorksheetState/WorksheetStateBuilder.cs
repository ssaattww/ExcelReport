using ExcelReportLib.DSL.AST;
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

    /// <summary>
    /// Builds worksheet state models from an expanded layout plan.
    /// </summary>
    /// <param name="layoutPlan">The layout plan.</param>
    /// <returns>A collection containing the result.</returns>
    public IReadOnlyList<WorksheetState> Build(LayoutPlan layoutPlan)
    {
        ArgumentNullException.ThrowIfNull(layoutPlan);

        return layoutPlan.Sheets
            .Select(BuildSheet)
            .ToArray();
    }

    private static WorksheetState BuildSheet(LayoutSheet layoutSheet)
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
        var resolvedCells = ResolveFormulaPlaceholders(cells, layoutSheet.Cells, formulaPlaceholderAreas);
        var options = BuildOptions(layoutSheet.Options, namedAreas, formulaPlaceholderAreas);

        return new WorksheetState(
            layoutSheet.Name,
            layoutSheet.Rows,
            layoutSheet.Cols,
            resolvedCells,
            mergedRanges,
            namedAreas,
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
        FormulaPlaceholderContext context)
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

            var resolvedFormula = ReplaceFormulaPlaceholders(cell.Formula, layoutCell.ScopePath, context);
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
        FormulaPlaceholderContext context) =>
        FormulaPlaceholderRegex.Replace(
            formula,
            match =>
            {
                var startName = match.Groups["start"].Value.Trim();
                if (startName.Length == 0)
                {
                    return match.Value;
                }

                var startArea = FindNamedArea(startName, scopePath, context);
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

                var endArea = FindNamedArea(endName, scopePath, context);
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
        FormulaPlaceholderContext context)
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

        return context.GlobalAreas.TryGetValue(name, out var area) ? area : null;
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
        IReadOnlyDictionary<string, NamedAreaState> namedAreas,
        FormulaPlaceholderContext formulaPlaceholderContext)
    {
        if (options is null)
        {
            return WorksheetOptionsState.Empty;
        }

        return new WorksheetOptionsState(
            options.Freeze is null
                ? null
                : new FreezePaneState(ResolveFreezeTarget(options.Freeze.At, namedAreas)),
            options.GroupRows
                .Select(group => new WorksheetGroupState(ResolveRowGroupTarget(group.At, namedAreas), group.Collapsed))
                .ToArray(),
            options.GroupCols
                .Select(group => new WorksheetGroupState(ResolveColumnGroupTarget(group.At, namedAreas), group.Collapsed))
                .ToArray(),
            options.AutoFilter is null
                ? null
                : new AutoFilterState(ResolveAutoFilterTarget(options.AutoFilter.At, namedAreas)),
            options.ConditionalFormattings
                .Select(rule =>
                {
                    var resolvedTarget = ResolveConditionalFormattingTarget(rule.At, namedAreas);
                    return new ConditionalFormattingState(
                        resolvedTarget,
                        rule.MinColor,
                        rule.MaxColor,
                        rule.MidColor,
                        rule.Formula,
                        ResolveConditionalFormulaRefTarget(rule.FormulaRef, resolvedTarget, namedAreas, formulaPlaceholderContext),
                        rule.FillColor,
                        rule.FontName,
                        rule.FontSize,
                        rule.FontBold,
                        rule.FontItalic,
                        rule.FontUnderline,
                        rule.NumberFormatCode,
                        rule.BorderTop,
                        rule.BorderBottom,
                        rule.BorderLeft,
                        rule.BorderRight,
                        rule.BorderColor);
                })
                .ToArray());
    }

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

    private static string ResolveConditionalFormattingTarget(
        string target,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas) =>
        ResolveAutoFilterTarget(target, namedAreas);

    private static string? ResolveConditionalFormulaRefTarget(
        string? formulaRef,
        string conditionalFormattingTarget,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas,
        FormulaPlaceholderContext formulaPlaceholderContext)
    {
        if (string.IsNullOrWhiteSpace(formulaRef))
        {
            return null;
        }

        if (formulaPlaceholderContext.GlobalAreas.TryGetValue(formulaRef, out var area) ||
            namedAreas.TryGetValue(formulaRef, out area))
        {
            return ToCellReference(area.TopRow, area.LeftColumn);
        }

        if (TryResolveTargetArea(conditionalFormattingTarget, out var targetArea))
        {
            var scopedCandidates = formulaPlaceholderContext.LocalAreasByScope
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
                return ToCellReference(selected.Area.TopRow, selected.Area.LeftColumn);
            }
        }

        var uniqueLocalArea = formulaPlaceholderContext.LocalAreasByScope
            .Values
            .Select(areasByName => areasByName.TryGetValue(formulaRef, out var localArea) ? localArea : null)
            .Where(localArea => localArea is not null)
            .Cast<NamedAreaState>()
            .Distinct()
            .ToArray();
        if (uniqueLocalArea.Length == 1)
        {
            return ToCellReference(uniqueLocalArea[0].TopRow, uniqueLocalArea[0].LeftColumn);
        }

        return formulaRef;
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
