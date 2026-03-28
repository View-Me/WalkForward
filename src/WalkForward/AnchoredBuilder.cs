using WalkForward.Internal;

namespace WalkForward;

/// <summary>
/// Builder for anchored walk-forward fold generation.
/// Anchored mode walks backwards from the end of data, producing folds
/// with the most recent test window first.
/// </summary>
/// <remarks>
/// Use <see cref="WalkForwardBuilder.Anchored"/> to obtain an instance of this builder.
/// Anchored mode does not support stride configuration; folds step by the test window size.
/// </remarks>
public sealed class AnchoredBuilder
{
    private readonly int _totalDataPoints;
    private readonly TimeSpan _dataFrequency;
    private TimeSpan _trainingWindow;
    private TimeSpan _testWindow;
    private TimeSpan _embargo;
    private int _warmupPoints;
    private int? _maxFolds;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnchoredBuilder"/> class.
    /// </summary>
    /// <param name="totalDataPoints">Total number of data points in the dataset.</param>
    /// <param name="dataFrequency">Time interval between consecutive data points.</param>
    internal AnchoredBuilder(int totalDataPoints, TimeSpan dataFrequency)
    {
        _totalDataPoints = totalDataPoints;
        _dataFrequency = dataFrequency;
    }

    /// <summary>
    /// Sets the duration of the training window for each fold.
    /// </summary>
    /// <param name="window">Training window duration (e.g., 90 days).</param>
    /// <returns>This builder for chaining.</returns>
    public AnchoredBuilder WithTrainingWindow(TimeSpan window)
    {
        _trainingWindow = window;
        return this;
    }

    /// <summary>
    /// Sets the duration of the test (out-of-sample) window for each fold.
    /// </summary>
    /// <param name="window">Test window duration (e.g., 7 days).</param>
    /// <returns>This builder for chaining.</returns>
    public AnchoredBuilder WithTestWindow(TimeSpan window)
    {
        _testWindow = window;
        return this;
    }

    /// <summary>
    /// Sets the embargo gap duration between training and test windows.
    /// Embargo prevents lookahead bias from autocorrelated features.
    /// </summary>
    /// <param name="embargo">Embargo duration (e.g., 4 hours). Defaults to zero.</param>
    /// <returns>This builder for chaining.</returns>
    public AnchoredBuilder WithEmbargo(TimeSpan embargo)
    {
        _embargo = embargo;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of data points required before the first training window.
    /// Folds whose training window would start before this threshold are skipped.
    /// </summary>
    /// <param name="warmupPoints">Number of warmup data points. Defaults to 0.</param>
    /// <returns>This builder for chaining.</returns>
    public AnchoredBuilder WithWarmup(int warmupPoints)
    {
        _warmupPoints = warmupPoints;
        return this;
    }

    /// <summary>
    /// Limits the maximum number of folds generated. When not set, the fold count
    /// is auto-computed based on available data.
    /// </summary>
    /// <param name="maxFolds">Maximum number of folds to generate.</param>
    /// <returns>This builder for chaining.</returns>
    public AnchoredBuilder WithMaxFolds(int maxFolds)
    {
        _maxFolds = maxFolds;
        return this;
    }

    /// <summary>
    /// Builds the fold boundaries using anchored walk-forward logic.
    /// Validates all parameters and delegates to the internal fold generator.
    /// </summary>
    /// <returns>An ordered list of folds, with fold 0 being the most recent.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any parameter has an invalid value (e.g., non-positive data points,
    /// zero data frequency, non-positive training or test window).
    /// </exception>
    public IReadOnlyList<Fold> Build()
    {
        var options = new AnchoredOptions
        {
            TotalDataPoints = _totalDataPoints,
            DataFrequency = _dataFrequency,
            TrainingWindow = _trainingWindow,
            TestWindow = _testWindow,
            Embargo = _embargo,
            WarmupPoints = _warmupPoints,
            MaxFolds = _maxFolds,
        };

        return AnchoredFoldGenerator.Generate(options);
    }
}
