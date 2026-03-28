namespace WalkForward;

/// <summary>
/// Aggregate metrics computed from per-fold returns across a walk-forward validation.
/// </summary>
/// <param name="ConsistencyPercent">Percentage of folds with positive returns (0-100).</param>
/// <param name="MagnitudeConsistency">Sortino-like ratio normalized to [0, 1]. Higher is better.</param>
/// <param name="WorstFold">The minimum return across all folds.</param>
/// <param name="AverageReturn">The arithmetic mean of all fold returns.</param>
public sealed record ConsistencyMetrics(
    double ConsistencyPercent,
    double MagnitudeConsistency,
    double WorstFold,
    double AverageReturn);
