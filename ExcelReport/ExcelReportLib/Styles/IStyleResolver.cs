using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.Styles;

public enum StyleTarget
{
    Cell,
    Grid,
}

public enum StyleSourceKind
{
    WorkbookDefault,
    SheetDefault,
    Reference,
    Inline,
    Computed,
}

public interface IStyleResolver
{
    IReadOnlyDictionary<string, StyleAst> GlobalStyles { get; }

    StyleAst? ResolveByName(string name);

    ResolvedStyle? Resolve(
        string styleName,
        StyleTarget target,
        IList<Issue> issues,
        SourceSpan? span = null);

    StylePlan BuildPlan(
        IEnumerable<StyleRefAst>? styleRefs,
        IEnumerable<StyleAst>? inlineStyles,
        StyleAst? sheetDefault,
        StyleAst? workbookDefault,
        StyleTarget target,
        IList<Issue> issues);
}
