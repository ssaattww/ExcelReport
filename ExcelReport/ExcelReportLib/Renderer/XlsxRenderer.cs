using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.Styles;
using ExcelReportLib.WorksheetState;
using WorksheetStateModel = ExcelReportLib.WorksheetState.WorksheetState;

namespace ExcelReportLib.Renderer;

/// <summary>
/// Represents xlsx renderer.
/// </summary>
public sealed class XlsxRenderer : IRenderer
{
    private const string DefaultDateFormatCode = "yyyy-mm-dd";

    /// <summary>
    /// Renders worksheet state into an XLSX workbook stream.
    /// </summary>
    /// <param name="worksheets">The worksheets.</param>
    /// <param name="options">Options that control the operation.</param>
    /// <param name="issues">The collection used to collect discovered issues.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The resulting render result.</returns>
    public RenderResult Render(
        IReadOnlyList<WorksheetStateModel> worksheets,
        RenderOptions? options = null,
        IReadOnlyList<Issue>? issues = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worksheets);

        var effectiveOptions = options ?? new RenderOptions();
        var effectiveIssues = issues ?? Array.Empty<Issue>();
        var output = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            var styleCatalog = StyleCatalog.Create(worksheets);
            stylesPart.Stylesheet = styleCatalog.Stylesheet;

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            uint nextSheetId = 1;

