using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for <see cref="ExcelTemplateOutputContractBuilder" />.
/// </summary>
public sealed class ExcelTemplateOutputContractBuilderTests
{
    /// <summary>
    /// Verifies that the builder classifies component and sheet scopes and normalizes cells, use, and repeat-use entries.
    /// </summary>
    [Fact]
    public void Build_ClassifiesScopesAndNormalizesOutputEntries()
    {
        var workbook = ExcelTemplateOutputContractFixture.CreateStandardWorkbook();
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        Assert.Empty(contract.Issues);
        Assert.Equal(2, contract.Components.Count);
        Assert.Single(contract.Sheets);

        var header = contract.Components.Single(component => component.Name == "Header");
        Assert.Equal("__component_Header", header.SourceSheetName);
        Assert.True(header.IsRangeResolved);
        Assert.Equal("A1:B2", header.RangeReference);
        Assert.Equal(3, header.Items.Count);

        var titleCell = Assert.IsType<ExcelTemplateOutputCell>(header.Items[0]);
        Assert.Equal("A1", titleCell.Reference);
        Assert.Equal("請求書", titleCell.Value);
        Assert.Null(titleCell.Formula);

        var repeatUse = Assert.IsType<ExcelTemplateOutputRepeatUse>(header.Items[1]);
        Assert.Equal("A2", repeatUse.Reference);
        Assert.Equal("ItemRow", repeatUse.ComponentName);
        Assert.Equal("@(root.Items)", repeatUse.FromExpression);
        Assert.Equal("item", repeatUse.VariableName);
        Assert.Equal("down", repeatUse.Direction);
        Assert.Null(repeatUse.StyleOverflow);

        var formulaCell = Assert.IsType<ExcelTemplateOutputCell>(header.Items[2]);
        Assert.Equal("B2", formulaCell.Reference);
        Assert.Null(formulaCell.Value);
        Assert.Equal("SUM(C1:C3)", formulaCell.Formula);
        Assert.Equal((uint)7, formulaCell.StyleIndex);

        Assert.DoesNotContain(header.Items, item => item.Reference == "C3");

        var invoice = Assert.Single(contract.Sheets);
        Assert.Equal("Invoice", invoice.Name);
        Assert.Equal(2, invoice.Items.Count);

        var headerUse = Assert.IsType<ExcelTemplateOutputUse>(invoice.Items[0]);
        Assert.Equal("A1", headerUse.Reference);
        Assert.Equal("Header", headerUse.ComponentName);
        Assert.Null(headerUse.StyleOverflow);

        var invoiceFormula = Assert.IsType<ExcelTemplateOutputCell>(invoice.Items[1]);
        Assert.Equal("B3", invoiceFormula.Reference);
        Assert.Equal("SUM(B4:B8)", invoiceFormula.Formula);

        var itemRow = contract.Components.Single(component => component.Name == "ItemRow");
        var itemNameCell = Assert.IsType<ExcelTemplateOutputCell>(Assert.Single(itemRow.Items));
        Assert.Equal("@(item.Name)", itemNameCell.Value);
    }

