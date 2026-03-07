using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.LayoutEngine;
using ExcelReportLib.Styles;
using ExcelReportLib.WorksheetState;
using Xunit;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>WorksheetState</c> feature.
/// </summary>
public sealed class WorksheetStateTests
{
    /// <summary>
    /// Verifies that build from layout plan produces cells.
    /// </summary>
    [Fact]
    public void Build_FromLayoutPlan_ProducesCells()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 1, col: 1, value: "Header"),
                        CreateCell(row: 2, col: 1, value: 42),
                    ],
                    rows: 10,
                    cols: 5),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));

        Assert.Equal("Summary", sheet.Name);
        Assert.Equal(10, sheet.RowCount);
        Assert.Equal(5, sheet.ColumnCount);
        Assert.Equal(2, sheet.Cells.Count);

        var header = sheet.Cells[(1, 1)];
        Assert.Equal("Header", header.Value);
        Assert.False(header.IsFormula);

        var valueCell = sheet.Cells[(2, 1)];
        Assert.Equal(42, valueCell.Value);
    }

    /// <summary>
    /// Verifies that build merged cells tracked correctly.
    /// </summary>
    [Fact]
    public void Build_MergedCells_TrackedCorrectly()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 2, col: 3, rowSpan: 2, colSpan: 3, value: "Merged"),
                    ],
                    rows: 10,
                    cols: 10),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));
        var mergedRange = Assert.Single(sheet.MergedRanges);
        var mergedHead = sheet.Cells[(2, 3)];

        Assert.Equal(2, mergedRange.TopRow);
        Assert.Equal(3, mergedRange.LeftColumn);
        Assert.Equal(3, mergedRange.BottomRow);
        Assert.Equal(5, mergedRange.RightColumn);
        Assert.True(mergedHead.IsMergedHead);
    }

    /// <summary>
    /// Verifies that build named area registered.
    /// </summary>
    [Fact]
    public void Build_NamedArea_Registered()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 1, col: 1, value: "Totals"),
                    ],
                    rows: 10,
                    cols: 10,
                    namedAreas:
                    [
                        new LayoutNamedArea("Totals", topRow: 1, leftColumn: 1, bottomRow: 3, rightColumn: 2),
                    ]),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));
        var area = sheet.NamedAreas["Totals"];

        Assert.Equal("Totals", area.Name);
        Assert.Equal(1, area.TopRow);
        Assert.Equal(1, area.LeftColumn);
        Assert.Equal(3, area.BottomRow);
        Assert.Equal(2, area.RightColumn);
    }

    /// <summary>
    /// Verifies that build freeze panes applied.
    /// </summary>
    [Fact]
    public void Build_FreezePanes_Applied()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 1, col: 1, value: "Header"),
                    ],
                    rows: 10,
                    cols: 10,
                    options: CreateSheetOptions("""<freeze at="header" />""")),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));

        Assert.NotNull(sheet.Options.FreezePanes);
        Assert.Equal("header", sheet.Options.FreezePanes!.Target);
    }

    /// <summary>
    /// Verifies that build auto filter applied.
    /// </summary>
    [Fact]
    public void Build_AutoFilter_Applied()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 1, col: 1, value: "Header"),
                    ],
                    rows: 10,
                    cols: 10,
                    options: CreateSheetOptions("""<autoFilter at="table" />""")),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));

        Assert.NotNull(sheet.Options.AutoFilter);
        Assert.Equal("table", sheet.Options.AutoFilter!.Target);
    }

    /// <summary>
    /// Verifies that build group rows applied.
    /// </summary>
    [Fact]
    public void Build_GroupRows_Applied()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 1, col: 1, value: "Header"),
                    ],
                    rows: 10,
                    cols: 10,
                    options: CreateSheetOptions(
                        """
                        <groups>
                          <groupRows at="detail" collapsed="true" />
                          <groupRows at="summary" collapsed="false" />
                        </groups>
                        """)),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));

        Assert.Collection(
            sheet.Options.RowGroups,
            first =>
            {
                Assert.Equal("detail", first.Target);
                Assert.True(first.Collapsed);
            },
            second =>
            {
                Assert.Equal("summary", second.Target);
                Assert.False(second.Collapsed);
            });
    }

    /// <summary>
    /// Verifies that build sheet option targets resolved from named areas.
    /// </summary>
    [Fact]
    public void Build_SheetOptionTargets_ResolvedFromNamedAreas()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 5, col: 2, value: "Header"),
                    ],
                    rows: 20,
                    cols: 10,
                    namedAreas:
                    [
                        new LayoutNamedArea("DetailHeader", topRow: 5, leftColumn: 2, bottomRow: 5, rightColumn: 4),
                        new LayoutNamedArea("DetailRows", topRow: 6, leftColumn: 2, bottomRow: 8, rightColumn: 4),
                    ],
                    options: CreateSheetOptions(
                        """
                        <freeze at="DetailHeader" />
                        <groups>
                          <groupRows at="DetailRows" collapsed="true" />
                          <groupCols at="DetailHeader" collapsed="false" />
                        </groups>
                        <autoFilter at="DetailHeader" />
                        """)),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));

        Assert.NotNull(sheet.Options.FreezePanes);
        Assert.Equal("B5", sheet.Options.FreezePanes!.Target);

        var rowGroup = Assert.Single(sheet.Options.RowGroups);
        Assert.Equal("6:8", rowGroup.Target);
        Assert.True(rowGroup.Collapsed);

        var columnGroup = Assert.Single(sheet.Options.ColumnGroups);
        Assert.Equal("B:D", columnGroup.Target);
        Assert.False(columnGroup.Collapsed);

        Assert.NotNull(sheet.Options.AutoFilter);
        Assert.Equal("B5:D5", sheet.Options.AutoFilter!.Target);
    }

    /// <summary>
    /// Verifies that build sheet bounds validated.
    /// </summary>
    [Fact]
    public void Build_SheetBounds_Validated()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 4, col: 3, rowSpan: 2, colSpan: 1, value: "Out"),
                    ],
                    rows: 4,
                    cols: 4),
            ]);

        var builder = new WorksheetStateBuilder();

        Assert.Throws<InvalidOperationException>(() => builder.Build(plan));
    }

    /// <summary>
    /// Verifies that build formula cells preserved.
    /// </summary>
    [Fact]
    public void Build_FormulaCells_Preserved()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 3, col: 2, value: null, formula: "=SUM(B1:B2)", formulaRef: "Total"),
                    ],
                    rows: 10,
                    cols: 10),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));
        var cell = sheet.Cells[(3, 2)];

        Assert.True(cell.IsFormula);
        Assert.Equal("=SUM(B1:B2)", cell.Formula);
        Assert.Equal("Total", cell.FormulaReference);
    }

    /// <summary>
    /// Verifies that build formula ref placeholders resolved to cell references without registering formula ref named areas.
    /// </summary>
    [Fact]
    public void Build_FormulaRefPlaceholders_ResolvedToCellReferences_WithoutRegisteringNamedAreas()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 6, col: 2, value: 100, formulaRef: "Detail.Value"),
                        CreateCell(row: 7, col: 2, value: 200, formulaRef: "Detail.Value"),
                        CreateCell(row: 8, col: 2, value: null, formula: "=SUM(#{Detail.Value:Detail.ValueEnd})+#{Detail.Value}"),
                    ],
                    rows: 20,
                    cols: 10),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));
        var formulaCell = sheet.Cells[(8, 2)];

        Assert.Equal("=SUM(B6:B7)+B6", formulaCell.Formula);
        Assert.DoesNotContain("Detail.Value", sheet.NamedAreas.Keys);
        Assert.DoesNotContain("Detail.ValueEnd", sheet.NamedAreas.Keys);
    }

    /// <summary>
    /// Verifies that build formula placeholder range with multi cell named area uses bottom right as end reference.
    /// </summary>
    [Fact]
    public void Build_FormulaPlaceholderRange_WithMultiCellNamedArea_UsesBottomRightAsEndReference()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 10, col: 2, value: null, formula: "=SUM(#{DetailRange:DetailRange})"),
                    ],
                    rows: 20,
                    cols: 10,
                    namedAreas:
                    [
                        new LayoutNamedArea("DetailRange", topRow: 6, leftColumn: 2, bottomRow: 8, rightColumn: 4),
                    ]),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));
        var formulaCell = sheet.Cells[(10, 2)];

        Assert.Equal("=SUM(B6:D8)", formulaCell.Formula);
    }

    /// <summary>
    /// Verifies that build formula ref named area when name collides preserves existing named area definition.
    /// </summary>
    [Fact]
    public void Build_FormulaRefNamedArea_WhenNameCollides_PreservesExistingNamedAreaDefinition()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 6, col: 2, value: 100, formulaRef: "Detail.Value"),
                        CreateCell(row: 7, col: 2, value: 200, formulaRef: "Detail.Value"),
                        CreateCell(row: 8, col: 2, value: null, formula: "=SUM(#{Detail.Value:Detail.ValueEnd})"),
                    ],
                    rows: 20,
                    cols: 10,
                    namedAreas:
                    [
                        new LayoutNamedArea("Detail.Value", topRow: 1, leftColumn: 1, bottomRow: 3, rightColumn: 1),
                        new LayoutNamedArea("Detail.ValueEnd", topRow: 4, leftColumn: 3, bottomRow: 5, rightColumn: 4),
                    ]),
            ]);

        var builder = new WorksheetStateBuilder();

        var sheet = Assert.Single(builder.Build(plan));
        var start = sheet.NamedAreas["Detail.Value"];
        var end = sheet.NamedAreas["Detail.ValueEnd"];
        var formulaCell = sheet.Cells[(8, 2)];

        Assert.Equal(1, start.TopRow);
        Assert.Equal(1, start.LeftColumn);
        Assert.Equal(3, start.BottomRow);
        Assert.Equal(1, start.RightColumn);
        Assert.Equal(4, end.TopRow);
        Assert.Equal(3, end.LeftColumn);
        Assert.Equal(5, end.BottomRow);
        Assert.Equal(4, end.RightColumn);
        Assert.Equal("=SUM(A1:D5)", formulaCell.Formula);
    }

    private static LayoutCell CreateCell(
        int row,
        int col,
        int rowSpan = 1,
        int colSpan = 1,
        object? value = null,
        string? formula = null,
        string? formulaRef = null) =>
        new(
            row,
            col,
            rowSpan,
            colSpan,
            value,
            formula,
            formulaRef,
            CreateStylePlan());

    private static SheetOptionsAst CreateSheetOptions(string innerXml)
    {
        var issues = new List<Issue>();
        var element = XElement.Parse(
            $$"""
            <sheetOptions xmlns="urn:excelreport:v1">
              {{innerXml}}
            </sheetOptions>
            """);

        var options = new SheetOptionsAst(element, issues);

        Assert.DoesNotContain(issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
        return options;
    }

    private static StylePlan CreateStylePlan()
    {
        var effectiveStyle = new ResolvedStyle(
            sourceName: "test",
            sourceKind: StyleSourceKind.Computed,
            declaredScope: StyleScope.Cell);

        return new StylePlan(
            effectiveStyle,
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
            borderTraces: []);
    }
}
