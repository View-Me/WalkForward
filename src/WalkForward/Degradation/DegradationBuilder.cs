namespace WalkForward.Degradation;

/// <summary>
/// Fluent builder for configuring and executing a degradation analysis that compares
/// in-sample and out-of-sample fitness across walk-forward folds.
/// Use <see cref="FoldBuilder.Degrade"/> to obtain an instance.
/// </summary>
/// <example>
/// <code>
/// var result = new FoldBuilder()
///     .WithDataPoints(10000)
///     .WithDataFrequency(TimeSpan.FromMinutes(15))
///     .Degrade()
///     .WithTrainingWindow(TimeSpan.FromDays(30))
///     .WithTestWindow(TimeSpan.FromDays(7))
///     .BackwardLooking()
///     .EvaluateInSample(fold => TrainAndScore(data, fold.TrainRange))
///     .EvaluateOutOfSample(fold => ScoreOnly(data, fold.TestRange))
///     .Build();
/// </code>
/// </example>
public sealed class DegradationBuilder
{
    private readonly int _totalDataPoints;
    private readonly TimeSpan _dataFrequency;
    private TimeSpan _trainingWindow;
    private TimeSpan _testWindow;
    private FoldMode? _mode;
    private Func<Fold, double>? _inSampleCallback;
    private Func<Fold, double>? _outOfSampleCallback;
    private int _warmupPoints;
    private TimeSpan _embargo;
    private int? _maxFolds;

    /// <summary>
    /// Initializes a new instance of the <see cref="DegradationBuilder"/> class.
    /// </summary>
    /// <param name="totalDataPoints">Total number of data points in the dataset.</param>
    /// <param name="dataFrequency">Time interval between consecutive data points.</param>
    internal DegradationBuilder(int totalDataPoints, TimeSpan dataFrequency)
    {
        _totalDataPoints = totalDataPoints;
        _dataFrequency = dataFrequency;
    }

    /// <summary>
    /// Sets the training window duration for fold generation.
    /// The training window defines the in-sample period.
    /// </summary>
    /// <param name="window">Duration of the training window.</param>
    /// <returns>This builder for chaining.</returns>
    public DegradationBuilder WithTrainingWindow(TimeSpan window)
    {
        _trainingWindow = window;
        return this;
    }

    /// <summary>
    /// Sets the test window duration for fold generation.
    /// The test window defines the out-of-sample period.
    /// </summary>
    /// <param name="window">Duration of the test window.</param>
    /// <returns>This builder for chaining.</returns>
    public DegradationBuilder WithTestWindow(TimeSpan window)
    {
        _testWindow = window;
        return this;
    }

    /// <summary>
    /// Sets the fold generation mode to backward-looking.
    /// Folds walk backwards from the end of data, with the most recent test window first.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public DegradationBuilder BackwardLooking()
    {
        _mode = FoldMode.BackwardLooking;
        return this;
    }

    /// <summary>
    /// Sets the fold generation mode to forward-looking.
    /// Folds walk forwards from the start of data.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public DegradationBuilder ForwardLooking()
    {
        _mode = FoldMode.ForwardLooking;
        return this;
    }

    /// <summary>
    /// Sets the in-sample fitness callback. This callback evaluates each fold's training window.
    /// </summary>
    /// <param name="callback">A function that evaluates a fold's training data and returns a fitness score.</param>
    /// <returns>This builder for chaining.</returns>
    public DegradationBuilder EvaluateInSample(Func<Fold, double> callback)
    {
        _inSampleCallback = callback;
        return this;
    }

    /// <summary>
    /// Sets the out-of-sample fitness callback. This callback evaluates each fold's test window.
    /// </summary>
    /// <param name="callback">A function that evaluates a fold's test data and returns a fitness score.</param>
    /// <returns>This builder for chaining.</returns>
    public DegradationBuilder EvaluateOutOfSample(Func<Fold, double> callback)
    {
        _outOfSampleCallback = callback;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of data points required before the first training window.
    /// Folds whose training window would start before this threshold are skipped.
    /// </summary>
    /// <param name="warmupPoints">Number of warmup data points. Defaults to 0.</param>
    /// <returns>This builder for chaining.</returns>
    public DegradationBuilder WithWarmup(int warmupPoints)
    {
        _warmupPoints = warmupPoints;
        return this;
    }

    /// <summary>
    /// Sets the embargo gap duration between training and test windows.
    /// Embargo prevents lookahead bias from autocorrelated features.
    /// </summary>
    /// <param name="embargo">Embargo duration (e.g., 4 hours). Defaults to zero.</param>
    /// <returns>This builder for chaining.</returns>
    public DegradationBuilder WithEmbargo(TimeSpan embargo)
    {
        _embargo = embargo;
        return this;
    }

    /// <summary>
    /// Limits the maximum number of folds generated.
    /// When not set, the fold count is auto-computed based on available data.
    /// </summary>
    /// <param name="maxFolds">Maximum number of folds.</param>
    /// <returns>This builder for chaining.</returns>
    public DegradationBuilder WithMaxFolds(int maxFolds)
    {
        _maxFolds = maxFolds;
        return this;
    }

    /// <summary>
    /// Executes the degradation analysis and returns aggregate metrics.
    /// </summary>
    /// <returns>A <see cref="DegradationResult"/> containing IS/OOS fitness comparison metrics.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration is missing (training window, test window, mode, IS callback, or OOS callback).
    /// </exception>
    public DegradationResult Build() => Build(CancellationToken.None);

    /// <summary>
    /// Executes the degradation analysis with cancellation support and returns aggregate metrics.
    /// Cancellation is checked between fold iterations.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the analysis between fold iterations.</param>
    /// <returns>A <see cref="DegradationResult"/> containing IS/OOS fitness comparison metrics.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration is missing (training window, test window, mode, IS callback, or OOS callback).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled between fold iterations.
    /// </exception>
    public DegradationResult Build(CancellationToken cancellationToken)
    {
        // Reference all fields to satisfy analyzers
        _ = _totalDataPoints;
        _ = _dataFrequency;
        _ = _trainingWindow;
        _ = _testWindow;
        _ = _mode;
        _ = _inSampleCallback;
        _ = _outOfSampleCallback;
        _ = _warmupPoints;
        _ = _embargo;
        _ = _maxFolds;
        _ = cancellationToken;

        throw new NotSupportedException("Stub: not yet implemented.");
    }
}
