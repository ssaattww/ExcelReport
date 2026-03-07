using DocumentFormat.OpenXml.Wordprocessing;
using ExcelReportLib;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;
using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExcelReportExe
{
    public static class Program
    {
        static void Main(string[] args)
        {

            //var sampleData = SampleDataFactory.Create();
            //var parsedResult = DslParser.ParseFromFile(@"C:\Users\taiga\source\repos\ExcelReport\ExcelReport\ExcelReportLibTest\TestDsl\DslDefinition_FullTemplate_Sample_v1.xml");
            //var parsedRoot = parsedResult.Root;
            //if (parsedRoot == null)
            //{
            //    Console.WriteLine("Failed to parse DSL.");
            //    foreach (var issue in parsedResult.Issues)
            //    {
            //        Console.WriteLine(issue.ToString());
            //    }
            //    return;
            //}
            //WalkAst(parsedRoot);
            var data = new
            {
                JobName = "Test Report",
                Summary = new
                {
                    Owner = "TestUser",
                    SuccessRate = 0.95,
                },
                Lines = new[]
    {
                new { Name = "Item1", Value = 100, Code = "A01" },
                new { Name = "Item2", Value = 200, Code = "A02" },
            },
            };

            var generator = new ReportGenerator();
            var result = generator.Generate(Dsl, data, CreateOptions());
            string exeDir = AppContext.BaseDirectory;
            string outputPath = Path.Combine(exeDir, "sample.xlsx");
            File.WriteAllBytes(outputPath, result.Output.ToArray());
        }

        static void WalkAst(WorkbookAst root)
        {
            foreach (var sheet in root.Sheets)
            {
                Console.WriteLine($"Sheet: {sheet.Name}");
                var childSorted = sheet.Children
                    .ToList()
                    .OrderBy(child => child.Key.Row)
                    .ThenBy(child => child.Key.Col);

                foreach (var child in childSorted)
                {
                    WalkLayoutNode(child.Value, 1);
                }
            }
        }

        static void WalkLayoutNode(LayoutNodeAst node, int indent = 0)
        {
            var indentStr = new string(' ', indent * 2);
            switch (node)
            {
                case CellAst cell:
                    Console.WriteLine($"{indentStr}Cell: {cell.Placement.Row},{cell.Placement.Col} - {cell.ValueRaw}");
                    break;
                case GridAst grid:
                    Console.WriteLine($"{indentStr}Grid: {grid.Placement.Row},{grid.Placement.Col}");
                    var gridChildSorted = grid.Children
                        .ToList()
                        .OrderBy(child => child.Key.Row)
                        .ThenBy(child => child.Key.Col);
                    
                    foreach (var child in gridChildSorted)
                    {
                        WalkLayoutNode(child.Value, indent + 1);
                    }
                    break;
                case RepeatAst repeat:
                    Console.WriteLine($"{indentStr}Repeat: {repeat.Placement.Row},{repeat.Placement.Col} {repeat.Name} From: {repeat.FromExprRaw} Var: {repeat.VarName} Direction: {repeat.Direction}");
                    if (repeat.Body != null)
                    {
                        WalkLayoutNode(repeat.Body, indent + 1);
                    }
                    break;
                case UseAst use:
                    Console.WriteLine($"{indentStr}Use: {use.Placement.Row},{use.Placement.Col} {use.ComponentName}");
                    WalkLayoutNode(use.ComponentRef, indent + 1);
                    break;
                default:
                    Console.WriteLine($"{indentStr}Unknown Layout Node Type.");
                    break;
            }
        }

        private static ReportGeneratorOptions CreateOptions(IReportLogger? logger = null) =>
            new()
            {
                EnableSchemaValidation = false,
                Logger = logger,
                RenderOptions = new RenderOptions
                {
                    TemplateName = "Exe",
                    DataSource = "Tests",
                    GeneratedAt = new DateTimeOffset(2026, 3, 3, 0, 0, 0, TimeSpan.Zero),
                },
            };

        const string Dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <styles>
                <style name="TitleCell" scope="cell">
                  <font name="Meiryo" size="16" bold="true"/>
                </style>
                <style name="BaseCell" scope="cell">
                  <font name="Meiryo" size="11"/>
                  <fill color="#FFFFFF"/>
                </style>
                <style name="HeaderCell" scope="cell">
                  <font bold="true"/>
                  <fill color="#F2F2F2"/>
                  <border mode="cell" bottom="thin" color="#000000"/>
                </style>
                <style name="Percent" scope="cell">
                  <numberFormat code="0.0%"/>
                </style>
                <style name="DetailHeaderGrid" scope="grid">
                  <border mode="outer" top="thin" bottom="thin" left="thin" right="thin" color="#000000"/>
                </style>
                <style name="DetailRowsGrid" scope="grid">
                  <border mode="all" top="thin" bottom="thin" left="thin" right="thin" color="#CCCCCC"/>
                </style>
              </styles>

              <component name="Title">
                <grid>
                  <cell r="1" c="1" colSpan="3" value="@(data.JobName)" styleRef="TitleCell"/>
                </grid>
              </component>

              <component name="KPI">
                <grid>
                  <cell r="1" c="1" value="Owner" styleRef="HeaderCell"/>
                  <cell r="1" c="2" value="@(data.Owner)" styleRef="BaseCell"/>
                  <cell r="2" c="1" value="Success Rate" styleRef="HeaderCell"/>
                  <cell r="2" c="2" value="@(data.SuccessRate)" styleRef="BaseCell">
                    <style>
                      <numberFormat code="0.0%"/>
                    </style>
                  </cell>
                </grid>
              </component>

              <component name="DetailHeader">
                <grid>
                  <cell r="1" c="1" value="Name" styleRef="HeaderCell"/>
                  <cell r="1" c="2" value="Value" styleRef="HeaderCell"/>
                  <cell r="1" c="3" value="Code" styleRef="HeaderCell"/>
                  <styleRef name="DetailHeaderGrid"/>
                </grid>
              </component>

              <component name="DetailRow">
                <grid>
                  <cell r="1" c="1" value="@(data.Name)">
                    <styleRef name="BaseCell"/>
                  </cell>
                  <cell r="1" c="2" value="@(data.Value)" formulaRef="Detail.Value">
                    <styleRef name="BaseCell"/>
                  </cell>
                  <cell r="1" c="3" value="@(data.Code)" formulaRef="Detail.Code">
                    <styleRef name="BaseCell"/>
                  </cell>
                </grid>
              </component>

              <component name="TotalsRow">
                <grid>
                  <cell r="1" c="1" value="Totals" styleRef="HeaderCell"/>
                  <cell r="1" c="2" value="=SUM(#{Detail.Value:Detail.ValueEnd})">
                    <styleRef name="BaseCell"/>
                  </cell>
                  <cell r="1" c="3" value="=AVERAGE(#{Detail.Value:Detail.ValueEnd})">
                    <styleRef name="BaseCell"/>
                  </cell>
                  <cell r="1" c="4" value="=COUNT(#{Detail.Value:Detail.ValueEnd})">
                    <styleRef name="BaseCell"/>
                  </cell>
                  <styleRef name="DetailHeaderGrid"/>
                </grid>
              </component>

              <sheet name="Summary">
                <use component="Title" instance="HeaderTitle" r="1" c="1" with="@(root)"/>
                <use component="KPI" instance="KPI" r="2" c="1" with="@(root.Summary)"/>
                <cell r="4" c="1" value="=TODAY()">
                  <styleRef name="BaseCell"/>
                </cell>
                <cell r="4" c="2" value="=TEXT(NOW(), &quot;yyyy-mm-dd hh:mm&quot;)">
                  <styleRef name="BaseCell"/>
                  <style>
                    <border mode="cell" bottom="thin" color="#000000"/>
                  </style>
                </cell>
                <use component="TotalsRow" instance="TotalsRow" r="5" c="1" with="@(root)"/>
                <use component="DetailHeader" instance="DetailHeader" r="6" c="1" with="@(root)"/>
                <repeat name="DetailRows" r="7" c="1" direction="down" from="@(root.Lines)" var="it">
                  <styleRef name="DetailRowsGrid"/>
                  <use component="DetailRow" with="@(it)"/>
                </repeat>
              </sheet>
            </workbook>
            """;
    }
}
