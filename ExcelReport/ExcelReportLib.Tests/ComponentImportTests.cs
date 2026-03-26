using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using System.Xml.Linq;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>ComponentImport</c> feature.
/// </summary>
public sealed class ComponentImportTests
{
    /// <summary>
    /// Verifies that parse component import loads external file.
    /// </summary>
    [Fact]
    public void Parse_ComponentImport_LoadsExternalFile()
    {
        var componentImport = CreateComponentImport();

        Assert.Equal(DslTestFixtures.GetPath(DslTestFixtures.ExternalComponentFile), componentImport.PathStr);
        Assert.Equal(5, componentImport.Components.ComponentList.Count);
    }

    /// <summary>
    /// Verifies that parse component import has styles.
    /// </summary>
    [Fact]
    public void Parse_ComponentImport_HasStyles()
    {
        var componentImport = CreateComponentImport();

        var styles = Assert.IsType<StylesAst>(componentImport.Styles);
        var styleImport = Assert.Single(styles.StyleImportAsts!);
        var importedStyles = Assert.IsAssignableFrom<IReadOnlyList<StyleAst>>(styleImport.StylesAst.Styles);

        Assert.Equal(6, importedStyles.Count);
        Assert.Contains(importedStyles, style => style.Name == "HeaderCell");
    }

    /// <summary>
    /// Verifies that component import requires components root element.
    /// </summary>
    [Fact]
    public void Parse_ComponentImport_InvalidRoot_ReturnsFatalSchemaViolation()
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempPath, """<workbook xmlns="urn:excelreport:v2"><sheet name="Summary" /></workbook>""");
            var issues = new List<Issue>();
            var element = new XElement(
                XName.Get(ComponentImportAst.TagName, "urn:excelreport:v2"),
                new XAttribute("href", tempPath));

            _ = new ComponentImportAst(element, issues, Path.GetDirectoryName(tempPath)!);

            Assert.Contains(
                issues,
                issue => issue.Severity == IssueSeverity.Fatal
                         && issue.Kind == IssueKind.SchemaViolation
                         && issue.Message.Contains("<components>", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static ComponentImportAst CreateComponentImport()
    {
        var issues = new List<Issue>();
        var componentImportElement = DslTestFixtures.GetRequiredDescendant(
            DslTestFixtures.FullTemplateFile,
            ComponentImportAst.TagName);
        var componentImport = new ComponentImportAst(
            componentImportElement,
            issues,
            Path.GetDirectoryName(DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile))!);

        Assert.DoesNotContain(issues, issue => issue.Severity == IssueSeverity.Fatal);
        return componentImport;
    }
}
