using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for <see cref="XmlTemplateSerializer" />.
/// </summary>
public sealed class XmlTemplateSerializerTests
{
    /// <summary>
    /// Verifies that serializer writes DSL-compatible workbook, component, sheet, cell, use, and repeat structures.
    /// </summary>
    [Fact]
    public void Serialize_WritesDslCompatibleWorkbookComponentsSheetsAndItems()
    {
        var builder = new ExcelTemplateOutputContractBuilder();
        var contract = builder.Build(ExcelTemplateOutputContractFixture.CreateStandardWorkbook());
        var serializer = new XmlTemplateSerializer();

        var document = serializer.Serialize(contract);
        var root = Assert.IsType<XElement>(document.Root);

        Assert.Equal("workbook", root.Name.LocalName);
        Assert.Equal("urn:excelreport:v2", root.Name.NamespaceName);

        var header = root.Elements(root.Name.Namespace + "component").Single(element => (string?)element.Attribute("name") == "Header");
        var grid = Assert.Single(header.Elements(root.Name.Namespace + "grid"));
        Assert.Equal("2", (string?)grid.Attribute("rows"));
        Assert.Equal("2", (string?)grid.Attribute("cols"));

        var headerItems = grid.Elements().ToArray();
        Assert.Equal("cell", headerItems[0].Name.LocalName);
        Assert.Equal("1", (string?)headerItems[0].Attribute("r"));
        Assert.Equal("1", (string?)headerItems[0].Attribute("c"));
        Assert.Equal("請求書", (string?)headerItems[0].Attribute("value"));
        Assert.Equal("repeat", headerItems[1].Name.LocalName);
        Assert.Equal("2", (string?)headerItems[1].Attribute("r"));
        Assert.Equal("1", (string?)headerItems[1].Attribute("c"));
        Assert.Equal("@(root.Items)", (string?)headerItems[1].Attribute("from"));
        Assert.Equal("item", (string?)headerItems[1].Attribute("var"));
        Assert.Equal("down", (string?)headerItems[1].Attribute("direction"));
        Assert.Equal("use", headerItems[1].Elements().Single().Name.LocalName);
        Assert.Equal("ItemRow", (string?)headerItems[1].Elements().Single().Attribute("component"));
        Assert.Equal("cell", headerItems[2].Name.LocalName);
        Assert.Equal("SUM(C1:C3)", (string?)headerItems[2].Attribute("formula"));

        var invoice = Assert.Single(root.Elements(root.Name.Namespace + "sheet"));
        Assert.Equal("Invoice", (string?)invoice.Attribute("name"));
        Assert.Equal("use", invoice.Elements().First().Name.LocalName);
        Assert.Equal("Header", (string?)invoice.Elements().First().Attribute("component"));
        var itemRow = root.Elements(root.Name.Namespace + "component").Single(element => (string?)element.Attribute("name") == "ItemRow");
        var itemNameCell = Assert.Single(itemRow.Descendants(root.Name.Namespace + "cell"));
        Assert.Equal("@(item.Name)", (string?)itemNameCell.Attribute("value"));

        var parseResult = DslParser.ParseFromText(
            document.ToString(),
            new DslParserOptions { EnableSchemaValidation = true });
        Assert.NotNull(parseResult.Root);
        Assert.DoesNotContain(parseResult.Issues, issue => issue.Severity == IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that serializer keeps DSL compatibility while preserving unresolved context and issues in comments.
    /// </summary>
    [Fact]
    public void Serialize_UnresolvedComponentAndIssues_ArePreservedInComments()
    {
        var builder = new ExcelTemplateOutputContractBuilder();
        var contract = builder.Build(ExcelTemplateOutputContractFixture.CreateWorkbookWithIssues());
        var serializer = new XmlTemplateSerializer();

        var document = serializer.Serialize(contract);
        var root = Assert.IsType<XElement>(document.Root);

        Assert.Empty(root.Elements(root.Name.Namespace + "component"));
        var comments = root.Nodes().OfType<XComment>().Select(comment => comment.Value).ToArray();
        Assert.Contains(comments, comment => comment.Contains("unresolved-component", StringComparison.Ordinal));
        Assert.Contains(comments, comment => comment.Contains("EmptyComponentRange", StringComparison.Ordinal));
        Assert.Contains(comments, comment => comment.Contains("UnsupportedExcelTemplateFeature", StringComparison.Ordinal));

        var summary = Assert.Single(root.Elements(root.Name.Namespace + "sheet"));
        var malformedCell = Assert.Single(summary.Elements(root.Name.Namespace + "cell"));
        Assert.Equal("{{use:Missing", (string?)malformedCell.Attribute("value"));

        var parseResult = DslParser.ParseFromText(
            document.ToString(),
            new DslParserOptions { EnableSchemaValidation = true });
        Assert.NotNull(parseResult.Root);
        Assert.DoesNotContain(parseResult.Issues, issue => issue.Severity == IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that style-only cells are serialized as empty DSL cells without breaking schema compatibility.
    /// </summary>
    [Fact]
    public void Serialize_StyleOnlyCell_RemainsDslCompatible()
    {
        var workbook = new ExcelTemplate.Model.ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplate.Model.ExcelTemplateSheet(
                    "__component_Header",
                    [
                        new ExcelTemplate.Model.ExcelTemplateCell("A1", 1, 1, "Title", null, null),
                        new ExcelTemplate.Model.ExcelTemplateCell("B1", 1, 2, null, null, new ExcelTemplate.Model.ExcelTemplateStyle(11)),
                    ]),
                new ExcelTemplate.Model.ExcelTemplateSheet("Invoice"),
            ],
            definedNames: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__component_range_Header"] = "'__component_Header'!$A$1:$B$1",
            });

        var builder = new ExcelTemplateOutputContractBuilder();
        var contract = builder.Build(workbook);
        var serializer = new XmlTemplateSerializer();

        var document = serializer.Serialize(contract);
        var root = Assert.IsType<XElement>(document.Root);
        var header = Assert.Single(root.Elements(root.Name.Namespace + "component"));
        var cells = header.Descendants(root.Name.Namespace + "cell").ToArray();
        Assert.Equal(2, cells.Length);
        Assert.Null(cells[1].Attribute("value"));
        Assert.Null(cells[1].Attribute("formula"));

        var parseResult = DslParser.ParseFromText(
            document.ToString(),
            new DslParserOptions { EnableSchemaValidation = true });
        Assert.NotNull(parseResult.Root);
        Assert.DoesNotContain(parseResult.Issues, issue => issue.Severity == IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that explicit styleOverflow values are emitted on use and repeat-use nodes.
    /// </summary>
    [Fact]
    public void Serialize_ExplicitStyleOverflow_EmitsDslAttributes()
    {
        var contract = new ExcelTemplate.Model.ExcelTemplateOutputContract(
            components:
            [
                new ExcelTemplate.Model.ExcelTemplateOutputComponent(
                    "Header",
                    "__component_Header",
                    "A1:A2",
                    isRangeResolved: true,
                    items:
                    [
                        new ExcelTemplate.Model.ExcelTemplateOutputUse("A1", 1, 1, null, "ItemRow", "edge"),
                        new ExcelTemplate.Model.ExcelTemplateOutputRepeatUse("A2", 2, 1, null, "ItemRow", "@items", "item", "down", "edge"),
                    ]),
            ],
            sheets:
            [
                new ExcelTemplate.Model.ExcelTemplateOutputSheet("Invoice"),
            ]);
        var serializer = new XmlTemplateSerializer();

        var document = serializer.Serialize(contract);
        var root = Assert.IsType<XElement>(document.Root);
        var use = root.Descendants(root.Name.Namespace + "use").First();
        var repeatUse = root.Descendants(root.Name.Namespace + "repeat").Single().Elements(root.Name.Namespace + "use").Single();

        Assert.Equal("edge", (string?)use.Attribute("styleOverflow"));
        Assert.Equal("edge", (string?)repeatUse.Attribute("styleOverflow"));

        var parseResult = DslParser.ParseFromText(
            document.ToString(),
            new DslParserOptions { EnableSchemaValidation = true });
        Assert.NotNull(parseResult.Root);
        Assert.DoesNotContain(parseResult.Issues, issue => issue.Severity == IssueSeverity.Fatal);
    }
}
