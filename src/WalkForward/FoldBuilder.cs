namespace WalkForward;

/// <summary>
/// Entry point for building walk-forward cross-validation folds.
/// Configure common parameters, then select a mode via <see cref="BackwardLooking"/> or <see cref="ForwardLooking"/>.
/// </summary>
/// <example>
/// <code>
/// var folds = new FoldBuilder()
///     .WithDataPoints(10000)
///     .WithDataFrequency(TimeSpan.FromMinutes(15))
///     .BackwardLooking()
///     .WithTrainingWindow(TimeSpan.FromDays(90))
///     .WithTestWindow(TimeSpan.FromDays(7))
///     .Build();
/// </code>
/// </example>
public sealed class FoldBuilder
{
    private int _totalDataPoints;
    private TimeSpan _dataFrequency;

    /// <summary>
    /// Sets the total number of data points in the dataset.
    /// </summary>
    /// <param name="count">Total data points. Must be positive.</param>
    /// <returns>This builder for chaining.</returns>
    public FoldBuilder WithDataPoints(int count)
    {
        _totalDataPoints = count;
        return this;
    }

    /// <summary>
    /// Sets the time interval between consecutive data points.
    /// </summary>
    /// <param name="frequency">Duration between data points (e.g., 15 minutes for 15-minute candles).</param>
    /// <returns>This builder for chaining.</returns>
    public FoldBuilder WithDataFrequency(TimeSpan frequency)
    {
        _dataFrequency = frequency;
        return this;
    }

    /// <summary>
    /// Selects backward-looking walk-forward mode.
    /// The training window has a fixed size per fold, and folds walk backwards from the end of data,
    /// with the most recent data tested first.
    /// </summary>
    /// <returns>A <see cref="BackwardLookingBuilder"/> for configuring backward-looking parameters.</returns>
    public BackwardLookingBuilder BackwardLooking() => new(_totalDataPoints, _dataFrequency);

    /// <summary>
    /// Selects forward-looking walk-forward mode.
    /// Both windows slide forward through the data with a configurable stride.
    /// </summary>
    /// <returns>A <see cref="ForwardLookingBuilder"/> for configuring forward-looking parameters.</returns>
    public ForwardLookingBuilder ForwardLooking() => new(_totalDataPoints, _dataFrequency);
}
