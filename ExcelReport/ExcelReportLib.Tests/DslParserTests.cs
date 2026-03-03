using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;

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
}
