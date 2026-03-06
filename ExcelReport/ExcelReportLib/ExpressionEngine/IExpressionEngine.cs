namespace ExcelReportLib.ExpressionEngine;

/// <summary>
/// Defines behavior for expression engine.
/// </summary>
public interface IExpressionEngine
{
    /// <summary>
    /// Evaluates an expression body or an <c>@(...)</c> expression against the supplied context.
    /// </summary>
    /// <param name="expression">The expression text to evaluate.</param>
    /// <param name="context">The expression context that provides roots, data, and variables.</param>
    /// <returns>The evaluation result including the computed value and any issues.</returns>
    ExpressionResult Evaluate(string expression, ExpressionContext context);
}

/// <summary>
/// Design-document alias kept for downstream modules that use the evaluator terminology.
/// </summary>
public interface IExpressionEvaluator : IExpressionEngine
{
}
