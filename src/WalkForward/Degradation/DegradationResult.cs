namespace WalkForward.Degradation;

/// <summary>
/// Aggregate degradation metrics comparing in-sample and out-of-sample fitness
/// across walk-forward folds.
/// </summary>
public sealed record DegradationResult
{
    /// <summary>Gets the arithmetic mean of in-sample fitness values across all folds.</summary>
    public required double InSampleMeanFitness { get; init; }

    /// <summary>Gets the arithmetic mean of out-of-sample fitness values across all folds.</summary>
    public required double OutOfSampleMeanFitness { get; init; }

    /// <summary>Gets the degradation percentage: (1 - OOS/IS) * 100. Higher means more overfitting.</summary>
    public required double DegradationPercent { get; init; }

    /// <summary>Gets the Walk-Forward Efficiency ratio: OOS / IS. Values near 1.0 indicate minimal overfitting.</summary>
    public required double WalkForwardEfficiency { get; init; }

    /// <summary>Gets the per-fold degradation details.</summary>
    public required IReadOnlyList<DegradationFoldResult> FoldResults { get; init; }

    /// <summary>
    /// Gets per-segment degradation results. Keys are segment labels assigned by the labeler callback.
    /// Each value is a <see cref="DegradationResult"/> computed from the subset of folds in that segment.
    /// Empty dictionary when no labeler is configured.
    /// </summary>
    public IReadOnlyDictionary<string, DegradationResult> SegmentResults { get; init; } =
        new Dictionary<string, DegradationResult>(StringComparer.Ordinal);
}
