using WalkForward.Degradation;
using WalkForward.GridSearch;
using WalkForward.Scoring;

namespace WalkForward.Pipeline;

/// <summary>
/// Fluent builder for composing multi-stage window discovery pipelines.
/// Use <see cref="FoldBuilder.Pipeline"/> to obtain an instance.
/// </summary>
/// <example>
/// <code>
/// var result = new FoldBuilder()
///     .WithDataPoints(10000)
///     .WithDataFrequency(TimeSpan.FromMinutes(15))
///     .Pipeline()
///     .CoarseScan(grid => grid
///         .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
///         .WithTestWindows(TimeSpan.FromDays(7))
///         .BackwardLooking()
///         .Evaluate(fold => ComputeFitness(data, fold)))
///     .Score(new CompositeScorer().WithWeights(0.6, 0.25, 0.15))
///     .TopCandidates(3)
///     .Validate(deg => deg
///         .BackwardLooking()
///         .EvaluateInSample(fold => TrainAndScore(data, fold))
///         .EvaluateOutOfSample(fold => ScoreOnly(data, fold)))
///     .Build();
/// </code>
/// </example>
public sealed class PipelineBuilder
{
    private readonly int _totalDataPoints;
    private readonly TimeSpan _dataFrequency;
    private Action<GridSearchBuilder>? _coarseScanConfig;
    private CompositeScorer? _scorer;
    private int? _topN;
    private Action<DegradationBuilder>? _validateConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineBuilder"/> class.
    /// </summary>
    /// <param name="totalDataPoints">Total number of data points in the dataset.</param>
    /// <param name="dataFrequency">Time interval between consecutive data points.</param>
    internal PipelineBuilder(int totalDataPoints, TimeSpan dataFrequency)
    {
        _totalDataPoints = totalDataPoints;
        _dataFrequency = dataFrequency;
    }

    /// <summary>
    /// Configures the CoarseScan stage, which performs a grid search over (train, test) window combinations.
    /// This is the only required stage -- all other stages are optional.
    /// </summary>
    /// <param name="configure">An action that configures the <see cref="GridSearchBuilder"/>.</param>
    /// <returns>This builder for chaining.</returns>
    public PipelineBuilder CoarseScan(Action<GridSearchBuilder> configure)
    {
        _coarseScanConfig = configure;
        return this;
    }

    /// <summary>
    /// Configures the Score stage, which applies a <see cref="CompositeScorer"/> to rank cells
    /// by a weighted combination of fitness, consistency, and smoothness.
    /// </summary>
    /// <param name="scorer">A configured <see cref="CompositeScorer"/> instance.</param>
    /// <returns>This builder for chaining.</returns>
    public PipelineBuilder Score(CompositeScorer scorer)
    {
        _scorer = scorer;
        return this;
    }

    /// <summary>
    /// Configures the TopCandidates stage, which selects the top N cells by score or fitness.
    /// </summary>
    /// <param name="count">Number of top candidates to select.</param>
    /// <returns>This builder for chaining.</returns>
    public PipelineBuilder TopCandidates(int count)
    {
        _topN = count;
        return this;
    }

    /// <summary>
    /// Configures the Validate stage, which runs degradation analysis on each top candidate.
    /// The pipeline automatically sets <see cref="DegradationBuilder.WithTrainingWindow"/>
    /// and <see cref="DegradationBuilder.WithTestWindow"/> per candidate -- do not set these in the callback.
    /// </summary>
    /// <param name="configure">An action that configures the <see cref="DegradationBuilder"/> for each candidate.</param>
    /// <returns>This builder for chaining.</returns>
    public PipelineBuilder Validate(Action<DegradationBuilder> configure)
    {
        _validateConfig = configure;
        return this;
    }

    /// <summary>
    /// Executes the pipeline and returns the aggregate result.
    /// </summary>
    /// <returns>A <see cref="PipelineResult"/> containing the full pipeline journey.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CoarseScan has not been configured.</exception>
    public PipelineResult Build() => Build(CancellationToken.None);

    /// <summary>
    /// Executes the pipeline with cancellation support.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the pipeline between stages and between validation candidates.</param>
    /// <returns>A <see cref="PipelineResult"/> containing the full pipeline journey.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CoarseScan has not been configured.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public PipelineResult Build(CancellationToken cancellationToken) => Build(cancellationToken, null);

    /// <summary>
    /// Executes the pipeline with cancellation and progress reporting.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the pipeline between stages and between validation candidates.</param>
    /// <param name="progress">Optional progress reporter for stage-level and overall progress.</param>
    /// <returns>A <see cref="PipelineResult"/> containing the full pipeline journey.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CoarseScan has not been configured.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public PipelineResult Build(CancellationToken cancellationToken, IProgress<PipelineProgress>? progress)
    {
        if (_coarseScanConfig is null)
        {
            throw new InvalidOperationException("CoarseScan must be configured before Build.");
        }

        // Stub: reference all fields to satisfy analyzers; implementation in Task 2
        _ = (_totalDataPoints, _dataFrequency, _scorer, _topN, _validateConfig, cancellationToken, progress);
        throw new NotSupportedException("Pipeline execution not yet implemented.");
    }
}
