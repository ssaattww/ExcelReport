using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.LayoutEngine;
using ExcelReportLib.Renderer;
using ExcelReportLib.Styles;
using ExcelReportLib.WorksheetState;
using Xunit;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>Renderer</c> feature.
/// </summary>
public sealed class RendererTests
{
    /// <summary>
    /// Verifies that render single sheet produces XLSX.
    /// </summary>
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

    /// <summary>
    /// Verifies that render cell values written.
    /// </summary>
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

    /// <summary>
    /// Verifies that render merged cells applied.
    /// </summary>
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

        Assert.Equal("$B$2:$D$3", mergeCell.Reference!.Value);
    }

    /// <summary>
    /// Verifies that render styles applied.
    /// </summary>
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
        Assert.Equal("FFFFFF00", fill.PatternFill!.ForegroundColor!.Rgb!.Value);

        var border = stylesheet.Borders!.Elements<Border>().ElementAt((int)cellFormat.BorderId!.Value);
        Assert.Equal(BorderStyleValues.Thick, border.TopBorder!.Style!.Value);
        Assert.Equal(BorderStyleValues.Thin, border.BottomBorder!.Style!.Value);
        Assert.Equal("FFFF0000", border.TopBorder.Color!.Rgb!.Value);
    }

    /// <summary>
    /// Verifies that render border child element order matches ct border schema.
    /// </summary>
    [Fact]
    public void Render_Border_ChildElementOrder_MatchesCTBorderSchema()
    {
        var style = CreateStyle(
            borders:
            [
                new BorderInfo
                {
                    Top = "thin",
                    Bottom = "thin",
                    Left = "thin",
                    Right = "thin",
                    Color = "#000000",
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
        var border = document.WorkbookPart!
            .WorkbookStylesPart!
            .Stylesheet
            .Borders!
            .Elements<Border>()
            .ElementAt((int)cellFormat.BorderId!.Value);

        Assert.Equal(
            ["left", "right", "top", "bottom", "diagonal"],
            border.ChildElements.Select(child => child.LocalName));
    }

    /// <summary>
    /// Verifies that render multiple borders merged by side.
    /// </summary>
    [Fact]
    public void Render_MultipleBorders_MergedBySide()
    {
        var style = CreateStyle(
            borders:
            [
                new BorderInfo
                {
                    Top = "thin",
                    Color = "#111111",
                },
                new BorderInfo
                {
                    Bottom = "medium",
                    Left = "dashed",
                },
                new BorderInfo
                {
                    Top = "double",
                    Right = "thick",
                    Color = "#00FF00",
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
        var border = document.WorkbookPart!
            .WorkbookStylesPart!
            .Stylesheet
            .Borders!
            .Elements<Border>()
            .ElementAt((int)cellFormat.BorderId!.Value);

        Assert.Equal(BorderStyleValues.Double, border.TopBorder!.Style!.Value);
        Assert.Equal(BorderStyleValues.Medium, border.BottomBorder!.Style!.Value);
        Assert.Equal(BorderStyleValues.Dashed, border.LeftBorder!.Style!.Value);
        Assert.Equal(BorderStyleValues.Thick, border.RightBorder!.Style!.Value);
        Assert.Equal("FF00FF00", border.TopBorder.Color!.Rgb!.Value);
        Assert.Equal("FF00FF00", border.BottomBorder.Color!.Rgb!.Value);
        Assert.Equal("FF00FF00", border.LeftBorder.Color!.Rgb!.Value);
        Assert.Equal("FF00FF00", border.RightBorder.Color!.Rgb!.Value);
    }

    /// <summary>
    /// Verifies that render freeze panes applied.
    /// </summary>
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

    /// <summary>
    /// Verifies that render sheet options with named targets applied after state build.
    /// </summary>
    [Fact]
    public void Render_SheetOptionsWithNamedTargets_AppliedAfterStateBuild()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCellState(5, 1, "Header"),
                    ],
                    rows: 20,
                    cols: 10,
                    namedAreas:
                    [
                        new LayoutNamedArea("DetailHeader", topRow: 5, leftColumn: 1, bottomRow: 5, rightColumn: 3),
                        new LayoutNamedArea("DetailRows", topRow: 6, leftColumn: 1, bottomRow: 8, rightColumn: 3),
                    ],
                    options: CreateSheetOptions(
                        """
                        <freeze at="DetailHeader" />
                        <groups>
                          <groupRows at="DetailRows" collapsed="true" />
                        </groups>
                        <autoFilter at="DetailHeader" />
                        """)),
            ]);
        var worksheet = Assert.Single(new WorksheetStateBuilder().Build(plan));
        var renderer = CreateRenderer();

        var result = renderer.Render([worksheet], CreateOptions());

        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");

        var pane = worksheetPart.Worksheet
            .GetFirstChild<SheetViews>()!
            .GetFirstChild<SheetView>()!
            .GetFirstChild<Pane>();
        Assert.NotNull(pane);
        Assert.Equal("A5", pane!.TopLeftCell!.Value);
        Assert.Equal(4D, pane.VerticalSplit!.Value);

        var autoFilter = worksheetPart.Worksheet.GetFirstChild<AutoFilter>();
        Assert.NotNull(autoFilter);
        Assert.Equal("$A$5:$C$5", autoFilter!.Reference!.Value);

        var rows = worksheetPart.Worksheet.Descendants<Row>()
            .Where(row => row.RowIndex is not null)
            .ToDictionary(row => row.RowIndex!.Value);

        Assert.Contains(6U, rows.Keys);
        Assert.Contains(7U, rows.Keys);
        Assert.Contains(8U, rows.Keys);
        Assert.Equal((byte)1, rows[6U].OutlineLevel!.Value);
        Assert.Equal((byte)1, rows[7U].OutlineLevel!.Value);
        Assert.Equal((byte)1, rows[8U].OutlineLevel!.Value);
        Assert.True(rows[6U].Hidden!.Value);
        Assert.True(rows[7U].Hidden!.Value);
        Assert.True(rows[8U].Hidden!.Value);
        Assert.True(rows[8U].Collapsed!.Value);
    }

    /// <summary>
    /// Verifies that render formula ref placeholders are resolved before writing formula.
    /// </summary>
    [Fact]
    public void Render_FormulaRefPlaceholders_AreResolvedBeforeWritingFormula()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCellState(6, 2, 100, formulaRef: "Detail.Value"),
                        CreateCellState(7, 2, 200, formulaRef: "Detail.Value"),
                        CreateCellState(8, 2, value: null, formula: "=SUM(#{Detail.Value:Detail.ValueEnd})+#{Detail.Value}"),
                    ],
                    rows: 20,
                    cols: 10),
            ]);
        var worksheet = Assert.Single(new WorksheetStateBuilder().Build(plan));
        var renderer = CreateRenderer();

        var result = renderer.Render([worksheet], CreateOptions());

        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");

        Assert.Equal("SUM(B6:B7)+B6", GetCell(worksheetPart, "B8").CellFormula!.Text);
    }

    /// <summary>
    /// Verifies that render issues sheet generated.
    /// </summary>
    [Fact]
    public void Render_IssuesSheet_Generated()
    {
        var sheet = CreateWorksheet(
            "Summary",
            cells:
            [
                CreateCell(1, 1, "Value"),
            ]);

        Issue[] issues =
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

    /// <summary>
    /// Verifies that render audit sheet generated.
    /// </summary>
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

    /// <summary>
    /// Verifies that render multiple sheets all rendered.
    /// </summary>
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

    private static SheetOptionsAst CreateSheetOptions(string innerXml)
    {
        var issues = new List<Issue>();
        var element = XElement.Parse(
            "<sheetOptions xmlns=\"urn:excelreport:v1\">" +
            innerXml +
            "</sheetOptions>");

        var options = new SheetOptionsAst(element, issues);

        Assert.DoesNotContain(issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
        return options;
    }

    private static ExcelReportLib.WorksheetState.WorksheetState CreateWorksheet(
        string name,
        IReadOnlyList<CellState> cells,
        IReadOnlyList<MergedCellRange>? mergedRanges = null,
        WorksheetOptionsState? options = null)
    {
        var dictionary = cells.ToDictionary(cell => (cell.Row, cell.Column));

        return new ExcelReportLib.WorksheetState.WorksheetState(
            name,
            rowCount: Math.Max(10, cells.DefaultIfEmpty().Max(cell => cell?.Row ?? 0)),
            columnCount: Math.Max(10, cells.DefaultIfEmpty().Max(cell => cell?.Column ?? 0)),
            cells: dictionary,
            mergedRanges: mergedRanges ?? [],
            namedAreas: new Dictionary<string, NamedAreaState>(StringComparer.Ordinal),
            options: options ?? WorksheetOptionsState.Empty);
    }

    private static LayoutCell CreateCellState(
        int row,
        int col,
        object? value,
        string? formula = null,
        string? formulaRef = null) =>
        new(
            row,
            col,
            rowSpan: 1,
            colSpan: 1,
            value,
            formula,
            formulaRef,
            new StylePlan(
                CreateStyle(),
                appliedStyles: [],
                workbookDefault: null,
                sheetDefault: null,
                referenceStyles: [],
                inlineStyles: [],
                fontNameTrace: null,
                fontSizeTrace: null,
                fontBoldTrace: null,
                fontItalicTrace: null,
                fontUnderlineTrace: null,
                fillColorTrace: null,
                numberFormatCodeTrace: null,
                borderTraces: []));

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
