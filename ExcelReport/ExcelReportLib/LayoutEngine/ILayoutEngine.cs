using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.LayoutEngine;

/// <summary>
/// Defines behavior for layout engine.
/// </summary>
public interface ILayoutEngine
{
    /// <summary>
    /// Expands workbook layout nodes into concrete cell positions.
    /// </summary>
    /// <param name="workbook">The workbook.</param>
    /// <param name="rootData">The root data.</param>
    /// <returns>The resulting layout plan.</returns>
    LayoutPlan Expand(WorkbookAst workbook, object? rootData);
}
