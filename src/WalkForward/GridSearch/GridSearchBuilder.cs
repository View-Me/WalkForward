using WalkForward.Internal;
using WalkForward.Scoring;

namespace WalkForward.GridSearch;

/// <summary>
/// Fluent builder for configuring and executing a grid search over (train, test) window combinations.
/// Use <see cref="FoldBuilder.GridSearch"/> to obtain an instance.
/// </summary>
/// <example>
/// <code>
/// var result = new FoldBuilder()
///     .WithDataPoints(10000)
///     .WithDataFrequency(TimeSpan.FromMinutes(15))
///     .GridSearch()
///     .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60), TimeSpan.FromDays(90))
///     .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
///     .BackwardLooking()
///     .Evaluate(fold => ComputeFitness(data, fold))
///     .Build();
/// </code>
/// </example>
public sealed class GridSearchBuilder
{
    private readonly int _totalDataPoints;
    private readonly TimeSpan _dataFrequency;
    private TimeSpan[]? _trainWindows;
    private TimeSpan[]? _testWindows;
    private FoldMode? _mode;
    private Func<Fold, double>? _fitnessCallback;
    private CompositeScorer? _scorer;
    private Func<Fold, IEnumerable<string>>? _labeler;
    private int _warmupPoints;
    private TimeSpan _embargo;
    private int _minimumFolds = 2;
    private int? _maxFoldsPerCell;
    private int _innerFolds;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridSearchBuilder"/> class.
    /// </summary>
    /// <param name="totalDataPoints">Total number of data points in the dataset.</param>
    /// <param name="dataFrequency">Time interval between consecutive data points.</param>
    internal GridSearchBuilder(int totalDataPoints, TimeSpan dataFrequency)
    {
        _totalDataPoints = totalDataPoints;
        _dataFrequency = dataFrequency;
    }

    /// <summary>
    /// Sets the training window durations to search over.
    /// </summary>
    /// <param name="windows">One or more training window durations.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="windows"/> is empty.</exception>
    public GridSearchBuilder WithTrainWindows(params TimeSpan[] windows)
    {
        if (windows.Length == 0)
        {
            throw new ArgumentException("At least one training window must be specified.", nameof(windows));
        }

        _trainWindows = windows;
        return this;
    }

    /// <summary>
    /// Sets the training window sizes in data points. Each value is converted to a
    /// <see cref="TimeSpan"/> using the configured data frequency.
    /// </summary>
    /// <param name="dataPoints">One or more training window sizes in data points.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="dataPoints"/> is empty.</exception>
    public GridSearchBuilder WithTrainWindows(params int[] dataPoints)
    {
        if (dataPoints.Length == 0)
        {
            throw new ArgumentException("At least one training window must be specified.", nameof(dataPoints));
        }

        _trainWindows = Array.ConvertAll(dataPoints, dp => _dataFrequency * dp);
        return this;
    }

    /// <summary>
    /// Sets the test window durations to search over.
    /// </summary>
    /// <param name="windows">One or more test window durations.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="windows"/> is empty.</exception>
    public GridSearchBuilder WithTestWindows(params TimeSpan[] windows)
    {
        if (windows.Length == 0)
        {
            throw new ArgumentException("At least one test window must be specified.", nameof(windows));
        }

        _testWindows = windows;
        return this;
    }

    /// <summary>
    /// Sets the test window sizes in data points. Each value is converted to a
    /// <see cref="TimeSpan"/> using the configured data frequency.
    /// </summary>
    /// <param name="dataPoints">One or more test window sizes in data points.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="dataPoints"/> is empty.</exception>
    public GridSearchBuilder WithTestWindows(params int[] dataPoints)
    {
        if (dataPoints.Length == 0)
        {
            throw new ArgumentException("At least one test window must be specified.", nameof(dataPoints));
        }

        _testWindows = Array.ConvertAll(dataPoints, dp => _dataFrequency * dp);
        return this;
    }

    /// <summary>
    /// Sets the fold generation mode to backward-looking.
    /// Folds walk backwards from the end of data, with the most recent test window first.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public GridSearchBuilder BackwardLooking()
    {
        _mode = FoldMode.BackwardLooking;
        return this;
    }

    /// <summary>
    /// Sets the fold generation mode to forward-looking.
    /// Folds walk forwards from the start of data.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public GridSearchBuilder ForwardLooking()
    {
        _mode = FoldMode.ForwardLooking;
        return this;
    }

    /// <summary>
    /// Sets the fitness callback that evaluates each fold.
    /// The callback receives a <see cref="Fold"/> and returns a fitness score (higher is better).
    /// </summary>
    /// <param name="fitnessCallback">A function that evaluates a fold and returns a fitness score.</param>
    /// <returns>This builder for chaining.</returns>
    public GridSearchBuilder Evaluate(Func<Fold, double> fitnessCallback)
    {
        _fitnessCallback = fitnessCallback;
        return this;
    }

