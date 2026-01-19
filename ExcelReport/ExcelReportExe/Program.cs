using System;
using System.Collections.Generic;
using System.Text;
using ExcelReportLib;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;

namespace ExcelReportExe
{
    public static class Program
    {
        static void Main(string[] args)
        {

            var sampleData = SampleDataFactory.Create();
            var parsedResult = DslParser.ParseFromFile(@"C:\Users\taiga\source\repos\ExcelReport\ExcelReport\ExcelReportLibTest\TestDsl\DslDefinition_FullTemplate_Sample_v1.xml");
            var parsedRoot = parsedResult.Root;
            if (parsedRoot == null)
            {
                Console.WriteLine("Failed to parse DSL.");
                foreach (var issue in parsedResult.Issues)
                {
                    Console.WriteLine(issue.ToString());
                }
                return;
            }
            WalkAst(parsedRoot);
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
    }
}
