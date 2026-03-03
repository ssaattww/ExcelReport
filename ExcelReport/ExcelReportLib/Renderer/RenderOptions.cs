namespace ExcelReportLib.Renderer;

public sealed class RenderOptions
{
    public string? TemplateName { get; init; }

    public string? DataSource { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }
}
