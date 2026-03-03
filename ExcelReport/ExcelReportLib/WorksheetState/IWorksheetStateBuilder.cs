using ExcelReportLib.LayoutEngine;

namespace ExcelReportLib.WorksheetState;

public interface IWorksheetStateBuilder
{
    IReadOnlyList<WorksheetState> Build(LayoutPlan layoutPlan);
}
