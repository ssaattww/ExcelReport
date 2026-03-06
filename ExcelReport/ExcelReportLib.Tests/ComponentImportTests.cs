using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;

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
