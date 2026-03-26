using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using System.Xml.Linq;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>StyleImport</c> feature.
/// </summary>
public sealed class StyleImportTests
{
    /// <summary>
    /// Verifies that style import requires styles root element.
    /// </summary>
    [Fact]
    public void Parse_StyleImport_InvalidRoot_ReturnsFatalSchemaViolation()
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempPath, """<workbook xmlns="urn:excelreport:v2"><sheet name="Summary" /></workbook>""");
            var issues = new List<Issue>();
            var element = new XElement(
                XName.Get(StyleImportAst.TagName, "urn:excelreport:v2"),
                new XAttribute("href", tempPath));

            _ = new StyleImportAst(element, issues, Path.GetDirectoryName(tempPath)!);

            Assert.Contains(
                issues,
                issue => issue.Severity == IssueSeverity.Fatal
                         && issue.Kind == IssueKind.SchemaViolation
                         && issue.Message.Contains("<styles>", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
