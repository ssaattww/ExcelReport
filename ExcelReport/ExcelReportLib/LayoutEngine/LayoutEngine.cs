using System.Collections;
using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;
using ExcelReportLib.ExpressionEngine;
using ExcelReportLib.Styles;

namespace ExcelReportLib.LayoutEngine;

public sealed class LayoutEngine : ILayoutEngine
{
    private readonly IExpressionEngine _expressionEngine;

    public LayoutEngine()
        : this(new ExpressionEngine.ExpressionEngine())
    {
    }

    public LayoutEngine(IExpressionEngine expressionEngine)
    {
        _expressionEngine = expressionEngine ?? throw new ArgumentNullException(nameof(expressionEngine));
    }

    public LayoutPlan Expand(WorkbookAst workbook, object? rootData)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        var issues = new List<Issue>();
        var styleResolver = new StyleResolver(workbook.Styles);
        var componentIndex = BuildComponentIndex(workbook.Components);
        var sheets = new List<LayoutSheet>(workbook.Sheets.Count);
        var rootVars = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var sheet in workbook.Sheets)
        {
            var cells = new List<LayoutCell>();
            var inheritedStyles = StyleScope.From(sheet.StyleRefs, null);

            foreach (var child in sheet.Children.Values)
            {
                var result = ExpandNode(
                    child,
                    baseRow: 1,
                    baseCol: 1,
                    rootData,
                    rootData,
                    rootVars,
                    inheritedStyles,
                    componentIndex,
                    styleResolver,
                    issues);

                if (result.Cells.Count == 0)
                {
                    continue;
                }

                cells.AddRange(result.Cells);
            }

            ValidateCoordinates(sheet, cells, issues);
            sheets.Add(new LayoutSheet(sheet.Name, cells, sheet.Rows, sheet.Cols));
        }

        return new LayoutPlan(sheets, issues);
    }

    private ExpandResult ExpandNode(
        LayoutNodeAst node,
        int baseRow,
        int baseCol,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        StyleScope inheritedStyles,
        IReadOnlyDictionary<string, ComponentAst> componentIndex,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        if (!ShouldRender(node.Placement.WhenExprRaw, rootData, dataContext, vars, issues))
        {
            return ExpandResult.Empty;
        }

        var rowOffset = ResolveOffset(node.Placement.Row);
        var colOffset = ResolveOffset(node.Placement.Col);
        var startRow = baseRow + rowOffset;
        var startCol = baseCol + colOffset;

        return node switch
        {
            CellAst cell => ExpandCell(
                cell,
                startRow,
                startCol,
                rootData,
                dataContext,
                vars,
                inheritedStyles,
                styleResolver,
                issues),
            GridAst grid => ExpandGrid(
                grid,
                startRow,
                startCol,
                rootData,
                dataContext,
                vars,
                inheritedStyles,
                componentIndex,
                styleResolver,
                issues),
            RepeatAst repeat => ExpandRepeat(
                repeat,
                startRow,
                startCol,
                rootData,
                dataContext,
                vars,
                inheritedStyles,
                componentIndex,
                styleResolver,
                issues),
            UseAst use => ExpandUse(
                use,
                startRow,
                startCol,
                rootData,
                dataContext,
                vars,
                inheritedStyles,
                componentIndex,
                styleResolver,
                issues),
            _ => ExpandResult.Empty,
        };
    }

    private ExpandResult ExpandCell(
        CellAst cell,
        int row,
        int col,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        StyleScope inheritedStyles,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        var styleScope = inheritedStyles.Append(cell.StyleRefs, cell.Style);
        if (!string.IsNullOrWhiteSpace(cell.StyleRefShortcut))
        {
            styleScope = styleScope.Append([CreateStyleRef(cell.StyleRefShortcut, issues)], null);
        }

        var rendered = EvaluateCellValue(cell.ValueRaw, rootData, dataContext, vars, issues);
        var stylePlan = styleResolver.BuildPlan(
            styleScope.StyleRefs,
            styleScope.InlineStyles,
            sheetDefault: null,
            workbookDefault: null,
            StyleTarget.Cell,
            issues);

        var layoutCell = new LayoutCell(
            row,
            col,
            cell.Placement.RowSpan,
            cell.Placement.ColSpan,
            rendered.Value,
            rendered.Formula,
            cell.FormulaRef,
            stylePlan);

        return new ExpandResult(
            [layoutCell],
            cell.Placement.RowSpan,
            cell.Placement.ColSpan);
    }

    private ExpandResult ExpandGrid(
        GridAst grid,
        int baseRow,
        int baseCol,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        StyleScope inheritedStyles,
        IReadOnlyDictionary<string, ComponentAst> componentIndex,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        var styleScope = inheritedStyles.Append(grid.StyleRefs, grid.Style);
        var cells = new List<LayoutCell>();
        var maxHeight = 0;
        var maxWidth = 0;

        foreach (var child in grid.Children.Values)
        {
            var result = ExpandNode(
                child,
                baseRow,
                baseCol,
                rootData,
                dataContext,
                vars,
                styleScope,
                componentIndex,
                styleResolver,
                issues);

            if (result.Cells.Count == 0)
            {
                continue;
            }

            cells.AddRange(result.Cells);
            maxHeight = Math.Max(maxHeight, result.Height);
            maxWidth = Math.Max(maxWidth, result.Width);
        }

        return new ExpandResult(
            cells,
            Math.Max(maxHeight, grid.Placement.RowSpan),
            Math.Max(maxWidth, grid.Placement.ColSpan));
    }

    private ExpandResult ExpandRepeat(
        RepeatAst repeat,
        int baseRow,
        int baseCol,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        StyleScope inheritedStyles,
        IReadOnlyDictionary<string, ComponentAst> componentIndex,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        if (repeat.Body is null || repeat.Direction == RepeatDirection.Err)
        {
            return ExpandResult.Empty;
        }

        var sequence = EvaluateSequence(repeat.FromExprRaw, rootData, dataContext, vars, issues);
        if (sequence is null)
        {
            return ExpandResult.Empty;
        }

        var styleScope = inheritedStyles.Append(repeat.StyleRefs, repeat.Style);
        var cells = new List<LayoutCell>();
        var nextRow = baseRow;
        var nextCol = baseCol;
        var totalHeight = 0;
        var totalWidth = 0;
        var maxHeight = 0;
        var maxWidth = 0;

        foreach (var item in sequence)
        {
            var itemVars = CloneVars(vars);
            itemVars[repeat.VarName] = item;

            var result = ExpandNode(
                repeat.Body,
                nextRow,
                nextCol,
                rootData,
                dataContext,
                itemVars,
                styleScope,
                componentIndex,
                styleResolver,
                issues);

            if (result.Cells.Count > 0)
            {
                cells.AddRange(result.Cells);
            }

            if (repeat.Direction == RepeatDirection.Down)
            {
                nextRow += result.Height;
                totalHeight += result.Height;
                maxWidth = Math.Max(maxWidth, result.Width);
            }
            else
            {
                nextCol += result.Width;
                totalWidth += result.Width;
                maxHeight = Math.Max(maxHeight, result.Height);
            }
        }

        if (repeat.Direction == RepeatDirection.Down)
        {
            totalHeight = Math.Max(totalHeight, repeat.Placement.RowSpan);
            maxWidth = Math.Max(maxWidth, repeat.Placement.ColSpan);
            return new ExpandResult(cells, totalHeight, maxWidth);
        }

        totalWidth = Math.Max(totalWidth, repeat.Placement.ColSpan);
        maxHeight = Math.Max(maxHeight, repeat.Placement.RowSpan);
        return new ExpandResult(cells, maxHeight, totalWidth);
    }

    private ExpandResult ExpandUse(
        UseAst use,
        int baseRow,
        int baseCol,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        StyleScope inheritedStyles,
        IReadOnlyDictionary<string, ComponentAst> componentIndex,
        IStyleResolver styleResolver,
        IList<Issue> issues)
    {
        if (!componentIndex.TryGetValue(use.ComponentName, out var component))
        {
            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.UndefinedComponent,
                Message = $"未定義の component 参照: {use.ComponentName}",
                Span = use.Span,
            });
            return ExpandResult.Empty;
        }

        var boundData = dataContext;
        if (!string.IsNullOrWhiteSpace(use.WithExprRaw))
        {
            boundData = EvaluateExpressionValue(use.WithExprRaw, rootData, dataContext, vars, issues);
        }

        if (!ShouldRender(component.Placement.WhenExprRaw, rootData, boundData, vars, issues))
        {
            return ExpandResult.Empty;
        }

        var componentBaseRow = baseRow + ResolveOffset(component.Placement.Row);
        var componentBaseCol = baseCol + ResolveOffset(component.Placement.Col);
        var styleScope = inheritedStyles
            .Append(component.StyleRefs, component.Style)
            .Append(use.StyleRefs, use.Style);

        var result = ExpandNode(
            component.Body,
            componentBaseRow,
            componentBaseCol,
            rootData,
            boundData,
            vars,
            styleScope,
            componentIndex,
            styleResolver,
            issues);

        var heightOffset = ResolveOffset(component.Placement.Row);
        var widthOffset = ResolveOffset(component.Placement.Col);

        return new ExpandResult(
            result.Cells,
            Math.Max(result.Height + heightOffset, use.Placement.RowSpan),
            Math.Max(result.Width + widthOffset, use.Placement.ColSpan));
    }

    private object? EvaluateExpressionValue(
        string? expression,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        if (TryEvaluateLiteralExpression(expression, out var literalValue))
        {
            return literalValue;
        }

        if (TryEvaluateVariableShortcut(expression, vars, out var shortcutResult))
        {
            return shortcutResult;
        }

        if (TryRewriteVariableScopedExpression(expression, vars, out var rewrittenExpression, out var scopedData))
        {
            var scopedResult = _expressionEngine.Evaluate(
                rewrittenExpression,
                new ExpressionContext(rootData, scopedData, CloneVars(vars)));

            AppendIssues(issues, scopedResult.Issues);
            return scopedResult.Value;
        }

        var result = _expressionEngine.Evaluate(
            expression,
            new ExpressionContext(rootData, dataContext, CloneVars(vars)));

        AppendIssues(issues, result.Issues);
        return result.Value;
    }

    private RenderedValue EvaluateCellValue(
        string? valueRaw,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        if (string.IsNullOrWhiteSpace(valueRaw))
        {
            return RenderedValue.Empty;
        }

        if (LooksLikeExpression(valueRaw))
        {
            return new RenderedValue(
                EvaluateExpressionValue(valueRaw, rootData, dataContext, vars, issues),
                Formula: null);
        }

        if (valueRaw.StartsWith("=", StringComparison.Ordinal))
        {
            return new RenderedValue(valueRaw, valueRaw);
        }

        return new RenderedValue(valueRaw, Formula: null);
    }

    private bool ShouldRender(
        string? whenExprRaw,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        if (string.IsNullOrWhiteSpace(whenExprRaw))
        {
            return true;
        }

        var value = EvaluateExpressionValue(whenExprRaw, rootData, dataContext, vars, issues);
        return ToBoolean(value);
    }

    private IEnumerable<object?>? EvaluateSequence(
        string expression,
        object? rootData,
        object? dataContext,
        IReadOnlyDictionary<string, object?> vars,
        IList<Issue> issues)
    {
        var value = EvaluateExpressionValue(expression, rootData, dataContext, vars, issues);
        if (value is null)
        {
            return Array.Empty<object?>();
        }

        if (value is string)
        {
            issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.InvalidAttributeValue,
                Message = $"repeat from はコレクションである必要があります: {expression}",
            });
            return null;
        }

        if (value is IEnumerable sequence)
        {
            return sequence.Cast<object?>();
        }

        issues.Add(new Issue
        {
            Severity = IssueSeverity.Error,
            Kind = IssueKind.InvalidAttributeValue,
            Message = $"repeat from はコレクションである必要があります: {expression}",
        });
        return null;
    }

    private static IReadOnlyDictionary<string, ComponentAst> BuildComponentIndex(
        IReadOnlyList<ComponentAst>? components)
    {
        var index = new Dictionary<string, ComponentAst>(StringComparer.Ordinal);
        if (components is null)
        {
            return index;
        }

        foreach (var component in components)
        {
            if (!string.IsNullOrWhiteSpace(component.Name))
            {
                index[component.Name] = component;
            }
        }

        return index;
    }

    private static void ValidateCoordinates(SheetAst sheet, IEnumerable<LayoutCell> cells, IList<Issue> issues)
    {
        foreach (var cell in cells)
        {
            var endRow = cell.Row + cell.RowSpan - 1;
            var endCol = cell.Col + cell.ColSpan - 1;
            if (cell.Row < 1 || cell.Col < 1 || endRow > sheet.Rows || endCol > sheet.Cols)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.CoordinateOutOfRange,
                    Message = $"セル配置がシート範囲外です: sheet={sheet.Name}, r={cell.Row}, c={cell.Col}, rowSpan={cell.RowSpan}, colSpan={cell.ColSpan}",
                    Span = null,
                });
            }
        }
    }

    private static StyleRefAst CreateStyleRef(string name, IList<Issue> issues)
    {
        var styleRefIssues = new List<Issue>();
        var styleRef = new StyleRefAst(
            new XElement("styleRef", new XAttribute("name", name)),
            styleRefIssues);
        AppendIssues(issues, styleRefIssues);
        return styleRef;
    }

    private static Dictionary<string, object?> CloneVars(IReadOnlyDictionary<string, object?> vars) =>
        vars.Count == 0
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(vars, StringComparer.Ordinal);

    private static bool LooksLikeExpression(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("@(", StringComparison.Ordinal)
            && trimmed.EndsWith(')');
    }

    private static int ResolveOffset(int? coordinate) => coordinate.GetValueOrDefault(1) - 1;

    private static bool TryEvaluateVariableShortcut(
        string expression,
        IReadOnlyDictionary<string, object?> vars,
        out object? value)
    {
        var body = NormalizeExpressionBody(expression);
        if (body.Length > 0 && body.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            return vars.TryGetValue(body, out value);
        }

        value = null;
        return false;
    }

    private static bool TryEvaluateLiteralExpression(string expression, out object? value)
    {
        var body = NormalizeExpressionBody(expression);
        if (string.Equals(body, "true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (string.Equals(body, "false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        if (string.Equals(body, "null", StringComparison.OrdinalIgnoreCase))
        {
            value = null;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryRewriteVariableScopedExpression(
        string expression,
        IReadOnlyDictionary<string, object?> vars,
        out string rewrittenExpression,
        out object? scopedData)
    {
        var body = NormalizeExpressionBody(expression);
        foreach (var pair in vars)
        {
            var variableName = pair.Key;
            if (body.StartsWith(variableName + ".", StringComparison.Ordinal))
            {
                rewrittenExpression = "@(data." + body[(variableName.Length + 1)..] + ")";
                scopedData = pair.Value;
                return true;
            }

            if (body.StartsWith(variableName + "[", StringComparison.Ordinal))
            {
                rewrittenExpression = "@(data" + body[variableName.Length..] + ")";
                scopedData = pair.Value;
                return true;
            }
        }

        rewrittenExpression = string.Empty;
        scopedData = null;
        return false;
    }

    private static string NormalizeExpressionBody(string expression)
    {
        var trimmed = expression.Trim();
        if (trimmed.StartsWith("@(", StringComparison.Ordinal) && trimmed.EndsWith(')'))
        {
            return trimmed[2..^1].Trim();
        }

        return trimmed;
    }

    private static bool ToBoolean(object? value) =>
        value switch
        {
            null => false,
            bool boolean => boolean,
            string text when bool.TryParse(text, out var parsed) => parsed,
            sbyte number => number != 0,
            byte number => number != 0,
            short number => number != 0,
            ushort number => number != 0,
            int number => number != 0,
            uint number => number != 0,
            long number => number != 0,
            ulong number => number != 0,
            float number => Math.Abs(number) > float.Epsilon,
            double number => Math.Abs(number) > double.Epsilon,
            decimal number => number != 0,
            _ => true,
        };

    private static void AppendIssues(IList<Issue> target, IEnumerable<Issue>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var issue in source)
        {
            target.Add(issue);
        }
    }

    private sealed record ExpandResult(IReadOnlyList<LayoutCell> Cells, int Height, int Width)
    {
        public static readonly ExpandResult Empty = new(Array.Empty<LayoutCell>(), 0, 0);
    }

    private sealed record RenderedValue(object? Value, string? Formula)
    {
        public static readonly RenderedValue Empty = new(null, null);
    }

    private sealed record StyleScope(
        IReadOnlyList<StyleRefAst> StyleRefs,
        IReadOnlyList<StyleAst> InlineStyles)
    {
        public static readonly StyleScope Empty = new(
            Array.Empty<StyleRefAst>(),
            Array.Empty<StyleAst>());

        public static StyleScope From(
            IEnumerable<StyleRefAst>? styleRefs,
            IEnumerable<StyleAst>? inlineStyles) =>
            Empty.Append(styleRefs, inlineStyles);

        public StyleScope Append(
            IEnumerable<StyleRefAst>? styleRefs,
            IEnumerable<StyleAst>? inlineStyles)
        {
            var mergedRefs = Merge(StyleRefs, styleRefs);
            var mergedInlineStyles = Merge(InlineStyles, inlineStyles);
            return new StyleScope(mergedRefs, mergedInlineStyles);
        }

        private static IReadOnlyList<T> Merge<T>(IReadOnlyList<T> baseItems, IEnumerable<T>? additionalItems)
        {
            if (additionalItems is null)
            {
                return baseItems;
            }

            var merged = new List<T>(baseItems.Count);
            merged.AddRange(baseItems);
            merged.AddRange(additionalItems);
            return merged;
        }
    }
}
