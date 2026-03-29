namespace WalkForward.Internal;

/// <summary>
/// Internal engine that evaluates in-sample and out-of-sample fitness across walk-forward folds
/// and computes degradation metrics for overfitting detection.
/// </summary>
internal static class DegradationEngine
{
    /// <summary>
    /// Executes the degradation analysis: generates folds, invokes both IS and OOS callbacks per fold,
    /// and computes aggregate degradation metrics.
    /// </summary>
    /// <param name="totalDataPoints">Total number of data points in the dataset.</param>
    /// <param name="dataFrequency">Time interval between consecutive data points.</param>
    /// <param name="trainWindow">Training window duration.</param>
    /// <param name="testWindow">Test window duration.</param>
    /// <param name="mode">Fold generation mode (backward or forward looking).</param>
    /// <param name="inSampleCallback">Callback invoked per fold to compute in-sample fitness.</param>
    /// <param name="outOfSampleCallback">Callback invoked per fold to compute out-of-sample fitness.</param>
    /// <param name="warmupPoints">Minimum data points before the first training window.</param>
    /// <param name="embargo">Embargo gap duration between training and test windows.</param>
    /// <param name="maxFolds">Optional cap on fold count.</param>
    /// <param name="cancellationToken">Token checked between fold iterations.</param>
    /// <returns>A <see cref="Degradation.DegradationResult"/> with IS/OOS fitness comparison metrics.</returns>
    internal static Degradation.DegradationResult Execute(
        int totalDataPoints,
        TimeSpan dataFrequency,
        TimeSpan trainWindow,
        TimeSpan testWindow,
        FoldMode mode,
        Func<Fold, double> inSampleCallback,
        Func<Fold, double> outOfSampleCallback,
        int warmupPoints,
        TimeSpan embargo,
        int? maxFolds,
        CancellationToken cancellationToken)
    {
        // Reference all parameters to satisfy analyzers
        _ = totalDataPoints;
        _ = dataFrequency;
        _ = trainWindow;
        _ = testWindow;
        _ = mode;
        _ = inSampleCallback;
        _ = outOfSampleCallback;
        _ = warmupPoints;
        _ = embargo;
        _ = maxFolds;
        _ = cancellationToken;

        throw new NotSupportedException("Stub: not yet implemented.");
    }
}