    /// <summary>
    /// Sets the composite scorer used to rank cells by a weighted combination of
    /// fitness, consistency, and smoothness. When set, <see cref="Build()"/> returns
    /// cells ordered by <see cref="GridCellResult.CompositeScore"/> descending instead
    /// of <see cref="GridCellResult.MeanFitness"/> descending.
    /// </summary>
    /// <param name="scorer">A configured <see cref="CompositeScorer"/> instance.</param>
    /// <returns>This builder for chaining.</returns>
    public GridSearchBuilder WithScoring(CompositeScorer scorer)
    {
        _scorer = scorer;
        return this;
    }

    /// <summary>
    /// Sets a labeler callback that assigns arbitrary string labels to each fold.
    /// When set, the grid search groups results by label and provides per-segment
    /// best window selection via <see cref="GridSearchResult.SegmentResults"/>
    /// and <see cref="GridSearchResult.BestPerSegment"/>.
    /// Folds for which the labeler returns an empty enumerable are excluded from all segments
    /// but still contribute to the overall <see cref="GridSearchResult.Cells"/>.
    /// </summary>
    /// <param name="labeler">A function that receives a fold and returns zero or more labels.</param>
    /// <returns>This builder for chaining.</returns>
    public GridSearchBuilder WithLabeler(Func<Fold, IEnumerable<string>> labeler)
    {
        _labeler = labeler;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of data points required before the first training window.
    /// Folds whose training window would start before this threshold are skipped.
    /// </summary>
    /// <param name="warmupPoints">Number of warmup data points. Defaults to 0.</param>
    /// <returns>This builder for chaining.</returns>
    public GridSearchBuilder WithWarmup(int warmupPoints)
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
    public GridSearchBuilder WithEmbargo(TimeSpan embargo)
    {
        _embargo = embargo;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of folds required for a cell to be included in results.
    /// Cells with fewer folds are excluded. Defaults to 2.
    /// </summary>
    /// <param name="minimumFolds">Minimum fold count per cell.</param>
    /// <returns>This builder for chaining.</returns>
    public GridSearchBuilder WithMinimumFolds(int minimumFolds)
    {
        _minimumFolds = minimumFolds;
        return this;
    }

    /// <summary>
    /// Limits the maximum number of folds generated per cell.
    /// When not set, the fold count is auto-computed based on available data.
    /// </summary>
    /// <param name="maxFoldsPerCell">Maximum number of folds per cell.</param>
    /// <returns>This builder for chaining.</returns>
    public GridSearchBuilder WithMaxFoldsPerCell(int maxFoldsPerCell)
    {
        _maxFoldsPerCell = maxFoldsPerCell;
        return this;
    }

    /// <summary>
    /// Enables inner K-fold temporal cross-validation within each training window.
    /// When <paramref name="k"/> is 2 or greater, each outer fold's training window
    /// is split into <paramref name="k"/> equal temporal blocks. Each block takes a turn
    /// as hold-out while the rest serve as training data. The fitness callback is invoked
    /// per sub-fold and results are aggregated via weighted average by fold size.
    /// When <paramref name="k"/> is 0 or 1, inner cross-validation is disabled and
    /// the grid search behaves identically to calling without this method.
    /// </summary>
    /// <param name="k">Number of inner folds. Must be 2 or greater to enable. Default is 0 (disabled).</param>
    /// <returns>This builder for chaining.</returns>
    public GridSearchBuilder WithInnerFolds(int k)
    {
        _innerFolds = k;
        return this;
    }

    /// <summary>
    /// Executes the grid search and returns ranked results.
    /// </summary>
    /// <returns>A <see cref="GridSearchResult"/> containing ranked cell results.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration is missing (train windows, test windows, mode, or fitness callback).
    /// </exception>
    public GridSearchResult Build() => Build(CancellationToken.None);

    /// <summary>
    /// Executes the grid search with cancellation support and returns ranked results.
    /// Cancellation is checked between cell iterations.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the grid search between cell iterations.</param>
    /// <returns>A <see cref="GridSearchResult"/> containing ranked cell results.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration is missing (train windows, test windows, mode, or fitness callback).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled between cell iterations.
    /// </exception>
    public GridSearchResult Build(CancellationToken cancellationToken)
    {
        if (_trainWindows is null || _trainWindows.Length == 0)
        {
            throw new InvalidOperationException("WithTrainWindows must be called before Build.");
        }

        if (_testWindows is null || _testWindows.Length == 0)
        {
            throw new InvalidOperationException("WithTestWindows must be called before Build.");
        }

        if (_fitnessCallback is null)
        {
            throw new InvalidOperationException("Evaluate must be called before Build.");
        }

        if (_mode is null)
        {
            throw new InvalidOperationException("BackwardLooking or ForwardLooking must be called before Build.");
        }

        return GridSearchEngine.Execute(
            _totalDataPoints,
            _dataFrequency,
            _trainWindows,
            _testWindows,
            _mode.Value,
            _fitnessCallback,
            _warmupPoints,
            _embargo,
            _minimumFolds,
            _maxFoldsPerCell,
            _scorer,
            _labeler,
            _innerFolds,
            cancellationToken);
    }
}
