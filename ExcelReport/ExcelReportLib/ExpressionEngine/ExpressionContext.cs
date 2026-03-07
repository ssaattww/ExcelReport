using System.Collections.ObjectModel;

namespace ExcelReportLib.ExpressionEngine;

/// <summary>
/// Represents expression context.
/// </summary>
public class ExpressionContext
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyVars =
        new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

    /// <summary>
    /// Gets the root.
    /// </summary>
    public object? Root { get; }

    /// <summary>
    /// Gets the data.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Gets the vars.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Vars { get; }

    /// <summary>
    /// Initializes a new instance of the expression context type.
    /// </summary>
    /// <param name="root">The root.</param>
    /// <param name="data">The runtime data context.</param>
    /// <param name="vars">The vars.</param>
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
    /// <summary>
    /// Initializes a new instance of the evaluation context type.
    /// </summary>
    /// <param name="root">The root.</param>
    /// <param name="data">The runtime data context.</param>
    /// <param name="vars">The vars.</param>
    public EvaluationContext(object? root, object? data, IDictionary<string, object?>? vars = null)
        : base(root, data, vars)
    {
    }
}
