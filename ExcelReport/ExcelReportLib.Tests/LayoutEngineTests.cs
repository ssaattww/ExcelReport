using ExcelReportLib.DSL;
using ExcelReportLib.LayoutEngine;

namespace ExcelReportLib.Tests;

public sealed class LayoutEngineTests
{
    [Fact]
    public void Expand_SingleCell_ProducesLayoutCell()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
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

    [Fact]
    public void Expand_Grid_ChildrenPositioned()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
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
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
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

    [Fact]
    public void Expand_Use_ResolvesComponent()
    {
        var root = new UseRoot
        {
            Person = new RepeatItem { Name = "Alice" },
        };

        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v1">
              <component name="PersonRow">
                <grid>
                  <cell r="1" c="1" value="@(data.Name)" />
                </grid>
              </component>
              <sheet name="Summary" rows="10" cols="10">
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

    [Fact]
    public void Expand_WhenFalse_SkipsNode()
    {
        var plan = Expand(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
                <cell r="1" c="1" when="@(false)" value="Hidden" />
              </sheet>
            </workbook>
            """);

        Assert.Empty(Assert.Single(plan.Sheets).Cells);
        Assert.Empty(plan.Issues);
    }

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
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
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
