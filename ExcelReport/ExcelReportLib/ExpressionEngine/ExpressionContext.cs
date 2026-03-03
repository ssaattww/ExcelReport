using System.Collections.ObjectModel;

namespace ExcelReportLib.ExpressionEngine;

public class ExpressionContext
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyVars =
        new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

    public object? Root { get; }

    public object? Data { get; }

    public IReadOnlyDictionary<string, object?> Vars { get; }

    public ExpressionContext(object? root, object? data, IDictionary<string, object?>? vars = null)
    {
        Root = root;
        Data = data;
        Vars = vars is null
            ? EmptyVars
            : new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(vars));
    }
}

/// <summary>
/// Design-document alias kept for compatibility with the detailed design terminology.
/// </summary>
public sealed class EvaluationContext : ExpressionContext
{
    public EvaluationContext(object? root, object? data, IDictionary<string, object?>? vars = null)
        : base(root, data, vars)
    {
    }
}
