using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.LayoutEngine;

public interface ILayoutEngine
{
    LayoutPlan Expand(WorkbookAst workbook, object? rootData);
}
