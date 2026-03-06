namespace ExcelReportLib.Logger;

/// <summary>
/// Specifies report phase values.
/// </summary>
public enum ReportPhase
{
    /// <summary>
    /// Represents the parsing option.
    /// </summary>
    Parsing,
    /// <summary>
    /// Represents the style resolving option.
    /// </summary>
    StyleResolving,
    /// <summary>
    /// Represents the layout expanding option.
    /// </summary>
    LayoutExpanding,
    /// <summary>
    /// Represents the rendering option.
    /// </summary>
    Rendering,
}
