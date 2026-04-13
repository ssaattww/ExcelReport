using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for <see cref="DslEmitter" />.
/// </summary>
public sealed class DslEmitterTests
{
    /// <summary>
    /// Verifies that emitter writes DSL text with workbook, component, and sheet structure.
    /// </summary>
    [Fact]
    public void Emit_WritesDslTextForWorkbookComponentsAndSheets()
    {
        var builder = new ExcelTemplateOutputContractBuilder();
        var contract = builder.Build(ExcelTemplateOutputContractFixture.CreateStandardWorkbook());
        var emitter = new DslEmitter();

        var text = emitter.Emit(contract);

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", text, StringComparison.Ordinal);
        Assert.Contains("<workbook xmlns=\"urn:excelreport:v2\">", text, StringComparison.Ordinal);
        Assert.Contains("<component name=\"Header\">", text, StringComparison.Ordinal);
        Assert.Contains("<sheet name=\"Invoice\">", text, StringComparison.Ordinal);

        var parseResult = DslParser.ParseFromText(
            text,
            new DslParserOptions { EnableSchemaValidation = true });
        Assert.NotNull(parseResult.Root);
        Assert.DoesNotContain(parseResult.Issues, issue => issue.Severity == IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that emitter preserves formula, explicit styleOverflow, and repeat direction in DSL text.
    /// </summary>
    [Fact]
    public void Emit_PreservesFormulaStyleOverflowAndDirection()
    {
        var contract = new ExcelReportLib.ExcelTemplate.Model.ExcelTemplateOutputContract(
            components:
            [
                new ExcelReportLib.ExcelTemplate.Model.ExcelTemplateOutputComponent(
                    "Header",
                    "__component_Header",
                    "A1:B2",
                    isRangeResolved: true,
                    items:
                    [
                        new ExcelReportLib.ExcelTemplate.Model.ExcelTemplateOutputCell("A1", 1, 1, null, null, "SUM(C1:C3)"),
                        new ExcelReportLib.ExcelTemplate.Model.ExcelTemplateOutputRepeatUse("A2", 2, 1, null, "ItemRow", "@items", "item", "down", "edge"),
                    ]),
            ],
            sheets:
            [
                new ExcelReportLib.ExcelTemplate.Model.ExcelTemplateOutputSheet(
                    "Invoice",
                    [
                        new ExcelReportLib.ExcelTemplate.Model.ExcelTemplateOutputUse("A1", 1, 1, null, "Header", "edge"),
                    ]),
            ]);
        var emitter = new DslEmitter();

        var text = emitter.Emit(contract);

        Assert.Contains("formula=\"SUM(C1:C3)\"", text, StringComparison.Ordinal);
        Assert.Contains("styleOverflow=\"edge\"", text, StringComparison.Ordinal);
        Assert.Contains("direction=\"down\"", text, StringComparison.Ordinal);

        var parseResult = DslParser.ParseFromText(
            text,
            new DslParserOptions { EnableSchemaValidation = true });
        Assert.NotNull(parseResult.Root);
        Assert.DoesNotContain(parseResult.Issues, issue => issue.Severity == IssueSeverity.Fatal);
    }
}
