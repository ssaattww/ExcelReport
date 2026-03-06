using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;

namespace ExcelReportLib.Tests;

public sealed class DslParserTests
{
    [Fact]
    public void ParseFromText_ValidXml_ReturnsWorkbookAst()
    {
        var fixturePath = DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile);
        var xmlText = DslTestFixtures.ReadText(DslTestFixtures.FullTemplateFile);

        var result = DslParser.ParseFromText(
            xmlText,
            new DslParserOptions
            {
                RootFilePath = fixturePath,
            });

        Assert.False(result.HasFatal);
        var root = Assert.IsType<WorkbookAst>(result.Root);
        Assert.Single(root.Sheets);
    }

    [Fact]
    public void ParseFromText_InvalidXml_ReturnsFatalIssue()
    {
        const string invalidXml = "<workbook><sheet></workbook>";

        var result = DslParser.ParseFromText(invalidXml);

        Assert.True(result.HasFatal);
        Assert.Null(result.Root);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(IssueSeverity.Fatal, issue.Severity);
        Assert.Equal(IssueKind.XmlMalformed, issue.Kind);
    }

    [Fact]
    public void ParseFromText_EmptyInput_ReturnsFatalIssue()
    {
        var result = DslParser.ParseFromText(string.Empty);

        Assert.True(result.HasFatal);
        Assert.Null(result.Root);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Fatal && issue.Kind == IssueKind.XmlMalformed);
    }

    [Fact]
    public void ParseFromFile_FullTemplate_ResolvesAllImports()
    {
        var filePath = DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile);

        var result = DslParser.ParseFromFile(filePath);

        Assert.False(result.HasFatal);
        var root = Assert.IsType<WorkbookAst>(result.Root);

        var styles = Assert.IsType<StylesAst>(root.Styles);
        var styleImport = Assert.Single(styles.StyleImportAsts!);
        var importedStyles = Assert.IsAssignableFrom<IReadOnlyList<StyleAst>>(styleImport.StylesAst.Styles);
        Assert.Equal(6, importedStyles.Count);

        var componentImport = Assert.Single(root.ComponentInports!);
        Assert.Equal(5, componentImport.Components.ComponentList.Count);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.LoadFile && issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
    }

    [Fact]
    public void ParseFromFile_FullTemplate_DoesNotEmitDuplicateStyleName()
    {
        var filePath = DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile);

        var result = DslParser.ParseFromFile(filePath);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.DuplicateStyleName && issue.Severity == IssueSeverity.Error);
    }

    [Fact]
    public void DuplicateInlineStyleName_StillEmitsDuplicateStyleName()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v1">
              <styles>
                <style name="Base" scope="cell" />
                <style name="Base" scope="cell" />
              </styles>
              <sheet name="Summary" rows="1" cols="1" />
            </workbook>
            """);

        Assert.Contains(
            result.Issues,
            issue => issue.Kind == IssueKind.DuplicateStyleName && issue.Severity == IssueSeverity.Error);
    }

    [Fact]
    public void ParseFromText_OmittedSheetAndGridRowsCols_DoesNotError()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary">
                <grid>
                  <cell r="1" c="1" value="A" />
                </grid>
              </sheet>
            </workbook>
            """);

        Assert.False(result.HasFatal);
        var root = Assert.IsType<WorkbookAst>(result.Root);
        var sheet = Assert.Single(root.Sheets);
        Assert.Equal(0, sheet.Rows);
        Assert.Equal(0, sheet.Cols);

        var grid = Assert.IsType<GridAst>(Assert.Single(sheet.Children.Values));
        Assert.Equal(0, grid.Rows);
        Assert.Equal(0, grid.Cols);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.UndefinedRequiredAttribute && issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.SchemaViolation && issue.Severity == IssueSeverity.Fatal);
    }
}
