using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.Renderer;
using ExcelReportLib.Styles;
using ExcelReportLib.WorksheetState;
using Xunit;

namespace ExcelReportLib.Tests;

public sealed class RendererTests
{
    [Fact]
    public void Render_SingleSheet_ProducesXlsx()
    {
        var sheet = CreateWorksheet(
            "Summary",
            cells:
            [
                CreateCell(1, 1, "Hello"),
            ]);

        var renderer = CreateRenderer();

        var result = renderer.Render([sheet], CreateOptions());

        Assert.True(result.Output.Length > 0);
        Assert.Equal(1, result.SheetCount);
        Assert.Equal(1, result.CellCount);
        Assert.Equal(0, result.IssueCount);

        using var document = OpenWorkbook(result);
        Assert.NotNull(document.WorkbookPart);
        Assert.NotNull(GetSheet(document, "Summary"));
        Assert.NotNull(GetSheet(document, "_Audit"));
    }

    [Fact]
    public void Render_CellValues_Written()
    {
        var style = CreateStyle(numberFormatCode: "yyyy-mm-dd");
        var date = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc);
        var sheet = CreateWorksheet(
            "Values",
            cells:
            [
                CreateCell(1, 1, "Hello"),
                CreateCell(2, 1, 42),
                CreateCell(3, 1, value: null, formula: "SUM(A2:A2)"),
                CreateCell(4, 1, date, style),
            ]);

        var renderer = CreateRenderer();

        var result = renderer.Render([sheet], CreateOptions());

        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Values");

