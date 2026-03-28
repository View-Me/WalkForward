namespace WalkForward;

/// <summary>
/// Aggregate metrics computed from per-fold classifier evaluation results.
/// </summary>
/// <param name="AverageAccuracy">The arithmetic mean accuracy across all folds.</param>
/// <param name="AverageLogLoss">The arithmetic mean log-loss across all folds.</param>
/// <param name="ConsistencyAboveBaseline">Percentage of folds whose accuracy exceeds the baseline (0-100).</param>
/// <param name="FoldCount">The number of folds evaluated.</param>
public sealed record ClassifierConsistencyMetrics(
    double AverageAccuracy,
    double AverageLogLoss,
    double ConsistencyAboveBaseline,
    int FoldCount);
