namespace WalkForward.GridSearch;

/// <summary>
/// Aggregated metrics for a single (train window, test window) cell in a grid search.
/// </summary>
public sealed record GridCellResult
{
    /// <summary>Gets the training window duration.</summary>
    public required TimeSpan TrainWindow { get; init; }

    /// <summary>Gets the test window duration.</summary>
    public required TimeSpan TestWindow { get; init; }

    /// <summary>Gets the training window size in data points.</summary>
    public required int TrainDataPoints { get; init; }

    /// <summary>Gets the test window size in data points.</summary>
    public required int TestDataPoints { get; init; }

    /// <summary>Gets the arithmetic mean fitness across all folds.</summary>
    public required double MeanFitness { get; init; }

    /// <summary>Gets the consistency metrics computed from per-fold fitness values.</summary>
    public required ConsistencyMetrics Consistency { get; init; }

    /// <summary>Gets the number of folds evaluated for this cell.</summary>
    public required int FoldCount { get; init; }

    /// <summary>Gets the minimum fitness value across all folds.</summary>
    public required double WorstFold { get; init; }

    /// <summary>Gets the smoothness bonus for this cell (0.0 to 1.0).</summary>
    public double SmoothnessBonus { get; init; }

    /// <summary>Gets the weighted composite score for this cell.</summary>
    public double CompositeScore { get; init; }

    /// <summary>Gets the per-fold fitness values.</summary>
    public required IReadOnlyList<double> FoldFitnesses { get; init; }
}
