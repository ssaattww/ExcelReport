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
        var resolvedCells = ResolveFormulaPlaceholders(cells, namedAreas);
        var options = BuildOptions(layoutSheet.Options, namedAreas);

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

        AddFormulaReferenceNamedAreas(layoutSheet, namedAreas);
        return namedAreas;
    }

    private static IReadOnlyDictionary<(int Row, int Column), CellState> ResolveFormulaPlaceholders(
        IReadOnlyDictionary<(int Row, int Column), CellState> cells,
        IReadOnlyDictionary<string, NamedAreaState> namedAreas)
    {
        var resolvedCells = new Dictionary<(int Row, int Column), CellState>(cells.Count);

        foreach (var (coordinate, cell) in cells)
        {
            if (!cell.IsFormula || cell.Formula is null)
            {
                resolvedCells[coordinate] = cell;
                continue;
            }

            var resolvedFormula = ReplaceFormulaPlaceholders(cell.Formula, namedAreas);
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
        IReadOnlyDictionary<string, NamedAreaState> namedAreas) =>
        FormulaPlaceholderRegex.Replace(
            formula,
            match =>
            {
                var startName = match.Groups["start"].Value.Trim();
                if (startName.Length == 0 || !namedAreas.TryGetValue(startName, out var startArea))
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
                if (endName.Length == 0 || !namedAreas.TryGetValue(endName, out var endArea))
                {
                    return match.Value;
                }

                var endReference = ToCellReference(endArea.BottomRow, endArea.RightColumn);
                return $"{startReference}:{endReference}";
            });

    private static void AddFormulaReferenceNamedAreas(
        LayoutSheet layoutSheet,
        IDictionary<string, NamedAreaState> namedAreas)
    {
        var seriesByName = new Dictionary<string, List<LayoutCell>>(StringComparer.Ordinal);

        foreach (var cell in layoutSheet.Cells)
        {
            var formulaRefName = cell.FormulaRef?.Trim();
            if (string.IsNullOrWhiteSpace(formulaRefName))
            {
                continue;
            }

            if (!seriesByName.TryGetValue(formulaRefName, out var seriesCells))
            {
                seriesCells = [];
                seriesByName[formulaRefName] = seriesCells;
            }

            seriesCells.Add(cell);
        }

        foreach (var (name, seriesCells) in seriesByName)
        {
            var orderedCells = seriesCells
                .OrderBy(cell => cell.Row)
                .ThenBy(cell => cell.Col)
                .ToArray();
            var firstCell = orderedCells[0];
            var lastCell = orderedCells[^1];

            TryAddFormulaReferenceNamedArea(namedAreas, name, firstCell);
            TryAddFormulaReferenceNamedArea(namedAreas, $"{name}End", lastCell);
        }
    }

    private static void TryAddFormulaReferenceNamedArea(
        IDictionary<string, NamedAreaState> namedAreas,
        string name,
        LayoutCell cell)
    {
        if (namedAreas.ContainsKey(name))
        {
            namedAreas.TryAdd(name, CreateCellNamedArea(name, cell));
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
        IReadOnlyDictionary<string, NamedAreaState> namedAreas)
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
                : new AutoFilterState(ResolveAutoFilterTarget(options.AutoFilter.At, namedAreas)));
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
