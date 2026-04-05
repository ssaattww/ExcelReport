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
                        """),
                    conditionalFormattings: CreateConditionalFormattings(
                        """
                        <conditionalFormatting at="DetailRows" minColor="#112233" maxColor="#AABBCC" />
                        <conditionalFormatting at="DetailHeader" formula="A5&gt;0" formulaRef="DetailHeader" fillColor="#FFEEDD" fontBold="true" borderBottom="thin" borderColor="#222222" />
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

        Assert.Equal(2, sheet.Options.ConditionalFormattings.Count);

        var colorScaleRule = sheet.Options.ConditionalFormattings[0];
        Assert.Equal("B6:D8", colorScaleRule.Target);
        Assert.Equal("#112233", colorScaleRule.MinColor);
        Assert.Equal("#AABBCC", colorScaleRule.MaxColor);

        var formulaRule = sheet.Options.ConditionalFormattings[1];
        Assert.Equal("B5:D5", formulaRule.Target);
        Assert.Equal("A5>0", formulaRule.Formula);
        Assert.Equal("B5", formulaRule.FormulaRef);
        Assert.Equal("#FFEEDD", formulaRule.FillColor);
        Assert.True(formulaRule.FontBold);
        Assert.Equal("thin", formulaRule.BorderBottom);
        Assert.Equal("#222222", formulaRule.BorderColor);
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

    /// <summary>
    /// Verifies that local formula refs resolve within nearest scope while global refs remain accessible.
    /// </summary>
    [Fact]
    public void Build_FormulaRefPlaceholders_LocalScope_ResolvesByNearestScopeAndGlobalFallback()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 1, col: 1, value: 10, formulaRef: "GlobalTotal", formulaRefScope: "global", scopePath: "/sheet/0"),
                        CreateCell(row: 5, col: 2, value: 100, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 5, col: 4, value: null, formula: "=SUM(#{RowData:RowDataEnd})+#{GlobalTotal}", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 6, col: 2, value: 200, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                        CreateCell(row: 6, col: 4, value: null, formula: "=SUM(#{RowData:RowDataEnd})+#{GlobalTotal}", scopePath: "/sheet/0/repeat-1"),
                    ],
                    rows: 20,
                    cols: 10),
            ]);

        var builder = new WorksheetStateBuilder();
        var sheet = Assert.Single(builder.Build(plan));

        Assert.Equal("=SUM(B5:B5)+A1", sheet.Cells[(5, 4)].Formula);
        Assert.Equal("=SUM(B6:B6)+A1", sheet.Cells[(6, 4)].Formula);
    }

    /// <summary>
    /// Verifies that local formula refs fall back to parent local scope before global scope.
    /// </summary>
    [Fact]
    public void Build_FormulaRefPlaceholders_LocalScope_FallsBackToParentScope()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 2, col: 2, value: 50, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0"),
                        CreateCell(row: 5, col: 4, value: null, formula: "=SUM(#{RowData:RowDataEnd})", scopePath: "/sheet/0/repeat-0/0"),
                    ],
                    rows: 20,
                    cols: 10),
            ]);

        var builder = new WorksheetStateBuilder();
        var sheet = Assert.Single(builder.Build(plan));

        Assert.Equal("=SUM(B2:B2)", sheet.Cells[(5, 4)].Formula);
    }

    /// <summary>
    /// Verifies that local formula series do not cross top-level sibling scopes.
    /// </summary>
    [Fact]
    public void Build_FormulaPlaceholder_LocalSeries_DoesNotCrossTopLevelSiblings()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 1, col: 2, value: 10, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/node-0"),
                        CreateCell(row: 1, col: 3, value: null, formula: "=SUM(#{RowData:RowDataEnd})", scopePath: "/sheet/node-0"),
                        CreateCell(row: 10, col: 2, value: 20, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/node-1"),
                        CreateCell(row: 10, col: 3, value: null, formula: "=SUM(#{RowData:RowDataEnd})", scopePath: "/sheet/node-1"),
                    ],
                    rows: 20,
                    cols: 10),
            ]);

        var builder = new WorksheetStateBuilder();
        var sheet = Assert.Single(builder.Build(plan));

        Assert.Equal("=SUM(B1:B1)", sheet.Cells[(1, 3)].Formula);
        Assert.Equal("=SUM(B10:B10)", sheet.Cells[(10, 3)].Formula);
    }

    /// <summary>
    /// Verifies that conditional formatting formula refs resolve from local scope using the target range.
    /// </summary>
    [Fact]
    public void Build_ConditionalFormatting_FormulaRef_LocalScope_ResolvedFromTargetScope()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 5, col: 2, value: 100, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 10, col: 2, value: 200, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                    ],
                    rows: 20,
                    cols: 10,
                    conditionalFormattings: CreateConditionalFormattings(
                        """
                        <conditionalFormatting at="B5:D5" formulaRef="RowData" />
                        <conditionalFormatting at="B10:D10" formulaRef="RowData" />
                        """)),
            ]);

        var builder = new WorksheetStateBuilder();
        var sheet = Assert.Single(builder.Build(plan));

        Assert.Equal(2, sheet.Options.ConditionalFormattings.Count);
        Assert.Equal("B5", sheet.Options.ConditionalFormattings[0].FormulaRef);
        Assert.Equal("B10", sheet.Options.ConditionalFormattings[1].FormulaRef);
    }

    /// <summary>
    /// Verifies that conditional formatting target can resolve from global formulaRef series.
    /// </summary>
    [Fact]
    public void Build_ConditionalFormatting_Target_GlobalFormulaRefSeries_ResolvesRange()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 6, col: 2, value: 100, formulaRef: "Detail.Value"),
                        CreateCell(row: 7, col: 2, value: 200, formulaRef: "Detail.Value"),
                    ],
                    rows: 20,
                    cols: 10,
                    conditionalFormattings: CreateConditionalFormattings("""<conditionalFormatting at="Detail.Value" minColor="#112233" maxColor="#AABBCC" />""")),
            ]);

        var builder = new WorksheetStateBuilder();
        var sheet = Assert.Single(builder.Build(plan));

        var rule = Assert.Single(sheet.Options.ConditionalFormattings);
        Assert.Equal("B6:B7", rule.Target);
    }

    /// <summary>
    /// Verifies that conditional formatting target resolves named area before formulaRef series.
    /// </summary>
    [Fact]
    public void Build_ConditionalFormatting_Target_NamedArea_PrecedesFormulaRefSeries()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 6, col: 2, value: 100, formulaRef: "RowData"),
                        CreateCell(row: 7, col: 2, value: 200, formulaRef: "RowData"),
                    ],
                    rows: 20,
                    cols: 10,
                    namedAreas:
                    [
                        new LayoutNamedArea("RowData", topRow: 1, leftColumn: 1, bottomRow: 1, rightColumn: 1),
                    ],
                    conditionalFormattings: CreateConditionalFormattings("""<conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />""")),
            ]);

        var builder = new WorksheetStateBuilder();
        var sheet = Assert.Single(builder.Build(plan));

        var rule = Assert.Single(sheet.Options.ConditionalFormattings);
        Assert.Equal("A1:A1", rule.Target);
    }

    /// <summary>
    /// Verifies that sheet-scope conditional formatting target does not resolve local formulaRef series from child scopes.
    /// </summary>
    [Fact]
    public void Build_ConditionalFormatting_Target_LocalFormulaRefSeries_FromSheetScope_DoesNotExpand()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 5, col: 2, value: 10, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 6, col: 2, value: 20, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 10, col: 2, value: 30, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                        CreateCell(row: 11, col: 2, value: 40, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                    ],
                    rows: 20,
                    cols: 10,
                    conditionalFormattings: CreateConditionalFormattings("""<conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />""")),
            ]);

        var builder = new WorksheetStateBuilder();
        var sheet = Assert.Single(builder.Build(plan));

        var rule = Assert.Single(sheet.Options.ConditionalFormattings);
        Assert.Equal("RowData", rule.Target);
    }

    /// <summary>
    /// Verifies that global formulaRef series is used when sheet-scope target collides with local series names.
    /// </summary>
    [Fact]
    public void Build_ConditionalFormatting_Target_FormulaRefNameCollision_UsesGlobalSeriesOutsideLocalScope()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 2, col: 2, value: 999, formulaRef: "RowData", formulaRefScope: "global", scopePath: "/sheet/0"),
                        CreateCell(row: 5, col: 2, value: 10, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 6, col: 2, value: 20, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 10, col: 2, value: 30, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                        CreateCell(row: 11, col: 2, value: 40, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                    ],
                    rows: 20,
                    cols: 10,
                    conditionalFormattings: CreateConditionalFormattings("""<conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />""")),
            ]);

        var builder = new WorksheetStateBuilder();
        var sheet = Assert.Single(builder.Build(plan));

        var rule = Assert.Single(sheet.Options.ConditionalFormattings);
        Assert.Equal("B2:B2", rule.Target);
    }

    /// <summary>
    /// Verifies that ambiguous descendant local formulaRef lookup emits warning and falls back.
    /// </summary>
    [Fact]
    public void Build_FormulaRefPlaceholders_AmbiguousDescendantLocalMatch_EmitsWarningAndFallsBack()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 2, col: 2, value: 999, formulaRef: "RowData", formulaRefScope: "global", scopePath: "/sheet/0"),
                        CreateCell(row: 5, col: 2, value: 10, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 10, col: 2, value: 20, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                        CreateCell(row: 3, col: 4, value: null, formula: "=#{RowData}", scopePath: "/sheet/0"),
                    ],
                    rows: 20,
                    cols: 10),
            ]);

        var builder = new WorksheetStateBuilder();
        var issues = new List<Issue>();
        var sheet = Assert.Single(builder.Build(plan, issues));

        Assert.Equal("=B2", sheet.Cells[(3, 4)].Formula);
        Assert.Contains(
            issues,
            issue =>
                issue.Severity == IssueSeverity.Warning &&
                issue.Kind == IssueKind.FormulaRefResolutionFallback &&
                issue.Message.Contains("target 'RowData'", StringComparison.Ordinal) &&
                issue.Message.Contains("descendant candidates", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that global formulaRef is preferred when unique descendant local uses the same name.
    /// </summary>
    [Fact]
    public void Build_FormulaRefPlaceholders_UniqueDescendantLocalWithExistingGlobal_PrefersGlobalWithWarning()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 2, col: 2, value: 999, formulaRef: "RowData", formulaRefScope: "global", scopePath: "/sheet/0"),
                        CreateCell(row: 5, col: 2, value: 10, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 3, col: 4, value: null, formula: "=#{RowData}", scopePath: "/sheet/0"),
                    ],
                    rows: 20,
                    cols: 10),
            ]);

        var builder = new WorksheetStateBuilder();
        var issues = new List<Issue>();
        var sheet = Assert.Single(builder.Build(plan, issues));

        Assert.Equal("=B2", sheet.Cells[(3, 4)].Formula);
        Assert.Contains(
            issues,
            issue =>
                issue.Severity == IssueSeverity.Warning &&
                issue.Kind == IssueKind.FormulaRefResolutionFallback &&
                issue.Message.Contains("preferring global lookup", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that conditional formulaRef deterministic tie-break emits warning.
    /// </summary>
    [Fact]
    public void Build_ConditionalFormatting_FormulaRef_AmbiguousScopedCandidates_EmitsWarningAndUsesDeterministicTieBreak()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 5, col: 2, value: 100, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 6, col: 2, value: 200, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                    ],
                    rows: 20,
                    cols: 10,
                    conditionalFormattings: CreateConditionalFormattings("""<conditionalFormatting at="B5:B6" formulaRef="RowData" />""")),
            ]);

        var builder = new WorksheetStateBuilder();
        var issues = new List<Issue>();
        var sheet = Assert.Single(builder.Build(plan, issues));

        var rule = Assert.Single(sheet.Options.ConditionalFormattings);
        Assert.Equal("B5", rule.FormulaRef);
        Assert.Contains(
            issues,
            issue =>
                issue.Severity == IssueSeverity.Warning &&
                issue.Kind == IssueKind.FormulaRefResolutionFallback &&
                issue.Message.Contains("target 'RowData'", StringComparison.Ordinal) &&
                issue.Message.Contains("deterministic tie-break", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that sheet-scope conditional formulaRef falls back to global without descendant local leak.
    /// </summary>
    [Fact]
    public void Build_ConditionalFormatting_FormulaRef_MultipleUniqueLocalCandidates_FallsBackToGlobalWithoutWarning()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 2, col: 2, value: 999, formulaRef: "RowData", formulaRefScope: "global", scopePath: "/sheet/0"),
                        CreateCell(row: 5, col: 2, value: 100, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 10, col: 2, value: 200, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                    ],
                    rows: 20,
                    cols: 10,
                    conditionalFormattings: CreateConditionalFormattings("""<conditionalFormatting at="UnknownTarget" formulaRef="RowData" />""")),
            ]);

        var builder = new WorksheetStateBuilder();
        var issues = new List<Issue>();
        var sheet = Assert.Single(builder.Build(plan, issues));

        var rule = Assert.Single(sheet.Options.ConditionalFormattings);
        Assert.Equal("B2", rule.FormulaRef);
        Assert.DoesNotContain(
            issues,
            issue =>
                issue.Severity == IssueSeverity.Warning &&
                issue.Kind == IssueKind.FormulaRefResolutionFallback &&
                issue.Message.Contains("scope '/sheet'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that sheet-scope conditional formulaRef does not resolve descendant local candidates.
    /// </summary>
    [Fact]
    public void Build_ConditionalFormatting_FormulaRef_FromSheetScope_DoesNotResolveDescendantLocal()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 2, col: 2, value: 999, formulaRef: "RowData", formulaRefScope: "global", scopePath: "/sheet/0"),
                        CreateCell(row: 5, col: 2, value: 100, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-0"),
                        CreateCell(row: 10, col: 2, value: 200, formulaRef: "RowData", formulaRefScope: "local", scopePath: "/sheet/0/repeat-1"),
                    ],
                    rows: 20,
                    cols: 10,
                    conditionalFormattings: CreateConditionalFormattings("""<conditionalFormatting at="A1:A1" formulaRef="RowData" fillColor="#FFEEDD" />""")),
            ]);

        var builder = new WorksheetStateBuilder();
        var issues = new List<Issue>();
        var sheet = Assert.Single(builder.Build(plan, issues));

        var rule = Assert.Single(sheet.Options.ConditionalFormattings);
        Assert.Equal("B2", rule.FormulaRef);
        Assert.DoesNotContain(
            issues,
            issue =>
                issue.Severity == IssueSeverity.Warning &&
                issue.Kind == IssueKind.FormulaRefResolutionFallback &&
                issue.Message.Contains("scope '/sheet'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that chart formulaRef series resolution does not use colliding named-area *End targets.
    /// </summary>
    [Fact]
    public void Build_Charts_GlobalFormulaRefSeries_IgnoresNamedAreaEndCollision()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 2, col: 1, value: "Task1", formulaRef: "Task.Name"),
                        CreateCell(row: 3, col: 1, value: "Task2", formulaRef: "Task.Name"),
                        CreateCell(row: 2, col: 2, value: 10, formulaRef: "Task.Workload"),
                        CreateCell(row: 3, col: 2, value: 20, formulaRef: "Task.Workload"),
                    ],
                    rows: 20,
                    cols: 20,
                    namedAreas:
                    [
                        new LayoutNamedArea("Task.WorkloadEnd", topRow: 2, leftColumn: 5, bottomRow: 2, rightColumn: 5),
                    ],
                    charts:
                    [
                        new LayoutChart(
                            chartType: "barStacked",
                            title: "Progress",
                            name: "ProgressChart",
                            topRow: 2,
                            leftColumn: 8,
                            widthColumns: 10,
                            heightRows: 16,
                            categoryReference: "Task.Name",
                            legendPosition: "right",
                            showDataLabels: true,
                            series:
                            [
                                new LayoutChartSeries(name: "Workload", valueReference: "Task.Workload", color: null, colorKey: "Done", colorByReference: null),
                            ]),
                    ]),
            ],
            chartPalette: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Done"] = "#4CAF50",
            });

        var builder = new WorksheetStateBuilder();
        var issues = new List<Issue>();
        var sheet = Assert.Single(builder.Build(plan, issues));
        var chart = Assert.Single(sheet.Charts);

        Assert.Equal("'Summary'!$B$2:$B$3", chart.Series[0].ValueFormula);
        Assert.DoesNotContain(
            issues,
            issue =>
                issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal &&
                issue.Message.Contains("系列長が不一致", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that chart references and colors are resolved into chart states.
    /// </summary>
    [Fact]
    public void Build_Charts_ResolvesReferencesAndColors()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "Summary",
                    [
                        CreateCell(row: 2, col: 1, value: "Task1"),
                        CreateCell(row: 3, col: 1, value: "Task2"),
                        CreateCell(row: 4, col: 1, value: "Task3"),
                        CreateCell(row: 2, col: 2, value: 10),
                        CreateCell(row: 3, col: 2, value: 20),
                        CreateCell(row: 4, col: 2, value: 30),
                        CreateCell(row: 2, col: 3, value: "Done"),
                        CreateCell(row: 3, col: 3, value: "Todo"),
                        CreateCell(row: 4, col: 3, value: "Done"),
                    ],
                    rows: 20,
                    cols: 20,
                    charts:
                    [
                        new LayoutChart(
                            chartType: "barStacked",
                            title: "Progress",
                            name: "ProgressChart",
                            topRow: 2,
                            leftColumn: 8,
                            widthColumns: 10,
                            heightRows: 16,
                            categoryReference: "A2:A4",
                            legendPosition: "right",
                            showDataLabels: true,
                            series:
                            [
                                new LayoutChartSeries(name: "ByColor", valueReference: "B2:B4", color: null, colorKey: null, colorByReference: "C2:C4"),
                                new LayoutChartSeries(name: "FixedKey", valueReference: "B2:B4", color: null, colorKey: "Done ", colorByReference: null),
                            ]),
                    ]),
            ],
            chartPalette: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Done"] = "#4CAF50",
                ["Todo"] = "#BDBDBD",
            });

        var builder = new WorksheetStateBuilder();
        var sheet = Assert.Single(builder.Build(plan));
        var chart = Assert.Single(sheet.Charts);

        Assert.Equal("barStacked", chart.ChartType);
        Assert.Equal("'Summary'!$A$2:$A$4", chart.CategoryFormula);
        Assert.Equal("right", chart.LegendPosition);
        Assert.True(chart.ShowDataLabels);
        Assert.Equal(2, chart.Series.Count);

        Assert.Equal("'Summary'!$B$2:$B$4", chart.Series[0].ValueFormula);
        Assert.Equal(["#4CAF50", "#BDBDBD", "#4CAF50"], chart.Series[0].PointColors);
        Assert.Equal(["#4CAF50", "#4CAF50", "#4CAF50"], chart.Series[1].PointColors);
    }

    /// <summary>
    /// Verifies that fallback color-key assignments remain consistent across sheets in one workbook build.
    /// </summary>
    [Fact]
    public void Build_Charts_ColorByFallbackAssignments_AreConsistentAcrossSheets()
    {
        var plan = new LayoutPlan(
            [
                new LayoutSheet(
                    "SummaryA",
                    [
                        CreateCell(row: 2, col: 1, value: "Task1"),
                        CreateCell(row: 3, col: 1, value: "Task2"),
                        CreateCell(row: 2, col: 2, value: 10),
                        CreateCell(row: 3, col: 2, value: 20),
                        CreateCell(row: 2, col: 3, value: "Alpha"),
                        CreateCell(row: 3, col: 3, value: "Beta"),
                    ],
                    rows: 20,
                    cols: 20,
                    charts:
                    [
                        new LayoutChart(
                            chartType: "barStacked",
                            title: "ProgressA",
                            name: "ProgressChartA",
                            topRow: 2,
                            leftColumn: 8,
                            widthColumns: 10,
                            heightRows: 16,
                            categoryReference: "A2:A3",
                            legendPosition: "right",
                            showDataLabels: true,
                            series:
                            [
                                new LayoutChartSeries(name: "Workload", valueReference: "B2:B3", color: null, colorKey: null, colorByReference: "C2:C3"),
                            ]),
                    ]),
                new LayoutSheet(
                    "SummaryB",
                    [
                        CreateCell(row: 2, col: 1, value: "Task3"),
                        CreateCell(row: 3, col: 1, value: "Task4"),
                        CreateCell(row: 2, col: 2, value: 30),
                        CreateCell(row: 3, col: 2, value: 40),
                        CreateCell(row: 2, col: 3, value: "Beta"),
                        CreateCell(row: 3, col: 3, value: "Alpha"),
                    ],
                    rows: 20,
                    cols: 20,
                    charts:
                    [
                        new LayoutChart(
                            chartType: "barStacked",
                            title: "ProgressB",
                            name: "ProgressChartB",
                            topRow: 2,
                            leftColumn: 8,
                            widthColumns: 10,
                            heightRows: 16,
                            categoryReference: "A2:A3",
                            legendPosition: "right",
                            showDataLabels: true,
                            series:
                            [
                                new LayoutChartSeries(name: "Workload", valueReference: "B2:B3", color: null, colorKey: null, colorByReference: "C2:C3"),
                            ]),
                    ]),
            ],
            chartPalette: new Dictionary<string, string>(StringComparer.Ordinal));

        var builder = new WorksheetStateBuilder();
        var sheets = builder.Build(plan);

        var firstSheetColors = Assert.Single(Assert.Single(sheets[0].Charts).Series).PointColors;
        var secondSheetColors = Assert.Single(Assert.Single(sheets[1].Charts).Series).PointColors;
        Assert.NotNull(firstSheetColors);
        Assert.NotNull(secondSheetColors);

        var firstColors = firstSheetColors!;
        var secondColors = secondSheetColors!;

        Assert.Equal(2, firstColors.Count);
        Assert.Equal(2, secondColors.Count);

        var alphaColor = firstColors[0];
        var betaColor = firstColors[1];

        Assert.NotEqual(alphaColor, betaColor);
        Assert.Equal(betaColor, secondColors[0]);
        Assert.Equal(alphaColor, secondColors[1]);
    }

    private static LayoutCell CreateCell(
        int row,
        int col,
        int rowSpan = 1,
        int colSpan = 1,
        object? value = null,
        string? formula = null,
        string? formulaRef = null,
        string? formulaRefScope = null,
        string? scopePath = "/sheet/0") =>
        new(
            row,
            col,
            rowSpan,
            colSpan,
            value,
            formula,
            formulaRef,
            formulaRefScope,
            scopePath ?? "/sheet/0",
            CreateStylePlan());

    private static SheetOptionsAst CreateSheetOptions(string innerXml)
    {
        var issues = new List<Issue>();
        var element = XElement.Parse(
            $$"""
            <sheetOptions xmlns="urn:excelreport:v2">
              {{innerXml}}
            </sheetOptions>
            """);

        var options = new SheetOptionsAst(element, issues);

        Assert.DoesNotContain(issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
        return options;
    }

    private static IReadOnlyList<ConditionalFormattingAst> CreateConditionalFormattings(string innerXml)
    {
        var issues = new List<Issue>();
        var sheetElement = XElement.Parse(
            $$"""
            <sheet xmlns="urn:excelreport:v2" name="Summary">
              {{innerXml}}
            </sheet>
            """);

        var sheet = new SheetAst(sheetElement, issues);
        var rules = sheet.ConditionalFormattings;

        Assert.DoesNotContain(issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
        return rules;
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
