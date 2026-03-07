using ExcelReportLib.DSL;
using WorksheetStateModel = ExcelReportLib.WorksheetState.WorksheetState;

namespace ExcelReportLib.Renderer;

/// <summary>
/// Defines behavior for renderer.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Renders worksheet state into an XLSX result.
    /// </summary>
    /// <param name="worksheets">The worksheets.</param>
    /// <param name="options">Options that control the operation.</param>
    /// <param name="issues">The collection used to collect discovered issues.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The resulting render result.</returns>
    RenderResult Render(
        IReadOnlyList<WorksheetStateModel> worksheets,
        RenderOptions? options = null,
        IReadOnlyList<Issue>? issues = null,
        CancellationToken cancellationToken = default);
}
