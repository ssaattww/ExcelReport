namespace ExcelReportLib.ExpressionEngine;

public interface IExpressionEngine
{
    /// <summary>
    /// Evaluates an expression body or an <c>@(...)</c> expression against the supplied context.
    /// </summary>
    ExpressionResult Evaluate(string expression, ExpressionContext context);
}

/// <summary>
/// Design-document alias kept for downstream modules that use the evaluator terminology.
/// </summary>
public interface IExpressionEvaluator : IExpressionEngine
{
}
