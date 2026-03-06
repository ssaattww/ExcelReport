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
        var componentIndex = BuildComponentIndex(workbook);
        var sheets = new List<LayoutSheet>(workbook.Sheets.Count);
        var rootVars = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var sheet in workbook.Sheets)
        {
            var cells = new List<LayoutCell>();
            var namedAreas = new List<LayoutNamedArea>();
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

                if (result.Cells.Count > 0)
                {
                    cells.AddRange(result.Cells);
                }

                if (result.NamedAreas.Count > 0)
                {
                    namedAreas.AddRange(result.NamedAreas);
                }
            }

            var maxUsedRow = GetMaxUsedRow(cells);
            var maxUsedCol = GetMaxUsedCol(cells);
            var resolvedSheetRows = ResolveContainerSize(sheet.Rows, maxUsedRow, minimumSize: 1);
            var resolvedSheetCols = ResolveContainerSize(sheet.Cols, maxUsedCol, minimumSize: 1);

            ValidateCoordinates(sheet.Name, resolvedSheetRows, resolvedSheetCols, cells, issues);
            sheets.Add(new LayoutSheet(sheet.Name, cells, resolvedSheetRows, resolvedSheetCols, namedAreas, sheet.Options));
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
            cell.Placement.ColSpan,
            Array.Empty<LayoutNamedArea>());
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
        var namedAreas = new List<LayoutNamedArea>();
        var maxHeight = 0;
        var maxWidth = 0;

        foreach (var child in grid.Children.Values)
        {
            var childResult = ExpandNode(
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

            if (childResult.NamedAreas.Count > 0)
            {
                namedAreas.AddRange(childResult.NamedAreas);
            }

            if (childResult.Cells.Count == 0)
            {
                continue;
            }

            cells.AddRange(childResult.Cells);
            var childHeightWithOffset = ResolveOffset(child.Placement.Row) + childResult.Height;
            var childWidthWithOffset = ResolveOffset(child.Placement.Col) + childResult.Width;
            maxHeight = Math.Max(maxHeight, childHeightWithOffset);
            maxWidth = Math.Max(maxWidth, childWidthWithOffset);
        }

        var result = new ExpandResult(
            cells,
            ResolveContainerSize(grid.Rows, maxHeight, grid.Placement.RowSpan),
            ResolveContainerSize(grid.Cols, maxWidth, grid.Placement.ColSpan),
            namedAreas);

        return ApplyGridBorders(result, styleScope, styleResolver);
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
        var namedAreas = new List<LayoutNamedArea>();
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

            if (result.NamedAreas.Count > 0)
            {
                namedAreas.AddRange(result.NamedAreas);
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
            TryAddNamedArea(namedAreas, repeat.Name, cells);
            return new ExpandResult(cells, totalHeight, maxWidth, namedAreas);
        }

        totalWidth = Math.Max(totalWidth, repeat.Placement.ColSpan);
        maxHeight = Math.Max(maxHeight, repeat.Placement.RowSpan);
        TryAddNamedArea(namedAreas, repeat.Name, cells);
        return new ExpandResult(cells, maxHeight, totalWidth, namedAreas);
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
        var namedAreas = new List<LayoutNamedArea>(result.NamedAreas);
        TryAddNamedArea(namedAreas, use.InstanceName, result.Cells);

        return new ExpandResult(
            result.Cells,
            Math.Max(result.Height + heightOffset, use.Placement.RowSpan),
            Math.Max(result.Width + widthOffset, use.Placement.ColSpan),
            namedAreas);
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

    private static IReadOnlyDictionary<string, ComponentAst> BuildComponentIndex(WorkbookAst workbook)
    {
        var index = new Dictionary<string, ComponentAst>(StringComparer.Ordinal);
        IndexComponents(workbook.Components, index);

        if (workbook.ComponentInports != null)
        {
            foreach (var componentImport in workbook.ComponentInports)
            {
                IndexComponents(componentImport.Components?.ComponentList, index);
            }
        }

        return index;
    }

    private static void IndexComponents(
        IReadOnlyList<ComponentAst>? components,
        IDictionary<string, ComponentAst> index)
    {
        if (components is null)
        {
            return;
        }

        foreach (var component in components)
        {
            if (!string.IsNullOrWhiteSpace(component.Name))
            {
                index.TryAdd(component.Name, component);
            }
        }
    }

    private static void ValidateCoordinates(
        string sheetName,
        int sheetRows,
        int sheetCols,
        IEnumerable<LayoutCell> cells,
        IList<Issue> issues)
    {
        foreach (var cell in cells)
        {
            var endRow = cell.Row + cell.RowSpan - 1;
            var endCol = cell.Col + cell.ColSpan - 1;
            if (cell.Row < 1 || cell.Col < 1 || endRow > sheetRows || endCol > sheetCols)
            {
                issues.Add(new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.CoordinateOutOfRange,
                    Message = $"セル配置がシート範囲外です: sheet={sheetName}, r={cell.Row}, c={cell.Col}, rowSpan={cell.RowSpan}, colSpan={cell.ColSpan}",
                    Span = null,
                });
            }
        }
    }

    private static int GetMaxUsedRow(IReadOnlyCollection<LayoutCell> cells) =>
        cells.Count == 0
            ? 0
            : cells.Max(cell => cell.Row + cell.RowSpan - 1);

    private static int GetMaxUsedCol(IReadOnlyCollection<LayoutCell> cells) =>
        cells.Count == 0
            ? 0
            : cells.Max(cell => cell.Col + cell.ColSpan - 1);

    private static int ResolveContainerSize(int declaredSize, int contentSize, int minimumSize) =>
        Math.Max(declaredSize > 0 ? declaredSize : contentSize, minimumSize);

    private static void TryAddNamedArea(
        ICollection<LayoutNamedArea> areas,
        string? name,
        IReadOnlyList<LayoutCell> cells)
    {
        if (string.IsNullOrWhiteSpace(name) || cells.Count == 0)
        {
            return;
        }

        areas.Add(CreateBoundingNamedArea(name, cells));
    }

    private static LayoutNamedArea CreateBoundingNamedArea(string name, IReadOnlyList<LayoutCell> cells)
    {
        var topRow = cells.Min(cell => cell.Row);
        var leftColumn = cells.Min(cell => cell.Col);
        var bottomRow = cells.Max(cell => cell.Row + cell.RowSpan - 1);
        var rightColumn = cells.Max(cell => cell.Col + cell.ColSpan - 1);

        return new LayoutNamedArea(name, topRow, leftColumn, bottomRow, rightColumn);
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

    /// <summary>
    /// グリッド解決後のセル集合へ、mode=outer/all の border を mode=cell として展開する。
    /// </summary>
    /// <param name="result">グリッド展開結果。</param>
    /// <param name="styleScope">グリッドに適用されるスタイルスコープ。</param>
    /// <param name="styleResolver">スタイル解決器。</param>
    /// <returns>border 展開後の展開結果。</returns>
    private static ExpandResult ApplyGridBorders(
        ExpandResult result,
        StyleScope styleScope,
        IStyleResolver styleResolver)
    {
        if (result.Cells.Count == 0)
        {
            return result;
        }

        var gridBorders = ResolveGridBorders(styleScope, styleResolver);
        if (gridBorders.Count == 0)
        {
            return result;
        }

        var topRow = result.Cells.Min(cell => cell.Row);
        var bottomRow = result.Cells.Max(cell => cell.Row + cell.RowSpan - 1);
        var leftCol = result.Cells.Min(cell => cell.Col);
        var rightCol = result.Cells.Max(cell => cell.Col + cell.ColSpan - 1);

        var expandedCells = new List<LayoutCell>(result.Cells.Count);
        foreach (var cell in result.Cells)
        {
            var expandedBorders = ExpandGridBordersForCell(gridBorders, cell, topRow, bottomRow, leftCol, rightCol);
            expandedCells.Add(expandedBorders.Count == 0 ? cell : AppendBordersToCell(cell, expandedBorders));
        }

        return new ExpandResult(expandedCells, result.Height, result.Width, result.NamedAreas);
    }

    /// <summary>
    /// スタイルスコープから mode=outer/all の border を抽出する。
    /// </summary>
    /// <param name="styleScope">スタイルスコープ。</param>
    /// <param name="styleResolver">スタイル解決器。</param>
    /// <returns>抽出された grid border 一覧。</returns>
    private static IReadOnlyList<BorderInfo> ResolveGridBorders(
        StyleScope styleScope,
        IStyleResolver styleResolver)
    {
        var borders = new List<BorderInfo>();

        foreach (var styleRef in EnumerateStyleRefs(styleScope.StyleRefs))
        {
            var style = styleResolver.ResolveByName(styleRef.Name);
            if (style is null || style.Scope == ExcelReportLib.DSL.AST.StyleScope.Cell)
            {
                continue;
            }

            borders.AddRange(style.Borders.Where(border => IsGridBorderMode(border.Mode)).Select(CloneBorder));
        }

        foreach (var inlineStyle in styleScope.InlineStyles)
        {
            if (inlineStyle.Scope == ExcelReportLib.DSL.AST.StyleScope.Cell)
            {
                continue;
            }

            borders.AddRange(inlineStyle.Borders.Where(border => IsGridBorderMode(border.Mode)).Select(CloneBorder));
        }

        return borders;
    }

    /// <summary>
    /// styleRef の入れ子を含めて深さ優先で列挙する。
    /// </summary>
    /// <param name="roots">列挙開始する styleRef 群。</param>
    /// <returns>順序を維持した styleRef 列挙。</returns>
    private static IEnumerable<StyleRefAst> EnumerateStyleRefs(IEnumerable<StyleRefAst>? roots)
    {
        if (roots is null)
        {
            yield break;
        }

        var stack = new Stack<StyleRefAst>(roots.Reverse());
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            for (var i = current.StyleRefs.Count - 1; i >= 0; i--)
            {
                stack.Push(current.StyleRefs[i]);
            }
        }
    }

    /// <summary>
    /// セル位置に応じて grid border を cell border に展開する。
    /// </summary>
    /// <param name="gridBorders">mode=outer/all の border 一覧。</param>
    /// <param name="cell">対象セル。</param>
    /// <param name="topRow">グリッド上端行。</param>
    /// <param name="bottomRow">グリッド下端行。</param>
    /// <param name="leftCol">グリッド左端列。</param>
    /// <param name="rightCol">グリッド右端列。</param>
    /// <returns>展開された mode=cell border 一覧。</returns>
    private static IReadOnlyList<BorderInfo> ExpandGridBordersForCell(
        IReadOnlyList<BorderInfo> gridBorders,
        LayoutCell cell,
        int topRow,
        int bottomRow,
        int leftCol,
        int rightCol)
    {
        var expanded = new List<BorderInfo>();
        foreach (var gridBorder in gridBorders)
        {
            var border = ExpandBorderForCell(gridBorder, cell, topRow, bottomRow, leftCol, rightCol);
            if (border is not null)
            {
                expanded.Add(border);
            }
        }

        return expanded;
    }

    /// <summary>
    /// 単一 border をセル境界に合わせて展開する。
    /// </summary>
    /// <param name="gridBorder">展開対象 border。</param>
    /// <param name="cell">対象セル。</param>
    /// <param name="topRow">グリッド上端行。</param>
    /// <param name="bottomRow">グリッド下端行。</param>
    /// <param name="leftCol">グリッド左端列。</param>
    /// <param name="rightCol">グリッド右端列。</param>
    /// <returns>展開後 border。適用不要なら <see langword="null"/>。</returns>
    private static BorderInfo? ExpandBorderForCell(
        BorderInfo gridBorder,
        LayoutCell cell,
        int topRow,
        int bottomRow,
        int leftCol,
        int rightCol)
    {
        if (string.Equals(gridBorder.Mode, "all", StringComparison.OrdinalIgnoreCase))
        {
            var allBorder = new BorderInfo
            {
                Mode = "cell",
                Top = gridBorder.Top,
                Bottom = gridBorder.Bottom,
                Left = gridBorder.Left,
                Right = gridBorder.Right,
                Color = gridBorder.Color,
            };

            return HasAnyBorderSide(allBorder) ? allBorder : null;
        }

        if (!string.Equals(gridBorder.Mode, "outer", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var cellBottom = cell.Row + cell.RowSpan - 1;
        var cellRight = cell.Col + cell.ColSpan - 1;
        var outerBorder = new BorderInfo
        {
            Mode = "cell",
            Top = cell.Row == topRow ? gridBorder.Top : null,
            Bottom = cellBottom == bottomRow ? gridBorder.Bottom : null,
            Left = cell.Col == leftCol ? gridBorder.Left : null,
            Right = cellRight == rightCol ? gridBorder.Right : null,
            Color = gridBorder.Color,
        };

        return HasAnyBorderSide(outerBorder) ? outerBorder : null;
    }

    /// <summary>
    /// 既存セルへ追加 border を適用した新しいセルを生成する。
    /// </summary>
    /// <param name="cell">元セル。</param>
    /// <param name="additionalBorders">追加する border 一覧。</param>
    /// <returns>border 追加後セル。</returns>
    private static LayoutCell AppendBordersToCell(LayoutCell cell, IReadOnlyList<BorderInfo> additionalBorders)
    {
        var updatedStylePlan = CreateStylePlanWithAdditionalBorders(cell.StylePlan, additionalBorders);
        return new LayoutCell(
            cell.Row,
            cell.Col,
            cell.RowSpan,
            cell.ColSpan,
            cell.Value,
            cell.Formula,
            cell.FormulaRef,
            updatedStylePlan);
    }

    /// <summary>
    /// 既存 StylePlan に border を追記した新しい StylePlan を生成する。
    /// </summary>
    /// <param name="stylePlan">元のスタイルプラン。</param>
    /// <param name="additionalBorders">追記する border 一覧。</param>
    /// <returns>border 追記後のスタイルプラン。</returns>
    private static StylePlan CreateStylePlanWithAdditionalBorders(
        StylePlan stylePlan,
        IReadOnlyList<BorderInfo> additionalBorders)
    {
        if (additionalBorders.Count == 0)
        {
            return stylePlan;
        }

        var mergedBorders = additionalBorders
            .Concat(stylePlan.Borders)
            .Select(CloneBorder)
            .ToArray();

        var effectiveStyle = stylePlan.EffectiveStyle;
        var updatedEffectiveStyle = new ResolvedStyle(
            effectiveStyle.SourceName,
            effectiveStyle.SourceKind,
            effectiveStyle.DeclaredScope,
            effectiveStyle.FontName,
            effectiveStyle.FontSize,
            effectiveStyle.FontBold,
            effectiveStyle.FontItalic,
            effectiveStyle.FontUnderline,
            effectiveStyle.FillColor,
            effectiveStyle.NumberFormatCode,
            mergedBorders);

        return new StylePlan(
            updatedEffectiveStyle,
            stylePlan.AppliedStyles,
            stylePlan.WorkbookDefault,
            stylePlan.SheetDefault,
            stylePlan.ReferenceStyles,
            stylePlan.InlineStyles,
            stylePlan.FontNameTrace,
            stylePlan.FontSizeTrace,
            stylePlan.FontBoldTrace,
            stylePlan.FontItalicTrace,
            stylePlan.FontUnderlineTrace,
            stylePlan.FillColorTrace,
            stylePlan.NumberFormatCodeTrace,
            stylePlan.BorderTraces);
    }

    /// <summary>
    /// border mode が grid 展開対象かを判定する。
    /// </summary>
    /// <param name="mode">判定対象 mode。</param>
    /// <returns>outer または all の場合 <see langword="true"/>。</returns>
    private static bool IsGridBorderMode(string? mode) =>
        string.Equals(mode, "outer", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// border に少なくとも1辺の定義があるか判定する。
    /// </summary>
    /// <param name="border">判定対象 border。</param>
    /// <returns>いずれかの辺が定義されている場合 <see langword="true"/>。</returns>
    private static bool HasAnyBorderSide(BorderInfo border) =>
        border.Top is not null ||
        border.Bottom is not null ||
        border.Left is not null ||
        border.Right is not null;

    /// <summary>
    /// border 情報を複製する。
    /// </summary>
    /// <param name="border">複製対象 border。</param>
    /// <returns>複製された border。</returns>
    private static BorderInfo CloneBorder(BorderInfo border) =>
        new()
        {
            Mode = border.Mode,
            Top = border.Top,
            Bottom = border.Bottom,
            Left = border.Left,
            Right = border.Right,
            Color = border.Color,
        };

    private sealed record ExpandResult(
        IReadOnlyList<LayoutCell> Cells,
        int Height,
        int Width,
        IReadOnlyList<LayoutNamedArea> NamedAreas)
    {
        public static readonly ExpandResult Empty = new(
            Array.Empty<LayoutCell>(),
            0,
            0,
            Array.Empty<LayoutNamedArea>());
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
