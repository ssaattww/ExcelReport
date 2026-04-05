using ExcelReportLib.DSL;
using ExcelReportLib.LayoutEngine;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>LayoutEngine</c> feature.
/// </summary>
public sealed class LayoutEngineTests
{
    /// <summary>
    /// Verifies that expand single cell produces layout cell.
    /// </summary>
    [Fact]
    public void Expand_SingleCell_ProducesLayoutCell()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" value="Hello" />
              </sheet>
            </workbook>
            """);

        var sheet = Assert.Single(plan.Sheets);
        var cell = Assert.Single(sheet.Cells);

        Assert.Equal("Summary", sheet.Name);
        Assert.Equal(1, cell.Row);
        Assert.Equal(1, cell.Col);
        Assert.Equal("Hello", cell.Value);
        Assert.NotNull(cell.StylePlan);
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that expand grid children positioned.
    /// </summary>
    [Fact]
    public void Expand_Grid_ChildrenPositioned()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <grid r="2" c="3">
                  <cell r="1" c="1" value="A" />
                  <cell r="2" c="2" value="B" />
                </grid>
              </sheet>
            </workbook>
            """);

        var cells = Assert.Single(plan.Sheets).Cells;

        Assert.Collection(
            cells,
            first =>
            {
                Assert.Equal(2, first.Row);
                Assert.Equal(3, first.Col);
                Assert.Equal("A", first.Value);
            },
            second =>
            {
                Assert.Equal(3, second.Row);
                Assert.Equal(4, second.Col);
                Assert.Equal("B", second.Value);
            });
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that expand repeat expands collection.
    /// </summary>
    [Fact]
    public void Expand_Repeat_ExpandsCollection()
    {
        var root = new RepeatRoot
        {
            Items =
            [
                new RepeatItem { Name = "First" },
                new RepeatItem { Name = "Second" },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
                  <cell value="@(it.Name)" />
                </repeat>
              </sheet>
            </workbook>
            """,
            root);

        var cells = Assert.Single(plan.Sheets).Cells;

        Assert.Collection(
            cells,
            first =>
            {
                Assert.Equal(1, first.Row);
                Assert.Equal(1, first.Col);
                Assert.Equal("First", first.Value);
            },
            second =>
            {
                Assert.Equal(2, second.Row);
                Assert.Equal(1, second.Col);
                Assert.Equal("Second", second.Value);
            });
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that repeat grid direct siblings share local scope paths.
    /// </summary>
    [Fact]
    public void Expand_RepeatGridSiblings_ShareLocalScopePath()
    {
        var root = new RepeatRoot
        {
            Items =
            [
                new RepeatItem { Name = "First" },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
                  <grid>
                    <cell r="1" c="1" value="@(it.Name)" formulaRef="RowData" formulaRefScope="local" />
                    <cell r="1" c="2" value="=SUM(#{RowData:RowDataEnd})" />
                  </grid>
                </repeat>
              </sheet>
            </workbook>
            """,
            root);

        var sheet = Assert.Single(plan.Sheets);
        var rowDataCell = Assert.Single(sheet.Cells.Where(cell => string.Equals(cell.FormulaRef, "RowData", StringComparison.Ordinal)));
        var formulaCell = Assert.Single(sheet.Cells.Where(cell => string.Equals(cell.Formula, "=SUM(#{RowData:RowDataEnd})", StringComparison.Ordinal)));

        Assert.Equal(rowDataCell.ScopePath, formulaCell.ScopePath);
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that top-level sheet cell siblings share local scope paths.
    /// </summary>
    [Fact]
    public void Expand_SheetCellSiblings_ShareLocalScopePath()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell c="2" value="10" formulaRef="RowData" formulaRefScope="local" />
                <cell c="3" value="=SUM(#{RowData:RowDataEnd})" />
              </sheet>
            </workbook>
            """);

        var sheet = Assert.Single(plan.Sheets);
        var rowDataCell = Assert.Single(sheet.Cells.Where(cell => string.Equals(cell.FormulaRef, "RowData", StringComparison.Ordinal)));
        var formulaCell = Assert.Single(sheet.Cells.Where(cell => string.Equals(cell.Formula, "=SUM(#{RowData:RowDataEnd})", StringComparison.Ordinal)));

        Assert.Equal(rowDataCell.ScopePath, formulaCell.ScopePath);
        Assert.Equal("/sheet", rowDataCell.ScopePath);
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that chart expansion keeps cell scope path behavior and captures chart metadata.
    /// </summary>
    [Fact]
    public void Expand_SheetChart_KeepsCellScopePathAndCapturesChart()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell c="2" value="10" formulaRef="RowData" formulaRefScope="local" />
                <cell c="3" value="=SUM(#{RowData:RowDataEnd})" />
                <chart type="barStacked" r="2" c="8" width="10" height="16" category="A2:A4">
                  <series name="Done" value="B2:B4" />
                </chart>
              </sheet>
            </workbook>
            """);

        var sheet = Assert.Single(plan.Sheets);
        var rowDataCell = Assert.Single(sheet.Cells.Where(cell => string.Equals(cell.FormulaRef, "RowData", StringComparison.Ordinal)));
        var formulaCell = Assert.Single(sheet.Cells.Where(cell => string.Equals(cell.Formula, "=SUM(#{RowData:RowDataEnd})", StringComparison.Ordinal)));
        var chart = Assert.Single(sheet.Charts);

        Assert.Equal("/sheet", rowDataCell.ScopePath);
        Assert.Equal(rowDataCell.ScopePath, formulaCell.ScopePath);
        Assert.Equal("barStacked", chart.ChartType);
        Assert.Equal(2, chart.TopRow);
        Assert.Equal(8, chart.LeftColumn);
        Assert.Equal("A2:A4", chart.CategoryReference);
        Assert.Single(chart.Series);
        Assert.Equal("B2:B4", chart.Series[0].ValueReference);
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that chart with invalid coordinates is reported and excluded from layout sheet.
    /// </summary>
    [Fact]
    public void Expand_SheetChart_InvalidCoordinates_IsExcludedFromLayoutSheet()
    {
        var parseResult = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary" rows="10" cols="10">
                <chart type="barStacked" r="1" c="10" width="2" height="1" category="A1:A1">
                  <series name="Done" value="B1:B1" />
                </chart>
              </sheet>
            </workbook>
            """,
            new DslParserOptions
            {
                EnableSchemaValidation = false,
            });

        Assert.NotNull(parseResult.Root);
        var plan = new LayoutEngine.LayoutEngine().Expand(parseResult.Root!, rootData: null);

        var sheet = Assert.Single(plan.Sheets);
        Assert.Empty(sheet.Charts);
        Assert.Contains(
            plan.Issues,
            issue =>
                issue.Kind == IssueKind.CoordinateOutOfRange &&
                issue.Severity == IssueSeverity.Error &&
                (issue.Message.Contains("グラフ配置がシート範囲外", StringComparison.Ordinal) ||
                 issue.Message.Contains("グラフ配置がシートまたは Excel の範囲外", StringComparison.Ordinal) ||
                 issue.Message.Contains("chart の配置がシートまたは Excel の上限を超えています", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Verifies that chart exceeding Excel max row/column is reported and excluded even when sheet rows/cols are omitted.
    /// </summary>
    [Fact]
    public void Expand_SheetChart_ExceedsExcelLimitsWithoutSheetBounds_IsExcludedFromLayoutSheet()
    {
        var parseResult = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <chart type="barStacked" r="1048576" c="1" width="1" height="2" category="A1:A1">
                  <series name="Done" value="B1:B1" />
                </chart>
              </sheet>
            </workbook>
            """,
            new DslParserOptions
            {
                EnableSchemaValidation = false,
            });

        Assert.NotNull(parseResult.Root);
        var plan = new LayoutEngine.LayoutEngine().Expand(parseResult.Root!, rootData: null);

        var sheet = Assert.Single(plan.Sheets);
        Assert.Empty(sheet.Charts);
        Assert.Contains(
            plan.Issues,
            issue =>
                issue.Kind == IssueKind.CoordinateOutOfRange &&
                issue.Severity == IssueSeverity.Error &&
                issue.Message.Contains("Excel", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that top-level siblings isolate local scope paths.
    /// </summary>
    [Fact]
    public void Expand_TopLevelSiblings_IsolateLocalScopePath()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <grid r="1" c="1">
                  <cell c="2" value="10" formulaRef="RowData" formulaRefScope="local" />
                  <cell c="3" value="=SUM(#{RowData:RowDataEnd})" />
                </grid>
                <grid r="10" c="1">
                  <cell c="2" value="20" formulaRef="RowData" formulaRefScope="local" />
                  <cell c="3" value="=SUM(#{RowData:RowDataEnd})" />
                </grid>
              </sheet>
            </workbook>
            """);

        var sheet = Assert.Single(plan.Sheets);
        var rowDataCells = sheet.Cells
            .Where(cell => string.Equals(cell.FormulaRef, "RowData", StringComparison.Ordinal))
            .OrderBy(cell => cell.Row)
            .ToArray();
        var formulaCells = sheet.Cells
            .Where(cell => string.Equals(cell.Formula, "=SUM(#{RowData:RowDataEnd})", StringComparison.Ordinal))
            .OrderBy(cell => cell.Row)
            .ToArray();

        Assert.Equal(2, rowDataCells.Length);
        Assert.Equal(2, formulaCells.Length);
        Assert.Equal(rowDataCells[0].ScopePath, formulaCells[0].ScopePath);
        Assert.Equal(rowDataCells[1].ScopePath, formulaCells[1].ScopePath);
        Assert.NotEqual(rowDataCells[0].ScopePath, rowDataCells[1].ScopePath);
        Assert.NotEqual(formulaCells[0].ScopePath, formulaCells[1].ScopePath);
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that expand use resolves component.
    /// </summary>
    [Fact]
    public void Expand_Use_ResolvesComponent()
    {
        var root = new UseRoot
        {
            Person = new RepeatItem { Name = "Alice" },
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <component name="PersonRow">
                <grid>
                  <cell r="1" c="1" value="@(data.Name)" />
                </grid>
              </component>
              <sheet name="Summary">
                <use component="PersonRow" r="2" c="3" with="@(root.Person)" />
              </sheet>
            </workbook>
            """,
            root);

        var cell = Assert.Single(Assert.Single(plan.Sheets).Cells);

        Assert.Equal(2, cell.Row);
        Assert.Equal(3, cell.Col);
        Assert.Equal("Alice", cell.Value);
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that conditional formatting defined on component is expanded per use scope.
    /// </summary>
    [Fact]
    public void Expand_ComponentConditionalFormatting_ExpandsPerUseScope()
    {
        var root = new RepeatRoot
        {
            Items =
            [
                new RepeatItem { Name = "A" },
                new RepeatItem { Name = "B" },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <component name="DetailRow">
                <conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />
                <grid>
                  <cell c="2" value="@(data.Name)" formulaRef="RowData" formulaRefScope="local" />
                </grid>
              </component>
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
                  <use component="DetailRow" with="@(it)" />
                </repeat>
              </sheet>
            </workbook>
            """,
            root);

        var sheet = Assert.Single(plan.Sheets);
        Assert.Equal(2, sheet.ConditionalFormattings.Count);
        Assert.Equal("/sheet/node-0/repeat-0/use", sheet.ConditionalFormattings[0].ScopePath);
        Assert.Equal("/sheet/node-0/repeat-1/use", sheet.ConditionalFormattings[1].ScopePath);
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that expand use area, repeat area and grid area generate named areas.
    /// </summary>
    [Fact]
    public void Expand_UseAreaAndRepeatAreaAndGridArea_GeneratesNamedAreas()
    {
        var root = new RepeatRoot
        {
            Items =
            [
                new RepeatItem { Name = "First" },
                new RepeatItem { Name = "Second" },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <component name="DetailHeader">
                <grid>
                  <cell r="1" c="1" value="H1" />
                  <cell r="1" c="2" value="H2" />
                  <cell r="1" c="3" value="H3" />
                </grid>
              </component>
              <component name="DetailRow">
                <grid>
                  <cell r="1" c="1" value="@(data.Name)" />
                  <cell r="1" c="2" value="V" />
                  <cell r="1" c="3" value="C" />
                </grid>
              </component>
              <sheet name="Summary">
                <grid r="4" c="2" area="SummaryGrid">
                  <cell value="G1" />
                  <cell r="2" value="G2" />
                </grid>
                <use component="DetailHeader" area="DetailHeader" r="6" c="2" />
                <repeat area="DetailRows" r="7" c="2" direction="down" from="@(root.Items)" var="it">
                  <use component="DetailRow" with="@(it)" />
                </repeat>
              </sheet>
            </workbook>
            """,
            root);

        var sheet = Assert.Single(plan.Sheets);
        var areas = sheet.NamedAreas.ToDictionary(area => area.Name, StringComparer.Ordinal);

        var summaryGrid = areas["SummaryGrid"];
        Assert.Equal(4, summaryGrid.TopRow);
        Assert.Equal(2, summaryGrid.LeftColumn);
        Assert.Equal(5, summaryGrid.BottomRow);
        Assert.Equal(2, summaryGrid.RightColumn);

        var detailHeader = areas["DetailHeader"];
        Assert.Equal(6, detailHeader.TopRow);
        Assert.Equal(2, detailHeader.LeftColumn);
        Assert.Equal(6, detailHeader.BottomRow);
        Assert.Equal(4, detailHeader.RightColumn);

        var detailRows = areas["DetailRows"];
        Assert.Equal(7, detailRows.TopRow);
        Assert.Equal(2, detailRows.LeftColumn);
        Assert.Equal(8, detailRows.BottomRow);
        Assert.Equal(4, detailRows.RightColumn);

        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that expand with component import resolves imported component use.
    /// </summary>
    [Fact]
    public void Expand_WithComponentImport_ResolvesImportedComponentUse()
    {
        var filePath = DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile);
        var parseResult = DslParser.ParseFromFile(filePath);

        Assert.False(parseResult.HasFatal);
        Assert.NotNull(parseResult.Root);
        var workbook = parseResult.Root!;

        var engine = new LayoutEngine.LayoutEngine();
        var plan = engine.Expand(workbook, DslTestFixtures.CreateFullTemplateData());

        Assert.DoesNotContain(plan.Issues, issue => issue.Kind == IssueKind.UndefinedComponent);
        Assert.Contains(
            Assert.Single(plan.Sheets).Cells,
            cell => cell.Row == 1 && cell.Col == 1 && cell.Value?.ToString() == "Test Report");
    }

    /// <summary>
    /// Verifies that expand when component import load fails does not throw null reference.
    /// </summary>
    [Fact]
    public void Expand_WhenComponentImportLoadFails_DoesNotThrowNullReference()
    {
        var missingImportPath = Path.Combine(
            Path.GetTempPath(),
            $"excelreport-missing-component-{Guid.NewGuid():N}.xml");

        var parseResult = DslParser.ParseFromText(
            $"""
             <workbook xmlns="urn:excelreport:v2">
               <componentImport href="{missingImportPath}" />
               <sheet name="Summary">
                 <cell r="1" c="1" value="Safe" />
               </sheet>
             </workbook>
             """,
            new DslParserOptions
            {
                EnableSchemaValidation = false,
            });

        Assert.False(parseResult.HasFatal);
        Assert.NotNull(parseResult.Root);
        Assert.Contains(parseResult.Issues, issue => issue.Kind == IssueKind.LoadFile);

        LayoutPlan? plan = null;
        var exception = Record.Exception(() => plan = new LayoutEngine.LayoutEngine().Expand(parseResult.Root!, null));

        Assert.Null(exception);
        var cell = Assert.Single(Assert.Single(plan!.Sheets).Cells);
        Assert.Equal("Safe", cell.Value);
    }

    /// <summary>
    /// Verifies that expand when local and imported components share name local component wins.
    /// </summary>
    [Fact]
    public void Expand_WhenLocalAndImportedComponentsShareName_LocalComponentWins()
    {
        var importPath = Path.Combine(
            Path.GetTempPath(),
            $"excelreport-import-{Guid.NewGuid():N}.xml");
        File.WriteAllText(
            importPath,
            """
            <components xmlns="urn:excelreport:v2">
              <component name="SharedComp">
                <cell r="1" c="1" value="Imported" />
              </component>
            </components>
            """);

        try
        {
            var parseResult = DslParser.ParseFromText(
                $"""
                 <workbook xmlns="urn:excelreport:v2">
                   <component name="SharedComp">
                     <cell r="1" c="1" value="Local" />
                   </component>
                   <componentImport href="{importPath}" />
                   <sheet name="Summary">
                     <use component="SharedComp" r="1" c="1" />
                   </sheet>
                 </workbook>
                 """,
                new DslParserOptions
                {
                    EnableSchemaValidation = false,
                });

            Assert.False(parseResult.HasFatal);
            Assert.NotNull(parseResult.Root);

            var plan = new LayoutEngine.LayoutEngine().Expand(parseResult.Root!, null);
            var cell = Assert.Single(Assert.Single(plan.Sheets).Cells);

            Assert.Equal("Local", cell.Value);
        }
        finally
        {
            if (File.Exists(importPath))
            {
                File.Delete(importPath);
            }
        }
    }

    /// <summary>
    /// Verifies that expand when false skips node.
    /// </summary>
    [Fact]
    public void Expand_WhenFalse_SkipsNode()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" when="@(false)" value="Hidden" />
              </sheet>
            </workbook>
            """);

        Assert.Empty(Assert.Single(plan.Sheets).Cells);
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that expand nested repeat grid correct positions.
    /// </summary>
    [Fact]
    public void Expand_NestedRepeatGrid_CorrectPositions()
    {
        var root = new NestedGridRoot
        {
            Rows =
            [
                new GridRow { Left = "A1", Right = "B1" },
                new GridRow { Left = "A2", Right = "B2" },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.Rows)" var="row">
                  <grid>
                    <cell r="1" c="1" value="@(row.Left)" />
                    <cell r="1" c="2" value="@(row.Right)" />
                  </grid>
                </repeat>
              </sheet>
            </workbook>
            """,
            root);

        var cells = Assert.Single(plan.Sheets).Cells;

        Assert.Collection(
            cells,
            first =>
            {
                Assert.Equal(1, first.Row);
                Assert.Equal(1, first.Col);
                Assert.Equal("A1", first.Value);
            },
            second =>
            {
                Assert.Equal(1, second.Row);
                Assert.Equal(2, second.Col);
                Assert.Equal("B1", second.Value);
            },
            third =>
            {
                Assert.Equal(2, third.Row);
                Assert.Equal(1, third.Col);
                Assert.Equal("A2", third.Value);
            },
            fourth =>
            {
                Assert.Equal(2, fourth.Row);
                Assert.Equal(2, fourth.Col);
                Assert.Equal("B2", fourth.Value);
            });
        Assert.Empty(plan.Issues);
    }

    /// <summary>
    /// Verifies that nested repeat var references in a conditional expression are fully rewritten.
    /// </summary>
    [Fact]
    public void Expand_NestedRepeat_ConditionalExpressionUsingVarMultipleTimes_DoesNotEmitExpressionSyntaxError()
    {
        var root = new CompareRoot
        {
            Pairs =
            [
                new ComparePair
                {
                    Key = "L1",
                    Mchs =
                    [
                        new Mch { Name = "Machine1" },
                        new Mch { Name = "Machine2" },
                    ],
                },
                new ComparePair
                {
                    Key = "L2",
                    Mchs =
                    [
                        new Mch { Name = "Machine3" },
                        new Mch { Name = "Machine4" },
                    ],
                },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Compare">
                <repeat r="1" c="1" direction="down" from="@(root.Pairs)" var="p">
                  <grid>
                    <cell r="1" c="1" value="@(p.Key)" />
                    <repeat r="1" c="2" direction="right" from="@(p.Mchs)" var="m">
                      <cell r="1" c="1" value="@(m.Name != &quot;Machine1&quot; ? m.Name : &quot;&quot;)" />
                    </repeat>
                  </grid>
                </repeat>
              </sheet>
            </workbook>
            """,
            root);

        Assert.DoesNotContain(plan.Issues, issue => issue.Kind == IssueKind.ExpressionSyntaxError);

        var values = Assert.Single(plan.Sheets).Cells.Select(cell => cell.Value?.ToString()).ToArray();
        Assert.Contains("L1", values);
        Assert.Contains("L2", values);
        Assert.Contains(string.Empty, values);
        Assert.Contains("Machine2", values);
        Assert.Contains("Machine3", values);
        Assert.Contains("Machine4", values);
    }

    /// <summary>
    /// Verifies that repeat var references wrapped by parentheses do not emit expression syntax error.
    /// </summary>
    [Fact]
    public void Expand_RepeatVarExpressionWrappedWithParentheses_DoesNotEmitExpressionSyntaxError()
    {
        var root = new CompareRoot
        {
            Pairs =
            [
                new ComparePair { LeftBoard = "L1", RightBoard = "R1" },
                new ComparePair { LeftBoard = null, RightBoard = "R2" },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Compare">
                <repeat r="1" c="1" direction="down" from="@(root.Pairs)" var="p">
                  <cell r="1" c="12" value="@((p.LeftBoard ?? &quot;&quot;) + &quot;/&quot; + (p.RightBoard ?? &quot;&quot;))" />
                </repeat>
              </sheet>
            </workbook>
            """,
            root);

        Assert.DoesNotContain(plan.Issues, issue => issue.Kind == IssueKind.ExpressionSyntaxError);

        var cells = Assert.Single(plan.Sheets).Cells;
        Assert.Equal(2, cells.Count);
        Assert.Equal("L1/R1", cells[0].Value);
        Assert.Equal("/R2", cells[1].Value);
    }

    /// <summary>
    /// Verifies that repeat var identifier references are rewritten in any expression position.
    /// </summary>
    [Fact]
    public void Expand_RepeatVarIdentifierReferenceInConditional_DoesNotEmitExpressionSyntaxError()
    {
        var root = new CompareRoot
        {
            Pairs =
            [
                new ComparePair { LeftBoard = "L1", RightBoard = "R1" },
                new ComparePair { LeftBoard = null, RightBoard = "R2" },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Compare">
                <repeat r="1" c="1" direction="down" from="@(root.Pairs)" var="p">
                  <cell r="1" c="12" value="@(p == null ? &quot;NA&quot; : ((p.LeftBoard ?? &quot;&quot;) + &quot;/&quot; + (p.RightBoard ?? &quot;&quot;)))" />
                </repeat>
              </sheet>
            </workbook>
            """,
            root);

        Assert.DoesNotContain(plan.Issues, issue => issue.Kind == IssueKind.ExpressionSyntaxError);

        var cells = Assert.Single(plan.Sheets).Cells;
        Assert.Equal(2, cells.Count);
        Assert.Equal("L1/R1", cells[0].Value);
        Assert.Equal("/R2", cells[1].Value);
    }

    /// <summary>
    /// Verifies that expand grid border mode all applied to all cells.
    /// </summary>
    [Fact]
    public void Expand_GridBorderModeAll_AppliedToAllCells()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <styles>
                <style name="GridAll" scope="grid">
                  <border mode="all" top="thin" bottom="thin" left="thin" right="thin" color="#123456" />
                </style>
              </styles>
              <sheet name="Summary">
                <grid r="1" c="1">
                  <styleRef name="GridAll" />
                  <cell r="1" c="1" value="A" />
                  <cell r="1" c="2" value="B" />
                  <cell r="2" c="1" value="C" />
                  <cell r="2" c="2" value="D" />
                </grid>
              </sheet>
            </workbook>
            """);

        var cells = Assert.Single(plan.Sheets).Cells;
        Assert.Equal(4, cells.Count);

        foreach (var cell in cells)
        {
            var border = Assert.Single(cell.StylePlan.Borders);
            Assert.Equal("cell", border.Mode);
            Assert.Equal("thin", border.Top);
            Assert.Equal("thin", border.Bottom);
            Assert.Equal("thin", border.Left);
            Assert.Equal("thin", border.Right);
            Assert.Equal("#123456", border.Color);
        }

        Assert.DoesNotContain(
            plan.Issues,
            issue => issue.Kind == IssueKind.StyleScopeViolation);
    }

    /// <summary>
    /// Verifies that expand grid border mode outer applied to edge cells.
    /// </summary>
    [Fact]
    public void Expand_GridBorderModeOuter_AppliedToEdgeCells()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <styles>
                <style name="GridOuter" scope="grid">
                  <border mode="outer" top="thin" bottom="thin" left="thin" right="thin" color="#654321" />
                </style>
              </styles>
              <sheet name="Summary">
                <grid r="1" c="1">
                  <styleRef name="GridOuter" />
                  <cell r="1" c="1" value="A" />
                  <cell r="1" c="2" value="B" />
                  <cell r="2" c="1" value="C" />
                  <cell r="2" c="2" value="D" />
                </grid>
              </sheet>
            </workbook>
            """);

        var cells = Assert.Single(plan.Sheets).Cells.ToDictionary(cell => (cell.Row, cell.Col));
        Assert.Equal(4, cells.Count);

        var topLeft = Assert.Single(cells[(1, 1)].StylePlan.Borders);
        Assert.Equal("cell", topLeft.Mode);
        Assert.Equal("thin", topLeft.Top);
        Assert.Null(topLeft.Bottom);
        Assert.Equal("thin", topLeft.Left);
        Assert.Null(topLeft.Right);
        Assert.Equal("#654321", topLeft.Color);

        var topRight = Assert.Single(cells[(1, 2)].StylePlan.Borders);
        Assert.Equal("cell", topRight.Mode);
        Assert.Equal("thin", topRight.Top);
        Assert.Null(topRight.Bottom);
        Assert.Null(topRight.Left);
        Assert.Equal("thin", topRight.Right);

        var bottomLeft = Assert.Single(cells[(2, 1)].StylePlan.Borders);
        Assert.Equal("cell", bottomLeft.Mode);
        Assert.Null(bottomLeft.Top);
        Assert.Equal("thin", bottomLeft.Bottom);
        Assert.Equal("thin", bottomLeft.Left);
        Assert.Null(bottomLeft.Right);

        var bottomRight = Assert.Single(cells[(2, 2)].StylePlan.Borders);
        Assert.Equal("cell", bottomRight.Mode);
        Assert.Null(bottomRight.Top);
        Assert.Equal("thin", bottomRight.Bottom);
        Assert.Null(bottomRight.Left);
        Assert.Equal("thin", bottomRight.Right);

        Assert.DoesNotContain(
            plan.Issues,
            issue => issue.Kind == IssueKind.StyleScopeViolation);
    }

    /// <summary>
    /// Verifies that expand grid border and cell inline border cell border wins by order.
    /// </summary>
    [Fact]
    public void Expand_GridBorderAndCellInlineBorder_CellBorderWinsByOrder()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <styles>
                <style name="GridAll" scope="grid">
                  <border mode="all" top="thin" color="#111111" />
                </style>
              </styles>
              <sheet name="Summary">
                <grid r="1" c="1">
                  <styleRef name="GridAll" />
                  <cell r="1" c="1" value="A">
                    <style>
                      <border mode="cell" top="thick" color="#222222" />
                    </style>
                  </cell>
                </grid>
              </sheet>
            </workbook>
            """);

        var cell = Assert.Single(Assert.Single(plan.Sheets).Cells);
        Assert.Equal(2, cell.StylePlan.Borders.Count);

        var expandedGridBorder = cell.StylePlan.Borders[0];
        Assert.Equal("cell", expandedGridBorder.Mode);
        Assert.Equal("thin", expandedGridBorder.Top);
        Assert.Equal("#111111", expandedGridBorder.Color);

        var inlineCellBorder = cell.StylePlan.Borders[1];
        Assert.Equal("cell", inlineCellBorder.Mode);
        Assert.Equal("thick", inlineCellBorder.Top);
        Assert.Equal("#222222", inlineCellBorder.Color);

        Assert.DoesNotContain(
            plan.Issues,
            issue => issue.Kind == IssueKind.StyleScopeViolation);
    }

    /// <summary>
    /// Verifies that expand formula ref and formula placeholder preserved for state build.
    /// </summary>
    [Fact]
    public void Expand_FormulaRefAndFormulaPlaceholder_PreservedForStateBuild()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <repeat r="6" c="2" direction="down" from="@(root.Values)" var="it">
                  <cell value="@(it)" formulaRef="Detail.Value" />
                </repeat>
                <cell r="8" c="2" value="=SUM(#{Detail.Value:Detail.ValueEnd})" />
              </sheet>
            </workbook>
            """,
            new
            {
                Values = new[] { 100, 200 },
            });

        var cells = Assert.Single(plan.Sheets).Cells;
        var detailCells = cells
            .Where(cell => cell.FormulaRef == "Detail.Value")
            .ToArray();

        Assert.Equal(2, detailCells.Length);
        Assert.Contains(detailCells, cell => cell.Row == 6 && cell.Col == 2);
        Assert.Contains(detailCells, cell => cell.Row == 7 && cell.Col == 2);

        var totalsCell = cells.Single(cell => cell.Row == 8 && cell.Col == 2);
        Assert.Equal("=SUM(#{Detail.Value:Detail.ValueEnd})", totalsCell.Formula);
    }

    /// <summary>
    /// Verifies that expand omitted sheet rows cols auto calculates from cell placement.
    /// </summary>
    [Fact]
    public void Expand_OmittedSheetRowsCols_AutoCalculatesFromCellPlacement()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" value="A" />
                <grid r="2" c="3">
                  <cell r="2" c="2" rowSpan="2" colSpan="3" value="B" />
                </grid>
              </sheet>
            </workbook>
            """);

        var sheet = Assert.Single(plan.Sheets);
        Assert.Equal(4, sheet.Rows);
        Assert.Equal(6, sheet.Cols);
        Assert.DoesNotContain(plan.Issues, issue => issue.Kind == IssueKind.CoordinateOutOfRange);
    }

    /// <summary>
    /// Verifies that expand grid auto size accounts for child offsets in repeat directions.
    /// </summary>
    [Fact]
    public void Expand_GridAutoSize_AccountsForChildOffsetsInRepeatDirections()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.DownItems)" var="it">
                  <grid>
                    <cell r="2" c="1" value="@(it)" />
                  </grid>
                </repeat>
                <repeat r="1" c="10" direction="right" from="@(root.RightItems)" var="it">
                  <grid>
                    <cell r="1" c="3" value="@(it)" />
                  </grid>
                </repeat>
              </sheet>
            </workbook>
            """,
            new
            {
                DownItems = new[] { "D1", "D2" },
                RightItems = new[] { "R1", "R2" },
            });

        var cellsByValue = Assert.Single(plan.Sheets).Cells.ToDictionary(cell => cell.Value?.ToString()!);
        Assert.Equal(4, cellsByValue.Count);

        Assert.Equal(2, cellsByValue["D1"].Row);
        Assert.Equal(1, cellsByValue["D1"].Col);
        Assert.Equal(4, cellsByValue["D2"].Row);
        Assert.Equal(1, cellsByValue["D2"].Col);

        Assert.Equal(1, cellsByValue["R1"].Row);
        Assert.Equal(12, cellsByValue["R1"].Col);
        Assert.Equal(1, cellsByValue["R2"].Row);
        Assert.Equal(15, cellsByValue["R2"].Col);
    }

    /// <summary>
    /// Verifies that expand sheet from expression expands multiple sheets.
    /// </summary>
    [Fact]
    public void Expand_SheetRepeat_ExpandsMultipleSheets()
    {
        var root = new RepeatRoot
        {
            Items =
            [
                new RepeatItem { Name = "North" },
                new RepeatItem { Name = "South" },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="@(it.Name)" from="@(root.Items)" var="it">
                <cell r="1" c="1" value="@(it.Name)" />
              </sheet>
            </workbook>
            """,
            root);

        Assert.Equal(2, plan.Sheets.Count);
        var first = plan.Sheets[0];
        var second = plan.Sheets[1];

        Assert.Equal("North", first.Name);
        Assert.Equal("South", second.Name);
        Assert.Equal("North", Assert.Single(first.Cells).Value);
        Assert.Equal("South", Assert.Single(second.Cells).Value);
        Assert.DoesNotContain(plan.Issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that expand sheet repeat duplicate names emits duplicate sheet issue.
    /// </summary>
    [Fact]
    public void Expand_SheetRepeat_DuplicateNames_EmitsDuplicateSheetIssue()
    {
        var root = new RepeatRoot
        {
            Items =
            [
                new RepeatItem { Name = "Same" },
                new RepeatItem { Name = "Same" },
            ],
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="@(it.Name)" from="@(root.Items)" var="it">
                <cell r="1" c="1" value="Value" />
              </sheet>
            </workbook>
            """,
            root);

        Assert.Contains(
            plan.Issues,
            issue => issue.Severity == IssueSeverity.Error && issue.Kind == IssueKind.DuplicateSheetName);
    }

    private static LayoutPlan Expand(string workbookXml, object? rootData = null)
    {
        var parseResult = DslParser.ParseFromText(
            workbookXml,
            new DslParserOptions
            {
                EnableSchemaValidation = false,
            });

        Assert.NotNull(parseResult.Root);
        Assert.DoesNotContain(
            parseResult.Issues,
            issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);

        var engine = new LayoutEngine.LayoutEngine();
        return engine.Expand(parseResult.Root!, rootData);
    }

    private sealed class RepeatRoot
    {
        public List<RepeatItem> Items { get; init; } = [];
    }

    private sealed class UseRoot
    {
        public RepeatItem? Person { get; init; }
    }

    private sealed class NestedGridRoot
    {
        public List<GridRow> Rows { get; init; } = [];
    }

    private sealed class CompareRoot
    {
        public List<ComparePair> Pairs { get; init; } = [];
    }

    private sealed class ComparePair
    {
        public string Key { get; init; } = string.Empty;

        public string? LeftBoard { get; init; }

        public string? RightBoard { get; init; }

        public List<Mch> Mchs { get; init; } = [];
    }

    private sealed class Mch
    {
        public string? Name { get; init; }
    }

    private sealed class RepeatItem
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class GridRow
    {
        public string Left { get; init; } = string.Empty;

        public string Right { get; init; } = string.Empty;
    }
}
