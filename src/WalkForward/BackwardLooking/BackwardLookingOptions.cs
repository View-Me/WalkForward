using WalkForward.Internal;

namespace WalkForward;

/// <summary>
/// Configuration options for backward-looking walk-forward fold generation.
/// BackwardLooking mode walks backwards from the end of data, producing folds
/// with the most recent data tested first.
/// </summary>
public sealed record BackwardLookingOptions
{
    /// <summary>
    /// Gets the total number of data points in the dataset.
    /// </summary>
    public required int TotalDataPoints { get; init; }

    /// <summary>
    /// Gets the time interval between consecutive data points (e.g., 15 minutes for 15-min candles).
    /// </summary>
    public required TimeSpan DataFrequency { get; init; }

    /// <summary>
    /// Gets the duration of the training window for each fold.
    /// </summary>
    public required TimeSpan TrainingWindow { get; init; }

    /// <summary>
    /// Gets the duration of the test (out-of-sample) window for each fold.
    /// </summary>
    public required TimeSpan TestWindow { get; init; }

    /// <summary>
    /// Gets the duration of the embargo gap between training and test windows.
    /// Embargo prevents lookahead bias from autocorrelated features.
    /// Defaults to <see cref="TimeSpan.Zero"/>.
    /// </summary>
    public TimeSpan Embargo { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Gets the minimum number of data points required before the first training window start.
    /// Folds whose training window would start before this threshold are skipped.
    /// Defaults to 0.
    /// </summary>
    public int WarmupPoints { get; init; }

    /// <summary>
    /// Gets the maximum number of folds to generate, or <c>null</c> to auto-compute
    /// based on available data.
    /// </summary>
    public int? MaxFolds { get; init; }

    /// <summary>
    /// Validates all option values and throws <see cref="ArgumentOutOfRangeException"/>
    /// for invalid configurations.
    /// </summary>
    internal void Validate()
    {
        Validation.EnsurePositive(TotalDataPoints, nameof(TotalDataPoints));
        Validation.EnsurePositive(DataFrequency, nameof(DataFrequency));
        Validation.EnsurePositive(TrainingWindow, nameof(TrainingWindow));
        Validation.EnsurePositive(TestWindow, nameof(TestWindow));
        Validation.EnsureNonNegative(Embargo, nameof(Embargo));
        Validation.EnsureNonNegative(WarmupPoints, nameof(WarmupPoints));
    }
}
