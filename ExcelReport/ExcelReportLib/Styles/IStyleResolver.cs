using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.Styles;

/// <summary>
/// Specifies style target values.
/// </summary>
public enum StyleTarget
{
    /// <summary>
    /// Represents the cell option.
    /// </summary>
    Cell,
    /// <summary>
    /// Represents the grid option.
    /// </summary>
    Grid,
}

/// <summary>
/// Specifies style source kind values.
/// </summary>
public enum StyleSourceKind
{
    /// <summary>
    /// Represents the workbook default option.
    /// </summary>
    WorkbookDefault,
    /// <summary>
    /// Represents the sheet default option.
    /// </summary>
    SheetDefault,
    /// <summary>
    /// Represents the reference option.
    /// </summary>
    Reference,
    /// <summary>
    /// Represents the inline option.
    /// </summary>
    Inline,
    /// <summary>
    /// Represents the computed option.
    /// </summary>
    Computed,
}

/// <summary>
/// Defines behavior for style resolver.
/// </summary>
public interface IStyleResolver
{
    /// <summary>
    /// Gets the global styles.
    /// </summary>
    IReadOnlyDictionary<string, StyleAst> GlobalStyles { get; }

    /// <summary>
    /// Resolves by name.
    /// </summary>
    /// <param name="name">The target name.</param>
    /// <returns>The resulting style ast.</returns>
    StyleAst? ResolveByName(string name);

    /// <summary>
    /// Resolves a named style for the specified target scope.
    /// </summary>
    /// <param name="styleName">The style name.</param>
    /// <param name="target">The target.</param>
    /// <param name="issues">The collection used to collect discovered issues.</param>
    /// <param name="span">The span.</param>
    /// <returns>The resulting resolved style.</returns>
    ResolvedStyle? Resolve(
        string styleName,
        StyleTarget target,
        IList<Issue> issues,
        SourceSpan? span = null);

    /// <summary>
    /// Builds plan.
    /// </summary>
    /// <param name="styleRefs">The style refs.</param>
    /// <param name="inlineStyles">The inline styles.</param>
    /// <param name="sheetDefault">The sheet default.</param>
    /// <param name="workbookDefault">The workbook default.</param>
    /// <param name="target">The target.</param>
    /// <param name="issues">The collection used to collect discovered issues.</param>
    /// <returns>The resulting style plan.</returns>
    StylePlan BuildPlan(
        IEnumerable<StyleRefAst>? styleRefs,
        IEnumerable<StyleAst>? inlineStyles,
        StyleAst? sheetDefault,
        StyleAst? workbookDefault,
        StyleTarget target,
        IList<Issue> issues);
}
