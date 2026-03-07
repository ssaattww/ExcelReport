using ExcelReportLib.LayoutEngine;

namespace ExcelReportLib.WorksheetState;

/// <summary>
/// Defines behavior for worksheet state builder.
/// </summary>
public interface IWorksheetStateBuilder
{
    /// <summary>
    /// Builds worksheet state models from an expanded layout plan.
    /// </summary>
    /// <param name="layoutPlan">The layout plan.</param>
    /// <returns>A collection containing the result.</returns>
    IReadOnlyList<WorksheetState> Build(LayoutPlan layoutPlan);
}
