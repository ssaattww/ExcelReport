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





