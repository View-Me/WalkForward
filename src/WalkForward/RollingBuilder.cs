using WalkForward.Internal;

namespace WalkForward;

/// <summary>
/// Builder for rolling walk-forward fold generation.
/// Rolling mode walks forwards from the start of data, producing folds
/// with fixed-size training windows that slide through the dataset.
/// </summary>
/// <remarks>
/// Use <see cref="WalkForwardBuilder.Rolling"/> to obtain an instance of this builder.
/// Rolling mode supports stride configuration via <see cref="WithStride"/>.
/// When stride is not set, it defaults to the test window size.
/// </remarks>
public sealed class RollingBuilder
{
    private readonly int _totalDataPoints;
    private readonly TimeSpan _dataFrequency;
    private TimeSpan _trainingWindow;
    private TimeSpan _testWindow;
    private TimeSpan _embargo;
    private int _warmupPoints;
    private TimeSpan? _stride;
    private int? _maxFolds;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollingBuilder"/> class.
    /// </summary>
    /// <param name="totalDataPoints">Total number of data points in the dataset.</param>
    /// <param name="dataFrequency">Time interval between consecutive data points.</param>
    internal RollingBuilder(int totalDataPoints, TimeSpan dataFrequency)
    {
        _totalDataPoints = totalDataPoints;
        _dataFrequency = dataFrequency;
    }

    /// <summary>
    /// Sets the duration of the training window for each fold.
    /// </summary>
    /// <param name="window">Training window duration (e.g., 30 days).</param>
    /// <returns>This builder for chaining.</returns>
    public RollingBuilder WithTrainingWindow(TimeSpan window)
    {
        _trainingWindow = window;
        return this;
    }

    /// <summary>
    /// Sets the duration of the test (out-of-sample) window for each fold.
    /// </summary>
    /// <param name="window">Test window duration (e.g., 7 days).</param>
    /// <returns>This builder for chaining.</returns>
    public RollingBuilder WithTestWindow(TimeSpan window)
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
    public RollingBuilder WithEmbargo(TimeSpan embargo)
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
    public RollingBuilder WithWarmup(int warmupPoints)
    {
        _warmupPoints = warmupPoints;
        return this;
    }

    /// <summary>
    /// Sets the stride duration between consecutive folds. When not set,
    /// defaults to the test window size so that consecutive test windows are non-overlapping.
    /// </summary>
    /// <param name="stride">Stride duration between fold starts (e.g., 3 days).</param>
    /// <returns>This builder for chaining.</returns>
    public RollingBuilder WithStride(TimeSpan stride)
    {
        _stride = stride;
        return this;
    }

    /// <summary>
    /// Limits the maximum number of folds generated. When not set, the fold count
    /// is auto-computed based on available data.
    /// </summary>
    /// <param name="maxFolds">Maximum number of folds to generate.</param>
    /// <returns>This builder for chaining.</returns>
    public RollingBuilder WithMaxFolds(int maxFolds)
    {
        _maxFolds = maxFolds;
        return this;
    }

    /// <summary>
    /// Builds the fold boundaries using rolling walk-forward logic.
    /// Validates all parameters and delegates to the internal fold generator.
    /// </summary>
    /// <returns>An ordered list of folds, with fold 0 being the earliest.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any parameter has an invalid value (e.g., non-positive data points,
    /// zero data frequency, non-positive training or test window).
    /// </exception>
    public IReadOnlyList<Fold> Build()
    {
        var options = new RollingOptions
        {
            TotalDataPoints = _totalDataPoints,
            DataFrequency = _dataFrequency,
            TrainingWindow = _trainingWindow,
            TestWindow = _testWindow,
            Embargo = _embargo,
            WarmupPoints = _warmupPoints,
            Stride = _stride,
            MaxFolds = _maxFolds,
        };

        return RollingFoldGenerator.Generate(options);
    }
}