        Assert.Equal("Hello", ReadCellValue(document, GetCell(worksheetPart, "A1")));
        Assert.Equal("42", GetCell(worksheetPart, "A2").CellValue!.Text);
        Assert.Equal("SUM(A2:A2)", GetCell(worksheetPart, "A3").CellFormula!.Text);
        Assert.NotNull(GetCell(worksheetPart, "A4").CellValue);
        Assert.NotEqual("0", GetCell(worksheetPart, "A4").CellValue!.Text);
    }

    [Fact]
    public void Render_MergedCells_Applied()
    {
        var sheet = CreateWorksheet(
            "Merge",
            cells:
            [
                CreateCell(2, 2, "Merged", isMergedHead: true),
            ],
            mergedRanges:
            [
                new MergedCellRange(2, 2, 3, 4),
            ]);

        var renderer = CreateRenderer();

        var result = renderer.Render([sheet], CreateOptions());

        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Merge");
        var mergeCells = worksheetPart.Worksheet.Elements<MergeCells>().Single();
        var mergeCell = mergeCells.Elements<MergeCell>().Single();

        Assert.Equal("B2:D3", mergeCell.Reference!.Value);
    }

    [Fact]
    public void Render_Styles_Applied()
    {
        var style = CreateStyle(
            fontName: "Consolas",
            fontSize: 14,
            fontBold: true,
            fillColor: "FFFF00",
            borders:
            [
                new BorderInfo
                {
                    Top = "thick",
                    Bottom = "thin",
                    Left = "thin",
                    Right = "thick",
                    Color = "FF0000",
                },
            ]);

        var sheet = CreateWorksheet(
            "Styles",
            cells:
            [
                CreateCell(1, 1, "Styled", style),
            ]);

        var renderer = CreateRenderer();

        var result = renderer.Render([sheet], CreateOptions());

        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Styles");
        var cell = GetCell(worksheetPart, "A1");
        var cellFormat = GetCellFormat(document, cell);
        var stylesheet = document.WorkbookPart!.WorkbookStylesPart!.Stylesheet;

        var font = stylesheet.Fonts!.Elements<Font>().ElementAt((int)cellFormat.FontId!.Value);
        Assert.Equal("Consolas", font.FontName!.Val!.Value);
        Assert.Equal(14D, font.FontSize!.Val!.Value);
        Assert.NotNull(font.Bold);

        var fill = stylesheet.Fills!.Elements<Fill>().ElementAt((int)cellFormat.FillId!.Value);
        Assert.Equal("FFFF00", fill.PatternFill!.ForegroundColor!.Rgb!.Value);

        var border = stylesheet.Borders!.Elements<Border>().ElementAt((int)cellFormat.BorderId!.Value);
        Assert.Equal(BorderStyleValues.Thick, border.TopBorder!.Style!.Value);
        Assert.Equal(BorderStyleValues.Thin, border.BottomBorder!.Style!.Value);
        Assert.Equal("FF0000", border.TopBorder.Color!.Rgb!.Value);
    }

    [Fact]
    public void Render_FreezePanes_Applied()
    {
        var sheet = CreateWorksheet(
            "Frozen",
            cells:
            [
                CreateCell(1, 1, "Header"),
            ],
            options: new WorksheetOptionsState(
                new FreezePaneState("B2"),
                rowGroups: [],
                columnGroups: [],
                autoFilter: null));

        var renderer = CreateRenderer();

        var result = renderer.Render([sheet], CreateOptions());

        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Frozen");
        var pane = worksheetPart.Worksheet
            .GetFirstChild<SheetViews>()!
            .GetFirstChild<SheetView>()!
            .GetFirstChild<Pane>();

        Assert.NotNull(pane);
        Assert.Equal("B2", pane!.TopLeftCell!.Value);
        Assert.Equal(PaneStateValues.Frozen, pane.State!.Value);
    }

    [Fact]
    public void Render_IssuesSheet_Generated()
    {
        var sheet = CreateWorksheet(
            "Summary",
            cells:
            [
                CreateCell(1, 1, "Value"),
            ]);

        var issues =
        [
            new Issue
            {
                Severity = IssueSeverity.Warning,
                Kind = IssueKind.InvalidAttributeValue,
                Message = "A warning was emitted.",
            },
        ];

        var renderer = CreateRenderer();

        var result = renderer.Render([sheet], CreateOptions(), issues);

        using var document = OpenWorkbook(result);
        var issuesPart = GetWorksheetPart(document, "_Issues");

        Assert.Equal("Severity", ReadCellValue(document, GetCell(issuesPart, "A1")));
        Assert.Equal("A warning was emitted.", ReadCellValue(document, GetCell(issuesPart, "B2")));
    }

    [Fact]
    public void Render_AuditSheet_Generated()
    {
        var sheet = CreateWorksheet(
            "Summary",
            cells:
            [
                CreateCell(1, 1, "Value"),
            ]);

        var renderer = CreateRenderer();
        var options = new RenderOptions
        {
            TemplateName = "MonthlyReport",
            DataSource = "WarehouseDb",
            GeneratedAt = new DateTimeOffset(2026, 3, 3, 10, 30, 0, TimeSpan.Zero),
        };

        var result = renderer.Render([sheet], options);

        using var document = OpenWorkbook(result);
        var auditSheet = GetSheet(document, "_Audit");
        var auditPart = GetWorksheetPart(document, "_Audit");

        Assert.Equal(SheetStateValues.Hidden, auditSheet.State!.Value);
        Assert.Equal("GeneratedAt", ReadCellValue(document, GetCell(auditPart, "A1")));
        Assert.Equal("TemplateName", ReadCellValue(document, GetCell(auditPart, "A2")));
        Assert.Equal("MonthlyReport", ReadCellValue(document, GetCell(auditPart, "B2")));
        Assert.Equal("WarehouseDb", ReadCellValue(document, GetCell(auditPart, "B3")));
    }

    [Fact]
    public void Render_MultipleSheets_AllRendered()
    {
        var first = CreateWorksheet(
            "Summary",
            cells:
            [
                CreateCell(1, 1, "Summary"),
            ]);
        var second = CreateWorksheet(
            "Detail",
            cells:
            [
                CreateCell(1, 1, "Detail"),
                CreateCell(2, 1, 100),
            ]);

        var renderer = CreateRenderer();

        var result = renderer.Render([first, second], CreateOptions());

        using var document = OpenWorkbook(result);
        var sheets = document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>().ToArray();

        Assert.Contains(sheets, sheet => sheet.Name == "Summary");
        Assert.Contains(sheets, sheet => sheet.Name == "Detail");
        Assert.Equal(2, result.SheetCount);
        Assert.Equal(3, result.CellCount);
    }

    private static IRenderer CreateRenderer() => new XlsxRenderer();

    private static RenderOptions CreateOptions() =>
        new()
        {
            TemplateName = "Template",
            DataSource = "DataSource",
            GeneratedAt = new DateTimeOffset(2026, 3, 3, 0, 0, 0, TimeSpan.Zero),
        };

    private static WorksheetState CreateWorksheet(
        string name,
        IReadOnlyList<CellState> cells,
        IReadOnlyList<MergedCellRange>? mergedRanges = null,
        WorksheetOptionsState? options = null)
    {
        var dictionary = cells.ToDictionary(cell => (cell.Row, cell.Column));

        return new WorksheetState(
            name,
            rowCount: Math.Max(10, cells.DefaultIfEmpty().Max(cell => cell?.Row ?? 0)),
            columnCount: Math.Max(10, cells.DefaultIfEmpty().Max(cell => cell?.Column ?? 0)),
            cells: dictionary,
            mergedRanges: mergedRanges ?? [],
            namedAreas: new Dictionary<string, NamedAreaState>(StringComparer.Ordinal),
            options: options ?? WorksheetOptionsState.Empty);
    }

    private static CellState CreateCell(
        int row,
        int column,
        object? value,
        ResolvedStyle? style = null,
        string? formula = null,
        bool isMergedHead = false) =>
        new(
            row,
            column,
            value,
            formula,
            formulaReference: null,
            style ?? CreateStyle(),
            isMergedHead);

    private static ResolvedStyle CreateStyle(
        string? fontName = null,
        double? fontSize = null,
        bool? fontBold = null,
        bool? fontItalic = null,
        bool? fontUnderline = null,
        string? fillColor = null,
        string? numberFormatCode = null,
        IReadOnlyList<BorderInfo>? borders = null) =>
        new(
            sourceName: "test",
            sourceKind: StyleSourceKind.Computed,
            declaredScope: StyleScope.Cell,
            fontName,
            fontSize,
            fontBold,
            fontItalic,
            fontUnderline,
            fillColor,
            numberFormatCode,
            borders);

    private static SpreadsheetDocument OpenWorkbook(RenderResult result)
    {
        result.Output.Position = 0;
        return SpreadsheetDocument.Open(result.Output, false);
    }

    private static Sheet GetSheet(SpreadsheetDocument document, string sheetName) =>
        document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>().Single(sheet => sheet.Name == sheetName);

    private static WorksheetPart GetWorksheetPart(SpreadsheetDocument document, string sheetName) =>
        (WorksheetPart)document.WorkbookPart!.GetPartById(GetSheet(document, sheetName).Id!);

    private static Cell GetCell(WorksheetPart worksheetPart, string reference) =>
        worksheetPart.Worksheet.Descendants<Cell>().Single(cell => cell.CellReference!.Value == reference);

    private static CellFormat GetCellFormat(SpreadsheetDocument document, Cell cell)
    {
        Assert.NotNull(cell.StyleIndex);
        return document.WorkbookPart!
            .WorkbookStylesPart!
            .Stylesheet
            .CellFormats!
            .Elements<CellFormat>()
            .ElementAt((int)cell.StyleIndex!.Value);
    }

    private static string ReadCellValue(SpreadsheetDocument document, Cell cell)
    {
        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString!.InnerText;
        }

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            var sharedStringPart = document.WorkbookPart!.SharedStringTablePart!;
            return sharedStringPart.SharedStringTable.Elements<SharedStringItem>()
                .ElementAt(int.Parse(cell.CellValue!.Text))
                .InnerText;
        }

        return cell.CellValue?.Text ?? string.Empty;
    }
}
