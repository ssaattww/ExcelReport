using System.Reflection;
using System.Reflection.Emit;
using ExcelReportLib.DSL;
using ExcelReportLib.ExpressionEngine;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>ExpressionEngine</c> feature.
/// </summary>
public sealed class ExpressionEngineTests
{
    /// <summary>
    /// Verifies that evaluate simple property returns value.
    /// </summary>
    [Fact]
    public void Evaluate_SimpleProperty_ReturnsValue()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var data = new PersonRow { Name = "Alice" };

        var result = engine.Evaluate("@(data.Name)", new ExpressionContext(data, data));

        Assert.False(result.HasError);
        Assert.Equal("Alice", result.Value);
        Assert.False(result.UsedCache);
    }

    /// <summary>
    /// Verifies that evaluate nested property returns value.
    /// </summary>
    [Fact]
    public void Evaluate_NestedProperty_ReturnsValue()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var root = new ReportRoot
        {
            Summary = new SummaryRow { TotalAmount = 1250m },
        };

        var result = engine.Evaluate("@(root.Summary.TotalAmount)", new ExpressionContext(root, root));

        Assert.False(result.HasError);
        Assert.Equal(1250m, result.Value);
    }

    /// <summary>
    /// Verifies that evaluate collection access returns value.
    /// </summary>
    [Fact]
    public void Evaluate_CollectionAccess_ReturnsValue()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var root = new ReportRoot
        {
            Lines =
            [
                new LineRow { Amount = 100m },
                new LineRow { Amount = 250m },
            ],
        };

        var result = engine.Evaluate("@(root.Lines[0].Amount)", new ExpressionContext(root, root));

        Assert.False(result.HasError);
        Assert.Equal(100m, result.Value);
    }

    /// <summary>
    /// Verifies that evaluate invalid expression returns error.
    /// </summary>
    [Fact]
    public void Evaluate_InvalidExpression_ReturnsError()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var root = new ReportRoot();

        var result = engine.Evaluate("@(root.)", new ExpressionContext(root, root));

        Assert.True(result.HasError);
        Assert.Single(result.Issues);
        Assert.Equal(IssueKind.ExpressionSyntaxError, result.Issues[0].Kind);
        Assert.StartsWith("#ERR(", Assert.IsType<string>(result.Value));
    }

    /// <summary>
    /// Verifies that evaluate null property returns null.
    /// </summary>
    [Fact]
    public void Evaluate_NullProperty_ReturnsNull()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var root = new ReportRoot { Summary = null };

        var result = engine.Evaluate("@(root.Summary.TotalAmount)", new ExpressionContext(root, root));

        Assert.False(result.HasError);
        Assert.Null(result.Value);
    }

    /// <summary>
    /// Verifies that cache same expression returns cached result.
    /// </summary>
    [Fact]
    public void Cache_SameExpression_ReturnsCachedResult()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var root = new ReportRoot
        {
            Summary = new SummaryRow { TotalAmount = 1250m },
        };
        var context = new ExpressionContext(root, root);

        var first = engine.Evaluate("@(root.Summary.TotalAmount)", context);
        var second = engine.Evaluate("@(root.Summary.TotalAmount)", context);

        Assert.False(first.HasError);
        Assert.False(second.HasError);
        Assert.Equal(first.Value, second.Value);
        Assert.False(first.UsedCache);
        Assert.True(second.UsedCache);
        Assert.Equal(1, engine.CachedExpressionCount);
    }

    /// <summary>
    /// Verifies that evaluate arithmetic expression returns computed value.
    /// </summary>
    [Fact]
    public void Evaluate_ArithmeticExpression_ReturnsComputedValue()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var data = new MetricsRow { Amount = 90 };

        var result = engine.Evaluate("@(data.Amount + 10)", new ExpressionContext(data, data));

        Assert.False(result.HasError);
        Assert.Equal(100, Convert.ToInt32(result.Value));
    }

    /// <summary>
    /// Verifies that evaluate conditional expression returns expected label.
    /// </summary>
    [Fact]
    public void Evaluate_ConditionalExpression_ReturnsExpectedLabel()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var data = new MetricsRow { Score = 85 };

        var result = engine.Evaluate(
            "@(data.Score >= 80 ? \"Pass\" : \"Fail\")",
            new ExpressionContext(data, data));

        Assert.False(result.HasError);
        Assert.Equal("Pass", result.Value);
    }

    /// <summary>
    /// Verifies that evaluate null coalescing expression returns fallback text.
    /// </summary>
    [Fact]
    public void Evaluate_NullCoalescingExpression_ReturnsFallbackText()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var data = new MetricsRow { Address = null };

        var result = engine.Evaluate(
            "@(data.Address?.City ?? \"Unknown\")",
            new ExpressionContext(data, data));

        Assert.False(result.HasError);
        Assert.Equal("Unknown", result.Value);
    }

    /// <summary>
    /// Verifies that evaluate vars indexer with method call returns formatted value.
    /// </summary>
    [Fact]
    public void Evaluate_VarsIndexerWithMethodCall_ReturnsFormattedValue()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var context = new ExpressionContext(
            root: null,
            data: null,
            vars: new Dictionary<string, object?>
            {
                ["ReportDate"] = new DateTime(2026, 3, 19),
            });

        var result = engine.Evaluate(
            "@(((DateTime)vars[\"ReportDate\"]).ToString(\"yyyy-MM-dd\"))",
            context);

        Assert.False(result.HasError);
        Assert.Equal("2026-03-19", result.Value);
    }


    /// <summary>
    /// Verifies that evaluate runtime error returns runtime issue.
    /// </summary>
    [Fact]
    public void Evaluate_RuntimeError_ReturnsRuntimeIssue()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var data = new MetricsRow { Divisor = 0 };

        var result = engine.Evaluate("@(10 / data.Divisor)", new ExpressionContext(data, data));

        Assert.True(result.HasError);
        Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.ExpressionRuntimeError);
        Assert.StartsWith("#ERR(", Assert.IsType<string>(result.Value));
    }

    [Fact]
    public void Evaluate_TypeNameContainingHash_UsesDynamicFallback()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var scriptLikeType = CreatePublicTypeWithHashInName();
        var instance = Activator.CreateInstance(scriptLikeType);

        Assert.NotNull(instance);

        var pairsField = scriptLikeType.GetField("Pairs", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(pairsField);
        pairsField!.SetValue(instance, new List<string> { "No1" });

        var result = engine.Evaluate("@(root.Pairs)", new ExpressionContext(instance, instance));

        Assert.False(result.HasError);
        var pairs = Assert.IsAssignableFrom<IEnumerable<object?>>(result.Value);
        Assert.Equal(new object?[] { "No1" }, pairs.ToArray());
    }

    /// <summary>
    /// Verifies that xl helper escapes sheet names and builds formula references.
    /// </summary>
    [Fact]
    public void Evaluate_XlFormulaHelper_BuildsEscapedFormulaReference()
    {
        var engine = new ExpressionEngine.ExpressionEngine();

        var result = engine.Evaluate(
            "@(xl.FormulaRef(\"Summary O'Brien\", \"A1\"))",
            new ExpressionContext(root: null, data: null));

        Assert.False(result.HasError);
        Assert.Equal("='Summary O''Brien'!A1", result.Value);
    }

    /// <summary>
    /// Verifies that interpolated-string syntax works with xl helper methods.
    /// </summary>
    [Fact]
    public void Evaluate_XlFormulaHelper_WithInterpolatedString_Works()
    {
        var engine = new ExpressionEngine.ExpressionEngine();

        var result = engine.Evaluate(
            """@($"=SUM({xl.Ref("Summary O'Brien", "B2:B10")})")""",
            new ExpressionContext(root: null, data: null));

        Assert.False(result.HasError);
        Assert.Equal("=SUM('Summary O''Brien'!B2:B10)", result.Value);
    }

    /// <summary>
    /// Verifies that helper binding does not conflict with user lambda parameter named xl.
    /// </summary>
    [Fact]
    public void Evaluate_LinqLambdaParameterNamedXl_DoesNotConflictWithHelperBinding()
    {
        var engine = new ExpressionEngine.ExpressionEngine();
        var root = new
        {
            Items = new[]
            {
                new { Name = "A" },
                new { Name = "B" },
            },
        };

        var result = engine.Evaluate(
            "@(root.Items.Select(xl => xl.Name).ToArray())",
            new ExpressionContext(root, root));

        Assert.False(result.HasError);
        var values = Assert.IsAssignableFrom<IEnumerable<object?>>(result.Value);
        Assert.Equal(new object?[] { "A", "B" }, values.ToArray());
    }

    private static Type CreatePublicTypeWithHashInName()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("ExpressionEngineTests.ScriptLikeType" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        var typeBuilder = module.DefineType("Submission#0Root", TypeAttributes.Public | TypeAttributes.Class);

        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
        typeBuilder.DefineField("Pairs", typeof(List<string>), FieldAttributes.Public);

        return typeBuilder.CreateType()!;
    }
    private sealed class PersonRow
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class ReportRoot
    {
        public SummaryRow? Summary { get; init; }

        public List<LineRow> Lines { get; init; } = [];
    }

    private sealed class SummaryRow
    {
        public decimal TotalAmount { get; init; }
    }

    private sealed class LineRow
    {
        public decimal Amount { get; init; }
    }

    private sealed class MetricsRow
    {
        public int Amount { get; init; }
        public int Score { get; init; }

        public int Divisor { get; init; }

        public AddressRow? Address { get; init; }
    }

    private sealed class AddressRow
    {
        public string City { get; init; } = string.Empty;
    }
}
