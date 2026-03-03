using ExcelReportLib.DSL;
using WorksheetStateModel = ExcelReportLib.WorksheetState.WorksheetState;

namespace ExcelReportLib.Renderer;

public interface IRenderer
{
    RenderResult Render(
        IReadOnlyList<WorksheetStateModel> worksheets,
        RenderOptions? options = null,
        IReadOnlyList<Issue>? issues = null,
        CancellationToken cancellationToken = default);
}