            foreach (var worksheet in worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = BuildWorksheet(worksheet, styleCatalog, cancellationToken);
                worksheetPart.Worksheet.Save();

                sheets.Append(
                    new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = nextSheetId++,
                        Name = worksheet.Name,
                    });
            }

            if (effectiveIssues.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddSpecialSheet(
                    workbookPart,
                    sheets,
                    ref nextSheetId,
                    "_Issues",
                    BuildIssuesWorksheet(effectiveIssues));
            }

            cancellationToken.ThrowIfCancellationRequested();
            AddSpecialSheet(
                workbookPart,
                sheets,
                ref nextSheetId,
                "_Audit",
                BuildAuditWorksheet(effectiveOptions),
                SheetStateValues.Hidden);

            stylesPart.Stylesheet.Save();
            workbookPart.Workbook.Save();
        }

        var workbookBytes = output.ToArray();

        return new RenderResult(
            new MemoryStream(workbookBytes, writable: false),
            sheetCount: worksheets.Count,
            cellCount: worksheets.Sum(sheet => sheet.Cells.Count),
            issueCount: effectiveIssues.Count);
    }

    private static Worksheet BuildWorksheet(
        WorksheetStateModel sheetState,
        StyleCatalog styleCatalog,
        CancellationToken cancellationToken)
    {
        var worksheet = new Worksheet();

        var sheetViews = BuildSheetViews(sheetState);
        if (sheetViews is not null)
        {
            worksheet.Append(sheetViews);
        }

        var columns = BuildColumns(sheetState);
        if (columns is not null)
        {
            worksheet.Append(columns);
        }

        var rowMap = new Dictionary<uint, Row>();
        var sheetData = new SheetData();

        foreach (var cellState in sheetState.Cells.Values.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowIndex = (uint)cellState.Row;
            if (!rowMap.TryGetValue(rowIndex, out var row))
            {
                row = new Row { RowIndex = rowIndex };
                rowMap.Add(rowIndex, row);
            }

            row.Append(CreateCell(cellState, styleCatalog));
        }

        ApplyRowGroups(rowMap, sheetState.Options.RowGroups);

        foreach (var row in rowMap.Values.OrderBy(current => current.RowIndex!.Value))
        {
            sheetData.Append(row);
        }

        worksheet.Append(sheetData);

        var mergeCells = BuildMergeCells(sheetState.MergedRanges);
        if (mergeCells is not null)
        {
            worksheet.Append(mergeCells);
        }

        var autoFilter = BuildAutoFilter(sheetState);
        if (autoFilter is not null)
        {
            worksheet.Append(autoFilter);
        }

        foreach (var conditionalFormatting in BuildConditionalFormattings(sheetState, styleCatalog))
        {
            worksheet.Append(conditionalFormatting);
        }

        return worksheet;
    }

    private static SheetViews? BuildSheetViews(WorksheetStateModel sheetState)
    {
        if (sheetState.Options.FreezePanes is null)
        {
            return null;
        }

        if (!TryResolveCellReference(sheetState, sheetState.Options.FreezePanes.Target, out var reference, out var row, out var column))
        {
            return null;
        }

        var pane = new Pane
        {
            TopLeftCell = reference,
            State = PaneStateValues.Frozen,
        };

        if (column > 1)
        {
            pane.HorizontalSplit = column - 1;
        }

        if (row > 1)
        {
            pane.VerticalSplit = row - 1;
        }

        pane.ActivePane = row > 1 && column > 1
            ? PaneValues.BottomRight
            : row > 1
                ? PaneValues.BottomLeft
                : PaneValues.TopRight;

        return new SheetViews(new SheetView(pane) { WorkbookViewId = 0U });
    }

    private static Columns? BuildColumns(WorksheetStateModel sheetState)
    {
        if (sheetState.Options.ColumnGroups.Count == 0)
        {
            return null;
        }

        var ranges = new List<GroupRangeState>();
        foreach (var group in sheetState.Options.ColumnGroups)
        {
            if (!TryResolveColumnRange(sheetState, group.Target, out var start, out var end))
            {
                continue;
            }

            ranges.Add(new GroupRangeState(start, end, group.Collapsed));
        }

        var outlineStates = BuildOutlineStates(ranges);
        if (outlineStates.Count == 0)
        {
            return null;
        }

        var columns = new Columns();
        var orderedStates = outlineStates
            .OrderBy(entry => entry.Key)
            .ToArray();

        var segmentStart = orderedStates[0].Key;
        var segmentEnd = orderedStates[0].Key;
        var segmentState = orderedStates[0].Value;

        for (var index = 1; index < orderedStates.Length; index++)
        {
            var current = orderedStates[index];
            if (current.Key != segmentEnd + 1 || current.Value != segmentState)
            {
                columns.Append(CreateGroupedColumn(segmentStart, segmentEnd, segmentState));
                segmentStart = current.Key;
                segmentState = current.Value;
            }

            segmentEnd = current.Key;
        }

        columns.Append(CreateGroupedColumn(segmentStart, segmentEnd, segmentState));
        return columns;
    }

    private static Column CreateGroupedColumn(uint start, uint end, OutlineState state) =>
        new()
        {
            Min = start,
            Max = end,
            OutlineLevel = state.OutlineLevel,
            Hidden = state.Hidden ? true : null,
            Collapsed = state.Collapsed ? true : null,
        };

    private static void ApplyRowGroups(
        IDictionary<uint, Row> rowMap,
        IReadOnlyList<WorksheetGroupState> rowGroups)
    {
        var ranges = new List<GroupRangeState>();

        foreach (var group in rowGroups)
        {
            if (!TryParseRowRange(group.Target, out var start, out var end))
            {
                continue;
            }

            ranges.Add(new GroupRangeState(start, end, group.Collapsed));
        }

        var outlineStates = BuildOutlineStates(ranges);
        foreach (var (rowIndex, state) in outlineStates.OrderBy(entry => entry.Key))
        {
            if (!rowMap.TryGetValue(rowIndex, out var row))
            {
                row = new Row { RowIndex = rowIndex };
                rowMap[rowIndex] = row;
            }

            row.OutlineLevel = state.OutlineLevel;
            row.Hidden = state.Hidden ? true : null;
            row.Collapsed = state.Collapsed ? true : null;
        }
    }

    private static IReadOnlyDictionary<uint, OutlineState> BuildOutlineStates(
        IReadOnlyList<GroupRangeState> groups)
    {
        if (groups.Count == 0)
        {
            return new Dictionary<uint, OutlineState>();
        }

        var depths = new Dictionary<uint, int>();
        var hiddenIndexes = new HashSet<uint>();
        var collapsedIndexes = new HashSet<uint>();

        foreach (var group in groups)
        {
            for (var index = group.Start; index <= group.End; index++)
            {
                depths[index] = depths.TryGetValue(index, out var depth)
                    ? depth + 1
                    : 1;

                if (group.Collapsed)
                {
                    hiddenIndexes.Add(index);
                }

                if (index == group.End)
                {
                    break;
                }
            }

            if (group.Collapsed)
            {
                collapsedIndexes.Add(group.End);
            }
        }

        var states = new Dictionary<uint, OutlineState>(depths.Count);
        foreach (var (index, depth) in depths)
        {
            states[index] = new OutlineState(
                OutlineLevel: (byte)Math.Clamp(depth, 1, 8),
                Hidden: hiddenIndexes.Contains(index),
                Collapsed: collapsedIndexes.Contains(index));
        }

        return states;
    }

    private static Cell CreateCell(CellState cellState, StyleCatalog styleCatalog)
    {
        var cell = new Cell
        {
            CellReference = ToCellReference(cellState.Row, cellState.Column),
        };

        var styleIndex = styleCatalog.GetStyleIndex(cellState);
        if (styleIndex != 0U)
        {
            cell.StyleIndex = styleIndex;
        }

        if (cellState.IsFormula)
        {
            cell.CellFormula = new CellFormula(SanitizeFormula(cellState.Formula));
            return cell;
        }

        if (cellState.Value is null)
        {
            return cell;
        }

        switch (cellState.Value)
        {
            case string text:
                cell.DataType = CellValues.InlineString;
                cell.InlineString = new InlineString(new Text(text));
                break;
            case bool boolean:
                cell.DataType = CellValues.Boolean;
                cell.CellValue = new CellValue(boolean ? "1" : "0");
                break;
            case DateOnly dateOnly:
                cell.CellValue = new CellValue(dateOnly.ToDateTime(TimeOnly.MinValue).ToOADate().ToString(CultureInfo.InvariantCulture));
                break;
            case DateTimeOffset dateTimeOffset:
                cell.CellValue = new CellValue(dateTimeOffset.UtcDateTime.ToOADate().ToString(CultureInfo.InvariantCulture));
                break;
            case DateTime dateTime:
                cell.CellValue = new CellValue(dateTime.ToOADate().ToString(CultureInfo.InvariantCulture));
                break;
            case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                cell.CellValue = new CellValue(Convert.ToString(cellState.Value, CultureInfo.InvariantCulture));
                break;
            default:
                cell.DataType = CellValues.InlineString;
                cell.InlineString = new InlineString(new Text(cellState.Value.ToString() ?? string.Empty));
                break;
        }

        return cell;
    }

    private static MergeCells? BuildMergeCells(IReadOnlyList<MergedCellRange> mergedRanges)
    {
        if (mergedRanges.Count == 0)
        {
            return null;
        }

        var mergeCells = new MergeCells();
        foreach (var mergedRange in mergedRanges)
        {
            mergeCells.Append(
                new MergeCell
                {
                    Reference = $"{ToAbsoluteCellReference(mergedRange.TopRow, mergedRange.LeftColumn)}:{ToAbsoluteCellReference(mergedRange.BottomRow, mergedRange.RightColumn)}",
                });
        }

        return mergeCells;
    }

    private static AutoFilter? BuildAutoFilter(WorksheetStateModel sheetState)
    {
        var autoFilterTarget = sheetState.Options.AutoFilter?.Target;
        if (string.IsNullOrWhiteSpace(autoFilterTarget))
        {
            return null;
        }

        var reference = TryResolveRangeReference(sheetState, autoFilterTarget);
        return reference is null ? null : new AutoFilter { Reference = reference };
    }

    private static IReadOnlyList<ConditionalFormatting> BuildConditionalFormattings(
        WorksheetStateModel sheetState,
        StyleCatalog styleCatalog)
    {
        var result = new List<ConditionalFormatting>();
        var priority = 1;

        foreach (var rule in sheetState.Options.ConditionalFormattings)
        {
            var reference = TryResolveRangeReference(sheetState, rule.Target);
            if (reference is null)
            {
                continue;
            }

            var conditionalFormatting = new ConditionalFormatting
            {
                SequenceOfReferences = new ListValue<StringValue> { InnerText = reference },
            };

            var conditionalRule = BuildConditionalFormattingRule(rule, styleCatalog, priority++);

            conditionalFormatting.Append(conditionalRule);
            result.Add(conditionalFormatting);
        }

        return result;
    }

    private static HexBinaryValue NormalizeConditionalFormatColor(string color)
    {
        var normalized = color.Trim();
        if (normalized.StartsWith("#", StringComparison.Ordinal))
        {
            normalized = normalized[1..];
        }

        if (normalized.Length == 6)
        {
            normalized = $"FF{normalized}";
        }

        return normalized.ToUpperInvariant();
    }

    private static ConditionalFormattingRule BuildConditionalFormattingRule(
        ConditionalFormattingState rule,
        StyleCatalog styleCatalog,
        int priority)
    {
        var resolvedFormula = ResolveExpressionFormula(rule);
        if (!string.IsNullOrWhiteSpace(resolvedFormula))
        {
            var formula = SanitizeFormula(resolvedFormula);
            var dxfId = styleCatalog.GetOrCreateDifferentialFormat(rule);
            var expressionRule = new ConditionalFormattingRule
            {
                Type = ConditionalFormatValues.Expression,
                Priority = priority,
            };
            expressionRule.SetAttribute(new OpenXmlAttribute("dxfId", string.Empty, dxfId.ToString(CultureInfo.InvariantCulture)));
            expressionRule.Append(new Formula(formula));
            return expressionRule;
        }

        var colorScaleRule = new ConditionalFormattingRule
        {
            Type = ConditionalFormatValues.ColorScale,
            Priority = priority,
        };

        var colorScale = new ColorScale();
        colorScale.Append(new ConditionalFormatValueObject { Type = ConditionalFormatValueObjectValues.Min });

        if (!string.IsNullOrWhiteSpace(rule.MidColor))
        {
            colorScale.Append(new ConditionalFormatValueObject { Type = ConditionalFormatValueObjectValues.Percentile, Val = "50" });
        }

        colorScale.Append(new ConditionalFormatValueObject { Type = ConditionalFormatValueObjectValues.Max });
        colorScale.Append(new Color { Rgb = NormalizeConditionalFormatColor(rule.MinColor) });
        if (!string.IsNullOrWhiteSpace(rule.MidColor))
        {
            colorScale.Append(new Color { Rgb = NormalizeConditionalFormatColor(rule.MidColor) });
        }
        colorScale.Append(new Color { Rgb = NormalizeConditionalFormatColor(rule.MaxColor) });
        colorScaleRule.Append(colorScale);
        return colorScaleRule;
    }

    private static string? ResolveExpressionFormula(ConditionalFormattingState rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.Formula))
        {
            return rule.Formula;
        }

        if (!string.IsNullOrWhiteSpace(rule.FormulaRef))
        {
            return $"NOT(ISBLANK({rule.FormulaRef}))";
        }

        return null;
    }

    private static void AddSpecialSheet(
        WorkbookPart workbookPart,
        Sheets sheets,
        ref uint nextSheetId,
        string sheetName,
        Worksheet worksheet,
        EnumValue<SheetStateValues>? state = null)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = worksheet;
        worksheetPart.Worksheet.Save();

        var sheet = new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = nextSheetId++,
            Name = sheetName,
        };

        if (state is not null)
        {
            sheet.State = state;
        }

        sheets.Append(sheet);
    }

    private static Worksheet BuildIssuesWorksheet(IReadOnlyList<Issue> issues)
    {
        var rows = new[]
        {
            new[] { "Severity", "Message", "Kind", "Location" },
        }.Concat(
            issues.Select(
                issue => new[]
                {
                    issue.Severity.ToString(),
                    issue.Message,
                    issue.Kind.ToString(),
                    issue.Span is null ? string.Empty : $"L{issue.Span.Line}:C{issue.Span.Column}",
                }));

        return BuildKeyValueWorksheet(rows);
    }

    private static Worksheet BuildAuditWorksheet(RenderOptions options)
    {
        var generatedAt = (options.GeneratedAt ?? DateTimeOffset.UtcNow).ToString("O", CultureInfo.InvariantCulture);

        return BuildKeyValueWorksheet(
            [
                ["GeneratedAt", generatedAt],
                ["TemplateName", options.TemplateName ?? string.Empty],
                ["DataSource", options.DataSource ?? string.Empty],
            ]);
    }

    private static Worksheet BuildKeyValueWorksheet(IEnumerable<string[]> rows)
    {
        var sheetData = new SheetData();
        uint rowIndex = 1;

        foreach (var values in rows)
        {
            var row = new Row { RowIndex = rowIndex };
            for (var index = 0; index < values.Length; index++)
            {
                row.Append(
                    new Cell
                    {
                        CellReference = ToCellReference((int)rowIndex, index + 1),
                        DataType = CellValues.InlineString,
                        InlineString = new InlineString(new Text(values[index] ?? string.Empty)),
                    });
            }

            sheetData.Append(row);
            rowIndex++;
        }

        return new Worksheet(sheetData);
    }

    private static string SanitizeFormula(string? formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            return string.Empty;
        }

        return formula[0] == '=' ? formula[1..] : formula;
    }

    private static bool TryResolveCellReference(
        WorksheetStateModel sheetState,
        string target,
        out string reference,
        out uint row,
        out uint column)
    {
        if (TryParseCellReference(target, out row, out column))
        {
            reference = ToCellReference((int)row, (int)column);
            return true;
        }

        if (sheetState.NamedAreas.TryGetValue(target, out var namedArea))
        {
            row = (uint)namedArea.TopRow;
            column = (uint)namedArea.LeftColumn;
            reference = ToCellReference(namedArea.TopRow, namedArea.LeftColumn);
            return true;
        }

        reference = string.Empty;
        row = 0;
        column = 0;
        return false;
    }

    private static string? TryResolveRangeReference(WorksheetStateModel sheetState, string target)
    {
        if (TryParseRangeReference(target, out var rangeReference))
        {
            return rangeReference;
        }

        if (!sheetState.NamedAreas.TryGetValue(target, out var namedArea))
        {
            return null;
        }

        return $"{ToAbsoluteCellReference(namedArea.TopRow, namedArea.LeftColumn)}:{ToAbsoluteCellReference(namedArea.BottomRow, namedArea.RightColumn)}";
    }

    private static bool TryResolveColumnRange(
        WorksheetStateModel sheetState,
        string target,
        out uint start,
        out uint end)
    {
        if (TryParseColumnRange(target, out start, out end))
        {
            return true;
        }

        if (sheetState.NamedAreas.TryGetValue(target, out var namedArea))
        {
            start = (uint)namedArea.LeftColumn;
            end = (uint)namedArea.RightColumn;
            return true;
        }

        start = 0;
        end = 0;
        return false;
    }

    private static bool TryParseRangeReference(string target, out string reference)
    {
        var parts = target.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 &&
            TryParseCellReference(parts[0], out var firstRow, out var firstColumn) &&
            TryParseCellReference(parts[1], out var lastRow, out var lastColumn))
        {
            reference = $"{ToAbsoluteCellReference((int)firstRow, (int)firstColumn)}:{ToAbsoluteCellReference((int)lastRow, (int)lastColumn)}";
            return true;
        }

        reference = string.Empty;
        return false;
    }

    private static bool TryParseCellReference(string reference, out uint row, out uint column)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            row = 0;
            column = 0;
            return false;
        }

        var trimmed = reference.Trim().TrimStart('$');
        var letters = new StringBuilder();
        var digits = new StringBuilder();

        foreach (var character in trimmed)
        {
            if (char.IsLetter(character))
            {
                if (digits.Length > 0)
                {
                    row = 0;
                    column = 0;
                    return false;
                }

                letters.Append(char.ToUpperInvariant(character));
                continue;
            }

            if (char.IsDigit(character))
            {
                digits.Append(character);
                continue;
            }

            if (character != '$')
            {
                row = 0;
                column = 0;
                return false;
            }
        }

        if (letters.Length == 0 || digits.Length == 0 || !uint.TryParse(digits.ToString(), out row))
        {
            row = 0;
            column = 0;
            return false;
        }

        column = ColumnNameToIndex(letters.ToString());
        return column > 0;
    }

    private static bool TryParseRowRange(string target, out uint start, out uint end)
    {
        var parts = target.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 &&
            uint.TryParse(parts[0], out start) &&
            uint.TryParse(parts[1], out end) &&
            start > 0 &&
            end >= start)
        {
            return true;
        }

        start = 0;
        end = 0;
        return false;
    }

    private static bool TryParseColumnRange(string target, out uint start, out uint end)
    {
        var parts = target.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 &&
            IsColumnToken(parts[0]) &&
            IsColumnToken(parts[1]))
        {
            start = ColumnNameToIndex(parts[0].ToUpperInvariant());
            end = ColumnNameToIndex(parts[1].ToUpperInvariant());
            if (start > 0 && end >= start)
            {
                return true;
            }
        }

        start = 0;
        end = 0;
        return false;
    }

    private static bool IsColumnToken(string token) =>
        token.Length > 0 && token.All(char.IsLetter);

    private static string ToCellReference(int row, int column) =>
        $"{ColumnIndexToName(column)}{row}";

    private static string ToAbsoluteCellReference(int row, int column) =>
        $"${ColumnIndexToName(column)}${row}";

    private static string ColumnIndexToName(int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(column);

        var builder = new StringBuilder();
        var current = column;

        while (current > 0)
        {
            current--;
            builder.Insert(0, (char)('A' + (current % 26)));
            current /= 26;
        }

        return builder.ToString();
    }

    private static uint ColumnNameToIndex(string columnName)
    {
        uint value = 0;
        foreach (var character in columnName)
        {
            value = checked((value * 26) + (uint)(character - 'A' + 1));
        }

        return value;
    }

    private readonly record struct GroupRangeState(uint Start, uint End, bool Collapsed);

    private readonly record struct OutlineState(byte OutlineLevel, bool Hidden, bool Collapsed);

    private sealed class StyleCatalog
    {
        private readonly IReadOnlyDictionary<StyleKey, uint> _styleIndexes;
        private readonly Dictionary<string, uint> _fillDxfIndexes = new(StringComparer.Ordinal);
        private readonly DifferentialFormats _differentialFormats;

        private StyleCatalog(
            IReadOnlyDictionary<StyleKey, uint> styleIndexes,
            Stylesheet stylesheet,
            DifferentialFormats differentialFormats)
        {
            _styleIndexes = styleIndexes;
            Stylesheet = stylesheet;
            _differentialFormats = differentialFormats;
        }

        /// <summary>
        /// Gets the generated workbook stylesheet containing all registered formats.
        /// </summary>
        public Stylesheet Stylesheet { get; }

        public static StyleCatalog Create(IReadOnlyList<WorksheetStateModel> worksheets)
        {
            var styleIndexes = new Dictionary<StyleKey, uint>();
            var numberingFormats = new NumberingFormats();
            var fonts = new Fonts();
            var fills = new Fills();
            var borders = new Borders();
            var cellFormats = new CellFormats();

            fonts.Append(new Font());
            fills.Append(new Fill(new PatternFill { PatternType = PatternValues.None }));
            fills.Append(new Fill(new PatternFill { PatternType = PatternValues.Gray125 }));
            borders.Append(new Border());
            cellFormats.Append(new CellFormat());

            uint nextFontId = 1;
            uint nextFillId = 2;
            uint nextBorderId = 1;
            uint nextNumberFormatId = 164;
            uint nextStyleIndex = 1;

            foreach (var cellState in worksheets.SelectMany(sheet => sheet.Cells.Values))
            {
                var styleKey = StyleKey.FromCell(cellState);
                if (styleKey.IsDefault || styleIndexes.ContainsKey(styleKey))
                {
                    continue;
                }

                var fontId = 0U;
                if (styleKey.HasFont)
                {
                    fonts.Append(styleKey.ToFont());
                    fontId = nextFontId++;
                }

                var fillId = 0U;
                if (styleKey.HasFill)
                {
                    fills.Append(styleKey.ToFill());
                    fillId = nextFillId++;
                }

                var borderId = 0U;
                if (styleKey.HasBorder)
                {
                    borders.Append(styleKey.ToBorder());
                    borderId = nextBorderId++;
                }

                var numberFormatId = 0U;
                if (!string.IsNullOrWhiteSpace(styleKey.NumberFormatCode))
                {
                    numberingFormats.Append(
                        new NumberingFormat
                        {
                            NumberFormatId = nextNumberFormatId,
                            FormatCode = styleKey.NumberFormatCode,
                        });
                    numberFormatId = nextNumberFormatId++;
                }

                cellFormats.Append(
                    new CellFormat
                    {
                        FontId = fontId,
                        FillId = fillId,
                        BorderId = borderId,
                        NumberFormatId = numberFormatId,
                        ApplyFont = fontId > 0,
                        ApplyFill = fillId > 0,
                        ApplyBorder = borderId > 0,
                        ApplyNumberFormat = numberFormatId > 0,
                    });

                styleIndexes.Add(styleKey, nextStyleIndex++);
            }

            fonts.Count = (uint)fonts.ChildElements.Count;
            fills.Count = (uint)fills.ChildElements.Count;
            borders.Count = (uint)borders.ChildElements.Count;
            cellFormats.Count = (uint)cellFormats.ChildElements.Count;

            var stylesheet = new Stylesheet();
            if (numberingFormats.ChildElements.Count > 0)
            {
                numberingFormats.Count = (uint)numberingFormats.ChildElements.Count;
                stylesheet.Append(numberingFormats);
            }

            stylesheet.Append(fonts);
            stylesheet.Append(fills);
            stylesheet.Append(borders);
            stylesheet.Append(new CellStyleFormats(new CellFormat()) { Count = 1U });
            stylesheet.Append(cellFormats);
            stylesheet.Append(new CellStyles(new CellStyle { Name = "Normal", FormatId = 0U, BuiltinId = 0U }) { Count = 1U });
            var differentialFormats = new DifferentialFormats() { Count = 0U };
            stylesheet.Append(differentialFormats);
            stylesheet.Append(new TableStyles { Count = 0U, DefaultTableStyle = "TableStyleMedium2", DefaultPivotStyle = "PivotStyleLight16" });

            return new StyleCatalog(styleIndexes, stylesheet, differentialFormats);
        }

        public uint GetStyleIndex(CellState cellState)
        {
            var styleKey = StyleKey.FromCell(cellState);
            return styleKey.IsDefault ? 0U : _styleIndexes[styleKey];
        }

        public uint GetOrCreateDifferentialFormat(ConditionalFormattingState rule)
        {
            var key = string.Join(
                "|",
                NormalizeConditionalFormatColor(rule.FillColor).Value,
                rule.FontName ?? string.Empty,
                rule.FontSize?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                rule.FontBold?.ToString() ?? string.Empty,
                rule.FontItalic?.ToString() ?? string.Empty,
                rule.FontUnderline?.ToString() ?? string.Empty,
                rule.NumberFormatCode ?? string.Empty,
                rule.BorderTop ?? string.Empty,
                rule.BorderBottom ?? string.Empty,
                rule.BorderLeft ?? string.Empty,
                rule.BorderRight ?? string.Empty,
                NormalizeOptionalConditionalFormatColor(rule.BorderColor) ?? string.Empty);

            if (_fillDxfIndexes.TryGetValue(key, out var index))
            {
                return index;
            }

            var differentialFormat = new DifferentialFormat();

            if (HasFont(rule))
            {
                differentialFormat.Font = CreateConditionalFont(rule);
            }

            differentialFormat.Fill = new Fill(
                new PatternFill(
                    new ForegroundColor { Rgb = NormalizeConditionalFormatColor(rule.FillColor).Value! },
                    new BackgroundColor { Indexed = 64U })
                { PatternType = PatternValues.Solid });

            if (!string.IsNullOrWhiteSpace(rule.NumberFormatCode))
            {
                differentialFormat.NumberingFormat = new NumberingFormat
                {
                    NumberFormatId = 0U,
                    FormatCode = rule.NumberFormatCode,
                };
            }

            if (HasBorder(rule))
            {
                differentialFormat.Border = CreateConditionalBorder(rule);
            }

            _differentialFormats.Append(differentialFormat);
            var created = (uint)_differentialFormats.ChildElements.Count - 1;
            _differentialFormats.Count = (uint)_differentialFormats.ChildElements.Count;
            _fillDxfIndexes[key] = created;
            return created;
        }

        private static bool HasFont(ConditionalFormattingState rule) =>
            !string.IsNullOrWhiteSpace(rule.FontName) ||
            rule.FontSize is not null ||
            rule.FontBold is not null ||
            rule.FontItalic is not null ||
            rule.FontUnderline is not null;

        private static Font CreateConditionalFont(ConditionalFormattingState rule)
        {
            var font = new Font();
            if (!string.IsNullOrWhiteSpace(rule.FontName))
            {
                font.Append(new FontName { Val = rule.FontName });
            }

            if (rule.FontSize is not null)
            {
                font.Append(new FontSize { Val = rule.FontSize.Value });
            }

            if (rule.FontBold == true)
            {
                font.Append(new Bold());
            }

            if (rule.FontItalic == true)
            {
                font.Append(new Italic());
            }

            if (rule.FontUnderline == true)
            {
                font.Append(new Underline());
            }

            return font;
        }

        private static bool HasBorder(ConditionalFormattingState rule) =>
            !string.IsNullOrWhiteSpace(rule.BorderTop) ||
            !string.IsNullOrWhiteSpace(rule.BorderBottom) ||
            !string.IsNullOrWhiteSpace(rule.BorderLeft) ||
            !string.IsNullOrWhiteSpace(rule.BorderRight);

        private static Border CreateConditionalBorder(ConditionalFormattingState rule) =>
            new()
            {
                LeftBorder = CreateLeftBorder(rule.BorderLeft, rule.BorderColor),
                RightBorder = CreateRightBorder(rule.BorderRight, rule.BorderColor),
                TopBorder = CreateTopBorder(rule.BorderTop, rule.BorderColor),
                BottomBorder = CreateBottomBorder(rule.BorderBottom, rule.BorderColor),
                DiagonalBorder = new DiagonalBorder(),
            };

        private static LeftBorder CreateLeftBorder(string? style, string? color)
        {
            var border = new LeftBorder();
            ApplyBorderStyle(border, style, color);
            return border;
        }

        private static RightBorder CreateRightBorder(string? style, string? color)
        {
            var border = new RightBorder();
            ApplyBorderStyle(border, style, color);
            return border;
        }

        private static TopBorder CreateTopBorder(string? style, string? color)
        {
            var border = new TopBorder();
            ApplyBorderStyle(border, style, color);
            return border;
        }

        private static BottomBorder CreateBottomBorder(string? style, string? color)
        {
            var border = new BottomBorder();
            ApplyBorderStyle(border, style, color);
            return border;
        }

        private static void ApplyBorderStyle(OpenXmlCompositeElement borderElement, string? style, string? color)
        {
            if (string.IsNullOrWhiteSpace(style))
            {
                return;
            }

            var mappedStyle = MapBorderStyle(style);
            if (mappedStyle is null)
            {
                return;
            }

            switch (borderElement)
            {
                case LeftBorder left:
                    left.Style = mappedStyle;
                    break;
                case RightBorder right:
                    right.Style = mappedStyle;
                    break;
                case TopBorder top:
                    top.Style = mappedStyle;
                    break;
                case BottomBorder bottom:
                    bottom.Style = mappedStyle;
                    break;
            }

            var normalizedColor = NormalizeOptionalConditionalFormatColor(color);
            if (normalizedColor is null)
            {
                return;
            }

            var colorElement = new Color { Rgb = normalizedColor };
            switch (borderElement)
            {
                case LeftBorder left:
                    left.Color = colorElement;
                    break;
                case RightBorder right:
                    right.Color = colorElement;
                    break;
                case TopBorder top:
                    top.Color = colorElement;
                    break;
                case BottomBorder bottom:
                    bottom.Color = colorElement;
                    break;
            }
        }

        private static BorderStyleValues? MapBorderStyle(string style) =>
            style.Trim().ToLowerInvariant() switch
            {
                "thin" => BorderStyleValues.Thin,
                "medium" => BorderStyleValues.Medium,
                "thick" => BorderStyleValues.Thick,
                "dotted" => BorderStyleValues.Dotted,
                "dashed" => BorderStyleValues.Dashed,
                "double" => BorderStyleValues.Double,
                "hair" => BorderStyleValues.Hair,
                "dashdot" => BorderStyleValues.DashDot,
                "dashdotdot" => BorderStyleValues.DashDotDot,
                "mediumdashed" => BorderStyleValues.MediumDashed,
                "mediumdashdot" => BorderStyleValues.MediumDashDot,
                "mediumdashdotdot" => BorderStyleValues.MediumDashDotDot,
                "slantdashdot" => BorderStyleValues.SlantDashDot,
                _ => null,
            };

        private static string? NormalizeOptionalConditionalFormatColor(string? color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return null;
            }

            return NormalizeConditionalFormatColor(color).Value;
        }
    }

    private sealed record StyleKey(
        string? FontName,
        double? FontSize,
        bool? FontBold,
        bool? FontItalic,
        bool? FontUnderline,
        string? FillColor,
        string? NumberFormatCode,
        string? BorderTop,
        string? BorderBottom,
        string? BorderLeft,
        string? BorderRight,
        string? BorderColor)
    {
        public bool IsDefault =>
            FontName is null &&
            FontSize is null &&
            FontBold is null &&
            FontItalic is null &&
            FontUnderline is null &&
            FillColor is null &&
            NumberFormatCode is null &&
            BorderTop is null &&
            BorderBottom is null &&
            BorderLeft is null &&
            BorderRight is null &&
            BorderColor is null;

        public bool HasFont =>
            FontName is not null ||
            FontSize is not null ||
            FontBold is not null ||
            FontItalic is not null ||
            FontUnderline is not null;

        public bool HasFill => FillColor is not null;

        public bool HasBorder =>
            BorderTop is not null ||
            BorderBottom is not null ||
            BorderLeft is not null ||
            BorderRight is not null ||
            BorderColor is not null;

        public static StyleKey FromCell(CellState cellState)
        {
            var mergedBorder = MergeBorders(cellState.Style.Borders);
            var numberFormatCode = cellState.Style.NumberFormatCode;

            if (numberFormatCode is null && cellState.Value is DateOnly or DateTime or DateTimeOffset)
            {
                numberFormatCode = DefaultDateFormatCode;
            }

            return new StyleKey(
                cellState.Style.FontName,
                cellState.Style.FontSize,
                cellState.Style.FontBold,
                cellState.Style.FontItalic,
                cellState.Style.FontUnderline,
                NormalizeColor(cellState.Style.FillColor),
                numberFormatCode,
                mergedBorder.Top,
                mergedBorder.Bottom,
                mergedBorder.Left,
                mergedBorder.Right,
                mergedBorder.Color);
        }

        public Font ToFont()
        {
            var font = new Font();

            if (FontBold == true)
            {
                font.Append(new Bold());
            }

            if (FontItalic == true)
            {
                font.Append(new Italic());
            }

            if (FontUnderline == true)
            {
                font.Append(new Underline());
            }

            if (FontSize.HasValue)
            {
                font.Append(new FontSize { Val = FontSize.Value });
            }

            if (!string.IsNullOrWhiteSpace(FontName))
            {
                font.Append(new FontName { Val = FontName });
            }

            return font;
        }

        public Fill ToFill() =>
            new(
                new PatternFill(
                    new ForegroundColor { Rgb = FillColor },
                    new BackgroundColor { Indexed = 64U })
                {
                    PatternType = PatternValues.Solid,
                });

        public Border ToBorder() =>
            new(
                CreateLeftBorder(BorderLeft, BorderColor),
                CreateRightBorder(BorderRight, BorderColor),
                CreateTopBorder(BorderTop, BorderColor),
                CreateBottomBorder(BorderBottom, BorderColor),
                new DiagonalBorder());

        private static MergedBorder MergeBorders(IReadOnlyList<BorderInfo> borders)
        {
            var top = default(string);
            var bottom = default(string);
            var left = default(string);
            var right = default(string);
            var color = default(string);

            foreach (var border in borders)
            {
                if (border.Top is not null)
                {
                    top = border.Top;
                }

                if (border.Bottom is not null)
                {
                    bottom = border.Bottom;
                }

                if (border.Left is not null)
                {
                    left = border.Left;
                }

                if (border.Right is not null)
                {
                    right = border.Right;
                }

                if (border.Color is not null)
                {
                    color = NormalizeColor(border.Color);
                }
            }

            return new MergedBorder(top, bottom, left, right, color);
        }

        private static TopBorder CreateTopBorder(string? style, string? color) =>
            new()
            {
                Style = ParseBorderStyle(style),
                Color = style is null || color is null ? null : new Color { Rgb = color },
            };

        private static BottomBorder CreateBottomBorder(string? style, string? color) =>
            new()
            {
                Style = ParseBorderStyle(style),
                Color = style is null || color is null ? null : new Color { Rgb = color },
            };

        private static LeftBorder CreateLeftBorder(string? style, string? color) =>
            new()
            {
                Style = ParseBorderStyle(style),
                Color = style is null || color is null ? null : new Color { Rgb = color },
            };

        private static RightBorder CreateRightBorder(string? style, string? color) =>
            new()
            {
                Style = ParseBorderStyle(style),
                Color = style is null || color is null ? null : new Color { Rgb = color },
            };

        private static BorderStyleValues? ParseBorderStyle(string? style) =>
            style?.Trim().ToLowerInvariant() switch
            {
                "hair" => BorderStyleValues.Hair,
                "dotted" => BorderStyleValues.Dotted,
                "dashdotdot" => BorderStyleValues.DashDotDot,
                "dashdot" => BorderStyleValues.DashDot,
                "dashed" => BorderStyleValues.Dashed,
                "double" => BorderStyleValues.Double,
                "mediumdashdotdot" => BorderStyleValues.MediumDashDotDot,
                "mediumdashdot" => BorderStyleValues.MediumDashDot,
                "mediumdashed" => BorderStyleValues.MediumDashed,
                "medium" => BorderStyleValues.Medium,
                "slantdashdot" => BorderStyleValues.SlantDashDot,
                "thick" => BorderStyleValues.Thick,
                "thin" => BorderStyleValues.Thin,
                _ => null,
            };

        private static string? NormalizeColor(string? color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return null;
            }

            var normalized = color.Trim().TrimStart('#');
            if (normalized.Length == 6)
            {
                normalized = $"FF{normalized}";
            }

            return normalized.ToUpperInvariant();
        }

        private sealed record MergedBorder(
            string? Top,
            string? Bottom,
            string? Left,
            string? Right,
            string? Color);
    }
}