    /// <summary>
    /// Verifies that range resolution and validation issues are aggregated into the contract result.
    /// </summary>
    [Fact]
    public void Build_AggregatesRangeResolutionAndValidationIssues()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet("__component_Empty"),
                new ExcelTemplateSheet(
                    "Summary",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "{{use:Missing", null, null),
                    ],
                    hasConditionalFormatting: true),
            ]);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        var emptyComponent = Assert.Single(contract.Components);
        Assert.Equal("Empty", emptyComponent.Name);
        Assert.False(emptyComponent.IsRangeResolved);
        Assert.Null(emptyComponent.RangeReference);
        Assert.Empty(emptyComponent.Items);

        Assert.Single(contract.Sheets);
        var summary = Assert.Single(contract.Sheets);
        var malformedTriggerCell = Assert.IsType<ExcelTemplateOutputCell>(Assert.Single(summary.Items));
        Assert.Equal("A1", malformedTriggerCell.Reference);
        Assert.Equal("{{use:Missing", malformedTriggerCell.Value);

        Assert.Contains(contract.Issues, issue => issue.Kind == IssueKind.EmptyComponentRange);
        Assert.Contains(contract.Issues, issue => issue.Kind == IssueKind.UnsupportedExcelTemplateFeature);
        Assert.Contains(
            contract.Issues,
            issue => issue.Kind == IssueKind.InvalidAttributeValue
                && issue.Message.Contains("A1", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that style-only cells inside a resolved component range remain in the normalized output.
    /// </summary>
    [Fact]
    public void Build_ComponentRangeWithStyleOnlyCell_KeepsStyledEmptyCell()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_Header",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "Title", null, null),
                        new ExcelTemplateCell("B1", 1, 2, null, null, new ExcelTemplateStyle(11)),
                    ]),
            ],
            definedNames: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__component_range_Header"] = "'__component_Header'!$A$1:$B$1",
            });
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        var header = Assert.Single(contract.Components);
        Assert.Equal(2, header.Items.Count);

        var styledCell = Assert.IsType<ExcelTemplateOutputCell>(header.Items[1]);
        Assert.Equal("B1", styledCell.Reference);
        Assert.Null(styledCell.Value);
        Assert.Null(styledCell.Formula);
        Assert.Equal((uint)11, styledCell.StyleIndex);
    }

    /// <summary>
    /// Verifies that styleOverflow from Excel use triggers is preserved in the output contract.
    /// </summary>
    [Fact]
    public void Build_UseTriggerWithStyleOverflow_PreservesExplicitOverflow()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "Invoice",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "{{use:Header, styleOverflow:edge}}", null, null),
                        new ExcelTemplateCell("A2", 2, 1, "{{use:ItemRow, from:@items, var:item, styleOverflow:edge}}", null, null),
                    ]),
            ]);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        var invoice = Assert.Single(contract.Sheets);
        var simpleUse = Assert.IsType<ExcelTemplateOutputUse>(invoice.Items[0]);
        Assert.Equal("edge", simpleUse.StyleOverflow);

        var repeatUse = Assert.IsType<ExcelTemplateOutputRepeatUse>(invoice.Items[1]);
        Assert.Equal("edge", repeatUse.StyleOverflow);
        Assert.Equal("@(root.Items)", repeatUse.FromExpression);
    }

    /// <summary>
    /// Verifies that simple shorthand local variables are not rewritten to root scope.
    /// </summary>
    [Fact]
    public void Build_SimpleIdentifierValue_UsesLocalRepeatScopeWhenVariableExists()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_ItemRow",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "@item", null, null),
                    ]),
                new ExcelTemplateSheet(
                    "Invoice",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "{{use:ItemRow, from:@items, var:item}}", null, null),
                    ]),
            ]);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        var itemRow = Assert.Single(contract.Components);
        var itemCell = Assert.IsType<ExcelTemplateOutputCell>(Assert.Single(itemRow.Items));
        Assert.Equal("@(item)", itemCell.Value);

        var invoice = Assert.Single(contract.Sheets);
        var repeatUse = Assert.IsType<ExcelTemplateOutputRepeatUse>(Assert.Single(invoice.Items));
        Assert.Equal("@(root.Items)", repeatUse.FromExpression);
    }

    /// <summary>
    /// Verifies that repeat variable names do not leak to unrelated components.
    /// </summary>
    [Fact]
    public void Build_SimpleIdentifierValue_DoesNotLeakVariableScopeAcrossComponents()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_Target",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "@item", null, null),
                    ]),
                new ExcelTemplateSheet(
                    "__component_Other",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "X", null, null),
                    ]),
                new ExcelTemplateSheet(
                    "Invoice",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "{{use:Other, from:@items, var:item}}", null, null),
                    ]),
            ]);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        var target = contract.Components.Single(component => component.Name == "Target");
        var targetCell = Assert.IsType<ExcelTemplateOutputCell>(Assert.Single(target.Items));
        Assert.Equal("@(root.Item)", targetCell.Value);
    }

    /// <summary>
    /// Verifies that reusing one component with multiple variable names is reported as an ambiguous normalization error.
    /// </summary>
    [Fact]
    public void Build_ComponentReferencedByMultipleVariables_ReportsAmbiguousNormalization()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_ItemRow",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "@item", null, null),
                    ]),
                new ExcelTemplateSheet(
                    "Invoice",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "{{use:ItemRow, from:@items, var:item}}", null, null),
                        new ExcelTemplateCell("A2", 2, 1, "{{use:ItemRow, from:@rows, var:row}}", null, null),
                    ]),
            ]);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        var itemRow = Assert.Single(contract.Components);
        var itemCell = Assert.IsType<ExcelTemplateOutputCell>(Assert.Single(itemRow.Items));
        Assert.Equal("@(root.Item)", itemCell.Value);
        Assert.Contains(
            contract.Issues,
            issue => issue.Kind == IssueKind.InvalidAttributeValue &&
                     issue.Message.Contains("multiple repeat variables", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that workbook meta sheet-repeat mapping rewrites template sheets into sheet scopes with from/var.
    /// </summary>
    [Fact]
    public void Build_WorkbookMetaSheetRepeatDefinition_RewritesTemplateSheetOutput()
    {
        const string workbookMetaXml =
            """
            <workbook>
              <sheets>
                <sheet templateSheet="InvoiceTemplate" name="@grp.Name" from="@groups" var="grp" />
              </sheets>
            </workbook>
            """;
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__sheet_meta",
                    []),
                new ExcelTemplateSheet(
                    "InvoiceTemplate",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "@grp.Name", null, null),
                    ]),
                new ExcelTemplateSheet(
                    "Summary",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "Ready", null, null),
                    ]),
            ],
            workbookMetaXml: workbookMetaXml);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        Assert.Empty(contract.Issues);
        Assert.Equal(2, contract.Sheets.Count);

        var repeated = contract.Sheets.Single(sheet => sheet.Name == "@(grp.Name)");
        Assert.Equal("@(root.Groups)", repeated.FromExpression);
        Assert.Equal("grp", repeated.VariableName);
        var repeatedCell = Assert.IsType<ExcelTemplateOutputCell>(Assert.Single(repeated.Items));
        Assert.Equal("@(grp.Name)", repeatedCell.Value);

        var summary = contract.Sheets.Single(sheet => sheet.Name == "Summary");
        Assert.Null(summary.FromExpression);
        Assert.Null(summary.VariableName);
        Assert.Equal("Ready", Assert.IsType<ExcelTemplateOutputCell>(Assert.Single(summary.Items)).Value);
        Assert.DoesNotContain(contract.Sheets, sheet => sheet.Name == "InvoiceTemplate");
    }

    /// <summary>
    /// Verifies that meta sheet without fixed workbook meta shape payload reports a required-element error.
    /// </summary>
    [Fact]
    public void Build_MetaSheetWithoutWorkbookMetaXml_ReportsError()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet("__sheet_meta"),
                new ExcelTemplateSheet("InvoiceTemplate"),
            ]);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        Assert.Contains(
            contract.Issues,
            issue => issue.Kind == IssueKind.UndefinedRequiredElement &&
                     issue.Message.Contains("__workbook_meta", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that workbook meta sheet definitions reject var without from.
    /// </summary>
    [Fact]
    public void Build_WorkbookMetaVarWithoutFrom_ReportsInvalidAttribute()
    {
        const string workbookMetaXml =
            """
            <workbook>
              <sheets>
                <sheet templateSheet="InvoiceTemplate" name="Invoice" var="grp" />
              </sheets>
            </workbook>
            """;
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet("__sheet_meta"),
                new ExcelTemplateSheet("InvoiceTemplate"),
            ],
            workbookMetaXml: workbookMetaXml);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        Assert.Contains(
            contract.Issues,
            issue => issue.Kind == IssueKind.InvalidAttributeValue &&
                     issue.Message.Contains("var", StringComparison.Ordinal) &&
                     issue.Message.Contains("from", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that sheet repeat with from-only metadata uses the implicit var=item scope for shorthand normalization.
    /// </summary>
    [Fact]
    public void Build_WorkbookMetaFromWithoutVar_UsesImplicitItemScope()
    {
        const string workbookMetaXml =
            """
            <workbook>
              <sheets>
                <sheet templateSheet="InvoiceTemplate" name="Invoice" from="@(root.Groups)" />
              </sheets>
            </workbook>
            """;
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet("__sheet_meta"),
                new ExcelTemplateSheet(
                    "InvoiceTemplate",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "@item", null, null),
                    ]),
            ],
            workbookMetaXml: workbookMetaXml);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        Assert.Empty(contract.Issues);
        var sheet = Assert.Single(contract.Sheets);
        Assert.Equal("Invoice", sheet.Name);
        Assert.Equal("@(root.Groups)", sheet.FromExpression);
        Assert.Null(sheet.VariableName);
        Assert.Equal("@(item)", Assert.IsType<ExcelTemplateOutputCell>(Assert.Single(sheet.Items)).Value);
    }

}
