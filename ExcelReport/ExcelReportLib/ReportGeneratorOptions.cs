using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;

namespace ExcelReportLib;

public sealed class ReportGeneratorOptions
{
    public bool EnableSchemaValidation { get; init; } = true;

    public bool TreatExpressionSyntaxErrorAsFatal { get; init; } = true;

    public IReportLogger? Logger { get; init; }

    public RenderOptions? RenderOptions { get; init; }
}
